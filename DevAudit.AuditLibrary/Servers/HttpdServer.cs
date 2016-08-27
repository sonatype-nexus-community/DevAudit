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
        #region Overriden properties
        public override string ServerId { get { return "httpd"; } }

        public override string ServerLabel { get { return "Apache Httpd Server"; } }

        public override string ApplicationId { get { return "httpd"; } }

        public override string ApplicationLabel { get { return "Apache Httpd server"; } }

        public override  Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "bin", "@bin" }
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "httpd",  Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX
                ? CombinePathsUnderRoot("bin", "httpd") :  CombinePathsUnderRoot("bin", "httpd.exe") },
        };

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel { get { return "Apache Httpd"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string DefaultConfigurationFile { get; } = CombinePathsUnderRoot("conf", "httpd.conf");
        #endregion

        #region Public properties
        public FileInfo HttpdExe
        {
            get
            {
                return (FileInfo)this.ServerFileSystemMap["httpd"];
            }
        }
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
            Httpd httpd = new Httpd(this.ConfigurationFile.FullName);
            if (httpd.ParseSucceded)
            {
                this.Configuration = httpd;
            }
            return this.Configuration;
        }

        public override string GetVersion()
        {
            HostEnvironment.ProcessStatus process_status;
            string process_output;
            string process_error;
            HostEnvironment.Execute(HttpdExe.FullName, "-v", out process_status, out process_output, out process_error);
            if (process_status == HostEnvironment.ProcessStatus.Success)
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
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", HttpdExe.Name, process_error));
            }
        }


        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.Modules["httpd"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constructors
        public HttpdServer(Dictionary<string, object> server_options) : base(server_options) {}
        #endregion
    }
}
