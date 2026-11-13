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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG403TimeProviderCodeFixProvider)), Shared]
public sealed class DFG403TimeProviderCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG403.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess != null && memberAccess.ToString() is "DateTime.Now" or "DateTime.UtcNow")
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use 'TimeProvider.System.GetUtcNow()'",
                    createChangedDocument: c => ReplaceWithTimeProviderAsync(context.Document, root, memberAccess, c),
                    equivalenceKey: "UseTimeProvider"),
                diagnostic);
        }
    }

    private static Task<Document> ReplaceWithTimeProviderAsync(
        Document document,
        SyntaxNode root,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.ParseExpression("TimeProvider.System.GetUtcNow()")
            .WithTriviaFrom(memberAccess);

        var newRoot = root.ReplaceNode(memberAccess, replacement);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
