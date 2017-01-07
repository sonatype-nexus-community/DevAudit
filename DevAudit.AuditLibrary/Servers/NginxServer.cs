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
    public class NginxServer : ApplicationServer
    {
        #region Constructors
        public NginxServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "@", "etc", "nginx", "nginx.conf" } },
                { PlatformID.MacOSX, new string[] { "@", "etc", "nginx", "nginx.conf" } },
                { PlatformID.Win32NT, new string[] { "@", "conf", "nginx.conf" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["nginx"] = this.ApplicationBinary;
            }
            else
            {
                string fn = this.AuditEnvironment.OS.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePath("@", "usr", "bin", "nginx") : CombinePath("@", "nginx.exe");
                if (!this.AuditEnvironment.FileExists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for Nginx was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = this.AuditEnvironment.ConstructFile(fn);
                    this.ApplicationFileSystemMap["nginx"] = this.ApplicationBinary;
                }
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "nginx"; } }

        public override string ServerLabel { get { return "Nginx"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(ApplicationBinary.FullName, "-v", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed && (process_output.Contains("nginx version: ") || process_error.Contains("nginx version: ")))
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Substring("nginx version: ".Length);
                this.VersionInitialised = true;
                return this.Version;
            }
            else if (process_output.Contains("nginx version: ") || process_error.Contains("nginx version: "))
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Substring("nginx version: ".Length);
                this.VersionInitialised = true;
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully or could not parse output. Process output: {1}.\nProcess error: {2}.", ApplicationBinary.Name, process_output, process_error));
            }
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"nginx", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "nginx", this.Version) }}
            };
            this.Modules = m;
            this.PackageSourceInitialized = this.ModulesInitialised = true;
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            Nginx nginx = new Nginx(this.ConfigurationFile);
            if (nginx.ParseSucceded)
            {
                this.Configuration = nginx;
                this.ConfigurationInitialised = true;
            }
            return this.Configuration;
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            return this.GetModules()["nginx"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
    }
}
