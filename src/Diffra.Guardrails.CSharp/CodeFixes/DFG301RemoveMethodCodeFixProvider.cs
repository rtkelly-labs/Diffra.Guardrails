using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Diffra.Guardrails.Core.Catalogue;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Diffra.Guardrails.CSharp.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG301RemoveMethodCodeFixProvider)), Shared]
public sealed class DFG301RemoveMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG301.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var methodDecl = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Remove method '{methodDecl.Identifier.ValueText}' (relocate to domain function)",
                    createChangedDocument: c => RemoveMethodAsync(context.Document, root, methodDecl, c),
                    equivalenceKey: "RemoveContractMethod"),
                diagnostic);
        }
    }

    private static Task<Document> RemoveMethodAsync(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax methodDecl,
        CancellationToken cancellationToken)
    {
        var newRoot = root.RemoveNode(methodDecl, SyntaxRemoveOptions.KeepNoTrivia);
        return Task.FromResult(document.WithSyntaxRoot(newRoot!));
    }
}
