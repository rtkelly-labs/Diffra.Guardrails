using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Diffra.Guardrails.Core.Catalogue;
using Diffra.Guardrails.CSharp.Analyzers;
using Diffra.Guardrails.CSharp.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Diffra.Guardrails.Tests.AnalyzerTests;

public class DeepImmutabilityAndUnionAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> RunImmutabilityAnalyzerAsync(
        string source,
        string filePath = "src/Domain/Order.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new DFG4xxDeepImmutabilityAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunUnionAnalyzerAsync(
        string source,
        string filePath = "src/Domain/OrderService.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new DFG5xxAlgebraicTypesAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task DFG401_fires_when_record_contains_mutable_reference()
    {
        var source = """
            namespace Domain.Models;

            public class MutableAddress { public string City { get; set; } = ""; }

            public record Customer(string Id, MutableAddress Address);
            """;

        var diagnostics = await RunImmutabilityAnalyzerAsync(source);

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG401.Rule);
    }

    [Fact]
    public async Task DFG402_fires_and_codefix_replaces_set_with_init()
    {
        var source = """
            namespace Domain.Models;

            public class Order
            {
                public int Id { get; init; }
                public decimal Amount { get; set; }
            }
            """;

        var diagnostics = await RunImmutabilityAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG402.Rule);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Domain/Order.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG402MakePropertyInitCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.Contains("public decimal Amount { get; init; }", updatedSource);
    }

    [Fact]
    public async Task DFG403_fires_and_codefix_replaces_datetime_utcnow()
    {
        var source = """
            using System;

            namespace Domain.Models;

            public class ClockUser
            {
                public DateTime GetTime() => DateTime.UtcNow;
            }
            """;

        var diagnostics = await RunImmutabilityAnalyzerAsync(source);
        var targetDiag = Assert.Single(diagnostics, d => d.Id == RuleContracts.DFG403.Rule);

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProj", LanguageNames.CSharp);
        var document = project.AddDocument("src/Domain/ClockUser.cs", source);

        var actions = new System.Collections.Generic.List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            targetDiag,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        var codeFixProvider = new DFG403TimeProviderCodeFixProvider();
        await codeFixProvider.RegisterCodeFixesAsync(fixContext);

        Assert.NotEmpty(actions);
        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var updatedDoc = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        var updatedSource = (await updatedDoc.GetTextAsync()).ToString();

        Assert.Contains("TimeProvider.System.GetUtcNow()", updatedSource);
    }

    [Fact]
    public async Task DFG501_fires_when_switch_expression_uses_discard_on_union()
    {
        var source = """
            namespace Domain.Models;

            public enum OrderStatus { Pending, Approved, Cancelled }

            public class DiscountCalculator
            {
                public static decimal GetDiscount(OrderStatus status) => status switch
                {
                    OrderStatus.Pending => 0.0m,
                    OrderStatus.Approved => 0.05m,
                    _ => 0.0m
                };
            }
            """;

        var diagnostics = await RunUnionAnalyzerAsync(source);

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG501.Rule);
    }

    [Fact]
    public async Task DFG502_fires_when_domain_method_mutates_state()
    {
        var source = """
            namespace Domain.Models;

            public class BankAccount
            {
                private decimal _balance;
                public void Deposit(decimal amount)
                {
                    _balance += amount;
                }
            }
            """;

        var diagnostics = await RunUnionAnalyzerAsync(source);

        Assert.Contains(diagnostics, d => d.Id == RuleContracts.DFG502.Rule);
    }
}
