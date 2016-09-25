using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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
        public Dictionary<string, AuditFileSystemInfo> ApplicationFileSystemMap { get; } = new Dictionary<string, AuditFileSystemInfo>();

        public AuditDirectoryInfo RootDirectory
        {
            get
            {
                return (AuditDirectoryInfo) this.ApplicationFileSystemMap["RootDirectory"];
            }
        }

        public AuditFileInfo ApplicationBinary { get; protected set; }

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
        public Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, message_handler)
        {
            if (ReferenceEquals(application_options, null)) throw new ArgumentNullException("application_options");
            this.ApplicationOptions = application_options;
            if (!this.ApplicationOptions.ContainsKey("RootDirectory"))
            {
                throw new ArgumentException(string.Format("The root application directory was not specified."), "application_options");
            }
            else if (!this.AuditEnvironment.DirectoryExists((string)this.ApplicationOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root application directory {0} was not found.", this.ApplicationOptions["RootDirectory"]), "application_options");
            }
            else
            {
                this.ApplicationFileSystemMap.Add("RootDirectory", this.AuditEnvironment.ConstructDirectory((string)this.ApplicationOptions["RootDirectory"]));
            }

            foreach (KeyValuePair<string, string> f in RequiredFileLocations)
            {
                if (!this.ApplicationOptions.ContainsKey(f.Key))
                {
                    if (string.IsNullOrEmpty(f.Value))
                    {
                        throw new ArgumentException(string.Format("The required application file {0} was not specified and no default path exists.", f), "application_options");
                    }
                    else
                    {
                        string fn = CombinePath(f.Value);
                        if (!this.AuditEnvironment.FileExists(fn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application file {1} does not exist.",
                                fn, f.Key), "RequiredFileLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(f.Key, this.AuditEnvironment.ConstructFile(fn));
                        }
                    }

                }
                else
                {
                    string fn = CombinePath((string) ApplicationOptions[f.Key]);
                    if (!this.AuditEnvironment.FileExists(fn))
                    {
                        throw new ArgumentException(string.Format("The required application file {0} was not found.", f), "application_options");
                    }
                    else
                    {
                        this.ApplicationFileSystemMap.Add(f.Key, this.AuditEnvironment.ConstructFile(fn));
                    }
                }
            }

            foreach (KeyValuePair<string, string> d in RequiredDirectoryLocations)
            {
                string dn = CombinePath(d.Value);
                if (!this.ApplicationOptions.ContainsKey(d.Key))
                {
                    if (string.IsNullOrEmpty(d.Value))
                    {
                        throw new ArgumentException(string.Format("The required application directory {0} was not specified and no default path exists.", d.Key), "application_options");
                    }
                    else
                    {
                        if (!this.AuditEnvironment.DirectoryExists(dn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application directory {1} does not exist.",
                                dn, d.Key), "RequiredDirectoryLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(d.Key, this.AuditEnvironment.ConstructDirectory(dn));
                        }
                    }

                }
                else
                {
                    dn = CombinePath((string) ApplicationOptions[d.Key]);
                    if (!this.AuditEnvironment.DirectoryExists(dn))
                    {
                        throw new ArgumentException(string.Format("The required application directory {0} was not found.", dn), "application_options");
                    }
                    else
                    {
                        this.ApplicationFileSystemMap.Add(d.Key, this.AuditEnvironment.ConstructDirectory(dn));
                    }
                }
            }

            if (this.ApplicationOptions.ContainsKey("ApplicationBinary"))
            {
                string fn = CombinePath((string) this.ApplicationOptions["ApplicationBinary"]);
                if (!this.AuditEnvironment.FileExists(fn))
                {
                    throw new ArgumentException(string.Format("The specified application binary does not exist."), "application_options");
                }
                else
                {
                    this.ApplicationBinary = this.AuditEnvironment.ConstructFile(fn);
                }
            }
        }
        #endregion

        #region Public methods
        public string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("path", "paths must be non-null or at least length 1.");
            }
            if (this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX)
            {
                List<string> paths_list = new List<string>(paths.Length + 1);
                if (paths.First() == "@")
                {
                    paths[0] = this.RootDirectory.FullName == "/" ? "" : this.RootDirectory.FullName;
                }
                paths_list.AddRange(paths);
                return paths_list.Aggregate((p, n) => p + "/" + n);
            }
            else
            {
                if (paths.First() == "@")
                {
                    paths[0] = this.RootDirectory.FullName;
                    return Path.Combine(paths);
                }
                else
                {
                    return Path.Combine(paths);
                }
            }
        }

        public string LocatePathUnderRoot(params string[] paths)
        {
            if (this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX)
            {
                return "@" + paths.Aggregate((p, n) => p + "/" + n);
            }
            else
            {

                return "@" + Path.Combine(paths);
            }
                
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

        public static List<AuditFileInfo> RecursiveFolderScan(AuditDirectoryInfo dir, string pattern)
        {
            throw new NotImplementedException();
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
