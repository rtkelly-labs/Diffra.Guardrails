using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Diffra.Guardrails.Core.Catalogue;
using Diffra.Guardrails.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Diffra.Guardrails.CSharp;

public static class CSharpChecker
{
    public static IReadOnlyList<Finding> CheckText(string sourceText, string filePath = "CaseTests.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var root = syntaxTree.GetCompilationUnitRoot();
        var findings = new List<Finding>();

        // 0. Analyze contracts for DFG3xx (DFG301-DFG304)
        var contractPropertySpans = new List<(int Start, int End)>();
        AnalyzeContractPurity(root, filePath, findings, contractPropertySpans);

        // 0b. Analyze DFG4xx and DFG5xx
        AnalyzeDeepImmutabilityAndInit(root, filePath, findings, contractPropertySpans);
        AnalyzeBannedSymbols(root, filePath, findings);
        AnalyzePatternMatchingAndUnions(root, filePath, findings);
        AnalyzeDomainFunctions(root, filePath, findings);

        // 1. Analyze properties for DFG201
        var propertySpans = new List<(int Start, int End)>();
        foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (contractPropertySpans.Any(span => prop.SpanStart >= span.Start && prop.Span.End <= span.End))
                continue;

            if (AnalyzeProperty(prop, filePath, findings))
            {
                propertySpans.Add((prop.SpanStart, prop.Span.End));
            }
        }

