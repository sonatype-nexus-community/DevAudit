using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using Alpheus;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class AlEnvironment : AlpheusEnvironment
    {
        #region Constructors
        public AlEnvironment(AuditTarget at)
        {
            this.AuditTarget = at;
            this.AuditEnvironment = this.AuditTarget.AuditEnvironment;
        }
        #endregion

        #region Overriden methods
        public override IFileInfo ConstructFile(string file_path)
        {
            return this.AuditEnvironment.ConstructFile(file_path);
        }

        public override IDirectoryInfo ConstructDirectory(string dir_path)
        {
            return this.AuditEnvironment.ConstructDirectory(dir_path);
        }

        public override bool FileExists(string file_path)
        {
            return this.AuditEnvironment.FileExists(file_path);
        }

        public override bool DirectoryExists(string dir_path)
        {
            return this.AuditEnvironment.DirectoryExists(dir_path);
        }

        public override void Debug(string message_format, params object[] message)
        {
            this.AuditEnvironment.Debug(message_format, message);
        }

        public override void Info(string message_format, params object[] message)
        {
            this.AuditEnvironment.Info(message_format, message);
        }

        public override void Status(string message_format, params object[] message)
        {
            this.AuditEnvironment.Status(message_format, message);
        }

        public override void Progress(string message_format, params object[] message)
        {
            throw new NotImplementedException();
        }

        public override void Success(string message_format, params object[] message)
        {
            this.AuditEnvironment.Success(message_format, message);
        }

        public override void Warning(string message_format, params object[] message)
        {
            this.AuditEnvironment.Warning(message_format, message);
        }

        public override void Error(Exception e)
        {
            this.AuditEnvironment.Error(e);
        }

        public override void Error(Exception e, string message_format, params object[] message)
        {
            this.AuditEnvironment.Error(e, message_format, message);
        }

        public override void Error(string message_format, params object[] message)
        {
            this.AuditEnvironment.Error(message_format, message);
        }

        public override object InvokeXPathFunction(AlpheusXPathFunction f, XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            AlpheusXsltContext ctx = xsltContext as AlpheusXsltContext;
            if (this.AuditTarget is IDbAuditTarget)
            {
                IDbAuditTarget db = this.AuditTarget as IDbAuditTarget;
                return db.ExecuteDbQueryToXml(args);
            }
            else throw new InvalidOperationException("The current audit target does not support database operations.");
        }
        #endregion

        #region Properties
        public AuditTarget AuditTarget { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion
    }
}
