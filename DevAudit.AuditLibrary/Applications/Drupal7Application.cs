using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IniParser;
using IniParser.Parser;
using IniParser.Model;
using IniParser.Model.Configuration;

using Versatile;

namespace DevAudit.AuditLibrary
{
    public class Drupal7Application : Application
    {
        #region Overriden properties
        public override string ApplicationId { get { return "drupal7"; } }

        public override string ApplicationLabel { get { return "Drupal 7"; } }

        public override string PackageManagerId { get { return "drupal"; } }

        public override string PackageManagerLabel { get { return "Drupal"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "CoreModulesDirectory", "modules" },
            { "ContribModulesDirectory", Path.Combine("sites", "all", "modules") },
            { "DefaultSiteDirectory", Path.Combine("sites", "default") },
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>();
        #endregion

        #region Public properties
        public DirectoryInfo CoreModulesDirectory
        {
            get
            {
                return (DirectoryInfo)this.ApplicationFileSystemMap["CoreModulesDirectory"];
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
        #endregion

        #region Overriden methods
        public override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> modules = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>();
            List<FileInfo> core_module_files = RecursiveFolderScan(this.CoreModulesDirectory, "*.info").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<FileInfo> contrib_module_files = RecursiveFolderScan(this.ContribModulesDirectory, "*.info").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
            List<OSSIndexQueryObject> core_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            List<OSSIndexQueryObject> contrib_modules = new List<OSSIndexQueryObject>(contrib_module_files.Count);
            List<OSSIndexQueryObject> all_modules = new List<OSSIndexQueryObject>(core_module_files.Count + 1);
            core_modules.Add(new OSSIndexQueryObject("drupal", "drupal_core", "7.x"));
            IniParserConfiguration ini_parser_cfg = new IniParserConfiguration();
            ini_parser_cfg.CommentString = ";";
            ini_parser_cfg.AllowDuplicateKeys = true;
            ini_parser_cfg.OverrideDuplicateKeys = true;
            IniDataParser ini_parser = new IniDataParser(ini_parser_cfg);
            foreach (FileInfo f in core_module_files)
            {
                using (FileStream fs = f.OpenRead())
                {
                    using (StreamReader r = new StreamReader(f.OpenRead()))
                    {
                        IniData data = ini_parser.Parse(r.ReadToEnd());
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
                        core_modules.Add(new OSSIndexQueryObject("drupal", m.Name, m.Version == "VERSION" ? m.Core : m.Version, "", m.Project));
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
                        IniData data = ini_parser.Parse(r.ReadToEnd());
                        foreach(KeyData d in data.Global)
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
                        contrib_modules.Add(new OSSIndexQueryObject("drupal", m.Name, m.Version, "", m.Project));
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
                List<FileInfo> sites_all_contrib_modules_files = RecursiveFolderScan(this.SitesAllModulesDirectory, "*.info").Where(f => !f.Name.Contains("_test") && !f.Name.Contains("test_")).ToList();
                if (sites_all_contrib_modules_files.Count > 0)
                {
                    List<OSSIndexQueryObject> sites_all_contrib_modules = new List<OSSIndexQueryObject>(sites_all_contrib_modules_files.Count + 1);
                    foreach (FileInfo f in sites_all_contrib_modules_files)
                    {
                        using (FileStream fs = f.OpenRead())
                        {
                            using (StreamReader r = new StreamReader(f.OpenRead()))
                            {
                                IniData data = ini_parser.Parse(r.ReadToEnd());
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
        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";        
            if (!package_version.StartsWith("7.x") && package_version.StartsWith("7."))
            {
                package_version = "7.x-" + package_version;
            }
            if (!vulnerability_version.StartsWith("7.x") && vulnerability_version.StartsWith("7."))
            {
                vulnerability_version = "7.x-" + vulnerability_version;
            }
            bool r = Drupal.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }
        #endregion

        #region Constructors
        public Drupal7Application(Dictionary<string, object> application_options) : base(application_options) {}
        #endregion

    }
}
