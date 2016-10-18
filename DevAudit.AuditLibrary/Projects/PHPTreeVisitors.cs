using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace DevAudit.AuditLibrary
{
    public class DeclarationsTreeVisitor : TreeVisitor
    {
        public List<FunctionDecl> FunctionDeclarations { get; private set; } = new List<FunctionDecl>();
        public List<MethodDecl> MethodDeclarations { get; private set; } = new List<MethodDecl>();
        public List<NamedTypeDecl> ClassDeclarations { get; private set; } = new List<NamedTypeDecl>();
        public List<ClassConstantDecl> ClassConstantDeclarations { get; private set; } = new List<ClassConstantDecl>();

        public DeclarationsTreeVisitor() : base() {}

        public override void VisitFunctionDecl(FunctionDecl x)
        {
            base.VisitFunctionDecl(x);
            this.FunctionDeclarations.Add(x);
        }

        public override void VisitMethodDecl(MethodDecl x)
        {
            base.VisitMethodDecl(x);
            this.MethodDeclarations.Add(x);
        }

        public override void VisitClassConstantDecl(ClassConstantDecl x)
        {
            base.VisitClassConstantDecl(x);
            this.ClassConstantDeclarations.Add(x);
        }
        public override void VisitNamedTypeDecl(NamedTypeDecl x)
        {
            base.VisitNamedTypeDecl(x);
            this.ClassDeclarations.Add(x);
        }
    }

    public class CheckClassesDeclaredTreeVisitor : TreeVisitor
    {
        public Dictionary<string, bool> ClassesDeclared { get; private set; }

        public CheckClassesDeclaredTreeVisitor(List<string> class_names) : base()
        {
            this.ClassesDeclared = new Dictionary<string, bool>();
            foreach (string cn in class_names)
            {
                this.ClassesDeclared.Add(cn, false);
            }
        }

        
        public override void VisitNamedTypeDecl(NamedTypeDecl x)
        {
            base.VisitNamedTypeDecl(x);
            if (ClassesDeclared.Any(kv => x.BaseClass.QualifiedName.Name.Value.EndsWith(kv.Key)))
            {
                string k = ClassesDeclared.First(kv => x.BaseClass.QualifiedName.Name.Value.EndsWith(kv.Key)).Key;
                ClassesDeclared[k] = true; 
            }

        }


    }

}