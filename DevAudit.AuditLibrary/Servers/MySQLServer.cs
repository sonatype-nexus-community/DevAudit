using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class MySQLServer : ApplicationServer
    {
        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override string ApplicationId { get { return "mysql"; } }

        public override string ApplicationLabel { get { return "MySQL"; } }

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>() {};

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>() {};

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel { get { return "MySQL"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"mysqld", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "mysqld", this.Version) }}
            };
            this.Modules = m;
            return this.Modules;
        }

        protected override IConfiguration GetConfiguration()
        {
            MySQL mysql = new MySQL(this.ConfigurationFile);
            ;
            if (mysql.ParseSucceded)
            {
                this.Configuration = mysql;
            }
            return this.Configuration;
        }

        public override string GetVersion()
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(this.ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                this.Version = process_output.Substring(process_output.IndexOf("Ver"));
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.Modules["mysqld"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constructors
        public MySQLServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "@", "etc", "mysql", "my.conf" } },
                { PlatformID.MacOSX, new string[] { "@", "etc", "mysql", "my.conf" } },
                { PlatformID.Win32NT, new string[] { "@", "my.ini" } }
            }, message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["mysql"] = this.ApplicationBinary;
            }
            else
            {
                string fn = this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX
                ? CombinePath("@", "usr", "bin", "mysql") : CombinePath("@", "bin", "mysql.exe");
                if (!this.AuditEnvironment.FileExists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for SSHD was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = this.AuditEnvironment.ConstructFile(fn);
                    this.ApplicationFileSystemMap["mysql"] = this.ApplicationBinary;
                }
            }

        }
        #endregion
    }
}
