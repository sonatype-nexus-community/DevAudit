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
        #region Constructors
        public MySQLServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "@", "etc", "mysql", "my.cnf" } },
                { PlatformID.MacOSX, new string[] { "@", "etc", "mysql", "my.cnf" } },
                { PlatformID.Win32NT, new string[] { "@", "my.ini" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
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

        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"mysqld", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "mysqld", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized =  this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override string GetVersion()
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(this.ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                this.Version = process_output.Substring(process_output.IndexOf("Ver"));
                this.VersionInitialised = true;
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        protected override IConfiguration GetConfiguration()
        {
            MySQL mysql = new MySQL(this.ConfigurationFile);
            ;
            if (mysql.ParseSucceded)
            {
                this.Configuration = mysql;
                this.ConfigurationInitialised = true;
            }
            return this.Configuration;
        }


        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            return this.ModulePackages["mysqld"];
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

    }
}
