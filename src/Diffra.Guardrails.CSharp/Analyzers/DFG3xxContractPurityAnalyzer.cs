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
public sealed class DFG3xxContractPurityAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "ContractPurity";

    public static readonly DiagnosticDescriptor Rule301 = new(
        id: RuleContracts.DFG301.Rule,
        title: "Pure contract types must not declare methods",
        messageFormat: "Contract type '{0}' declares method '{1}'; pure contract types must contain only data schemas and no behavior.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Pure contract types define schemas and data contracts; methods and business logic must live in standalone domain functions.");

    public static readonly DiagnosticDescriptor Rule302 = new(
        id: RuleContracts.DFG302.Rule,
        title: "Contract property getters must have zero behavior/computation",
        messageFormat: "Contract property '{0}' in '{1}' contains computed behavior; contract properties must be pure auto-properties with '{{ get; init; }}' or '{{ get; }}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Contract properties must be pure auto-properties without body computations, LINQ queries, or allocations.");

    public static readonly DiagnosticDescriptor Rule303 = new(
        id: RuleContracts.DFG303.Rule,
        title: "Contract properties must be immutable and use read-only collections",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Contract properties must be immutable ('init;' or get-only) and avoid mutable collection types like List<T> or Dictionary<K, V>.");

    public static readonly DiagnosticDescriptor Rule304 = new(
        id: RuleContracts.DFG304.Rule,
        title: "Permitted declaration kinds only in pure contract boundaries",
        messageFormat: "Type '{0}' is declared as '{1}'; contract types must be 'record class', 'record struct', 'enum', 'interface', or a static constants container.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types inside a contract boundary must be records, enums, interfaces, or static classes holding constants.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule301, Rule302, Rule303, Rule304);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.EnumDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDecl = (BaseTypeDeclarationSyntax)context.Node;
        var tree = typeDecl.SyntaxTree;
        var filePath = tree.FilePath ?? string.Empty;
        var assemblyName = context.Compilation.AssemblyName;

        if (IsTestClass(typeDecl) || !IsInContractBoundary(typeDecl, filePath, context.Options, assemblyName))
        {
            return;
        }

        var typeName = typeDecl switch
        {
            TypeDeclarationSyntax td => td.Identifier.ValueText,
            EnumDeclarationSyntax ed => ed.Identifier.ValueText,
            _ => "Unknown"
        };

        // DFG304: Permitted declaration kinds
        if (typeDecl is ClassDeclarationSyntax classDecl)
        {
            var isStatic = classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword));
            var members = classDecl.Members;
            var onlyConstFields = members.All(static m => m is FieldDeclarationSyntax fd && fd.Modifiers.Any(static mod => mod.IsKind(SyntaxKind.ConstKeyword)));

            if (!isStatic || !onlyConstFields)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule304,
                    classDecl.Identifier.GetLocation(),
                    typeName,
                    "class"));
            }
        }
        else if (typeDecl is StructDeclarationSyntax && typeDecl is not RecordDeclarationSyntax)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule304,
                typeDecl.GetLocation(),
                typeName,
                "struct"));
        }

        // Inspect members for DFG301, DFG302, DFG303
        if (typeDecl is TypeDeclarationSyntax typeDef)
        {
            foreach (var member in typeDef.Members)
            {
                if (member is MethodDeclarationSyntax method)
                {
                    // DFG301: Pure contract types must not declare methods
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule301,
                        method.Identifier.GetLocation(),
                        typeName,
                        method.Identifier.ValueText));
                }
                else if (member is PropertyDeclarationSyntax prop)
                {
                    var propName = prop.Identifier.ValueText;

                    // DFG302: Zero behavior/computation
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
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule302,
                            prop.Identifier.GetLocation(),
                            typeName,
                            propName));
                    }

                    // DFG303: Mutable setter
                    if (prop.AccessorList != null)
                    {
                        var setter = prop.AccessorList.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                        if (setter != null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Rule303,
                                setter.Keyword.GetLocation(),
                                RuleContracts.DFG303.MutableSetterMessage(propName)));
                        }
                    }

                    // DFG303: Mutable collection
                    var propTypeStr = prop.Type.ToString();
                    if (IsMutableCollectionType(propTypeStr))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Rule303,
                            prop.Type.GetLocation(),
                            RuleContracts.DFG303.MutableCollectionMessage(propName, propTypeStr)));
                    }
                }
            }

            // Primary constructor parameter inspection for records
            if (typeDecl is RecordDeclarationSyntax recordDecl && recordDecl.ParameterList != null)
            {
                foreach (var param in recordDecl.ParameterList.Parameters)
                {
                    if (param.Type != null)
                    {
                        var paramTypeStr = param.Type.ToString();
                        if (IsMutableCollectionType(paramTypeStr))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Rule303,
                                param.Type.GetLocation(),
                                RuleContracts.DFG303.MutableCollectionMessage(param.Identifier.ValueText, paramTypeStr)));
                        }
                    }
                }
            }
        }
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

            if (classDecl.Members.OfType<MethodDeclarationSyntax>().Any(static m =>
                m.AttributeLists.SelectMany(static al => al.Attributes).Any(static a => a.Name.ToString() is "Fact" or "Theory" or "Test" or "TestMethod")))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsInContractBoundary(SyntaxNode node, string filePath, AnalyzerOptions options, string? assemblyName = null)
    {
        // 0. Check compilation/assembly name conventions
        if (assemblyName != null && (
            assemblyName.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.EndsWith(".Models", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.EndsWith(".Dtos", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.EndsWith(".Specs", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Contains(".Contracts.", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Contains(".Models.", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Contains(".Dtos.", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 1. Check analyzer options (.editorconfig & build property)
        var tree = node.SyntaxTree;
        var config = options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        var globalConfig = options.AnalyzerConfigOptionsProvider.GlobalOptions;

        if ((globalConfig.TryGetValue("build_property.EnforcePureContracts", out var globalEnforce) &&
             string.Equals(globalEnforce, "true", StringComparison.OrdinalIgnoreCase)) ||
            (config.TryGetValue("build_property.EnforcePureContracts", out var enforceProp) &&
             string.Equals(enforceProp, "true", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (config.TryGetValue("diffra_guardrails.pure_contract", out var pureContract) &&
            string.Equals(pureContract, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (config.TryGetValue("diffra_guardrails.contract_namespaces", out var contractNamespaces) &&
            !string.IsNullOrWhiteSpace(contractNamespaces))
        {
            var currentNs = GetNamespace(node);
            if (currentNs != null && contractNamespaces.Split(';').Any(ns => currentNs.Contains(ns.Trim('*', ' '))))
            {
                return true;
            }
        }

        if (config.TryGetValue("diffra_guardrails.contract_paths", out var contractPaths) &&
            !string.IsNullOrWhiteSpace(contractPaths))
        {
            if (contractPaths.Split(';').Any(p => filePath.Contains(p.Trim('*', ' '))))
            {
                return true;
            }
        }

        // 2. Check path conventions
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

        // 3. Check namespace
        var namespaceName = GetNamespace(node);
        if (namespaceName != null && (
            namespaceName.EndsWith("Contracts", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.EndsWith("Models", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.EndsWith("Dtos", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.EndsWith("Specs", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.Contains(".Contracts.", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.Contains(".Models.", StringComparison.OrdinalIgnoreCase) ||
            namespaceName.Contains(".Dtos.", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 4. Check Attributes on the type
        if (node is BaseTypeDeclarationSyntax baseType)
        {
            if (baseType.AttributeLists.SelectMany(static al => al.Attributes).Any(static a =>
                a.Name.ToString() is "PureContract" or "PureContractAttribute" or "Contract" or "ContractAttribute"))
            {
                return true;
            }
        }

        // 5. Check leading trivia comments
        var trivia = node.GetLeadingTrivia();
        if (trivia.Any(static t => t.ToString().Contains("diffra:contract-boundary") || t.ToString().Contains("diffra:pure-contract")))
        {
            return true;
        }

        return false;
    }

    private static string? GetNamespace(SyntaxNode node) =>
        node.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
        ?? node.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();

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
}
