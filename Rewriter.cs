using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SimpleSourceProtector
{
    public class Rewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;

        public Rewriter(SemanticModel semanticModel) : base(true)
        {
            this.SemanticModel = semanticModel;
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Declaration.Variables.Count > 1)
            {
                return node;
            }
            if (node.Declaration.Variables[0].Initializer == null)
            {
                return node;
            }

            VariableDeclaratorSyntax declarator = node.Declaration.Variables.First();
            TypeSyntax variableTypeName = node.Declaration.Type;

            ITypeSymbol variableType = (ITypeSymbol)SemanticModel.GetSymbolInfo(variableTypeName).Symbol;

            TypeInfo initializerInfo = SemanticModel.GetTypeInfo(declarator.Initializer.Value);


            if (variableType == initializerInfo.Type)
            {
                TypeSyntax varTypeName = IdentifierName("var").WithLeadingTrivia(variableTypeName.GetLeadingTrivia()).WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

                return node.ReplaceNode(variableTypeName, varTypeName);
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode Visit(SyntaxNode node) => RemoveTriviaSpaces(base.Visit(node));
        public override SyntaxNode VisitBlock(BlockSyntax node) => RemoveTriviaSpaces(base.VisitBlock(node));
        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node) => RemoveTriviaSpaces(base.VisitExpressionStatement(node));
        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node) => SyntaxFactory.EmptyStatement();

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
                    || trivia.IsKind(SyntaxKind.EndOfDocumentationCommentToken)
                    || trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)
                    || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                    || trivia.IsKind(SyntaxKind.RegionKeyword)
                    || trivia.IsKind(SyntaxKind.EndRegionKeyword)
                    || trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                    || trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                    || trivia.IsKind(SyntaxKind.XmlCommentStartToken)
                    || trivia.IsKind(SyntaxKind.XmlComment)
                    || trivia.IsKind(SyntaxKind.XmlCommentEndToken)
                    || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                )
            {
                return SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "");
            }

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "");

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                return SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");

            return base.VisitTrivia(trivia);
        }

        private static SyntaxNode RemoveTriviaSpaces(SyntaxNode node)
        {
            if (node == null) return node;

            if (node.HasLeadingTrivia)
            {
                var triviaList = node.GetLeadingTrivia();

                foreach (var trivia in triviaList)
                {
                    if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                        || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                        || trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
                        || trivia.IsKind(SyntaxKind.EndOfDocumentationCommentToken)
                        || trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.RegionKeyword)
                        || trivia.IsKind(SyntaxKind.EndRegionKeyword)
                        || trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.XmlCommentStartToken)
                        || trivia.IsKind(SyntaxKind.XmlComment)
                        || trivia.IsKind(SyntaxKind.XmlCommentEndToken)
                        || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                    )
                    {
                        node = node.WithoutLeadingTrivia();
                    }
                    if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                        node = node.ReplaceTrivia(trivia, SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, ""));

                    if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                        node = node.ReplaceTrivia(trivia, SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));
                }
            }
            if (node.HasTrailingTrivia)
            {
                var triviaList = node.GetTrailingTrivia();

                foreach (var trivia in triviaList)
                {
                    if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                        || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                        || trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
                        || trivia.IsKind(SyntaxKind.EndOfDocumentationCommentToken)
                        || trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.RegionKeyword)
                        || trivia.IsKind(SyntaxKind.EndRegionKeyword)
                        || trivia.IsKind(SyntaxKind.IfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia)
                        || trivia.IsKind(SyntaxKind.XmlCommentStartToken)
                        || trivia.IsKind(SyntaxKind.XmlComment)
                        || trivia.IsKind(SyntaxKind.XmlCommentEndToken)
                        || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                    )
                    {
                        node = node.WithoutTrailingTrivia();
                    }
                    if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                        node = node.ReplaceTrivia(trivia, SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, ""));

                    if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                        node = node.ReplaceTrivia(trivia, SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "));
                }
            }
            return node;
        }

    }
}