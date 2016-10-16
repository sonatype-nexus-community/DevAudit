using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Devsense.PHP.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax.Ast.Serialization;
using Devsense.PHP.Errors;

namespace DevAudit.AuditLibrary
{
    public class PHPAuditErrorSink : IErrorSink<Span>
    {
        #region Public types
        public class ErrorInstance
        {
            public Span Span;
            public ErrorInfo Error;
            public string[] Args;
        }
        #endregion

        #region Public constructors
        public PHPAuditErrorSink(AuditEnvironment audit_env)
        {
            this.AuditEnvironment = audit_env;
        }
        #endregion

        #region Public properties
        public readonly List<ErrorInstance> Errors = new List<ErrorInstance>();

        public int Count => this.Errors.Count;
        #endregion

        #region Public methods
        public void Error(Span span, ErrorInfo info, params string[] argsOpt)
        {
            Errors.Add(new ErrorInstance()
            {
                Span = span,
                Error = info,
                Args = argsOpt,
            });
            AuditEnvironment.Debug("Error written to PHP parser sink.");
        }
        #endregion

        #region Protected properties
        AuditEnvironment AuditEnvironment { get; set; }
        #endregion

       
    
    }
}
