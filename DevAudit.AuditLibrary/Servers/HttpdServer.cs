using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class HttpdServer : ApplicationServer
    {
        #region Constructors
        public HttpdServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "@", "etc", "apache2", "apache2.conf" } },
                { PlatformID.MacOSX, new string[] { "@", "etc", "apache2", "apache2.conf" } },
                { PlatformID.Win32NT, new string[] { "@", "conf", "httpd.conf" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["httpd"] = this.ApplicationBinary;
            }
            else
            {
                string fn = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePath("@", "bin", "httpd") : CombinePath("@", "bin", "httpd.exe");
                if (!File.Exists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for Apache Httpd was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = this.AuditEnvironment.ConstructFile(fn);
                    this.ApplicationFileSystemMap["httpd"] = this.ApplicationBinary;
                }
                this.PackageSourceInitialized = true; //Only default module "httpd" detected presently.
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "httpd"; } }

        public override string ServerLabel { get { return "Apache httpd"; } }
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"httpd", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "httpd", this.Version) }}
            };
            this.Modules = m;
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            if (this.Configuration == null)
            { 
                Httpd httpd = new Httpd(this.ConfigurationFile);
                if (httpd.ParseSucceded)
                {
                    this.Configuration = httpd;
                }
                else
                {
                    this.AuditEnvironment.Error("Failed to parse {0} configuration file {1}.", this.ApplicationLabel, this.ConfigurationFile.FullName);
                    if (httpd.LastException != null) this.AuditEnvironment.Error(httpd.LastException);
                    throw new Exception("Exception thrown parsing configuration file.", httpd.LastException);
                }
            }
            return this.Configuration;
        }

        protected override string GetVersion()
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(ApplicationBinary.FullName, "-v", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Split(Environment.NewLine.ToCharArray())[0];
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", ApplicationBinary.Name, process_error));
            }
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.GetModules()["httpd"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
        
    }
}
