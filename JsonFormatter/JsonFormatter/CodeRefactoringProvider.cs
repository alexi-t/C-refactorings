using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JsonFormatter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(JsonFormatterCodeRefactoringProvider)), Shared]
    internal class JsonFormatterCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl != null)
            {
                // For any type declaration node, create a code action to reverse the identifier text.
                var action = CodeAction.Create("Format for JSON.NET", c => FormatTypeAsync(context, typeDecl, c));

                // Register this code action.
                context.RegisterRefactoring(action);
            }

            var propDecl = node as PropertyDeclarationSyntax;
            if (propDecl != null)
            {
                var action = CodeAction.Create("Format for JSON.NET", c => FormatPropertyAsync(context, propDecl, c));

                // Register this code action.
                context.RegisterRefactoring(action);
            }

        }

        private async Task<Document> FormatTypeAsync(CodeRefactoringContext context, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await context.Document.GetSemanticModelAsync(cancellationToken);

            var rewriter = new JsonFormatRewriter();

            var newType = rewriter.Visit(typeDecl);
            var newRoot = root.ReplaceNode(typeDecl, newType);

            return context.Document.WithSyntaxRoot(newRoot);
        }

        internal static string CapitalizeToken(SyntaxToken token)
        {
            return token.Text.Substring(0, 1).ToUpper() +
                   token.Text.Substring(1);
        }
        
        private async Task<Document> FormatPropertyAsync(CodeRefactoringContext context, PropertyDeclarationSyntax propDecl, CancellationToken cancellationToken)
        {
            // Produce a reversed version of the type declaration's identifier token.
            var identifierToken = propDecl.Identifier;
            var oldName = identifierToken.Text;
            var newName = CapitalizeToken(identifierToken);
            
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var attribute =
                SingletonList(
                        AttributeList(
                            SingletonSeparatedList(
                                Attribute(
                                    IdentifierName("JsonProperty"))
                                .WithArgumentList(
                                    AttributeArgumentList(
                                        SingletonSeparatedList(
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(oldName)))))))));

            var newProp = propDecl.
                WithIdentifier(Identifier(newName)).
                WithAttributeLists(attribute);
            
            var newRoot = root.ReplaceNode(propDecl, newProp);
            
            return context.Document.WithSyntaxRoot(newRoot);
        }
    }
}