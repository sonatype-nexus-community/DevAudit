using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Versatile;
using Alpheus;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class Drupal8Application : Application
    {
        #region Constructors
        public Drupal8Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, new Dictionary<string, string[]>()
            {
                { "ChangeLog", new string[] { "@", "core", "CHANGELOG.txt" } },
                { "CorePackagesFile", new string[] { "@", "core", "composer.json" } }
            }, new Dictionary<string, string[]>()
            {
                { "CoreModulesDirectory", new string[] { "@", "core", "modules" } },
                { "ContribModulesDirectory", new string[] { "@", "modules" } },
                { "DefaultSiteDirectory", new string[] { "@", "sites", "default" } }
            }, message_handler)
        {
            this.PackageSourceInitialized = true; //Packages are read from the modules directories.
        }
        #endregion

        #region Overriden properties
        public override string ApplicationId { get { return "drupal8"; } }

        public override string ApplicationLabel { get { return "Drupal 8"; } }

        public override string PackageManagerId { get { return "drupal"; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            string core_version = "8.x";
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AuditFileInfo changelog = this.ApplicationFileSystemMap["ChangeLog"] as AuditFileInfo;
            string[] c = changelog.ReadAsText()?.Split(this.AuditEnvironment.LineTerminator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (c != null && c.Count() > 0)
            {
                foreach (string l in c)
                {
                    if (l.StartsWith("Drupal "))
                    {
                        core_version = l.Split(',')[0].Substring(7);
                    }
                }
            }
            this.Version = core_version;
            sw.Stop();
            return this.Version;
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            if (this.Modules != null) return this.Modules;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var M = this.Modules = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>();
            object modules_lock = new object();
            string core_version = this.Version;
            List<AuditFileInfo> core_module_files = this.CoreModulesDirectory.GetFiles("*.info.yml")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<AuditFileInfo> contrib_module_files = this.ContribModulesDirectory.GetFiles("*.info.yml")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<OSSIndexQueryObject> all_modules = new List<OSSIndexQueryObject>(100);
            if (core_module_files != null && core_module_files.Count > 0)
            {
                List<OSSIndexQueryObject> core_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
                this.AuditEnvironment.Info("Reading Drupal 8 core module files from environment...", core_module_files.Count);
                Dictionary<AuditFileInfo, string> core_modules_files_text = this.CoreModulesDirectory.ReadFilesAsText(core_module_files);
                Parallel.ForEach(core_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                        m.ShortName = kv.Key.Name.Split('.')[0];
                        lock (modules_lock)
                        {
                            core_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version == "VERSION" ? core_version : m.Version, "", string.Empty));
                        }
                    }
                });
                M.Add("core", core_modules);
                core_modules.Add(new OSSIndexQueryObject("drupal", "drupal_core", core_version));
                all_modules.AddRange(core_modules);
            }
            if (contrib_module_files != null && contrib_module_files.Count > 0)
            {
                List<OSSIndexQueryObject> contrib_modules = new List<OSSIndexQueryObject>(contrib_module_files.Count);
                this.AuditEnvironment.Info("Reading Drupal 8 contrib module files from environment...", core_module_files.Count);
                Dictionary<AuditFileInfo, string> contrib_modules_files_text = this.ContribModulesDirectory.ReadFilesAsText(contrib_module_files);
                Parallel.ForEach(contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                        m.ShortName = kv.Key.Name.Split('.')[0];
                        lock (modules_lock)
                        {
                            contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", string.Empty));
                        }
                    }
                });
                if (contrib_modules.Count > 0)
                {
                    M.Add("contrib", contrib_modules);
                    all_modules.AddRange(contrib_modules);
                }
            }
            if (this.SitesAllModulesDirectory != null)
            {
                List<AuditFileInfo> sites_all_contrib_modules_files = this.SitesAllModulesDirectory.GetFiles("*.info.yml")?
                    .Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
                if (sites_all_contrib_modules_files != null && sites_all_contrib_modules_files.Count > 0)
                {
                    Dictionary<AuditFileInfo, string> sites_all_contrib_modules_files_text = this.SitesAllModulesDirectory.ReadFilesAsText(sites_all_contrib_modules_files);
                    List<OSSIndexQueryObject> sites_all_contrib_modules = new List<OSSIndexQueryObject>(sites_all_contrib_modules_files.Count + 1);
                    Parallel.ForEach(sites_all_contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                    {
                        if (!string.IsNullOrEmpty(kv.Value))
                        {
                            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
                            DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                            m.ShortName = kv.Key.Name.Split('.')[0];
                            lock (modules_lock)
                            {
                                sites_all_contrib_modules.Add(new OSSIndexQueryObject("drupal", m.ShortName, m.Version, "", string.Empty));
                            }
                        }
                    });
                    if (sites_all_contrib_modules.Count > 0)
                    {
                        M.Add("sites_all_contrib", sites_all_contrib_modules);
                        all_modules.AddRange(sites_all_contrib_modules);
                    }
                }
            }
            M.Add("all", all_modules);
            this.Modules = M;
            sw.Stop();
            this.AuditEnvironment.Success("Got {0} total {1} modules in {2} ms.", Modules["all"].Count(), this.ApplicationLabel, sw.ElapsedMilliseconds);
            return Modules;
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            this.GetModules();
            return this.Modules["all"];
        }

        protected override IConfiguration GetConfiguration()
        {
            throw new NotImplementedException();
        }

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

        #region Properties
        public AuditDirectoryInfo CoreModulesDirectory
        {
            get
            {
                return (AuditDirectoryInfo) this.ApplicationFileSystemMap["CoreModulesDirectory"];
            }
        }

        public AuditDirectoryInfo ContribModulesDirectory
        {
            get
            {
                return (AuditDirectoryInfo)this.ApplicationFileSystemMap["ContribModulesDirectory"];
            }
        }

        public AuditDirectoryInfo SitesAllModulesDirectory
        {
            get
            {
                IDirectoryInfo sites_all = this.RootDirectory.GetDirectories(CombinePath("sites", "all", "modules"))?.FirstOrDefault();
                {
                    return sites_all == null ? (AuditDirectoryInfo) sites_all : null;
                }
                
            }
        }

        public AuditFileInfo CorePackagesFile
        {
            get
            {
                return (AuditFileInfo) this.ApplicationFileSystemMap["CorePackagesFile"];
            }
        }
        #endregion
    }
}
