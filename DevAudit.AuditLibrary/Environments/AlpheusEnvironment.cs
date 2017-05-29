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

        public override object InvokeXPathFunction(AlpheusXPathFunction f, AlpheusXsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (f.Prefix == "db")
            {
                if (f.Name == "query")
                {
                    if (this.AuditTarget is IDbAuditTarget)
                    {
                        IDbAuditTarget db = this.AuditTarget as IDbAuditTarget;
                        return db.ExecuteDbQueryToXml(args);
                    }
                    else
                    {
                        this.AuditEnvironment.Error("The current target is not a database server or application and does not support queries.");
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml("<error>No Database</error>");
                        return doc.CreateNavigator().Select("/");
                    }
                }
                else throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by IDbAuditTarget.");
            }
            else if (f.Prefix == "os")
            {
                if (f.Name == "exec")
                {
                    AuditEnvironment.ProcessExecuteStatus status = AuditEnvironment.ProcessExecuteStatus.Unknown;
                    string output = string.Empty, error = string.Empty;
                    string command = "", arguments = "";
                    XmlDocument xml = new XmlDocument();
                    if (args.Count() == 1)
                    {
                        command = (string)args[0];
                    }
                    else
                    {
                        command = (string)args[0];
                        arguments = (string)args[1];
                    }
                    if (this.AuditEnvironment.Execute(command, arguments, out status, out output, out error))
                    {
                        this.AuditEnvironment.Debug("os exec {0} {1} returned {2}", command, arguments, output);
                        return output;
                    }
                    else
                    {
                        this.AuditEnvironment.Error("Could not execute \"{0} {1}\" in audit environment. Error: {2} {3}", command, arguments, error, output);
                        return string.Empty;
                    }
                }
                else throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by any audit targets.");
            }
            else throw new NotImplementedException("The XPath function prefix" + f.Prefix + " is not supported by any audit targets.");
        }

        public override object EvaluateXPathVariable(AlpheusXPathVariable v, AlpheusXsltContext xslt_context)
        {
            if (v.Prefix == "env")
            {
                return this.AuditTarget.GetEnvironmentVar((string) v.Name);
            }
            else
            {
                this.AuditEnvironment.Error("Unknown variable type: {0}:{1}", v.Prefix, v.Name);
                return string.Empty;
            }
        }
        #endregion

        #region Properties
        public AuditTarget AuditTarget { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion
    }
}
