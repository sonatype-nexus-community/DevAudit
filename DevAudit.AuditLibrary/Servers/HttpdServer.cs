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
        public HttpdServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, 
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "find", "@", "*bin", "httpd" } },
                { PlatformID.Win32NT, new string[] { "@", "bin", "httpd.exe" } }
            },
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "@", "etc", "apache2", "apache2.conf" } },
                { PlatformID.Win32NT, new string[] { "@", "conf", "httpd.conf" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["httpd"] = this.ApplicationBinary;
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "httpd"; } }

        public override string ServerLabel { get { return "Apache httpd"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<Package>> GetModules()
        {
            Dictionary<string, IEnumerable<Package>> m = new Dictionary<string, IEnumerable<Package>>
            {
                {"httpd", new List<Package> {new Package(this.PackageManagerId, "httpd", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized =  this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override IConfiguration GetConfiguration()
        {
            if (this.Configuration == null)
            { 
                Httpd httpd = new Httpd(this.ConfigurationFile, this.AlpheusEnvironment);
                if (httpd.ParseSucceded)
                {
                    this.Configuration = httpd;
                    this.ConfigurationInitialised = true;
                }
                else
                {
                    this.AuditEnvironment.Error("Could not parse configuration from {0}.", httpd.FullFilePath);
                    if (httpd.LastParseException != null) this.AuditEnvironment.Error(httpd.LastParseException);
                    if (httpd.LastIOException != null) this.AuditEnvironment.Error(httpd.LastIOException);
                    this.Configuration = null;
                    this.ConfigurationInitialised = false;
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
                this.VersionInitialised = true;
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
        
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            return this.ModulePackages["httpd"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
        
    }
}
