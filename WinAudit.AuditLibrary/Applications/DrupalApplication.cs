using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinAudit.AuditLibrary
{
    public class DrupalApplication : Application
    {
        #region Overriden properties

        public override string ApplicationId { get { return "drupal8"; } }

        public override string ApplicationLabel { get { return "Drupal 8"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "CoreModulesDirectory", Path.Combine("core", "modules") }
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "CorePackagesFile", Path.Combine("core", "composer.json") }
        };

        #endregion

        #region Public properties

        public DirectoryInfo CoreModulesDirectory
        {
            get
            {
                return (DirectoryInfo) this.ApplicationFileSystemMap["CoreModulesDirectory"];
            }
        }

        public FileInfo CorePackagesFile
        {
            get
            {
                return (FileInfo) this.ApplicationFileSystemMap["CorePackagesFile"];
            }
        }

        #endregion

        #region Overriden methods

        public override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> modules = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>();            
            List<FileSystemInfo> core_module_files = this.CoreModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
                .Where(f => !f.Name.Contains("_test")).ToList();
            List<OSSIndexQueryObject> core_modules = new List<OSSIndexQueryObject>(core_module_files.Count);
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            foreach (FileInfo f in core_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                        m.ShortName = f.Name.Split('.')[0];
                        core_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", m.Package));
                    }
                }                               
            }
            modules.Add("core", core_modules);

            return modules;
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
