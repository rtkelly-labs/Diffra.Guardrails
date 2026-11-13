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
public sealed class DFG4xxDeepImmutabilityAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "DeepImmutability";

    public static readonly DiagnosticDescriptor Rule401 = new(
        id: RuleContracts.DFG401.Rule,
        title: "Records must be deeply immutable",
        messageFormat: "Record '{0}' has property '{1}' of mutable reference type '{2}'; records must be deeply immutable.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Records must guarantee deep immutability: all properties must be immutable value types, records, or immutable collections.");

    public static readonly DiagnosticDescriptor Rule402 = new(
        id: RuleContracts.DFG402.Rule,
        title: "Domain types must use immutable 'init;' or get-only properties",
        messageFormat: "Property '{0}' in domain type '{1}' declares a mutable setter 'set;'; domain properties must be immutable ('init;' or get-only).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "In functional architecture, domain properties must be immutable to eliminate temporal coupling and concurrency hazards.");

    public static readonly DiagnosticDescriptor Rule403 = new(
        id: RuleContracts.DFG403.Rule,
        title: "Use of banned non-deterministic or blocking APIs",
        messageFormat: "Use of banned symbol '{0}': {1}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Banned non-deterministic symbols (DateTime.Now/UtcNow), synchronous blocking calls (Thread.Sleep, Task.Result), or untyped collections.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule401, Rule402, Rule403);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeRecordDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static bool IsTestClass(BaseTypeDeclarationSyntax typeDecl)
    {
        var name = typeDecl.Identifier.ValueText;
        return name.EndsWith("Tests", StringComparison.Ordinal) || name.EndsWith("Test", StringComparison.Ordinal);
    }

    private static bool IsMutableCollection(string typeName)
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
               typeName.StartsWith("ICollection<", StringComparison.Ordinal);
    }

    private static void AnalyzeRecordDeclaration(SyntaxNodeAnalysisContext context)
    {
        var recordDecl = (RecordDeclarationSyntax)context.Node;
        if (IsTestClass(recordDecl)) return;
        var recordName = recordDecl.Identifier.ValueText;

        if (recordDecl.ParameterList != null)
        {
            foreach (var param in recordDecl.ParameterList.Parameters)
            {
                if (param.Type != null)
                {
                    var typeStr = param.Type.ToString();
                    if (IsMutableCollection(typeStr) || typeStr.StartsWith("Mutable", StringComparison.Ordinal) || typeStr.EndsWith("Class", StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule401,
                            param.Identifier.GetLocation(),
                            recordName,
                            param.Identifier.ValueText,
                            typeStr));
                    }
                }
            }
        }

        foreach (var prop in recordDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var typeStr = prop.Type.ToString();
            if (IsMutableCollection(typeStr) || typeStr.StartsWith("Mutable", StringComparison.Ordinal) || typeStr.EndsWith("Class", StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule401,
                    prop.Identifier.GetLocation(),
                    recordName,
                    prop.Identifier.ValueText,
                    typeStr));
            }
        }
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var prop = (PropertyDeclarationSyntax)context.Node;
        var enclosingType = prop.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        if (enclosingType == null || IsTestClass(enclosingType)) return;

        var setter = prop.AccessorList?.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (setter != null)
        {
            var typeName = enclosingType switch
            {
                TypeDeclarationSyntax td => td.Identifier.ValueText,
                _ => "Unknown"
            };
            context.ReportDiagnostic(Diagnostic.Create(
                Rule402,
                setter.Keyword.GetLocation(),
                prop.Identifier.ValueText,
                typeName));
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var text = memberAccess.ToString();

        if (text is "DateTime.Now" or "DateTime.UtcNow")
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule403,
                memberAccess.GetLocation(),
                text,
                "non-deterministic system clock dependency"));
        }
        else if (text is "Thread.Sleep")
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule403,
                memberAccess.GetLocation(),
                text,
                "synchronous thread blocking"));
        }
        else if (memberAccess.Name.Identifier.ValueText is "Result" && text.EndsWith(".Result", StringComparison.Ordinal) && !text.Contains("Should()"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule403,
                memberAccess.GetLocation(),
                text,
                "synchronous blocking on task risks deadlock"));
        }
        else if (memberAccess.Name.Identifier.ValueText is "Wait" && text.EndsWith(".Wait", StringComparison.Ordinal) && !text.Contains("Should()"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule403,
                memberAccess.GetLocation(),
                text,
                "synchronous blocking on task risks deadlock"));
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objCreation = (ObjectCreationExpressionSyntax)context.Node;
        var typeName = objCreation.Type.ToString();
        if (typeName is "ArrayList" or "Hashtable" or "System.Collections.ArrayList" or "System.Collections.Hashtable")
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule403,
                objCreation.GetLocation(),
                typeName,
                "untyped legacy collection"));
        }
    }
}
