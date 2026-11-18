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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG303MakePropertyInitCodeFixProvider)), Shared]
public sealed class DFG303MakePropertyInitCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG303.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        // Case 1: Mutable setter -> change set to init
        var accessor = node.AncestorsAndSelf().OfType<AccessorDeclarationSyntax>().FirstOrDefault();
        if (accessor != null && accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Change 'set;' to 'init;'",
                    createChangedDocument: c => ChangeSetToInitAsync(context.Document, root, accessor, c),
                    equivalenceKey: "ChangeSetToInit"),
                diagnostic);
            return;
        }

        // Case 2: Mutable collection type -> change to IReadOnlyList / IReadOnlyDictionary
        var typeSyntax = node.AncestorsAndSelf().OfType<TypeSyntax>().FirstOrDefault();
        if (typeSyntax != null)
        {
            var typeText = typeSyntax.ToString();
            string? replacement = null;
            if (typeText.StartsWith("List<"))
            {
                replacement = "IReadOnly" + typeText;
            }
            else if (typeText.StartsWith("Dictionary<"))
            {
                replacement = "IReadOnly" + typeText;
            }

            if (replacement != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Change '{typeText}' to '{replacement}'",
                        createChangedDocument: c => ReplaceTypeAsync(context.Document, root, typeSyntax, replacement, c),
                        equivalenceKey: "MakeCollectionReadOnly"),
                    diagnostic);
            }
        }
    }

    private static Task<Document> ChangeSetToInitAsync(
        Document document,
        SyntaxNode root,
        AccessorDeclarationSyntax accessor,
        CancellationToken cancellationToken)
    {
        var newKeyword = SyntaxFactory.Token(SyntaxKind.InitKeyword).WithTriviaFrom(accessor.Keyword);
        var newAccessor = accessor.WithKeyword(newKeyword);
        var newRoot = root.ReplaceNode(accessor, newAccessor);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> ReplaceTypeAsync(
        Document document,
        SyntaxNode root,
        TypeSyntax typeSyntax,
        string replacement,
        CancellationToken cancellationToken)
    {
        var newType = SyntaxFactory.ParseTypeName(replacement).WithTriviaFrom(typeSyntax);
        var newRoot = root.ReplaceNode(typeSyntax, newType);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
