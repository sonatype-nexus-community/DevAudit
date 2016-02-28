using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevAudit.AuditLibrary
{
    public class DrupalApplication : Application
    {
        #region Overriden properties

        public override string ApplicationId { get { return "drupal8"; } }

        public override string ApplicationLabel { get { return "Drupal 8"; } }

        public override string PackageManagerId { get { return "drupal"; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");
       
        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "CoreModulesDirectory", Path.Combine("core", "modules") },
            { "ContribModulesDirectory", "modules" },
            { "DefaultSiteDirectory", Path.Combine("sites", "default") },
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

        public DirectoryInfo ContribModulesDirectory
        {
            get
            {
                return (DirectoryInfo)this.ApplicationFileSystemMap["ContribModulesDirectory"];
            }
        }

        public DirectoryInfo SitesAllModulesDirectory
        {
            get
            {
                return this.RootDirectory.GetDirectories(Path.Combine("sites", "all", "modules")).FirstOrDefault();
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
                .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<FileSystemInfo> contrib_module_files = this.ContribModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
                .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<OSSIndexQueryObject> core_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            List<OSSIndexQueryObject> contrib_modules = new List<OSSIndexQueryObject>(contrib_module_files.Count);
            List<OSSIndexQueryObject> all_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            core_modules.Add(new OSSIndexQueryObject("drupal", "drupal_core", "8.x"));
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            foreach (FileInfo f in core_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                        m.ShortName = f.Name.Split('.')[0];
                        core_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", m.Project));
                    }
                }                               
            }
            modules.Add("core", core_modules);
            all_modules.AddRange(core_modules);
            foreach (FileInfo f in contrib_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                        m.ShortName = f.Name.Split('.')[0];
                        contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", m.Project));
                    }
                }
            }
            if (contrib_modules.Count > 0)
            {
                modules.Add("contrib", contrib_modules);
                all_modules.AddRange(contrib_modules);
            }
            if (this.SitesAllModulesDirectory != null)
            {
                List<FileSystemInfo> sites_all_contrib_modules_files = this.SitesAllModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
                    .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
                if (sites_all_contrib_modules_files.Count > 0)
                {
                    List<OSSIndexQueryObject> sites_all_contrib_modules = new List<OSSIndexQueryObject>(sites_all_contrib_modules_files.Count + 1);
                    foreach (FileInfo f in sites_all_contrib_modules_files)
                    {
                        using (FileStream fs = f.OpenRead())
                        {
                            using (StreamReader r = new StreamReader(f.OpenRead()))
                            {
                                DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                                m.ShortName = f.Name.Split('.')[0];
                                sites_all_contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", m.Project));
                            }
                        }
                    }
                    if (sites_all_contrib_modules.Count > 0)
                    {
                        modules.Add("sites_all_contrib", sites_all_contrib_modules);
                        all_modules.AddRange(sites_all_contrib_modules);
                    }
                }
            }
            modules.Add("all", all_modules);
            return modules;
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.GetModules()["all"];
        }

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
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
