using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace DevAudit.AuditLibrary
{
    public class StatisticsTreeVisitor : TreeVisitor
    {
        public int FunctionDeclarationCount { get; private set; } = 0;
        public int MethodDeclarationCount { get; private set; } = 0;
        public int NamedTypeDeclarationCount { get; private set; } = 0;

        public StatisticsTreeVisitor() : base() {}

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            base.VisitFunctionDecl(x);
            FunctionDeclarationCount++;
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            base.VisitMethodDecl(x);
            MethodDeclarationCount++;
        }

        public override void VisitNamedTypeDecl(NamedTypeDecl x)
        {
            base.VisitNamedTypeDecl(x);
            NamedTypeDeclarationCount++;
        }

        public override void VisitDirectTypeRef(DirectTypeRef x)
        {
            base.VisitDirectTypeRef(x);
        }

    }
}