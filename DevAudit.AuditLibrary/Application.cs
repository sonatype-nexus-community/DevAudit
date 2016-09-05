using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Alpheus;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevAudit.AuditLibrary
{
    public abstract class Application : PackageSource
    {
        #region Public abstract methods and properties
        public abstract bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version);

        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }
        
        public abstract Dictionary<string, string> RequiredFileLocations { get; }

        public abstract Dictionary<string, string> RequiredDirectoryLocations { get; }
        #endregion

        #region Protected abstract methods
        protected abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        protected abstract IConfiguration GetConfiguration();
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

        public FileInfo ApplicationBinary { get; protected set; }

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
                    this._ConfigurationRulesForProject = new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>();
                    this.LoadDefaultConfigurationRules();
                    List<string> projects_to_query = this.ArtifactsWithProjects.Select(a => a.ProjectId).ToList();
                    this._ConfigurationRulesTask = new List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>>(projects_to_query.Count);
                    projects_to_query.ForEach(pr => this._ConfigurationRulesTask.Add(Task<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>>.Factory.StartNew(async (o) =>    
                        {
                            string project_id = o as string;
                            OSSIndexProject project = null;
                            if (!ArtifactProject.Values.Any(ap => ap.Id.ToString() == project_id))
                            {
                                project = await this.HttpClient.GetProjectForIdAsync(project_id);

                            }
                            else
                            {
                                project = ArtifactProject.Values.Where(ap => ap.Id.ToString() == project_id).First();
                            }
                            IEnumerable<OSSIndexProjectConfigurationRule> rules = await this.HttpClient.GetConfigurationRulesForIdAsync(project_id);
                            if (rules != null)
                            {
                                this.AddConfigurationRules(project, rules);
                            }
                            return new KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>
                            (project, rules);
                        }, pr, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap()));
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
                throw new ArgumentException(string.Format("The root application directory was not specified."), "Application_options");
            }
            else if (!Directory.Exists((string)this.ApplicationOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root application directory {0} was not found.", this.ApplicationOptions["RootDirectory"]), "application_options");
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
                        throw new ArgumentException(string.Format("The required application file {0} was not found.", f), "application_options");
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
                        throw new ArgumentException(string.Format("The required application directory {0} was not specified and no default path exists.", d.Key), "application_options");
                    }
                    else
                    {
                        if (!Directory.Exists(dn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application directory {1} does not exist.",
                                dn, d.Key), "RequiredDirectoryLocations");
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

            if (this.ApplicationOptions.ContainsKey("ApplicationBinary"))
            {
                string fn = (string) this.ApplicationOptions["ApplicationBinary"];
                if (fn.StartsWith("@"))
                {
                    fn = CombinePathsUnderRoot(fn.Substring(1));
                }
                if (!File.Exists(fn))
                {
                    throw new ArgumentException(string.Format("The specified application binary does not exist."), "application_options");
                }
                else
                {
                    this.ApplicationBinary = new FileInfo(fn);
                }
            }
        }
        #endregion

        #region Public methods

        public string CombinePathsUnderRoot(params string[] paths)
        {
            return Path.Combine(this.RootDirectory.FullName, Path.Combine(paths));
        }

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

        public static string LocateUnderRoot(params string[] paths)
        {
            return "@" + Path.Combine(paths);
        }

       
        #endregion

        #region Protected and private fields
        protected Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> _ConfigurationRulesForProject;
        private Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> _ModulesTask;
        private Task _ConfigurationTask;
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>> _ConfigurationRulesTask;
        private object configuration_rules_lock = new object();
        #endregion

        #region Protected and private methods
        protected int LoadDefaultConfigurationRules()
        {
            string rules_file_name = Path.Combine("Rules", this.ApplicationId + "." + "yml");
            if (!File.Exists(rules_file_name)) throw new Exception(string.Format("The default rules file {0} does not exist.", rules_file_name));
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            Dictionary<string, List<OSSIndexProjectConfigurationRule>> rules;
            using (StreamReader r = new StreamReader(rules_file_name))
            {
                rules = yaml_deserializer.Deserialize<Dictionary<string, List<OSSIndexProjectConfigurationRule>>>(r);
            }
            if (rules == null)
            {
                throw new Exception(string.Format("Parsing the default rules file {0} returned null.", rules_file_name));
            }
            foreach (KeyValuePair<string, List<OSSIndexProjectConfigurationRule>> kv in rules)
            {
                this.AddConfigurationRules(kv.Key, kv.Value);
            }
            return rules.Count;
        }

        protected void AddConfigurationRules(OSSIndexProject project, IEnumerable<OSSIndexProjectConfigurationRule> rules)
        {
            lock (configuration_rules_lock)
            {
      
                this._ConfigurationRulesForProject.Add(project, rules);
            }
        }

        protected void AddConfigurationRules(string project_name, IEnumerable<OSSIndexProjectConfigurationRule> rules)
        {
            lock (configuration_rules_lock)
            {

                OSSIndexProject project = ArtifactProject.Values.Where(p => p.Name == project_name).FirstOrDefault();
                if (project == null)
                {
                    project = new OSSIndexProject() { Name = project_name };
                }
                this._ConfigurationRulesForProject.Add(project, rules);
            }
        }
        #endregion

    }
}
