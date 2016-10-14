using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using DevAudit.AuditLibrary;

namespace DevAudit.AuditLibrary.Analyzers
{
    public class SA1300ElementMustBeginWithUpperCaseLetterAnalyzer : StyleCopAnalyzer
    {
        #region Constructors
        public SA1300ElementMustBeginWithUpperCaseLetterAnalyzer(ScriptEnvironment script_env, object workspace, object project, object compilation) : base(script_env, "SA1300ElementMustBeginWithUpperCaseLetterAnalyzer", workspace, project, compilation)
        {
            this.Summary = "The name of a C# element does not begin with an upper-case letter.";
            this.Description = "A violation of this rule occurs when the names of certain types of elements do not begin with an upper-case letter. The following types of elements should use an upper-case letter as the first letter of the element name: namespaces, classes, enums, structs, delegates, events, methods, and properties. In addition, any field which is public, internal, or marked with the const attribute should begin with an upper-case letter. Non-private readonly fields must also be named using an upper-case letter. If the field or variable name is intended to match the name of an item associated with Win32 or COM, and thus needs to begin with a lower-case letter, place the field or variable within a special NativeMethods class. A <c>NativeMethods</c> class is any class which contains a name ending in NativeMethods, and is intended as a placeholder for Win32 or COM wrappers. StyleCop will ignore this violation if the item is placed within a NativeMethods class.";
        }
        #endregion

        #region Public overriden methods
        public override async Task<AnalyzerResult> Analyze()
        {
            foreach (SyntaxTree tree in this.Compilation.SyntaxTrees)
            {
                CompilationUnitSyntax root = (CompilationUnitSyntax) await tree.GetRootAsync();
                List<TypeDeclarationSyntax> declarations = root.DescendantNodesAndSelf().Where(
                    x => x.IsKind(SyntaxKind.InterfaceDeclaration) ||
                    x.IsKind(SyntaxKind.ClassDeclaration) ||
                    x.IsKind(SyntaxKind.StructDeclaration)
                    ).Select(d => d as TypeDeclarationSyntax).ToList();
                
                if (declarations.Count == 0)
                {
                    continue;
                }
                this.ScriptEnvironment.Info("Got {0} type declarations in compilation unit {1}: {2}", declarations.Count, tree.FilePath, declarations.Select(d => d.Identifier.ValueText).Where(i => !string.IsNullOrEmpty(i)).Aggregate((s1, s2) => s1 + " " + s2));
               
                /*
                foreach (TypeDeclarationSyntax d in declarations)
                {
                    string identifier = d.Identifier.ValueText;
                    this.ScriptEnvironment.Debug(c.Identifier.ValueText);
                }*/
            }
            return this.AnalyzerResult;
        }
        #endregion
    }
}