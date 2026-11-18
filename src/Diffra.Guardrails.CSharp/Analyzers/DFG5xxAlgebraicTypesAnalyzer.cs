using System;
using System.Collections.Immutable;
using System.Linq;
using Diffra.Guardrails.Core.Catalogue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Diffra.Guardrails.CSharp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DFG5xxAlgebraicTypesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "AlgebraicTypes";

    public static readonly DiagnosticDescriptor Rule501 = new(
        id: RuleContracts.DFG501.Rule,
        title: "Full-time exhaustive pattern matching required on closed union types",
        messageFormat: "Switch expression over closed union type '{0}' uses a wildcard discard '_' which masks unhandled variants; pattern matching must explicitly handle all cases.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Banning discard '_' on closed union and enum hierarchies guarantees full-time compile-time exhaustiveness when new variants are added.");

    public static readonly DiagnosticDescriptor Rule502 = new(
        id: RuleContracts.DFG502.Rule,
        title: "Domain operations must be pure functions",
        messageFormat: "Domain method '{0}' in '{1}' is non-static or writes to state; domain operations must be pure static functions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "In functional architecture, domain operations are pure static functions (State -> State) rather than mutating instance methods.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule501, Rule502);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static bool IsTestClass(ClassDeclarationSyntax classDecl)
    {
        var name = classDecl.Identifier.ValueText;
        return name.EndsWith("Tests", StringComparison.Ordinal) || name.EndsWith("Test", StringComparison.Ordinal);
    }

    private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        var switchExpr = (SwitchExpressionSyntax)context.Node;
        var governingExpr = switchExpr.GoverningExpression.ToString();

        // Check if switch expression matches on closed union type / enum
        var hasUnionArm = switchExpr.Arms.Any(static arm => arm.Pattern switch
        {
            ConstantPatternSyntax cp => cp.Expression is MemberAccessExpressionSyntax,
            DeclarationPatternSyntax => true,
            TypePatternSyntax => true,
            RecursivePatternSyntax => true,
            _ => false
        });

        if (!hasUnionArm) return;

        foreach (var arm in switchExpr.Arms)
        {
            if (arm.Pattern is DiscardPatternSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule501,
                    arm.GetLocation(),
                    governingExpr));
            }
        }
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        if (IsTestClass(classDecl)) return;

        var fieldNames = classDecl.Members.OfType<FieldDeclarationSyntax>()
            .Where(static f => !f.Modifiers.Any(SyntaxKind.StaticKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
            .SelectMany(static f => f.Declaration.Variables)
            .Select(static v => v.Identifier.ValueText)
            .ToHashSet();

        var propNames = classDecl.Members.OfType<PropertyDeclarationSyntax>()
            .Where(static p => !p.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Select(static p => p.Identifier.ValueText)
            .ToHashSet();

        if (fieldNames.Count == 0 && propNames.Count == 0) return;

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
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule502,
                    method.Identifier.GetLocation(),
                    method.Identifier.ValueText,
                    classDecl.Identifier.ValueText));
            }
        }
    }
}
