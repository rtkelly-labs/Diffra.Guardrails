using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Diffra.Guardrails.Core.Catalogue;
using Diffra.Guardrails.CSharp.Analyzers;
using Diffra.Guardrails.CSharp.Architecture;
using Diffra.Guardrails.CSharp.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Diffra.Guardrails.Tests.AnalyzerTests;

public class ContractPurityAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
        string source,
        string filePath = "src/Contracts/OrderDto.cs",
        string assemblyName = "TestAssembly")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var compilation = CSharpCompilation.Create(assemblyName,
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new DFG3xxContractPurityAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task DFG301_fires_when_contract_type_declares_method()
    {
        var source = """
            namespace Domain.Contracts;

            public record OrderDto(int Id, decimal Price)
            {
                public decimal CalculateTotal() => Price * 1.2m;
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG301.Rule);
    }

    [Fact]
    public async Task DFG301_fires_and_codefix_removes_method()
    {
        var source = """
            namespace Domain.Contracts;

            public record OrderDto(int Id, decimal Price)
            {
                public decimal CalculateTotal() => Price * 1.2m;
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG301.Rule);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Contracts/OrderDto.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG301RemoveMethodCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.DoesNotContain("CalculateTotal", updatedSource);
    }

    [Fact]
    public async Task DFG302_fires_when_contract_property_has_computed_getter()
    {
        var source = """
            namespace Domain.Contracts;

            public record CustomerDto(string FirstName, string LastName)
            {
                public string FullName => $"{FirstName} {LastName}";
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG302.Rule);
    }

    [Fact]
    public async Task DFG302_fires_and_codefix_converts_to_auto_property()
    {
        var source = """
            namespace Domain.Contracts;

            public record CustomerDto(string FirstName, string LastName)
            {
                public string FullName => $"{FirstName} {LastName}";
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG302.Rule);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Contracts/CustomerDto.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG302ConvertComputedPropertyToAutoPropertyCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.Contains("public string FullName { get; init; }", updatedSource);
        Assert.DoesNotContain("=>", updatedSource);
    }

    [Fact]
    public async Task DFG303_fires_and_codefix_replaces_mutable_setter()
    {
        var source = """
            namespace Domain.Contracts;

            public record InvoiceDto
            {
                public int Id { get; init; }
                public decimal Amount { get; set; }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG303.Rule);

        // Verify CodeFixProvider
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Contracts/InvoiceDto.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG303MakePropertyInitCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.Contains("public decimal Amount { get; init; }", updatedSource);
    }

    [Fact]
    public async Task DFG304_fires_and_codefix_converts_class_to_record()
    {
        var source = """
            namespace Domain.Contracts;

            public class UserContract
            {
                public int Id { get; init; }
                public string Name { get; init; } = "";
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG304.Rule);

        // Verify CodeFixProvider
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Contracts/UserContract.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG304ConvertToRecordCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.Contains("public record class UserContract", updatedSource);
    }

    [Fact]
    public async Task Clean_pure_contract_reports_zero_diagnostics()
    {
        var source = """
            using System.Collections.Generic;

            namespace Domain.Contracts;

            public record ProductDto(int Id, string Sku, IReadOnlyList<string> Tags);
            """;

        var diagnostics = await RunAnalyzerAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Project_level_assembly_convention_enforces_contract_purity()
    {
        // Path has no 'Contracts', namespace has no 'Contracts', but assembly is 'Enterprise.Billing.Contracts'
        var source = """
            namespace Enterprise.Billing;

            public class Invoice
            {
                public int Id { get; set; }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(
            source,
            filePath: "src/Invoice.cs",
            assemblyName: "Enterprise.Billing.Contracts");

        // Should flag DFG304 (class) and DFG303 (mutable setter)
        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG304.Rule);
        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG303.Rule);
    }

    [Fact]
    public async Task Type_with_PureContract_attribute_enforces_contract_purity()
    {
        var source = """
            using System;

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
            public class PureContractAttribute : Attribute {}

            namespace Unrelated.Namespace;

            [PureContract]
            public class AdHocContract
            {
                public int Id { get; set; }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source, filePath: "src/AdHoc.cs", assemblyName: "App");

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG304.Rule);
        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG303.Rule);
    }

    [Fact]
    public void ContractBoundaryAssert_catches_violations_in_reflection_types()
    {
        var ex = Assert.Throws<ContractPurityViolationException>(() =>
        {
            ContractBoundaryAssert.AssertTypes(new[] { typeof(SampleViolatingContract) });
        });

        Assert.Contains(ex.Violations, v => v.Contains("[DFG304]"));
        Assert.Contains(ex.Violations, v => v.Contains("[DFG301]"));
        Assert.Contains(ex.Violations, v => v.Contains("[DFG303]"));
    }

    [Fact]
    public void ContractBoundaryAssert_passes_for_pure_record()
    {
        ContractBoundaryAssert.AssertTypes(new[] { typeof(SamplePureContract) });
    }
}

// Sample types for ContractBoundaryAssert reflection testing
public class SampleViolatingContract
{
    public int Id { get; set; }
    public System.Collections.Generic.List<string> Tags { get; set; } = new();
    public void Calculate() {}
}

public record SamplePureContract(int Id, string Sku, System.Collections.Generic.IReadOnlyList<string> Tags);
