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
        {}
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
                        break;
                    }
                }
            }
            this.Version = core_version;
            sw.Stop();
            this.VersionInitialised = true;
            this.AuditEnvironment.Success("Got Drupal 8 version {0} in {1} ms.", this.Version, sw.ElapsedMilliseconds);
            Package core = this.ModulePackages["core"].Where(p => p.Name == "drupal_core").First();
            core.Version = this.Version;
            return this.Version;
        }

        protected override Dictionary<string, IEnumerable<Package>> GetModules()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var M = this.ModulePackages = new Dictionary<string, IEnumerable<Package>>();
            object modules_lock = new object();
            string core_version = this.Version;
            List<AuditFileInfo> core_module_files = this.CoreModulesDirectory.GetFiles("*.info.yml")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<AuditFileInfo> contrib_module_files = this.ContribModulesDirectory.GetFiles("*.info.yml")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<Package> all_modules = new List<Package>(100);
            if (core_module_files != null && core_module_files.Count > 0)
            {
                List<Package> core_modules = new List<Package>(core_module_files.Count + 1);
                this.AuditEnvironment.Status("Reading Drupal 8 core module files from environment...", core_module_files.Count);
                Dictionary<AuditFileInfo, string> core_modules_files_text = this.CoreModulesDirectory.ReadFilesAsText(core_module_files);
                Parallel.ForEach(core_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        IDeserializer yaml_deserializer = new DeserializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .IgnoreUnmatchedProperties()
                        .Build();
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                        m.ShortName = kv.Key.Name.Split('.')[0];
                        lock (modules_lock)
                        {
                            core_modules.Add(new Package("drupal", m.ShortName, m.Version == "VERSION" ? core_version : m.Version, "", string.Empty));
                        }
                        this.AuditEnvironment.Debug("Added Drupal 8 core module {0}: {1}.", m.ShortName, m.Version);
                    }
                });
                M.Add("core", core_modules);
                core_modules.Add(new Package("drupal", "drupal_core", string.Empty));
                all_modules.AddRange(core_modules);
            }
            if (contrib_module_files != null && contrib_module_files.Count > 0)
            {
                List<Package> contrib_modules = new List<Package>(contrib_module_files.Count);
                this.AuditEnvironment.Info("Reading Drupal 8 contrib module files from environment...", core_module_files.Count);
                Dictionary<AuditFileInfo, string> contrib_modules_files_text = this.ContribModulesDirectory.ReadFilesAsText(contrib_module_files);
                Parallel.ForEach(contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                    {
                        IDeserializer yaml_deserializer = new DeserializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .IgnoreUnmatchedProperties()
                        .Build();
                        DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                        m.ShortName = kv.Key.Name.Split('.')[0];
                        lock (modules_lock)
                        {
                            contrib_modules.Add(new Package(this.PackageManagerId, m.ShortName, m.Version));
                        }
                        this.AuditEnvironment.Debug("Added Drupal 8 contrib module {0}: {1}.", m.ShortName, m.Version);
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
                    List<Package> sites_all_contrib_modules = new List<Package>(sites_all_contrib_modules_files.Count + 1);
                    Parallel.ForEach(sites_all_contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, kv =>
                    {
                        if (!string.IsNullOrEmpty(kv.Value))
                        {
                            IDeserializer yaml_deserializer = new DeserializerBuilder()
                            .WithNamingConvention(new CamelCaseNamingConvention())
                            .IgnoreUnmatchedProperties()
                            .Build();
                            DrupalModuleInfo m = yaml_deserializer.Deserialize<DrupalModuleInfo>(new System.IO.StringReader(kv.Value));
                            m.ShortName = kv.Key.Name.Split('.')[0];
                            lock (modules_lock)
                            {
                                sites_all_contrib_modules.Add(new Package("drupal", m.ShortName, m.Version, "", string.Empty));
                            }
                            this.AuditEnvironment.Debug("Added Drupal 8 contrib module {0}: {1}.", m.ShortName, m.Version);
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
            this.ModulePackages = M;
            this.ModulesInitialised = true;
            this.PackageSourceInitialized = true; //Packages are read from modules
            sw.Stop();
            this.AuditEnvironment.Success("Got {0} total {1} modules in {2} ms.", ModulePackages["all"].Count(), this.ApplicationLabel, sw.ElapsedMilliseconds);
            return this.ModulePackages;
        }

        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            if(!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialized before GetVersion is called.");
            return this.ModulePackages["all"];
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
