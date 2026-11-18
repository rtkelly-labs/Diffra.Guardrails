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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DFG304ConvertToRecordCodeFixProvider)), Shared]
public sealed class DFG304ConvertToRecordCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleContracts.DFG304.Rule);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        var classDecl = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDecl != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Convert '{classDecl.Identifier.ValueText}' to record class",
                    createChangedDocument: c => ConvertToRecordAsync(context.Document, root, classDecl, c),
                    equivalenceKey: "ConvertToRecordClass"),
                diagnostic);
        }
    }

    private static Task<Document> ConvertToRecordAsync(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDecl,
        CancellationToken cancellationToken)
    {
        // Replace 'class' keyword with 'record class'
        var recordKeyword = SyntaxFactory.Token(SyntaxKind.RecordKeyword).WithLeadingTrivia(classDecl.Keyword.LeadingTrivia);
        var classKeyword = SyntaxFactory.Token(SyntaxKind.ClassKeyword).WithTrailingTrivia(classDecl.Keyword.TrailingTrivia);

        var recordDecl = SyntaxFactory.RecordDeclaration(
            SyntaxKind.RecordDeclaration,
            classDecl.AttributeLists,
            classDecl.Modifiers,
            recordKeyword,
            classKeyword,
            classDecl.Identifier,
            classDecl.TypeParameterList,
            null,
            classDecl.BaseList,
            classDecl.ConstraintClauses,
            classDecl.OpenBraceToken,
            classDecl.Members,
            classDecl.CloseBraceToken,
            classDecl.SemicolonToken);

        var newRoot = root.ReplaceNode(classDecl, recordDecl);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
