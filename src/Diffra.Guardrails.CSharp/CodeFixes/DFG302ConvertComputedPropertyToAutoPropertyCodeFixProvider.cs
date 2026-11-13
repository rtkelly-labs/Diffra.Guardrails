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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG302ConvertComputedPropertyToAutoPropertyCodeFixProvider)), Shared]
public sealed class DFG302ConvertComputedPropertyToAutoPropertyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG302.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var propertyDecl = node.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        if (propertyDecl != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Convert '{propertyDecl.Identifier.ValueText}' to auto-property",
                    createChangedDocument: c => ConvertToAutoPropertyAsync(context.Document, root, propertyDecl, c),
                    equivalenceKey: "ConvertToAutoProperty"),
                diagnostic);
        }
    }

    private static Task<Document> ConvertToAutoPropertyAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDecl,
        CancellationToken cancellationToken)
    {
        var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var initAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var newAccessorList = SyntaxFactory.AccessorList(
            SyntaxFactory.List(new[] { getAccessor, initAccessor }));

        var updatedProperty = propertyDecl
            .WithExpressionBody(null)
            .WithSemicolonToken(default)
            .WithAccessorList(newAccessorList)
            .WithTrailingTrivia(propertyDecl.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(propertyDecl, updatedProperty);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
