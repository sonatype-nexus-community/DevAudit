using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Alpheus;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevAudit.AuditLibrary
{
    public abstract class Application : PackageSource
    {
        #region Public abstract properties
        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }
        
        public abstract Dictionary<string, string> RequiredFileLocations { get; }

        public abstract Dictionary<string, string> RequiredDirectoryLocations { get; }
        #endregion

        #region Public abstract methods
        public abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        public abstract IConfiguration GetConfiguration();
        #endregion

        #region Public properties
        public Dictionary<string, FileSystemInfo> ApplicationFileSystemMap { get; } = new Dictionary<string, FileSystemInfo>();

        public DirectoryInfo RootDirectory
        {
            get
            {
                return (DirectoryInfo)this.ApplicationFileSystemMap["RootDirectory"];
            }
        }

        public Dictionary<string, IEnumerable<OSSIndexQueryObject>> Modules { get; set; }

        public IConfiguration Configuration { get; set; } = null;

        public XDocument XmlConfiguration
        {
            get
            {
                if (this.Configuration != null)
                {
                    return this.Configuration.XmlConfiguration;
                }
                else return null;
            }
        }

        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> ProjectConfigurationRules
        {
            get
            {
                return _ConfigurationRulesForProject;
            }
        }

        public Dictionary<string, object> ApplicationOptions { get; set; } = new Dictionary<string, object>();

        public Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> ModulesTask
        {
            get
            {
                if (_ModulesTask == null)
                {
                    _ModulesTask = Task.Run(() => this.GetModules());
                }
                return _ModulesTask;
            }
        }

        public Task ConfigurationTask
        {
            get
            {
                if (_ConfigurationTask == null)
                {
                    _ConfigurationTask = Task.Run(() => this.GetConfiguration());
                }
                return _ConfigurationTask;
            }
        }

        public List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>> ConfigurationRulesTask
        {
            get
            {
                if (_ConfigurationRulesTask == null)
                {
                    this._ConfigurationRulesTask = new List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>>(1);

                }
                return this._ConfigurationRulesTask;
            }
        }
        #endregion

        #region Constructors
        public Application() { }

        public Application(Dictionary<string, object> application_options)
        {
            if (ReferenceEquals(application_options, null)) throw new ArgumentNullException("Application_options");
            this.ApplicationOptions = application_options;

            if (!this.ApplicationOptions.ContainsKey("RootDirectory"))
            {
                throw new ArgumentException(string.Format("The root Application directory was not specified."), "Application_options");
            }
            else if (!Directory.Exists((string)this.ApplicationOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root Application directory {0} was not found.", this.ApplicationOptions["RootDirectory"]), "Application_options");
            }
            else
            {
                this.ApplicationFileSystemMap.Add("RootDirectory", new DirectoryInfo((string)this.ApplicationOptions["RootDirectory"]));
            }

            foreach (KeyValuePair<string, string> f in RequiredFileLocations)
            {
                string fn = f.Value;
                if (f.Value.StartsWith("@"))
                {
                    fn = Path.Combine(this.RootDirectory.FullName, f.Value.Substring(1));
                }
                if (!this.ApplicationOptions.ContainsKey(f.Key))
                {
                    if (string.IsNullOrEmpty(f.Value))
                    {
                        throw new ArgumentException(string.Format("The required application file {0} was not specified and no default path exists.", f), "Application_options");
                    }
                    else
                    {
                        if (!File.Exists(fn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application file {1} does not exist.",
                                fn, f.Key), "RequiredFileLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(f.Key, new FileInfo(fn));
                        }
                    }

                }
                else
                {
                    fn = (string)ApplicationOptions[f.Key];
                    if (fn.StartsWith("@"))
                    {
                        fn = Path.Combine(this.RootDirectory.FullName, fn.Substring(1));
                    }
                    if (!File.Exists(fn))
                    {
                        throw new ArgumentException(string.Format("The required Application file {0} was not found.", f), "Application_options");
                    }
                    else
                    {
                        this.ApplicationFileSystemMap.Add(f.Key, new FileInfo(fn));
                    }
                }
            }

            foreach (KeyValuePair<string, string> d in RequiredDirectoryLocations)
            {
                string dn = d.Value;
                if (dn.StartsWith("@"))
                {
                    dn = Path.Combine(this.RootDirectory.FullName, dn.Substring(1));
                }
                if (!this.ApplicationOptions.ContainsKey(d.Key))
                {
                    if (string.IsNullOrEmpty(d.Value))
                    {
                        throw new ArgumentException(string.Format("The required Application directory {0} was not specified and no default path exists.", d.Key), "application_options");
                    }
                    else
                    {
                        if (!Directory.Exists(dn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required Application directory {1} does not exist.",
                                d.Key, dn), "RequiredDirectoryLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(d.Key, new DirectoryInfo(dn));
                        }
                    }

                }
                else
                {
                    dn = (string)ApplicationOptions[d.Key];
                    if (dn.StartsWith("@"))
                    {
                        dn = Path.Combine(this.RootDirectory.FullName, dn.Substring(1));
                    }

                    if (!Directory.Exists(dn))
                    {
                        throw new ArgumentException(string.Format("The required Application directory {0} was not found.", dn), "application_options");
                    }
                    else
                    {
                        this.ApplicationFileSystemMap.Add(d.Key, new DirectoryInfo(dn));
                    }
                }
            }
            string default_rules_exception_message;
            int l = LoadDefaultConfigurationRules(out default_rules_exception_message);

        }
        #endregion

        #region Public methods
        public Dictionary<string, bool> FilesExist(List<string> paths)
        {
            throw new NotImplementedException();
        }

        public bool FileContains(string path, string value)
        {
            throw new NotImplementedException();
        }

        public bool FileIsWritable(string path)
        {
            throw new NotImplementedException();
        }

        public bool WindowsRegistryKeyExists(string path)
        {
            throw new NotImplementedException();
        }

        public Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> EvaluateProjectConfigurationRules(IEnumerable<OSSIndexProjectConfigurationRule> rules)
        {
            Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> results = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>(rules.Count());
            foreach (OSSIndexProjectConfigurationRule r in rules)
            {
                List<string> result;
                string message;
                results.Add(r, new Tuple<bool, List<string>, string>(this.Configuration.XPathEvaluate(r.XPathTest, out result, out message), result, message));
            }
            return results;
        }
        #endregion

        #region Static members

        public static string DIR = new string(Path.DirectorySeparatorChar, 1);

        public static List<FileInfo> RecursiveFolderScan(DirectoryInfo dir, string pattern)
        {
            List<FileInfo> results = new List<FileInfo>();
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                results.AddRange(RecursiveFolderScan(d, pattern));
            }
            results.AddRange(dir.GetFiles(pattern));
            return results;
        }

        public static string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }
        #endregion

        #region Protected and private fields
        protected Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> _ConfigurationRulesForProject;
        private Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> _ModulesTask;
        private Task _ConfigurationTask;
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>> _ConfigurationRulesTask;
        #endregion

        #region Protected and private methods
        protected int LoadDefaultConfigurationRules(out string exception_message)
        {
            exception_message = string.Empty;
            string rules_file_name = Path.Combine("Rules", this.ApplicationId + "." + "yml");
            if (!File.Exists(rules_file_name)) return 0;
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            Dictionary<string, List<OSSIndexProjectConfigurationRule>> rules;
            using (StreamReader r = new StreamReader(rules_file_name))
            {
                rules = yaml_deserializer.Deserialize<Dictionary<string, List<OSSIndexProjectConfigurationRule>>>(r);
            }
            if (rules == null)
            {
                return -1;
            }
            this._ConfigurationRulesForProject = new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>(rules.Count);
            foreach (KeyValuePair<string, List<OSSIndexProjectConfigurationRule>> kv in rules)
            {
                OSSIndexProject p = new OSSIndexProject() { Name = kv.Key };
                this._ConfigurationRulesForProject.Add(p, kv.Value);
            }
            return rules.Count;
        }
        #endregion

    }
}
