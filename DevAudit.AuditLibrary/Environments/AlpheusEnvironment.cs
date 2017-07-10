using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Xml.Linq;

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

        public override void Message(string message_type, string message_format, params object[] message)
        {
            switch(message_type)
            {
                case "INFO":
                    this.AuditEnvironment.Message(EventMessageType.INFO, message_format, message);
                    break;
                case "DEBUG":
                    this.AuditEnvironment.Message(EventMessageType.DEBUG, message_format, message);
                    break;
                case "ERROR":
                    this.AuditEnvironment.Message(EventMessageType.ERROR, message_format, message);
                    break;
                case "SUCCESS":
                    this.AuditEnvironment.Message(EventMessageType.SUCCESS, message_format, message);
                    break;
                case "WARNING":
                    this.AuditEnvironment.Message(EventMessageType.WARNING, message_format, message);
                    break;
                case "PROGRESS":
                    this.AuditEnvironment.Message(EventMessageType.PROGRESS, message_format, message);
                    break;
                case "STATUS":
                    this.AuditEnvironment.Message(EventMessageType.STATUS, message_format, message);
                    break;
                default:
                    throw new Exception("Unknown message type: " + message_type);
            }
        }
   
        public override IXsltContextFunction ResolveXPathFunction(string prefix, string name, XPathResultType[] ArgTypes)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            switch (prefix)
            {
                case "l":
                    switch (name)
                    {
                        case "any":
                            return new AlpheusXPathFunction(prefix, name, 1, 2, new XPathResultType[] { XPathResultType.Navigator, XPathResultType.String }, XPathResultType.NodeSet);
                        default:
                            Error("Unknown XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                case "db":
                    switch (name)
                    {
                        case "query":
                            Debug("Resolved db:query function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 2, new XPathResultType[] { XPathResultType.String, XPathResultType.String }, XPathResultType.NodeSet);
                        default:
                            Error("Could not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                case "os":
                    switch (name)
                    {
                        case "exec":
                            Debug("Resolved os:exec function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 2, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        default:
                            Error("Could not resolve XPath function: {0}:{1}.", prefix, name);
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
                            Error("Could not resolve XPath function: {0}:{1}.", prefix, name);
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
                        case "unix-file-mode":
                            Debug("Resolved fs:unix-file-mode function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        case "find-files":
                            Debug("Resolved fs:find-files function.");
                            return new AlpheusXPathFunction(prefix, name, 2, 2, new XPathResultType[] { XPathResultType.String, XPathResultType.String }, XPathResultType.String);
                        case "find-dirs":
                            Debug("Resolved fs:find-dirs function.");
                            return new AlpheusXPathFunction(prefix, name, 2, 2, new XPathResultType[] { XPathResultType.String, XPathResultType.String }, XPathResultType.String);
                        case "is-symbolic-link":
                            Debug("Resolved fs:is-symbolic-link function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.Boolean);
                        case "symbolic-link-location":
                            Debug("Resolved fs:symbolic-link-location function.");
                            return new AlpheusXPathFunction(prefix, name, 1, 1, new XPathResultType[] { XPathResultType.String }, XPathResultType.String);
                        default:
                            Error("Could not resolve XPath function: {0}:{1}.", prefix, name);
                            return null;
                    }
                default: return null;

            }
        }

        public override object InvokeXPathFunction(AlpheusXPathFunction f, AlpheusXsltContext xslt_context, object[] args, XPathNavigator doc_context)
        {
            for (int argc = 0; argc < args.Length; argc++)
            {
                if (args[argc] is string)
                {
                    args[argc] = ResolveXPathFunctionArgVariables((string) args[argc], xslt_context);
                }
            }
            switch (f.Prefix)
            {
                case "l":
                    switch (f.Name)
                    {
                        case "any":
                            string e;
                            XPathNodeIterator iter = (args[0] as XPathNodeIterator).Clone();
                            if (args.Count() == 1)
                            {
                                e = "./";
                            }
                            else
                            {
                                e = (string)args[1];
                            }
                            XPathExpression expr = this.CompileXPathExpression(e, doc_context);
                            if (expr == null)
                            {
                                Error("Failed to execute l:any with expression {0}.", e);
                                return false;
                            }
                            object r;
                            while (iter.MoveNext())
                            {
                                r = iter.Current.Evaluate(expr);
                                if (r == null)
                                {
                                    continue;
                                }
                                else if (r as bool? != null)
                                {
                                    bool? b = r as bool?;
                                    if (b.HasValue && b.Value) return true;
                                }
                                else if (r is XPathNodeIterator)
                                {
                                    XPathNodeIterator i = r as XPathNodeIterator;
                                    if (i.Count > 0) return true;
                                }
                                else if (r is double?)
                                {
                                    double? d = r as double?;
                                    if (d.HasValue) return true;
                                }
                                else if (r is string)
                                {
                                    string s = (string)r;
                                    if (!string.IsNullOrEmpty(s)) return true;
                                }
                                else throw new Exception("Unknown result type from evaluating expression " + e + " at iterator position " + iter.CurrentPosition);
                            }
                            return f;
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not implemented.");
                    }
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
                            output = this.AuditEnvironment.OSExec(command, arguments);
                            this.AuditEnvironment.Debug("os:exec {0} {1} returned {2}.", command, arguments, string.IsNullOrEmpty(output) ? "null" : output);
                            return output;   
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by this audit target.");
                    }
                case "fs":
                    switch (f.Name)
                    {
                        case "unix-file-mode":
                            return this.AuditEnvironment.GetUnixFileMode((string)args[0]);
                        case "find-files":
                            return this.AuditEnvironment.FindFiles((string)args[0], (string)args[1]);
                        case "find-dirs":
                            return this.AuditEnvironment.FindDirectories((string)args[0], (string)args[1]);
                        case "is-symbolic-link":
                            return this.AuditEnvironment.GetIsSymbolicLink((string)args[0]);
                        case "symbolic-link-location":
                            return this.AuditEnvironment.GetSymbolicLinkLocation((string)args[0]);
                        default:
                            throw new NotImplementedException("The XPath function " + f.Prefix + ":" + f.Name + " is not supported by any audit target.");
                    }
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
                default:
                    throw new NotImplementedException("The XPath function prefix" + f.Prefix + " is not supported by any audit targets.");

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
                    return this.AuditEnvironment.GetEnvironmentVar((string) v.Name);

                case "fs":
                    if (v.Name.StartsWith("_"))
                    {
                        Debug("Function store reference fs:{0} not initialised yet.", v.Name);
                    }
                    else
                    {
                        this.AuditEnvironment.Error("Unknown fs variable: {1}", v.Name);
                    }
                    return string.Empty;
                case "app":
                    if (this.AuditTarget.AuditOptions.ContainsKey(v.Name))
                    {
                        string o = (string)this.AuditTarget.AuditOptions[v.Name];
                        string values = o.Split(';').Select(value => "<value>" + value +"</value>").Aggregate((v1, v2) => v1 + v2);
                        return XElement.Parse(values).CreateNavigator();
                    }
                    else
                    {
                        this.AuditEnvironment.Error("Unknown audit option: {0}:{1}", v.Prefix, v.Name);
                        return null;
                    }
                case "appfs":
                    if (this.Application.ApplicationFileSystemMap.ContainsKey(v.Name))
                    {
                        AuditFileSystemInfo f = this.Application.ApplicationFileSystemMap[v.Name];
                        Debug("Resolved appfs:{0} variable to {1}.", v.Name, f.FullName);
                        return f.FullName;
                    }
                    else
                    {
                        Error("appfs:{0} does not exist in the application file system map.", v.Name);
                        return string.Empty;
                    }
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

        #region Methods
        protected string ResolveXPathFunctionArgVariables(string arg, AlpheusXsltContext context)
        {
            CallerInformation here = this.AuditEnvironment.Here();
            Match m = Regex.Match(arg, @"\%\((\S+)\:(\S+)\)");
            if (m.Success)
            {
                string p = m.Groups[1].Value;
                string n = m.Groups[2].Value;
                object v = EvaluateXPathVariable(new AlpheusXPathVariable(p, n), context);
                if (v is string || v is bool || v is double)
                {
                    Debug("Resolving XPath variable reference ${0}:{1} in arg {2} to {3}.", p, n, arg, (string)v);
                    return arg.Replace(m.Groups[0].Value, (string)v);
                }
                else
                {
                    Error("Variable ${0}:{1} is not a value type.", p, n);
                    return arg;
                }
            }
            else
            {
                return arg;
            }
        }

        protected XPathExpression CompileXPathExpression(string e, XPathNavigator context, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
        
            XPathExpression expr;
            try
            {
                expr = context.Compile(e);
            }
            catch (XPathException xpe)
            {
                Error(xpe, "Could not compile XPath expression {0}.", e);
                return null;
            }
            catch (ArgumentException ae)
            {
                Error(ae, "Could not compile XPath expression {0}.", e);
                return null;
            }
            Debug("XPath expression {0} has type {1}.", e, expr.ReturnType.ToString());
            return expr;
        }
        #endregion
    }
}
