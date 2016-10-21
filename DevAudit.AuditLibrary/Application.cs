using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
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
        #region Public enums
        
        #endregion

        #region Constructors
        public Application(Dictionary<string, object> application_options, Dictionary<string, string[]> RequiredFileLocationPaths, Dictionary<string, string[]> RequiredDirectoryLocationPaths, EventHandler<EnvironmentEventArgs> message_handler = null) : base(application_options, message_handler)
        {
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

            this.RequiredFileLocations = RequiredFileLocationPaths.Select(kv => new KeyValuePair<string, string>(kv.Key, this.CombinePath(kv.Value))).ToDictionary(x => x.Key, x => x.Value);
            this.RequiredDirectoryLocations = RequiredDirectoryLocationPaths.Select(kv => new KeyValuePair<string, string>(kv.Key, this.CombinePath(kv.Value))).ToDictionary(x => x.Key, x => x.Value);

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
                    string fn = CombinePath((string)ApplicationOptions[f.Key]);
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
                    dn = CombinePath((string)ApplicationOptions[d.Key]);
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
                string fn = CombinePath((string)this.ApplicationOptions["ApplicationBinary"]);
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

        #region Public abstract properties
        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }
        #endregion

        #region Protected abstract methods
        protected abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        protected abstract IConfiguration GetConfiguration();
        public abstract bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version);
        #endregion

        #region Public properties
        public Dictionary<string, IEnumerable<OSSIndexQueryObject>> Modules { get; protected set; }  = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>();

        public IEnumerable<OSSIndexArtifact> ArtifactsForConfigurationRulesAudit { get; protected set; } = new List<OSSIndexArtifact>();

        public Dictionary<string, AuditFileSystemInfo> ApplicationFileSystemMap { get; } = new Dictionary<string, AuditFileSystemInfo>();

        public AuditDirectoryInfo RootDirectory
        {
            get
            {
                return (AuditDirectoryInfo) this.ApplicationFileSystemMap["RootDirectory"];
            }
        }

        public AuditFileInfo ApplicationBinary { get; protected set; }

        public Dictionary<string, string> RequiredFileLocations { get; protected set; }

        public Dictionary<string, string> RequiredDirectoryLocations { get; protected set; }

        public IConfiguration Configuration { get; protected set; } = null;

        public string ConfigurationStatistics
        {
            get
            {
                if (this.Configuration == null)
                {
                    throw new InvalidOperationException("Configuration has not been read.");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    IConfigurationStatistics cs = this.Configuration as IConfigurationStatistics;
                    
                    if (this.Configuration.IncludeFilesStatus != null)
                    {
                        foreach (Tuple<string, bool, IConfigurationStatistics> status in this.Configuration.IncludeFilesStatus)
                        {
                            if (status.Item2)
                            {
                                IConfigurationStatistics include_statistics = status.Item3;
                                sb.AppendLine(string.Format("Included {0}. File path: {1}. First line parsed {2}. Last line parsed: {3}. Parsed {4} top-level configuration nodes. Parsed {5} comments.", status.Item1, include_statistics.FullFilePath, include_statistics.FirstLineParsed, include_statistics.LastLineParsed, include_statistics.TotalFileTopLevelNodes, include_statistics.TotalFileComments));
                            }
                            else sb.AppendLine(string.Format("Failed to include {0}.", status.Item1));
                        }
                    }
                    sb.Append(string.Format("Parsed configuration from {0}. Successfully included {5} out of {6} include files. First line parsed {1}. Last line parsed: {2}. Parsed {3} total configuration nodes. Parsed {4} total comments.", cs.FullFilePath, cs.FirstLineParsed, cs.LastLineParsed, cs.TotalFileTopLevelNodes, cs.TotalFileComments, cs.IncludeFilesParsed.HasValue ? cs.IncludeFilesParsed : 0, cs.TotalIncludeFiles.HasValue ? cs.TotalIncludeFiles.Value : 0));
                    return sb.ToString();
                }
            }
        }

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


        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> ProjectConfigurationRules
        {
            get
            {
                return _ConfigurationRulesForProject;
            }
        }

        public Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> ProjectConfigurationRulesEvaluations = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>();

        public Dictionary<string, object> ApplicationOptions { get; set; } = new Dictionary<string, object>();
        #endregion

        #region Public methods
        public override AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here(); 
            try
            {
                Task.Run(() => this.GetModules(), ct).Wait();
            }
            catch (AggregateException ae)
            {                        
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetModules task");
                return AuditResult.ERROR_SCANNING_MODULES;
            }
            try
            {
                Task.Run(() => this.GetPackages(), ct).Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetPackages task");
                return AuditResult.ERROR_SCANNING_PACKAGES;
            }

            Task get_default_configuration_rules_task = Task.Run(() => this.GetDefaultConfigurationRules(), ct);
            Task get_artifacts_task = Task.Run(() => this.GetArtifacts(), ct);
            try
            {
                Task.WaitAll(get_default_configuration_rules_task, get_artifacts_task);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException.TargetSite.Name == "GetDefaultConfigurationRules")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetDefaultConfigurationRules task.");
                    return AuditResult.ERROR_SCANNING_DEFAULT_CONFIGURATION_RULES;
                }
                else if (ae.InnerException.TargetSite.Name == "GetArtifacts")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetArtifacts task.");
                    return AuditResult.ERROR_SEARCHING_ARTIFACTS;
                }
            }
            Task get_configuration_rules_task = Task.Run(() => this.GetConfigurationRules(), ct);
            Task get_vulnerabilities_task = Task.Run(() => this.GetVulnerabilties(), ct);
            try
            {
                Task.WaitAll(get_configuration_rules_task, get_vulnerabilities_task);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException.TargetSite.Name == "GetConfigurationRules")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetConfigurationRules task.");
                    return AuditResult.ERROR_SEARCHING_CONFIGURATION_RULES;
                }
                else if (ae.InnerException.TargetSite.Name == "GetVulnerabilities")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetVulnerabilities task.");
                    return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
                }
            }
            throw new NotImplementedException();
        }
        #endregion

        #region Protected methods
        protected int GetDefaultConfigurationRules()
        {
            this.AuditEnvironment.Info("Loading default configuration rules for {0} application.", this.ApplicationLabel);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            AuditFileInfo rules_file = this.HostEnvironment.ConstructFile(this.CombinePath("Rules", this.ApplicationId + "." + "yml"));
            if (!rules_file.Exists) throw new Exception(string.Format("The default rules file {0} does not exist.", rules_file.FullName));
            Deserializer yaml_deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention(), ignoreUnmatched: true);
            Dictionary<string, List<OSSIndexProjectConfigurationRule>> rules;
            rules = yaml_deserializer.Deserialize<Dictionary<string, List<OSSIndexProjectConfigurationRule>>>(new System.IO.StringReader(rules_file.ReadAsText()));
            if (rules == null)
            {
                sw.Stop();
                throw new Exception(string.Format("Parsing the default rules file {0} returned null.", rules_file.FullName));
            }
            foreach (KeyValuePair<string, List<OSSIndexProjectConfigurationRule>> kv in rules)
            {
                this.AddConfigurationRules(kv.Key, kv.Value);
            }
            sw.Stop();
            this.AuditEnvironment.Info("Got {0} default configuration rule(s) in {1} ms.", rules.Count, sw.ElapsedMilliseconds);
            return rules.Count;
        }

        protected void GetConfigurationRules()
        {
            if (this.ArtifactsForConfigurationRulesAudit.Count() == 0)
            {
                this.AuditEnvironment.Info("No artifacts with configuration rules specified.");
                return;
            }
            this.AuditEnvironment.Info("Searching OSS Index for configuration rules for {0} artifact(s).", this.ArtifactsWithProjects.Count);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Parallel.ForEach(this.ArtifactsForConfigurationRulesAudit, (a) =>
            {
                OSSIndexProject project;
                lock (artifact_project_lock)
                {

                    if (!ArtifactProject.Values.Any(p => p.Id.ToString() == a.ProjectId))
                    {
                        project = this.HttpClient.GetProjectForIdAsync(a.ProjectId).Result;
                        project.Artifact = a;
                        this._ArtifactProject.Add(a, project);
                    }
                    else
                    {
                        project = ArtifactProject.Values.Where(ap => ap.Id.ToString() == a.ProjectId).First();
                    }
                }
                IEnumerable<OSSIndexProjectConfigurationRule> rules = this.HttpClient.GetConfigurationRulesForIdAsync(project.Id.ToString()).Result;
                if (rules != null)
                {
                    this.AddConfigurationRules(project, rules);
                    this.AuditEnvironment.Info("Found {0} rule(s) for artifact {1}.", rules.Count(), project.Artifact.PackageName);
                }
            });
            sw.Stop();
            this.AuditEnvironment.Info("Searched OSS Index configuration rules in {0} ms.", sw.ElapsedMilliseconds);
            return;
        }

        protected Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> EvaluateProjectConfigurationRules(IEnumerable<OSSIndexProjectConfigurationRule> rules)
        {
            this.AuditEnvironment.Status("Evaluating {0} configuration rules.", this.ProjectConfigurationRules.Count);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> results = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>(this.ProjectConfigurationRules.Count());
            object evaluate_rules = new object();
            Parallel.ForEach(this.ProjectConfigurationRules.Values, (pr) =>
            {
                Parallel.ForEach(pr, (r) =>
               {
                   if (!string.IsNullOrEmpty(r.XPathTest))
                   {
                       List<string> result;
                       string message;
                       lock (evaluate_rules)
                       {
                           results.Add(r, new Tuple<bool, List<string>, string>(this.Configuration.XPathEvaluate(r.XPathTest, out result, out message), result, message));
                       }
                   }
               });
            });
            this.ProjectConfigurationRulesEvaluations = results;
            sw.Stop();
            this.AuditEnvironment.Success("Evaluated {0} configuration rules in {1} ms.", this.ProjectConfigurationRules.Count, sw.ElapsedMilliseconds);
            return this.ProjectConfigurationRulesEvaluations;
        }

        protected string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("paths", "paths must be non-null or at least length 1.");
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
                    return System.IO.Path.Combine(paths);
                }
                else
                {
                    return System.IO.Path.Combine(paths);
                }
            }
        }

        protected string LocatePathUnderRoot(params string[] paths)
        {
            if (this.AuditEnvironment.OS.Platform == PlatformID.Unix || this.AuditEnvironment.OS.Platform == PlatformID.MacOSX)
            {
                return "@" + paths.Aggregate((p, n) => p + "/" + n);
            }
            else
            {

                return "@" + System.IO.Path.Combine(paths);
            }

        }
        #endregion

        #region Private methods
        private KeyValuePair<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>
          AddPackageVulnerability(OSSIndexQueryObject package, IEnumerable<OSSIndexPackageVulnerability> vulnerability)
        {
            lock (package_vulnerabilities_lock)
            {
                this._VulnerabilitiesForPackage.Add(package, vulnerability);
                return new KeyValuePair<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>(package, vulnerability);
            }
        }

        private KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>
            AddProjectVulnerability(OSSIndexProject project, IEnumerable<OSSIndexProjectVulnerability> vulnerability)
        {
            lock (project_vulnerabilities_lock)
            {
                this._VulnerabilitiesForProject.Add(project, vulnerability);

                return new KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>(project, vulnerability);
            }
        }

        private KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> AddArtifiact(IEnumerable<OSSIndexQueryObject> query, IEnumerable<OSSIndexArtifact> artifact)
        {
            lock (artifacts_lock)
            {
                this._ArtifactsForQuery.Add(query, artifact);
                return new KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>(query, artifact);
            }
        }


        private void AddConfigurationRules(OSSIndexProject project, IEnumerable<OSSIndexProjectConfigurationRule> rules)
        {
            lock (configuration_rules_lock)
            {
                this._ConfigurationRulesForProject.Add(project, rules);
            }
        }

        private void AddConfigurationRules(string project_name, IEnumerable<OSSIndexProjectConfigurationRule> rules)
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

        #region Private fields
        private readonly object artifacts_lock = new object(), package_vulnerabilities_lock = new object(), project_vulnerabilities_lock = new object(), artifact_project_lock = new object();
        private Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery =
            new Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>();
        private Dictionary<OSSIndexArtifact, OSSIndexProject> _ArtifactProject = new Dictionary<OSSIndexArtifact, OSSIndexProject>();
        private Task<Tuple<int, int>> _ArtifactsTask;
        private Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>> _VulnerabilitiesForPackage =
            new Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>();
        private Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> _VulnerabilitiesForProject =
            new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>();
        protected Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> _ConfigurationRulesForProject = new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>();
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> _VulnerabilitiesTask;
        
        private Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> _ModulesTask;
        private Task _ConfigurationTask;
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>>>> _ConfigurationRulesTask;
        private object configuration_rules_lock = new object();
        #endregion

    }
}
