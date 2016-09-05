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
    public class SSHDServer : ApplicationServer
    {
        #region Overriden properties
        public override string ServerId { get { return "sshd"; } }

        public override string ServerLabel { get { return "SSHD Server"; } }

        public override string ApplicationId { get { return "sshd"; } }

        public override string ApplicationLabel { get { return "SSHD server"; } }

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>() {};

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            //{ "sshd",  Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX 
            //    ? "@sshd" :  "@sshd.exe"},
        };

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel { get { return "SSHD"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string DefaultConfigurationFile { get; } = Path.Combine(DIR, "etc", "sshd", "sshd_config");
        #endregion

        #region Public properties
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"sshd", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "sshd", this.Version) }}
            };
            this.Modules = m;
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            SSHD sshd = new SSHD(this.ConfigurationFile.FullName);
            if (sshd.ParseSucceded)
            {
                this.Configuration = sshd;
            }
            return this.Configuration;
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }

        public override string GetVersion()
        {
            HostEnvironment.ProcessStatus process_status;
            string process_output;
            string process_error;
            HostEnvironment.Execute(this.ApplicationBinary.FullName, "-?", out process_status, out process_output, out process_error);
            if (process_status == HostEnvironment.ProcessStatus.Success)
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Split(Environment.NewLine.ToCharArray())[1];
                return this.Version;
            }
            else if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output) && process_error.StartsWith("sshd: unknown option"))
            {
                this.Version = process_error.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.Modules["sshd"];
        }
        
        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constructors
        public SSHDServer(Dictionary<string, object> server_options) : base(server_options)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["sshd"] = this.ApplicationBinary;
            }
            else
            {
                string fn = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePathsUnderRoot("sshd") : CombinePathsUnderRoot("sshd.exe");
                if (!File.Exists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for SSHD was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = new FileInfo(fn);
                    this.ApplicationFileSystemMap["sshd"] = this.ApplicationBinary;
                }
            }
        }
        #endregion
    }
}
