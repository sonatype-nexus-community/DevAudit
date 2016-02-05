using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinAudit.AuditLibrary
{
    public class DrupalApplication : Application
    {
        #region Overriden properties
        public override string ApplicationId { get { return "drupal8"; } }

        public override string ApplicationLabel { get { return "Drupal 8"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override Dictionary<string, FileSystemInfo> ApplicationFileSystemMap { get; } = 
            new Dictionary<string, FileSystemInfo>()
            { 
                {"CorePackagesFile", new FileInfo("core" + Path.DirectorySeparatorChar + "composer.json") }
            };

        public override List<string> RequiredDirectoryLocations { get; } = new List<string>()
        {
            "RootDirectory",
        };

        public override List<string> RequiredFileLocations { get; } = new List<string>()
        {
            "CorePackagesFile"
        };

        #endregion

        #region Overriden methods

        public override Dictionary<string, PackageSource> GetModules()
        {
            throw new NotImplementedException();
        }



        #endregion

        #region Constructors
        public DrupalApplication(Dictionary<string, object> application_options) : base(application_options)
        {

        }
        #endregion

    }
}
