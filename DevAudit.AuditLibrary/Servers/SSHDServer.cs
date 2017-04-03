using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Alpheus;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class SSHDServer : ApplicationServer
    {
        #region Constructors
        public SSHDServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options,
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "find", "@", "*bin", "sshd" } },
                { PlatformID.Win32NT, new string[] { "@", "usr", "sbin", "sshd.exe" } }
            },
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "find", "@", "etc", "*", "sshd_config" } },
                { PlatformID.Win32NT, new string[] { "@", "etc", "ssh", "sshd_config" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["sshd"] = this.ApplicationBinary;
            }

        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "sshd"; } }

        public override string ServerLabel { get { return "OpenSSH sshd"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.AuditEnvironment.Status("Scanning {0} version.", this.ApplicationLabel);
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(this.ApplicationBinary.FullName, "-?", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                sw.Stop();
                this.AuditEnvironment.Success("Got {0} version {1} in {2} ms.", this.ApplicationLabel, this.Version, sw.ElapsedMilliseconds);
                this.VersionInitialised = true;
                return this.Version;
            }
            else if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output) && process_error.Contains("unknown option"))
            {
                this.Version = process_error.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                sw.Stop();
                this.AuditEnvironment.Success("Got {0} version {1} in {2} ms.", this.ApplicationLabel, this.Version, sw.ElapsedMilliseconds);
                this.VersionInitialised = true;
                return this.Version;
            }
            else
            {
                sw.Stop();
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"sshd", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "sshd", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized = this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override IConfiguration GetConfiguration()
        {
            this.AuditEnvironment.Status("Scanning {0} configuration.", this.ApplicationLabel);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SSHD sshd = new SSHD(this.ConfigurationFile);
            sw.Stop();
            if (sshd.ParseSucceded)
            {
                this.Configuration = sshd;
                this.ConfigurationInitialised = true;
                this.AuditEnvironment.Success("Read configuration from {0} in {1} ms.", this.Configuration.File.Name, sw.ElapsedMilliseconds);
            }
            else
            {
                this.AuditEnvironment.Error("Could not parse configuration from {0}.", sshd.FullFilePath);
                if (sshd.LastParseException != null) this.AuditEnvironment.Error(sshd.LastParseException);
                if (sshd.LastIOException != null) this.AuditEnvironment.Error(sshd.LastIOException);
                this.Configuration = null;
                this.ConfigurationInitialised = false;
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
            return this.ModulePackages["sshd"];
        }
        
        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
    }
}
