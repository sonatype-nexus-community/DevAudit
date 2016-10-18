using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;  

namespace DevAudit.AuditLibrary
{
    public class PHPAuditSourceUnit : CodeSourceUnit
    {
        #region Constructors
        public PHPAuditSourceUnit(AuditEnvironment audit_env, string code, FileInfo file) : base(code, file.FullName, Encoding.UTF8)
        {
            this.ErrorSink = new PHPAuditErrorSink(this.AuditEnvironment);
            BasicNodesFactory factory = new BasicNodesFactory(this);
            try
            {
                this.Parse(factory, this.ErrorSink);
                if (this.Ast != null)
                {
                    this.DTV = new DeclarationsTreeVisitor();
                    this.Ast.VisitMe(this.DTV);
                }
            }
            catch(Exception e)
            {
                this.AuditEnvironment.Error("Parsing file {0} through an exception.", file.FullName);
                this.AuditEnvironment.Error(e);
            }
        }
        #endregion

        #region Public properties;
        public DeclarationsTreeVisitor DTV  { get; protected set; }
        #endregion

        #region Protected Properties
        AuditEnvironment AuditEnvironment { get; set; }
        PHPAuditErrorSink ErrorSink { get; set; }
        #endregion



    }
}
