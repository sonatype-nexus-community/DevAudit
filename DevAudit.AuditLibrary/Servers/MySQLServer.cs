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

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            //{ "bin", "@bin" }
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            //{ "mysql",  Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX 
            //    ? "@" + Path.Combine("bin", "mysql") :  "@" + Path.Combine("bin", "mysql.exe")},
            
        };

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel { get { return "MySQL"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string DefaultConfigurationFile { get; } = "@my.ini";
        #endregion

        #region Public properties
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
            MySQL mysql = new MySQL(this.ConfigurationFile.FullName);
            if (mysql.ParseSucceded)
            {
                this.Configuration = mysql;
            }
            return this.Configuration;
        }

        public override string GetVersion()
        {
            HostEnvironment.ProcessStatus process_status;
            string process_output;
            string process_error;
            HostEnvironment.Execute(this.ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            if (process_status == HostEnvironment.ProcessStatus.Success)
            {
                this.Version = process_output.Substring(process_output.IndexOf("Ver"));
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
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
        public MySQLServer(Dictionary<string, object> server_options) : base(server_options)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["mysql"] = this.ApplicationBinary;
            }
            else
            {
                string fn = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePathsUnderRoot("bin", "mysql") : CombinePathsUnderRoot("bin", "mysql.exe");
                if (!File.Exists(fn))
                {
                    throw new ArgumentException(string.Format("The server binary for SSHD was not specified and the default file path {0} does not exist.", fn));
                }
                else
                {
                    this.ApplicationBinary = new FileInfo(fn);
                    this.ApplicationFileSystemMap["mysql"] = this.ApplicationBinary;
                }
            }

        }
        #endregion
    }
}