        // 2. Analyze collections for DFG202
        foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            AnalyzeInvocation(inv, filePath, findings);
        }

        // 3. Analyze catches for DFG203
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            AnalyzeCatch(catchClause, filePath, findings);
        }

        // 4. Analyze lambdas and local functions for DFG204
        foreach (var lambda in root.DescendantNodes().OfType<LambdaExpressionSyntax>())
        {
            // If lambda is inside a property getter that already failed DFG201, skip DFG204
            if (propertySpans.Any(span => lambda.SpanStart >= span.Start && lambda.Span.End <= span.End))
                continue;

            AnalyzeLambda(lambda, filePath, findings);
        }

        foreach (var localFunc in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            AnalyzeLocalFunction(localFunc, filePath, findings);
        }

        // 5. Analyze single assignment for DFG205
        foreach (var decl in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            AnalyzeLocalDeclaration(decl, filePath, findings);
        }

        // 6. Analyze test methods for DFG1xx
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (IsTestMethod(method))
            {
                AnalyzeTestMethod(method, filePath, findings);
            }
        }

        return findings;
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(static al => al.Attributes)
            .Any(static a => a.Name.ToString() is "Fact" or "Theory" or "Test" or "TestMethod");

    private static bool AnalyzeProperty(PropertyDeclarationSyntax prop, string filePath, List<Finding> findings)
    {
        var propName = prop.Identifier.ValueText;
        var line = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        // Check arrow expression or get accessor
        SyntaxNode? body = prop.ExpressionBody;
        if (body is null && prop.AccessorList != null)
        {
            var getter = prop.AccessorList.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            body = (SyntaxNode?)getter?.Body ?? getter?.ExpressionBody;
        }

        if (body is null) return false;

        // Check for LINQ
        var invocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var inv in invocations)
        {
            var text = inv.Expression.ToString();
            if (text.Contains(".Where") || text.Contains(".Select") || text.Contains(".ToList") || text.Contains(".ToArray"))
            {
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG201.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: null,
                    Message: RuleContracts.DFG201.Message(propName, "LINQ query invocation"),
                    Fix: RuleContracts.DFG201.Fix(propName)));
                return true;
            }
        }

        // Check for allocations (ArrayCreation, ImplicitArrayCreation, ObjectCreation for collections)
        var hasAllocation = body.DescendantNodes().Any(static n =>
            n is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or ObjectCreationExpressionSyntax { Type: GenericNameSyntax { Identifier.ValueText: "List" or "Dictionary" or "HashSet" } });

        if (hasAllocation)
        {
            findings.Add(new Finding(
                Rule: RuleContracts.DFG201.Rule,
                Path: filePath,
                Line: line,
                EndLine: line,
                Block: null,
                Message: RuleContracts.DFG201.Message(propName, "collection allocation"),
                Fix: RuleContracts.DFG201.Fix(propName)));
            return true;
        }

        // Check for control flow (switch, foreach, for, while)
        var hasControlFlow = body.DescendantNodes().Any(static n =>
            n is ForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax or SwitchStatementSyntax);

        if (hasControlFlow)
        {
            findings.Add(new Finding(
                Rule: RuleContracts.DFG201.Rule,
                Path: filePath,
                Line: line,
                EndLine: line,
                Block: null,
                Message: RuleContracts.DFG201.Message(propName, "control flow statements"),
                Fix: RuleContracts.DFG201.Fix(propName)));
            return true;
        }

        return false;
    }

    private static void AnalyzeInvocation(InvocationExpressionSyntax inv, string filePath, List<Finding> findings)
    {
        if (inv.Expression is not MemberAccessExpressionSyntax memberAccess) return;

        var methodName = memberAccess.Name.Identifier.ValueText;
        if (methodName is not ("Add" or "AddRange" or "Remove" or "RemoveAll" or "Clear" or "Sort" or "Reverse" or "Insert"))
            return;

        var receiver = memberAccess.Expression.ToString();
        if (receiver.Contains("builder", StringComparison.OrdinalIgnoreCase) ||
            receiver.Contains("Builder", StringComparison.Ordinal) ||
            receiver.Contains("immutable", StringComparison.OrdinalIgnoreCase) ||
            receiver.Contains("Immutable", StringComparison.Ordinal))
            return; // Scoped builder or immutable transformation allowed

        // If the invocation return value is consumed (assigned, returned, or passed as argument),
        // it is an immutable transformation rather than an in-place mutation statement.
        if (inv.Parent is not ExpressionStatementSyntax)
            return;

        var line = inv.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var block = DetermineBlock(inv);

        findings.Add(new Finding(
            Rule: RuleContracts.DFG202.Rule,
            Path: filePath,
            Line: line,
            EndLine: line,
            Block: block ?? "Act",
            Message: RuleContracts.DFG202.Message(methodName, receiver),
            Fix: RuleContracts.DFG202.Fix(methodName)));
    }

    private static void AnalyzeCatch(CatchClauseSyntax catchClause, string filePath, List<Finding> findings)
    {
        // Filtered catch is allowed
        if (catchClause.Filter != null) return;

        var isBare = catchClause.Declaration is null;
        var isBroad = isBare || catchClause.Declaration!.Type.ToString() is "Exception" or "System.Exception";

        if (!isBroad) return;

        // Check if body has bare throw; or if it rethrows with throw ex;
        var throws = catchClause.Block.DescendantNodes().OfType<ThrowStatementSyntax>().ToList();
        var hasBareThrow = throws.Any(static t => t.Expression is null);
        var hasThrowWithExpr = throws.FirstOrDefault(static t => t.Expression != null);

        var block = DetermineBlock(catchClause);

        if (hasThrowWithExpr != null)
        {
            var throwLine = hasThrowWithExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(new Finding(
                Rule: RuleContracts.DFG203.Rule,
                Path: filePath,
                Line: throwLine,
                EndLine: throwLine,
                Block: block,
                Message: RuleContracts.DFG203.Message("Exception"),
                Fix: RuleContracts.DFG203.Fix));
            return;
        }

        if (!hasBareThrow)
        {
            var line = catchClause.CatchKeyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(new Finding(
                Rule: RuleContracts.DFG203.Rule,
                Path: filePath,
                Line: line,
                EndLine: line,
                Block: block,
                Message: RuleContracts.DFG203.Message(isBare ? "all exceptions" : "Exception"),
                Fix: RuleContracts.DFG203.Fix));
        }
    }

    private static void AnalyzeLambda(LambdaExpressionSyntax lambda, string filePath, List<Finding> findings)
    {
        if (lambda.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

        // Extract parameters
        var paramNames = new HashSet<string>();
        if (lambda is SimpleLambdaExpressionSyntax s) paramNames.Add(s.Parameter.Identifier.ValueText);
        else if (lambda is ParenthesizedLambdaExpressionSyntax p)
        {
            foreach (var param in p.ParameterList.Parameters) paramNames.Add(param.Identifier.ValueText);
        }

        // Check if lambda references variables outside parameter list
        var identifiers = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Select(static id => id.Identifier.ValueText)
            .ToList();

        // Find enclosing method
        var enclosingMethod = lambda.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (enclosingMethod != null)
        {
            var localVars = enclosingMethod.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Where(v => v.SpanStart < lambda.SpanStart)
                .Select(static v => v.Identifier.ValueText)
                .ToHashSet();

            // Include enclosing method parameters as outer state
            foreach (var param in enclosingMethod.ParameterList.Parameters)
            {
                localVars.Add(param.Identifier.ValueText);
            }

            // If lambda references a local var or parameter declared outside, it captures outer state!
            if (identifiers.Any(id => localVars.Contains(id))) return;

            // If lambda references an instance field (like _discountRate)
            var enclosingClass = lambda.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (enclosingClass != null)
            {
                var fields = enclosingClass.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .SelectMany(static f => f.Declaration.Variables)
                    .Select(static v => v.Identifier.ValueText)
                    .ToHashSet();

                if (identifiers.Any(id => fields.Contains(id))) return;
            }
        }

        var line = lambda.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var block = DetermineBlock(lambda);

        findings.Add(new Finding(
            Rule: RuleContracts.DFG204.Rule,
            Path: filePath,
            Line: line,
            EndLine: line,
            Block: block ?? "Act",
            Message: RuleContracts.DFG204.Message,
            Fix: RuleContracts.DFG204.Fix));
    }

    private static void AnalyzeLocalFunction(LocalFunctionStatementSyntax func, string filePath, List<Finding> findings)
    {
        if (func.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

        var line = func.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var block = DetermineBlock(func);

        findings.Add(new Finding(
            Rule: RuleContracts.DFG204.Rule,
            Path: filePath,
            Line: line,
            EndLine: line,
            Block: block ?? "Arrange",
            Message: RuleContracts.DFG204.Message,
            Fix: RuleContracts.DFG204.Fix));
    }

    private static void AnalyzeLocalDeclaration(LocalDeclarationStatementSyntax decl, string filePath, List<Finding> findings)
    {
        var variable = decl.Declaration.Variables.FirstOrDefault();
        if (variable == null) return;

        var varName = variable.Identifier.ValueText;

        // Check if uninitialized or initialized to null/default
        var isUninitialized = variable.Initializer is null;
        if (!isUninitialized) return;

        // Check next statement in block
        if (decl.Parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf(decl);
            if (index >= 0 && index + 1 < block.Statements.Count)
            {
                var next = block.Statements[index + 1];
                if (next is IfStatementSyntax or SwitchStatementSyntax)
                {
                    var line = decl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    var testBlock = DetermineBlock(decl);

                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG205.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: testBlock ?? "Act",
                        Message: RuleContracts.DFG205.Message(varName),
                        Fix: RuleContracts.DFG205.Fix(varName)));
                }
            }
        }
    }

    private static void AnalyzeTestMethod(MethodDeclarationSyntax method, string filePath, List<Finding> findings)
    {
        if (method.Body is null) return;

        // Parse comments for markers
        var trivia = method.Body.DescendantTrivia()
            .Where(static t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .Select(static t => (Text: t.ToString().TrimStart('/', ' ').Trim(), Line: t.GetLocation().GetLineSpan().StartLinePosition.Line + 1))
            .ToList();

        var markers = new List<(string Name, int Line)>();
        foreach (var (comment, line) in trivia)
        {
            if (comment is "Arrange" or "Act" or "Assert" or "Act & Assert" or "Given" or "When" or "Then")
            {
                markers.Add((comment, line));
            }
            else if (comment is "Arange" or "Acct" or "Asert" or "Thenn" or "Whn")
            {
                // DFG102: Misspelt marker
                var corrected = comment switch
                {
                    "Arange" => "Arrange",
                    "Acct" => "Act",
                    _ => "Assert"
                };
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG102.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: corrected,
                    Message: $"The marker '{comment}' should be written '{corrected}'.",
                    Fix: RuleContracts.DFG102.SpellingFix(corrected)));
            }
        }

        // DFG101: Test has no markers
        var hasValidMarkers = markers.Count > 0;
        if (!hasValidMarkers)
        {
            var methodLine = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(new Finding(
                Rule: RuleContracts.DFG101.Rule,
                Path: filePath,
                Line: methodLine,
                EndLine: methodLine,
                Block: null,
                Message: RuleContracts.DFG101.Message,
                Fix: RuleContracts.DFG101.Fix));
        }

        // DFG108: Loops or control flow in test body
        var loops = method.Body.Statements.Where(static s => s is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax).ToList();
        foreach (var loop in loops)
        {
            var line = loop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            findings.Add(new Finding(
                Rule: RuleContracts.DFG108.Rule,
                Path: filePath,
                Line: line,
                EndLine: line,
                Block: null,
                Message: "Control flow statement in test body.",
                Fix: RuleContracts.DFG108.LoopFix));
        }

        // Inspect statements under markers
        var actMarker = markers.FirstOrDefault(static m => m.Name is "Act" or "When");
        var assertMarker = markers.FirstOrDefault(static m => m.Name is "Assert" or "Then");

        if (actMarker != default)
        {
            // Statements between Act and Assert
            var statementsInAct = method.Body.Statements
                .Where(s => s.GetLocation().GetLineSpan().StartLinePosition.Line + 1 > actMarker.Line &&
                            (assertMarker == default || s.GetLocation().GetLineSpan().StartLinePosition.Line + 1 < assertMarker.Line))
                .ToList();

            // Filter out misplaced assertions from Act statement count for DFG103
            var nonAssertionStatements = statementsInAct.Where(static s => !IsAssertionStatement(s)).ToList();

            // Collapse DFG205 declaration + conditional branch pairs
            var effectiveActStatements = new List<StatementSyntax>();
            for (var i = 0; i < nonAssertionStatements.Count; i++)
            {
                var current = nonAssertionStatements[i];
                if (current is LocalDeclarationStatementSyntax decl &&
                    i + 1 < nonAssertionStatements.Count &&
                    nonAssertionStatements[i + 1] is IfStatementSyntax or SwitchStatementSyntax &&
                    decl.Declaration.Variables.FirstOrDefault()?.Initializer == null)
                {
                    effectiveActStatements.Add(current);
                    i++; // skip conditional statement as part of the DFG205 compound pair
                    continue;
                }
                effectiveActStatements.Add(current);
            }

            // DFG103: Small test has budget of 1 statement
            if (effectiveActStatements.Count > 1)
            {
                var second = effectiveActStatements[1];
                var line = second.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG103.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: "Act",
                    Message: $"This Act block has {effectiveActStatements.Count} statements; a small test acts once.",
                    Fix: RuleContracts.DFG103.Fix));
            }

            // DFG104: Assertion in Act block
            foreach (var s in statementsInAct)
            {
                if (IsAssertionStatement(s))
                {
                    var line = s.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG104.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: "Act",
                        Message: "This assertion is in the Act block.",
                        Fix: RuleContracts.DFG104.Fix));
                }
            }
        }

        if (assertMarker != default)
        {
            var assertionsInAssert = method.Body.Statements
                .Where(s => s.GetLocation().GetLineSpan().StartLinePosition.Line + 1 > assertMarker.Line)
                .Where(IsAssertionStatement)
                .ToList();

            // DFG105: More than 1 assertion per Then
            if (assertionsInAssert.Count > 1)
            {
                for (var i = 1; i < assertionsInAssert.Count; i++)
                {
                    var line = assertionsInAssert[i].GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG105.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: "Assert",
                        Message: $"This Assert block has {assertionsInAssert.Count} assertions; a Then checks one thing.",
                        Fix: RuleContracts.DFG105.Fix));
                }
            }

            // DFG106: Assertion with no failure message
            foreach (var a in assertionsInAssert)
            {
                if (!HasFailureMessage(a))
                {
                    var line = a.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG106.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: "Assert",
                        Message: "This assertion has no failure message.",
                        Fix: RuleContracts.DFG106.Fix));
                }
            }
        }
    }

    private static bool IsAssertionStatement(StatementSyntax statement)
    {
        var text = statement.ToString();
        return text.Contains(".Should()") || text.Contains("Assert.");
    }

    private static bool HasFailureMessage(StatementSyntax statement)
    {
        var invs = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var inv in invs)
        {
            if (inv.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.ValueText;
                var receiver = memberAccess.Expression.ToString();

                // 1. FluentAssertions: .Should().*
                if (receiver.Contains(".Should()") || receiver.EndsWith("Should()"))
                {
                    // Single-value assertions: .BeTrue("msg"), .BeFalse("msg"), .BeNull("msg"), .NotBeNull("msg")
                    if (methodName is "BeTrue" or "BeFalse" or "BeNull" or "NotBeNull")
                    {
                        if (inv.ArgumentList.Arguments.Count >= 1 &&
                            inv.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.ValueText.Length: > 0 })
                        {
                            return true;
                        }
                    }
                    // Comparison assertions: .Be(expected, "because ..."), .Equal(expected, "because ...")
                    else if (inv.ArgumentList.Arguments.Count > 1)
                    {
                        var becauseArg = inv.ArgumentList.Arguments[1].Expression;
                        if (becauseArg is LiteralExpressionSyntax { Token.ValueText.Length: > 0 })
                        {
                            return true;
                        }
                    }
                }
                // 2. Standard xUnit / NUnit Assert static invocations
                else if (receiver is "Assert")
                {
                    if (methodName is "True" or "False")
                    {
                        if (inv.ArgumentList.Arguments.Count >= 2 &&
                            inv.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax { Token.ValueText.Length: > 0 })
                        {
                            return true;
                        }
                    }
                    else if (methodName is "Equal" or "NotEqual" or "Same" or "NotSame")
                    {
                        if (inv.ArgumentList.Arguments.Count >= 3 &&
                            inv.ArgumentList.Arguments[2].Expression is LiteralExpressionSyntax { Token.ValueText.Length: > 0 })
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private static string? DetermineBlock(SyntaxNode node)
    {
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method?.Body == null) return null;

        var nodeLine = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var trivia = method.Body.DescendantTrivia()
            .Where(static t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))
            .Select(static t => (Text: t.ToString().TrimStart('/', ' ').Trim(), Line: t.GetLocation().GetLineSpan().StartLinePosition.Line + 1))
            .OrderBy(static t => t.Line)
            .ToList();

        string? currentBlock = null;
        foreach (var (comment, line) in trivia)
        {
            if (line < nodeLine && comment is "Arrange" or "Act" or "Assert" or "Act & Assert" or "Given" or "When" or "Then")
            {
                currentBlock = comment;
            }
        }

        return currentBlock;
    }

    private static bool IsInContractBoundary(SyntaxNode node, string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        if (normalized.Contains("/Contracts/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/Models/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/Dtos/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/Specs/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("-contract-", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("-pure-contract", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".Contracts.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".Models.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".Dtos.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check namespace
        var ns = node.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
            ?? node.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();

        if (ns != null && (
            ns.EndsWith("Contracts", StringComparison.OrdinalIgnoreCase) ||
            ns.EndsWith("Models", StringComparison.OrdinalIgnoreCase) ||
            ns.EndsWith("Dtos", StringComparison.OrdinalIgnoreCase) ||
            ns.EndsWith("Specs", StringComparison.OrdinalIgnoreCase) ||
            ns.Contains(".Contracts.", StringComparison.OrdinalIgnoreCase) ||
            ns.Contains(".Models.", StringComparison.OrdinalIgnoreCase) ||
            ns.Contains(".Dtos.", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check Attributes on the type
        if (node is BaseTypeDeclarationSyntax baseType)
        {
            if (baseType.AttributeLists.SelectMany(static al => al.Attributes).Any(static a =>
                a.Name.ToString() is "PureContract" or "PureContractAttribute" or "Contract" or "ContractAttribute"))
            {
                return true;
            }
        }

        var trivia = node.GetLeadingTrivia();
        if (trivia.Any(static t => t.ToString().Contains("diffra:contract-boundary") || t.ToString().Contains("diffra:pure-contract")))
        {
            return true;
        }

        return false;
    }

    private static bool IsMutableCollectionType(string typeName)
    {
        if (typeName.StartsWith("IReadOnly", StringComparison.Ordinal) ||
            typeName.StartsWith("Immutable", StringComparison.Ordinal) ||
            typeName.StartsWith("System.Collections.Immutable.", StringComparison.Ordinal))
        {
            return false;
        }

        return typeName.StartsWith("List<", StringComparison.Ordinal) ||
               typeName.StartsWith("IList<", StringComparison.Ordinal) ||
               typeName.StartsWith("Dictionary<", StringComparison.Ordinal) ||
               typeName.StartsWith("IDictionary<", StringComparison.Ordinal) ||
               typeName.StartsWith("HashSet<", StringComparison.Ordinal) ||
               typeName.StartsWith("ISet<", StringComparison.Ordinal) ||
               typeName.StartsWith("Collection<", StringComparison.Ordinal) ||
               typeName.StartsWith("ICollection<", StringComparison.Ordinal) ||
               typeName.StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal) ||
               typeName.StartsWith("System.Collections.Generic.Dictionary<", StringComparison.Ordinal) ||
               typeName.StartsWith("System.Collections.Generic.HashSet<", StringComparison.Ordinal);
    }

    private static bool IsTestClass(BaseTypeDeclarationSyntax typeDecl)
    {
        if (typeDecl is ClassDeclarationSyntax classDecl)
        {
            if (classDecl.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal) ||
                classDecl.Identifier.ValueText.EndsWith("Test", StringComparison.Ordinal))
            {
                return true;
            }

            if (classDecl.Members.OfType<MethodDeclarationSyntax>().Any(IsTestMethod))
            {
                return true;
            }
        }
        return false;
    }

    private static void AnalyzeContractPurity(CompilationUnitSyntax root, string filePath, List<Finding> findings, List<(int Start, int End)> contractPropertySpans)
    {
        var inContractBoundary = IsInContractBoundary(root, filePath);

        var typeDeclarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>();
        foreach (var typeDecl in typeDeclarations)
        {
            if (IsTestClass(typeDecl)) continue;

            var typeInBoundary = inContractBoundary || IsInContractBoundary(typeDecl, filePath);
            if (!typeInBoundary) continue;

            var typeName = typeDecl switch
            {
                TypeDeclarationSyntax td => td.Identifier.ValueText,
                EnumDeclarationSyntax ed => ed.Identifier.ValueText,
                _ => "Unknown"
            };
            var typeLine = typeDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

            // DFG304: Permitted declaration kinds only
            if (typeDecl is ClassDeclarationSyntax classDecl)
            {
                var isStatic = classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword));
                var members = classDecl.Members;
                var onlyConstFields = members.All(static m => m is FieldDeclarationSyntax fd && fd.Modifiers.Any(static mod => mod.IsKind(SyntaxKind.ConstKeyword)));

                if (!isStatic || !onlyConstFields)
                {
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG304.Rule,
                        Path: filePath,
                        Line: typeLine,
                        EndLine: typeLine,
                        Block: null,
                        Message: RuleContracts.DFG304.Message(typeName, "class"),
                        Fix: RuleContracts.DFG304.Fix(typeName)));
                }
            }
            else if (typeDecl is StructDeclarationSyntax && typeDecl is not RecordDeclarationSyntax)
            {
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG304.Rule,
                    Path: filePath,
                    Line: typeLine,
                    EndLine: typeLine,
                    Block: null,
                    Message: RuleContracts.DFG304.Message(typeName, "struct"),
                    Fix: RuleContracts.DFG304.Fix(typeName)));
            }

            // Inspect members if TypeDeclarationSyntax (records, interfaces, classes, structs)
            if (typeDecl is TypeDeclarationSyntax typeDef)
            {
                // DFG301: Pure contract types must not declare methods
                foreach (var member in typeDef.Members)
                {
                    if (member is MethodDeclarationSyntax method)
                    {
                        var methodName = method.Identifier.ValueText;
                        var methodLine = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        findings.Add(new Finding(
                            Rule: RuleContracts.DFG301.Rule,
                            Path: filePath,
                            Line: methodLine,
                            EndLine: methodLine,
                            Block: null,
                            Message: RuleContracts.DFG301.Message(typeName, methodName),
                            Fix: RuleContracts.DFG301.Fix(methodName)));
                    }
                    else if (member is PropertyDeclarationSyntax prop)
                    {
                        var propName = prop.Identifier.ValueText;
                        var propLine = prop.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                        // DFG302: Property getters must have zero behavior/computation
                        var isComputed = prop.ExpressionBody != null;
                        if (!isComputed && prop.AccessorList != null)
                        {
                            var getter = prop.AccessorList.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
                            if (getter != null && (getter.Body != null || getter.ExpressionBody != null))
                            {
                                isComputed = true;
                            }
                        }

                        if (isComputed)
                        {
                            contractPropertySpans.Add((prop.SpanStart, prop.Span.End));
                            findings.Add(new Finding(
                                Rule: RuleContracts.DFG302.Rule,
                                Path: filePath,
                                Line: propLine,
                                EndLine: propLine,
                                Block: null,
                                Message: RuleContracts.DFG302.Message(typeName, propName),
                                Fix: RuleContracts.DFG302.Fix(propName)));
                        }

                        // DFG303: Immutability & collection safety
                        // 1. Mutable setter: 'set;' (not init)
                        if (prop.AccessorList != null)
                        {
                            var setter = prop.AccessorList.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                            if (setter != null)
                            {
                                var setterLine = setter.Keyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                findings.Add(new Finding(
                                    Rule: RuleContracts.DFG303.Rule,
                                    Path: filePath,
                                    Line: setterLine,
                                    EndLine: setterLine,
                                    Block: null,
                                    Message: RuleContracts.DFG303.MutableSetterMessage(propName),
                                    Fix: RuleContracts.DFG303.MutableSetterFix));
                            }
                        }

                        // 2. Mutable collection types
                        var propTypeStr = prop.Type.ToString();
                        if (IsMutableCollectionType(propTypeStr))
                        {
                            findings.Add(new Finding(
                                Rule: RuleContracts.DFG303.Rule,
                                Path: filePath,
                                Line: propLine,
                                EndLine: propLine,
                                Block: null,
                                Message: RuleContracts.DFG303.MutableCollectionMessage(propName, propTypeStr),
                                Fix: RuleContracts.DFG303.MutableCollectionFix(propTypeStr)));
                        }
                    }
                }

                // Check record primary constructor parameters for DFG303 mutable collections
                if (typeDecl is RecordDeclarationSyntax recordDecl && recordDecl.ParameterList != null)
                {
                    foreach (var param in recordDecl.ParameterList.Parameters)
                    {
                        if (param.Type != null)
                        {
                            var paramTypeStr = param.Type.ToString();
                            if (IsMutableCollectionType(paramTypeStr))
                            {
                                var paramLine = param.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                                findings.Add(new Finding(
                                    Rule: RuleContracts.DFG303.Rule,
                                    Path: filePath,
                                    Line: paramLine,
                                    EndLine: paramLine,
                                    Block: null,
                                    Message: RuleContracts.DFG303.MutableCollectionMessage(param.Identifier.ValueText, paramTypeStr),
                                    Fix: RuleContracts.DFG303.MutableCollectionFix(paramTypeStr)));
                            }
                        }
                    }
                }
            }
        }
    }

    private static void AnalyzeDeepImmutabilityAndInit(
        CompilationUnitSyntax root,
        string filePath,
        List<Finding> findings,
        List<(int Start, int End)> contractPropertySpans)
    {
        var inContractBoundary = IsInContractBoundary(root, filePath);

        var declaredClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(static c => !c.Modifiers.Any(SyntaxKind.StaticKeyword) && !IsTestClass(c))
            .Select(static c => c.Identifier.ValueText)
            .ToHashSet();

        // 1. DFG401: Deep Immutability for Records
        foreach (var record in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            if (IsTestClass(record)) continue;
            var recordInBoundary = inContractBoundary || IsInContractBoundary(record, filePath);
            var recordName = record.Identifier.ValueText;

            // Check primary constructor parameters
            if (record.ParameterList != null)
            {
                foreach (var param in record.ParameterList.Parameters)
                {
                    if (param.Type != null)
                    {
                        var typeStr = param.Type.ToString();
                        var isMutableRef = declaredClasses.Contains(typeStr) ||
                                           typeStr.StartsWith("Mutable", StringComparison.Ordinal) ||
                                           typeStr.EndsWith("Class", StringComparison.Ordinal);
                        var isMutableCol = IsMutableCollectionType(typeStr);

                        if (isMutableRef || (!recordInBoundary && isMutableCol))
                        {
                            var line = param.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            findings.Add(new Finding(
                                Rule: RuleContracts.DFG401.Rule,
                                Path: filePath,
                                Line: line,
                                EndLine: line,
                                Block: null,
                                Message: RuleContracts.DFG401.Message(recordName, param.Identifier.ValueText, typeStr),
                                Fix: RuleContracts.DFG401.Fix));
                        }
                    }
                }
            }

            // Check body properties
            foreach (var prop in record.Members.OfType<PropertyDeclarationSyntax>())
            {
                var typeStr = prop.Type.ToString();
                var isMutableRef = declaredClasses.Contains(typeStr) ||
                                   typeStr.StartsWith("Mutable", StringComparison.Ordinal) ||
                                   typeStr.EndsWith("Class", StringComparison.Ordinal);
                var isMutableCol = IsMutableCollectionType(typeStr);

                if (isMutableRef || (!recordInBoundary && isMutableCol))
                {
                    var line = prop.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG401.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: null,
                        Message: RuleContracts.DFG401.Message(recordName, prop.Identifier.ValueText, typeStr),
                        Fix: RuleContracts.DFG401.Fix));
                }
            }
        }

        // 2. DFG402: Mandatory init; in Domain Types
        foreach (var prop in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (contractPropertySpans.Any(span => prop.SpanStart >= span.Start && prop.Span.End <= span.End))
                continue;

            var enclosingType = prop.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
            if (enclosingType == null || IsTestClass(enclosingType)) continue;

            if (inContractBoundary || IsInContractBoundary(enclosingType, filePath)) continue;

            var setter = prop.AccessorList?.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
            if (setter != null)
            {
                var setterLine = setter.Keyword.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                var typeName = enclosingType switch
                {
                    TypeDeclarationSyntax td => td.Identifier.ValueText,
                    _ => "Unknown"
                };
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG402.Rule,
                    Path: filePath,
                    Line: setterLine,
                    EndLine: setterLine,
                    Block: null,
                    Message: RuleContracts.DFG402.Message(prop.Identifier.ValueText, typeName),
                    Fix: RuleContracts.DFG402.Fix));
            }
        }
    }

    private static void AnalyzeBannedSymbols(
        CompilationUnitSyntax root,
        string filePath,
        List<Finding> findings)
    {
        foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var text = memberAccess.ToString();
            if (text is "DateTime.Now" or "DateTime.UtcNow")
            {
                var line = memberAccess.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG403.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: DetermineBlock(memberAccess),
                    Message: RuleContracts.DFG403.Message(text, "non-deterministic system clock dependency"),
                    Fix: RuleContracts.DFG403.Fix("TimeProvider.GetUtcNow()")));
            }
            else if (text is "Thread.Sleep")
            {
                var line = memberAccess.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG403.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: DetermineBlock(memberAccess),
                    Message: RuleContracts.DFG403.Message(text, "synchronous thread blocking"),
                    Fix: RuleContracts.DFG403.Fix("Task.Delay")));
            }
            else if (memberAccess.Name.Identifier.ValueText is "Result" && text.EndsWith(".Result", StringComparison.Ordinal) && !text.Contains("Should()"))
            {
                var line = memberAccess.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG403.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: DetermineBlock(memberAccess),
                    Message: RuleContracts.DFG403.Message(text, "synchronous blocking on task risks deadlock"),
                    Fix: RuleContracts.DFG403.Fix("await")));
            }
            else if (memberAccess.Name.Identifier.ValueText is "Wait" && text.EndsWith(".Wait", StringComparison.Ordinal) && !text.Contains("Should()"))
            {
                var line = memberAccess.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG403.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: DetermineBlock(memberAccess),
                    Message: RuleContracts.DFG403.Message(text, "synchronous blocking on task risks deadlock"),
                    Fix: RuleContracts.DFG403.Fix("await")));
            }
        }

        foreach (var objCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = objCreation.Type.ToString();
            if (typeName is "ArrayList" or "Hashtable" or "System.Collections.ArrayList" or "System.Collections.Hashtable")
            {
                var line = objCreation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                findings.Add(new Finding(
                    Rule: RuleContracts.DFG403.Rule,
                    Path: filePath,
                    Line: line,
                    EndLine: line,
                    Block: DetermineBlock(objCreation),
                    Message: RuleContracts.DFG403.Message(typeName, "untyped legacy collection"),
                    Fix: RuleContracts.DFG403.Fix("ImmutableArray<T> or IReadOnlyList<T>")));
            }
        }
    }

    private static void AnalyzePatternMatchingAndUnions(
        CompilationUnitSyntax root,
        string filePath,
        List<Finding> findings)
    {
        foreach (var switchExpr in root.DescendantNodes().OfType<SwitchExpressionSyntax>())
        {
            var governingExpr = switchExpr.GoverningExpression.ToString();

            // Detect if this switch expression matches on an algebraic union or enum
            var hasUnionArm = switchExpr.Arms.Any(static arm => arm.Pattern switch
            {
                ConstantPatternSyntax cp => cp.Expression is MemberAccessExpressionSyntax,
                DeclarationPatternSyntax => true,
                TypePatternSyntax => true,
                RecursivePatternSyntax => true,
                _ => false
            });

            if (!hasUnionArm) continue;

            // Look for wildcard discard arm: _ => ...
            foreach (var arm in switchExpr.Arms)
            {
                if (arm.Pattern is DiscardPatternSyntax)
                {
                    var line = arm.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG501.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: DetermineBlock(arm),
                        Message: RuleContracts.DFG501.Message(governingExpr),
                        Fix: RuleContracts.DFG501.Fix(governingExpr)));
                }
            }
        }
    }

    private static void AnalyzeDomainFunctions(
        CompilationUnitSyntax root,
        string filePath,
        List<Finding> findings)
    {
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (IsTestClass(classDecl)) continue;

            var fieldNames = classDecl.Members.OfType<FieldDeclarationSyntax>()
                .Where(static f => !f.Modifiers.Any(SyntaxKind.StaticKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
                .SelectMany(static f => f.Declaration.Variables)
                .Select(static v => v.Identifier.ValueText)
                .ToHashSet();

            var propNames = classDecl.Members.OfType<PropertyDeclarationSyntax>()
                .Where(static p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
                .Select(static p => p.Identifier.ValueText)
                .ToHashSet();

            if (fieldNames.Count == 0 && propNames.Count == 0) continue;

            foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                if (method.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
                if (method.Body == null && method.ExpressionBody == null) continue;

                var hasMutation = false;
                var assignments = method.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                foreach (var assign in assignments)
                {
                    var target = assign.Left.ToString();
                    if (target.StartsWith("this.", StringComparison.Ordinal)) target = target.Substring(5);
                    if (fieldNames.Contains(target) || propNames.Contains(target))
                    {
                        hasMutation = true;
                        break;
                    }
                }

                if (hasMutation)
                {
                    var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    findings.Add(new Finding(
                        Rule: RuleContracts.DFG502.Rule,
                        Path: filePath,
                        Line: line,
                        EndLine: line,
                        Block: null,
                        Message: RuleContracts.DFG502.Message(method.Identifier.ValueText, classDecl.Identifier.ValueText),
                        Fix: RuleContracts.DFG502.Fix));
                }
            }
        }
    }
}
