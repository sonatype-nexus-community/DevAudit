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

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "CorePackagesFile", Path.Combine("core", "composer.json") }
        };

        #endregion

        #region Public properties

        

        public FileInfo CorePackagesFile
        {
            get
            {
                return (FileInfo) this.ApplicationFileSystemMap["CorePackagesFile"];
            }
        }


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

        #region Private fields
        
        #endregion

    }
}
