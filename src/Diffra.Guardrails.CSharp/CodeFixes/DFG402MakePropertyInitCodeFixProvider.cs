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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG402MakePropertyInitCodeFixProvider)), Shared]
public sealed class DFG402MakePropertyInitCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG402.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var accessor = node.AncestorsAndSelf().OfType<AccessorDeclarationSyntax>().FirstOrDefault();
        if (accessor != null && accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Change 'set;' to 'init;'",
                    createChangedDocument: c => ChangeSetToInitAsync(context.Document, root, accessor, c),
                    equivalenceKey: "ChangeSetToInit"),
                diagnostic);
        }
    }

    private static Task<Document> ChangeSetToInitAsync(
        Document document,
        SyntaxNode root,
        AccessorDeclarationSyntax setAccessor,
        CancellationToken cancellationToken)
    {
        var initAccessor = SyntaxFactory.AccessorDeclaration(
            SyntaxKind.InitAccessorDeclaration,
            setAccessor.AttributeLists,
            setAccessor.Modifiers,
            SyntaxFactory.Token(SyntaxKind.InitKeyword).WithTriviaFrom(setAccessor.Keyword),
            setAccessor.Body,
            setAccessor.ExpressionBody,
            setAccessor.SemicolonToken);

        var newRoot = root.ReplaceNode(setAccessor, initAccessor);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
