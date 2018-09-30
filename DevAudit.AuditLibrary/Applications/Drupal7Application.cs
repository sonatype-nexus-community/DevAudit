using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using IniParser;
using IniParser.Parser;
using IniParser.Model;
using IniParser.Model.Configuration;

using Versatile;
using Alpheus;
using Alpheus.IO;
using System.Xml;

namespace DevAudit.AuditLibrary
{
    public class Drupal7Application : Application
    {
        #region Constructors
        public Drupal7Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, new Dictionary<string, string[]>(), new Dictionary<string, string[]>()
            {
                { "CoreModulesDirectory", new string[] { "@", "modules" } },
                { "ContribModulesDirectory", new string[] { "@", "sites", "all", "modules" } },
                { "DefaultSiteDirectory", new string[] { "@", "sites", "default" } },
            }, message_handler)
        {}
        #endregion

        #region Overriden properties
        public override string ApplicationId { get { return "drupal7"; } }

        public override string ApplicationLabel { get { return "Drupal 7"; } }

        public override string PackageManagerId { get { return "drupal"; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialized before GetVersion is called.");
            Package core_module = this.ModulePackages["core"].Where(m => m.Name == "drupal_core").First();
            if (!string.IsNullOrEmpty(core_module.Version))
            {
                this.AuditEnvironment.Success("Got Drupal 7 version {0}.", core_module.Version);
                this.VersionInitialised = true;
                return this.Version;
            }
            else
            {
                return string.Empty;
            }
            
        }

        protected override Dictionary<string, IEnumerable<Package>> GetModules()
        {
            object modules_lock = new object();
            Dictionary<string, IEnumerable<Package>> modules = new Dictionary<string, IEnumerable<Package>>();
            List<AuditFileInfo> core_module_files = this.CoreModulesDirectory.GetFiles("*.info").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<AuditFileInfo> contrib_module_files = this.ContribModulesDirectory.GetFiles("*.info")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
            List<Package> core_modules = new List<Package>(core_module_files.Count + 1);
            List<Package> contrib_modules = contrib_module_files != null ? new List<Package>(contrib_module_files.Count) : new List<Package>(0);
            List<Package> all_modules = new List<Package>(core_module_files.Count + 1);
            IniParserConfiguration ini_parser_cfg = new IniParserConfiguration();
            ini_parser_cfg.CommentString = ";";
            ini_parser_cfg.AllowDuplicateKeys = true;
            ini_parser_cfg.OverrideDuplicateKeys = true;
            this.AuditEnvironment.Status("Reading Drupal 7 core module files from environment...", core_module_files.Count);
            Dictionary<AuditFileInfo, string> core_modules_files_text = this.CoreModulesDirectory.ReadFilesAsText(core_module_files);
            Parallel.ForEach(core_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, _kv =>
            { 
                IniDataParser ini_parser = new IniDataParser(ini_parser_cfg);
                IniData data = ini_parser.Parse(_kv.Value);
                foreach (KeyData d in data.Global)
                {
                    if (d.Value.First() == '"') d.Value = d.Value.Remove(0, 1);
                    if (d.Value.Last() == '"') d.Value = d.Value.Remove(d.Value.Length - 1, 1);
                }
                DrupalModuleInfo m = new DrupalModuleInfo
                {
                    Core = data.Global["core"],
                    Name = data.Global["name"],
                    Description = data.Global["description"],
                    Package = data.Global["package"],
                    Version = data.Global["version"],
                    Project = data.Global["project"]
                };
                lock (modules_lock)
                {
                    core_modules.Add(new Package("drupal", m.Name, m.Version == "VERSION" ? m.Core : m.Version, "", string.Empty));
                }
            });
            core_modules.Add(new Package("drupal", "drupal_core", core_modules.First().Version));
            modules.Add("core", core_modules);
            all_modules.AddRange(core_modules);
            if (contrib_module_files != null)
            {
                this.AuditEnvironment.Status("Reading contrib module files from environment...", core_module_files.Count);
                Dictionary<AuditFileInfo, string> contrib_modules_files_text = this.ContribModulesDirectory.ReadFilesAsText(contrib_module_files);
                Parallel.ForEach(contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, _kv =>
                {
                    IniDataParser ini_parser = new IniDataParser(ini_parser_cfg);
                    IniData data = ini_parser.Parse(_kv.Value);
                    foreach (KeyData d in data.Global)
                    {
                        if (d.Value.First() == '"') d.Value = d.Value.Remove(0, 1);
                        if (d.Value.Last() == '"') d.Value = d.Value.Remove(d.Value.Length - 1, 1);
                    }
                    DrupalModuleInfo m = new DrupalModuleInfo
                    {
                        Core = data.Global["core"],
                        Name = data.Global["name"],
                        Description = data.Global["description"],
                        Package = data.Global["package"],
                        Version = data.Global["version"],
                        Project = data.Global["project"]
                    };
                    lock(modules_lock)
                    { 
                        contrib_modules.Add(new Package("drupal", m.Name, m.Version == "VERSION" ? m.Core : m.Version, "", string.Empty));
                    }
                });
                
            }
            if (contrib_modules.Count > 0)
            {
                modules.Add("contrib", contrib_modules);
                all_modules.AddRange(contrib_modules);
            }
            List<Package> sites_all_contrib_modules = null;
            if (this.SitesAllModulesDirectory != null)
            {
                List<AuditFileInfo> sites_all_contrib_modules_files = this.SitesAllModulesDirectory.GetFiles("*.info")?.Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).Select(f => f as AuditFileInfo).ToList();
                if (sites_all_contrib_modules_files != null && sites_all_contrib_modules_files.Count > 0)
                {
                    Dictionary<AuditFileInfo, string> sites_all_contrib_modules_files_text = this.SitesAllModulesDirectory.ReadFilesAsText(sites_all_contrib_modules_files);
                    sites_all_contrib_modules = new List<Package>(sites_all_contrib_modules_files.Count);
                    Parallel.ForEach(sites_all_contrib_modules_files_text, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, _kv =>
                    {
                        IniDataParser ini_parser = new IniDataParser(ini_parser_cfg);
                        IniData data = ini_parser.Parse(_kv.Value);
                        foreach (KeyData d in data.Global)
                        {
                            if (d.Value.First() == '"') d.Value = d.Value.Remove(0, 1);
                            if (d.Value.Last() == '"') d.Value = d.Value.Remove(d.Value.Length - 1, 1);
                        }
                        DrupalModuleInfo m = new DrupalModuleInfo
                        {
                            Core = data.Global["core"],
                            Name = data.Global["name"],
                            Description = data.Global["description"],
                            Package = data.Global["package"],
                            Version = data.Global["version"],
                        };
                        lock (modules_lock)
                        {
                            sites_all_contrib_modules.Add(new Package("drupal", m.Name, m.Version == "VERSION" ? m.Core : m.Version, "", string.Empty));
                        }
                    });
                }
            }
            if (sites_all_contrib_modules != null && sites_all_contrib_modules.Count > 0)
            {
                modules.Add("sites_all_contrib", sites_all_contrib_modules);
                all_modules.AddRange(sites_all_contrib_modules);
            }
            modules.Add("all", all_modules);
            this.ModulePackages = modules;
            this.ModulesInitialised = true;
            this.PackageSourceInitialized = true; //Packages are read from modules.
            return this.ModulePackages;
        }

        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            try
            {
                AuditFileInfo config_file = this.AuditEnvironment.ConstructFile(this.PackageManagerConfigurationFile);
                string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                string xml = config_file.ReadAsText();
                if (xml.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
                {
                    var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
                    xml = xml.Remove(0, lastIndexOfUtf8);
                }
                XElement root = XElement.Parse(xml);
                IEnumerable<Package> packages;

                if (root.Name == "Project")
                {
                    // dotnet core csproj file
                    packages = root.Descendants().Where(x => x.Name == "PackageReference").Select(r =>
                        new Package("nuget", r.Attribute("Include").Value, r.Attribute("Version").Value)).ToList();
                }
                else
                {
                    packages =
                        from el in root.Elements("package")
                        select new Package("nuget", el.Attribute("id").Value, el.Attribute("version").Value, "");
                }



                return packages;
            }
            catch (XmlException e)
            {
                throw new Exception("XML exception thrown parsing file: " + this.PackageManagerConfigurationFile, e);
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception thrown attempting to get packages from file: "
                    + this.PackageManagerConfigurationFile, e);
            }
        }

        protected override IConfiguration GetConfiguration()
        {
            throw new NotImplementedException();
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            vulnerability_version = vulnerability_version.TrimEnd(".".ToCharArray());
            package_version = package_version.TrimEnd(".".ToCharArray());       
            bool r = Drupal.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        #endregion

        #region Properties
        public AuditDirectoryInfo CoreModulesDirectory
        {
            get
            {
                return (AuditDirectoryInfo)this.ApplicationFileSystemMap["CoreModulesDirectory"];
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
                IDirectoryInfo sites_all;
                if ((sites_all = this.RootDirectory.GetDirectories(CombinePath("sites", "all")).FirstOrDefault()) != null)
                {
                    return sites_all.GetDirectories(CombinePath("modules")).FirstOrDefault() as AuditDirectoryInfo;
                }
                else return null;
            }
        }
        #endregion
    }
}
