using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace SimpleSourceProtector
{
    public class UsingRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;

        private List<UsingDirectiveSyntax> m_temp = new List<UsingDirectiveSyntax>();
        public UsingRewriter(SemanticModel semanticModel) : base(true)
        {
            this.SemanticModel = semanticModel;
        }

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            m_temp.Add(node);
            return null;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            SyntaxList<UsingDirectiveSyntax> list = new SyntaxList<UsingDirectiveSyntax>();
            list = list.AddRange(m_temp);
            return node.WithUsings(list);
        }
    }
}