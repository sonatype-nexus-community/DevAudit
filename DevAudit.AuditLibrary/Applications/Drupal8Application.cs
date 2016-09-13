using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Versatile;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class Drupal8Application : Application
    {
        #region Overriden properties

        public override string ApplicationId { get { return "drupal8"; } }

        public override string ApplicationLabel { get { return "Drupal 8"; } }

        public override string PackageManagerId { get { return "drupal"; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");
       
        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "CoreModulesDirectory", LocateUnderRoot("core", "modules") },
            { "ContribModulesDirectory", LocateUnderRoot("modules") },
            { "DefaultSiteDirectory", LocateUnderRoot("sites", "default") },
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "ChangeLog", LocateUnderRoot("core", "CHANGELOG.TXT") },
            { "CorePackagesFile", LocateUnderRoot("core", "composer.json") }
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
                DirectoryInfo sites_all;
                if ((sites_all = this.RootDirectory.GetDirectories(Path.Combine("sites", "all")).FirstOrDefault()) != null)
                {
                    return sites_all.GetDirectories(Path.Combine("modules")).FirstOrDefault();
                }
                else return null;
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
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            FileInfo changelog = this.ApplicationFileSystemMap["ChangeLog"] as FileInfo;
            string l = string.Empty;
            string core_version = "8.x";
            using (StreamReader r = new StreamReader(changelog.OpenRead()))
            {
                while (!r.EndOfStream && !l.StartsWith("Drupal"))
                {
                    l = r.ReadLine();
                }
            }
            if (l.StartsWith("Drupal "))
            {
                core_version = l.Split(',')[0].Substring(7);
            }
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> modules = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>();            
            List<FileInfo> core_module_files = RecursiveFolderScan(this.CoreModulesDirectory, "*.info.yml").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<FileInfo> contrib_module_files = RecursiveFolderScan(this.ContribModulesDirectory, "*.info.yml").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            //this.CoreModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
            //.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            //List<FileSystemInfo> contrib_module_files = this.ContribModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
            //    .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<OSSIndexQueryObject> core_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            List<OSSIndexQueryObject> contrib_modules = new List<OSSIndexQueryObject>(contrib_module_files.Count);
            List<OSSIndexQueryObject> all_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            foreach (FileInfo f in core_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                        m.ShortName = f.Name.Split('.')[0];
                        core_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version  == "VERSION" ? core_version : m.Version, "", string.Empty));
                    }
                }                               
            }
            modules.Add("core", core_modules);
            core_modules.Add(new OSSIndexQueryObject("drupal", "drupal_core", core_version));
            all_modules.AddRange(core_modules);
            foreach (FileInfo f in contrib_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(r);
                        m.ShortName = f.Name.Split('.')[0];
                        contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", string.Empty));
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
                //                List<FileSystemInfo> sites_all_contrib_modules_files = this.SitesAllModulesDirectory.GetFileSystemInfos("*.info.yml", SearchOption.AllDirectories)
                //                    .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
                List<FileInfo> sites_all_contrib_modules_files = RecursiveFolderScan(this.SitesAllModulesDirectory, "*.info.yml").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
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
                                sites_all_contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", string.Empty));
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

        protected override IConfiguration GetConfiguration()
        {
            throw new NotImplementedException();
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

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = Drupal.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }

        #endregion

        #region Constructors
        public Drupal8Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, message_handler) {}
        #endregion

        #region Private fields

        #endregion

        #region Static methods
       
        #endregion

    }
}
