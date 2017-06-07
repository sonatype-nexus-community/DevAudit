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

        public override IXsltContextFunction ResolveXPathFunction(string prefix, string name, XPathResultType[] ArgTypes)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            switch (prefix)
            {
                case "db":
                    switch (name)
                    {
                        case "query":
                            Debug("Resolved db:query function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 2, new XPathResultType[] { XPathResultType.String, XPathResultType.String }, XPathResultType.NodeSet);
                        default:
                            Error("DevAudit coulld not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                case "os":
                    switch (name)
                    {
                        case "exec":
                            Debug("Resolved os:exec function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 2, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        default:
                            Error("DevAudit coulld not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                case "ver":                   
                    switch (name)
                    {
                        case "gt":
                            Debug("Resolved ver:gt function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "lt":
                            Debug("Resolved ver:lt function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "eq":
                            Debug("Resolved ver:eq function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "gte":
                            Debug("Resolved ver:gte function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "lte":
                            Debug("Resolved ver:lte function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        default:
                            Error("DevAudit coulld not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                case "fs":                    
                    switch (name)
                    {
                        case "exists":
                            Debug("Resolved fs:exists function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "text":
                            Debug("Resolved fs:text function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        case "unix_file_mode":
                            Debug("Resolved fs:unix_file_mode function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        default:
                            Error("DevAudit coulld not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                default: return null;

            }
        }

        public override object InvokeXPathFunction(AlpheusXPathFunction f, AlpheusXsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            switch (f.Prefix)
            {
                case "db":
                    switch (f.Name)
                    {
                        case "query":
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
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by IDbAuditTarget.");
                    }
                case "os":
                    switch (f.Name)
                    {
                        case "exec":
                            string output = string.Empty, command = "", arguments = "";
                            if (args.Count() == 1)
                            {
                                command = (string)args[0];
                            }
                            else
                            {
                                command = (string)args[0];
                                arguments = (string)args[1];
                            }
                            if (this.AuditEnvironment.ExecuteCommand(command, arguments, out output))
                            {
                                this.AuditEnvironment.Debug("os:exec {0} {1} returned {2}", command, arguments, output);
                                return output;
                            }
                            else
                            {
                                this.AuditEnvironment.Error("Could not os:exec \"{0} {1}\". Error: {2}", command, arguments, output);
                                return string.Empty;
                            }
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by IDbAuditTarget.");
                    }
                case "fs":
                    switch (f.Name)
                    {
                        case "unix_file_mode":
                            return this.AuditEnvironment.GetUnixFileMode((string)args[0]);

                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by any audit target.");
                    }
                default:
                    throw new NotImplementedException("The XPath function prefix" + f.Prefix + " is not supported by any audit targets.");

                case "ver":
                    string version = (string)args[0];
                    switch (f.Name)
                    {
                        case "lt":
                            return this.Application.IsVulnerabilityVersionInPackageVersionRange("<" + version, this.Application.Version);
                        case "gt":
                            return this.Application.IsVulnerabilityVersionInPackageVersionRange(">" + version, this.Application.Version);
                        case "eq":
                            return this.Application.IsVulnerabilityVersionInPackageVersionRange(version, this.Application.Version);
                        case "lte":
                            return this.Application.IsVulnerabilityVersionInPackageVersionRange("<=" + version, this.Application.Version);
                        case "gte":
                            return this.Application.IsVulnerabilityVersionInPackageVersionRange(">=" + version, this.Application.Version);
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by any audit target.");
                    }

            }
        }

        public override object EvaluateXPathVariable(AlpheusXPathVariable v, AlpheusXsltContext xslt_context)
        {
            switch (v.Prefix)
            {
                case "os":
                    switch(v.Name)
                    {
                        case "platform":
                            if (this.AuditEnvironment.IsWindows)
                            {
                                return "Windows";
                            }
                            else
                            {
                                return "Unix";
                            }
                        default:
                            this.AuditEnvironment.Error("Unknown os variable: {0}", v.Name);
                            return string.Empty;

                    }
                case "env":
                    return this.AuditTarget.GetEnvironmentVar((string) v.Name);
           
                default:
                    this.AuditEnvironment.Error("Unknown variable type: {0}:{1}", v.Prefix, v.Name);
                    return string.Empty;
            }
  
        }
        #endregion

        #region Properties
        public AuditTarget AuditTarget { get; protected set; }
        public Application Application
        {
            get
            {
                return this.AuditTarget as Application;
            }
        }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        #endregion
    }
}
