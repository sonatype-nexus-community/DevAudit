using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class MySQLServer : ApplicationServer
    {
        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "bin", "bin" }
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "mysql", Path.Combine("bin", "mysql.exe") },
            
        };

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "mysql"; } }

        public override string PackageManagerLabel { get { return "MySQL"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");
        #endregion

        #region Overriden methods
        public override string GetVersion()
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            throw new NotImplementedException();
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        public MySQLServer(Dictionary<string, object> application_options) : base(application_options) {}
    }
}
