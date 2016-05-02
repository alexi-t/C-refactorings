using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JsonFormatter
{
    public class JsonFormatRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var identifierToken = node.Identifier;
            var oldName = identifierToken.Text;
            var newName = JsonFormatterCodeRefactoringProvider.CapitalizeToken(identifierToken);

            return base.VisitClassDeclaration(node.WithIdentifier(Identifier(newName)));
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var identifierToken = node.Identifier;
            var oldName = identifierToken.Text;
            var newName = JsonFormatterCodeRefactoringProvider.CapitalizeToken(identifierToken);

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

            var newProp = node.
                WithIdentifier(Identifier(newName)).
                WithAttributeLists(attribute);

            return base.VisitPropertyDeclaration(newProp);
        }
    }
}
