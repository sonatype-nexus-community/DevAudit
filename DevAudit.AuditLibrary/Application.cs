using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
                                fn, f.Key), "application_options");
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
                                dn, d.Key), "application_options");
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

            if (this.ApplicationOptions.ContainsKey("ListConfigurationRules"))
            {
                this.ListConfigurationRules = true;
            }

            if (this.ApplicationOptions.ContainsKey("OnlyLocalRules"))
            {
                this.OnlyLocalRules = true;
            }

        }
        #endregion

        #region Public abstract properties
        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }
        #endregion

        #region Protected abstract methods
        protected abstract string GetVersion();
        protected abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        protected abstract IConfiguration GetConfiguration();
        public abstract bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version);
        #endregion

        #region Public properties
        public Dictionary<string, IEnumerable<OSSIndexQueryObject>> Modules { get; protected set; }

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

        public bool ListConfigurationRules { get; protected set; } = false;

        public bool OnlyLocalRules { get; protected set; } = false;

        public bool DefaultConfigurationRulesOnly { get; protected set; } = false;

        public string Version { get; protected set; }

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

        public Task GetVersionTask { get; protected set; }

        public Task GetModulesTask { get; protected set; }

        public Task GetConfigurationTask { get; protected set; }

        public Task GetDefaultConfigurationRulesTask { get; protected set; }

        public Task GetConfigurationRulesTask { get; protected set; }

        public Task EvaluateConfigurationRulesTask { get; protected set; }

        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> ProjectConfigurationRules
        {
            get
            {
                return _ConfigurationRulesForProject;
            }
        }
        public ConcurrentDictionary<OSSIndexArtifact, Exception> GetProjectConfigurationRulesExceptions { get; protected set; }

        public Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> ProjectConfigurationRulesEvaluations = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>();

        public Dictionary<string, object> ApplicationOptions { get; set; } = new Dictionary<string, object>();
        #endregion

        #region Public methods
        public override AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            this.GetVersion();
            this.GetPackagesTask(ct);
            if (this.ListPackages || this.ListArtifacts)
            {
                this.GetConfigurationTask = Task.CompletedTask;
            }
            else
            {
                this.GetConfigurationTask = Task.Run(() => this.GetConfiguration(), ct);
            }
            try
            {
                Task.WaitAll(this.PackagesTask, this.GetConfigurationTask);
                if (!this.SkipPackagesAudit) AuditEnvironment.Success("Scanned {0} {1} packages.", this.Packages.Count(), this.PackageManagerLabel);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is NotImplementedException && ae.InnerException.TargetSite.Name == "GetConfiguration")
                {
                    this.AuditEnvironment.Debug("{0} application doe not implement standalone GetConfiguration method.", this.ApplicationId);
                }
                else
                {
                    this.AuditEnvironment.Error(caller, ae, "Exception thrown in {0} task.", ae.InnerException.TargetSite.Name);
                    if (ae.TargetSite.Name == "GetPackages")
                    {
                        return AuditResult.ERROR_SCANNING_PACKAGES;
                    }
                    else
                    {
                        return AuditResult.ERROR_SCANNING_CONFIGURATION;
                    }
                }
            }

            if (this.ListPackages || this.Packages.Count() == 0 || (this.SkipPackagesAudit && this.OnlyLocalRules))
            {
                this.ArtifactsTask = this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else 
            {
                this.ArtifactsTask = Task.Run(() => this.GetArtifacts(), ct);
            }

            try
            {
                this.ArtifactsTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error("Exception thrown in GetArtifacts task.", ae.InnerException);
                return AuditResult.ERROR_SEARCHING_ARTIFACTS;
            }
            if (this.ListArtifacts || this.ListPackages || this.ListConfigurationRules || this.ArtifactsWithProjects.Count == 0)
            {
                this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask; ;
            }
            else
            {
                this.VulnerabilitiesTask = Task.Run(() => this.GetVulnerabilties(), ct);
            }

            if (this.ListPackages || this.ListArtifacts || this.SkipPackagesAudit || this.OnlyLocalRules || this.ArtifactsWithProjects.Count() == 0)
            {
                this.GetConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.GetConfigurationRulesTask = Task.Run(() => this.GetConfigurationRules(), ct);
            }
            if (this.ListPackages || this.ListArtifacts)
            {
                this.GetDefaultConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.GetDefaultConfigurationRulesTask = Task.Run(() => this.GetDefaultConfigurationRules());
            }
            try
            {
                Task.WaitAll(this.VulnerabilitiesTask, this.GetConfigurationRulesTask, this.GetDefaultConfigurationRulesTask);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException.TargetSite.Name == "GetVulnerabilities")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetVulnerabilities task.");
                    return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
                }
                else if (ae.InnerException.TargetSite.Name == "GetConfigurationRules")
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetConfigurationRules task.");
                    return AuditResult.ERROR_SEARCHING_CONFIGURATION_RULES;
                }
                else
                {  
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetDefaultConfigurationRules task.");
                    return AuditResult.ERROR_SCANNING_DEFAULT_CONFIGURATION_RULES;
                }
            }

            if (this.ListPackages || this.ListArtifacts || this.ListConfigurationRules || (this.PackageVulnerabilities.Count == 0 && this.ProjectVulnerabilities.Count == 0))
            { 
                this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateVulnerabilitiesTask = Task.WhenAll(Task.Run(() => this.EvaluateProjectVulnerabilities(), ct), Task.Run(() => this.EvaluatePackageVulnerabilities(), ct));
            }
            if (this.ListConfigurationRules)
            {
                this.EvaluateConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateConfigurationRulesTask = Task.Run(() => this.EvaluateProjectConfigurationRules(), ct);
            }

            try
            {
                Task.WaitAll(this.EvaluateVulnerabilitiesTask, this.EvaluateConfigurationRulesTask);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in {0} task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_EVALUATING_CONFIGURATION_RULES;
            }
            return AuditResult.SUCCESS;
        }
        #endregion

        #region Protected methods
        protected int GetDefaultConfigurationRules()
        {
            this.AuditEnvironment.Info("Loading default configuration rules for {0} application.", this.ApplicationLabel);
            Stopwatch sw = new Stopwatch();
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
            if (rules.ContainsKey(this.ApplicationId + "_default")) rules.Remove(this.ApplicationId + "_default");
            foreach (KeyValuePair<string, List<OSSIndexProjectConfigurationRule>> kv in rules)
            {
                this.AddConfigurationRules(kv.Key, kv.Value);
            }
            sw.Stop();
            this.AuditEnvironment.Info("Got {0} default configuration rule(s) for {1} project(s) from {2}.yml in {3} ms.", rules.Sum(kv => kv.Value.Count()), rules.Keys.Count, this.ApplicationId, sw.ElapsedMilliseconds);
            return rules.Count;
        }

        protected void GetConfigurationRules()
        {
            this.AuditEnvironment.Info("Searching OSS Index for configuration rules for {0} artifact(s).", this.ArtifactsWithProjects.Count);
            Stopwatch sw = new Stopwatch();
            List<Task> tasks = new List<Task>();
            this.GetProjectConfigurationRulesExceptions = new ConcurrentDictionary<OSSIndexArtifact, Exception>();
            this.ProjectConfigurationRulesEvaluations = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>();
            Int32 i = new Int32();
            sw.Start();
            foreach(OSSIndexArtifact a in this.ArtifactsWithProjects) 
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    OSSIndexProject project = null;
                    
                    lock (artifact_project_lock)
                    {
                        if (ArtifactProject.Values.Any(p => p.Id.ToString() == a.ProjectId))
                        {
                            project = ArtifactProject.Values.Where(ap => ap.Id.ToString() == a.ProjectId).First();
                        }
                    }
                    try
                    {
                        if (project == null)
                        {
                            project = await this.HttpClient.GetProjectForIdAsync(a.ProjectId);
                            project.Artifact = a;
                            lock (artifact_project_lock)
                            {
                                if (!ArtifactProject.Values.Any(p => p.Id.ToString() == a.ProjectId)) this._ArtifactProject.Add(a, project);
                            }
                        }
                        IEnumerable<OSSIndexProjectConfigurationRule> rules = await this.HttpClient.GetConfigurationRulesForIdAsync(project.Id.ToString());
                        if (rules != null && rules.Count() > 0)
                        {
                            this.AddConfigurationRules(project, rules);
                            this.AuditEnvironment.Debug("Found {0} rule(s) for artifact {1}.", rules.Count(), project.Artifact.PackageName);
                            Interlocked.Add(ref i, 1);
                        }
                    }
                    catch (AggregateException ae)
                    {
                        this.GetProjectConfigurationRulesExceptions.TryAdd(a, ae.InnerException);
                        this.AuditEnvironment.Warning("Exception thrown in {0} task: {1}", ae.InnerException.TargetSite.Name, ae.Message);
                    }
                    catch (Exception e)
                    {
                        this.GetProjectConfigurationRulesExceptions.TryAdd(a, e);
                        this.AuditEnvironment.Warning("Exception thrown in {0} task: {1}", e.TargetSite.Name, e.Message);
                    }
                    finally
                    {
                        sw.Stop();
                    }
                }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            sw.Stop();
            this.AuditEnvironment.Info("Found {0} configuration rule(s) on OSS Index in {1} ms.", i, sw.ElapsedMilliseconds);
            return;
        }

        protected Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> EvaluateProjectConfigurationRules()
        {
            Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> results = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>(this.ProjectConfigurationRules.Count());
            if (this.ProjectConfigurationRules.Count == 0)
            {
                return results;
            }
            this.AuditEnvironment.Status("Evaluating {0} configuration rule(s).", this.ProjectConfigurationRules.Sum(kv => kv.Value.Count()));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            object evaluate_rules = new object();
            this.ProjectConfigurationRules.Values.AsParallel().ForAll(pr =>
            {
                pr.AsParallel().ForAll(r =>
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
            this.AuditEnvironment.Success("Evaluated {0} configuration rule(s) in {1} ms.", this.ProjectConfigurationRulesEvaluations.Keys.Count, sw.ElapsedMilliseconds);
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
                foreach (OSSIndexProjectConfigurationRule r in rules)
                {
                    r.Project = project;
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
