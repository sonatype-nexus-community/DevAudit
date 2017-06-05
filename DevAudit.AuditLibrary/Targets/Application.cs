using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using CSScriptLibrary;


namespace DevAudit.AuditLibrary
{
    public abstract class Application : PackageSource
    {
        #region Constructors
        public Application(Dictionary<string, object> application_options, Dictionary<string, string[]> RequiredFileLocationPaths, Dictionary<string, string[]> RequiredDirectoryLocationPaths, EventHandler<EnvironmentEventArgs> message_handler) : base(application_options, message_handler)
        {
            this.ApplicationOptions = application_options;
            if (this.ApplicationOptions.ContainsKey("RootDirectory"))
            {
                if (!this.AuditEnvironment.DirectoryExists((string)this.ApplicationOptions["RootDirectory"]))
                {
                    throw new ArgumentException(string.Format("The root directory {0} was not found.", this.ApplicationOptions["RootDirectory"]), "package_source_options");
                }
                else
                {
                    this.ApplicationFileSystemMap.Add("RootDirectory", this.AuditEnvironment.ConstructDirectory((string)this.ApplicationOptions["RootDirectory"]));
                }

            }
            else
            {
                //throw new ArgumentException(string.Format("The root application directory was not specified."), "application_options");
                this.ApplicationFileSystemMap.Add("RootDirectory", this.AuditEnvironment.ConstructDirectory(this.AuditEnvironment.PathSeparator));
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
                    throw new ArgumentException(string.Format("The specified application binary {0} does not exist.", fn), "application_options");
                }
                else
                {
                    this.ApplicationBinary = this.AuditEnvironment.ConstructFile(fn);
                }
            }

            if (this.AuditProfile == null)
            {
                AuditFileInfo pf = this.AuditEnvironment.ConstructFile(".devaudit");
                if (pf.Exists)
                {
                    this.AuditProfile = new AuditProfile(this.AuditEnvironment, pf);
                }
            }
            if (this.ApplicationOptions.ContainsKey("PrintConfiguration"))
            {
                this.PrintConfiguration = true;
            }

            if (this.ApplicationOptions.ContainsKey("ListConfigurationRules"))
            {
                this.ListConfigurationRules = true;
            }

            if (this.ApplicationOptions.ContainsKey("OnlyLocalRules"))
            {
                this.OnlyLocalRules = true;
            }

            if ((this.ApplicationOptions.ContainsKey("AppDevMode")))
            {
                this.AppDevMode = true;
            }

            if ((this.ApplicationOptions.ContainsKey("OSUser")))
            {
                this.OSUser = (string)this.ApplicationOptions["OSUser"];
            }

            if ((this.ApplicationOptions.ContainsKey("OSPass")))
            {
                this.OSPass = this.AuditEnvironment.ToSecureString((string)this.ApplicationOptions["OSPass"]);
            }

            if ((this.ApplicationOptions.ContainsKey("AppUser")))
            {
                this.AppUser = (string)this.ApplicationOptions["AppUser"];
            }

            if ((this.ApplicationOptions.ContainsKey("AppPass")))
            {
                this.AppPass = this.AuditEnvironment.ToSecureString((string)this.ApplicationOptions["AppPass"]);
            }

        }

        public Application(Dictionary<string, object> application_options, Dictionary<string, string[]> RequiredFileLocationPaths, Dictionary<string, string[]> RequiredDirectoryLocationPaths, string analyzer_type, EventHandler<EnvironmentEventArgs> message_handler) : 
            this(application_options, RequiredFileLocationPaths, RequiredDirectoryLocationPaths, message_handler)
        {
            this.AnalyzerType = analyzer_type;
        }

        #endregion

        #region Abstract properties
        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }

        public abstract PackageSource PackageSource { get; }
        #endregion

        #region Abstract methods
        protected abstract string GetVersion();
        protected abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        protected abstract IConfiguration GetConfiguration();
        public abstract bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version);
        #endregion

        #region Overriden methods
        public override Task GetPackagesTask(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            if (this.SkipPackagesAudit || this.PrintConfiguration || this.ListConfigurationRules)
            {
                this.PackagesTask = this.ArtifactsTask = this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
                this.Packages = new List<OSSIndexQueryObject>();
            }
            else
            {
                this.AuditEnvironment.Status("Scanning {0} packages.", this.PackageManagerLabel);
                this.PackagesTask = Task.Run(() => this.Packages = this.GetPackages(), ct);
            }
            return this.PackagesTask;
        }
        #endregion

        #region Properties
        public Dictionary<string, AuditFileSystemInfo> ApplicationFileSystemMap { get; } = new Dictionary<string, AuditFileSystemInfo>();

        public AuditDirectoryInfo RootDirectory
        {
            get
            {
                return (AuditDirectoryInfo)this.ApplicationFileSystemMap["RootDirectory"];
            }
        }

        public Dictionary<string, string> RequiredFileLocations { get; protected set; }

        public Dictionary<string, string> RequiredDirectoryLocations { get; protected set; }

        public AuditFileInfo ApplicationBinary { get; protected set; }

        public object Modules { get; protected set; }

        public Dictionary<string, IEnumerable<OSSIndexQueryObject>> ModulePackages { get; protected set; }

        public string AnalyzerType { get; protected set; }

        public List<FileInfo> AnalyzerScripts { get; protected set; } = new List<FileInfo>();

        public List<ByteCodeAnalyzer> Analyzers { get; protected set; } = new List<ByteCodeAnalyzer>();

        public bool AnalyzersInitialized { get; protected set; } = false;

        public List<ByteCodeAnalyzerResult> AnalyzerResults { get; protected set; }

        public bool ModulesInitialised { get; protected set; } = false;

        public string Version { get; protected set; }

        public bool VersionInitialised { get; protected set; } = false;

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

        public bool ConfigurationInitialised { get; protected set; } = false;

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

        public Task GetAnalyzersTask { get; protected set; }

        public Task GetAnalyzersResultsTask { get; protected set; }

        public List<OSSIndexProjectConfigurationRule> DisabledForAppDevModeRules { get; }
            = new List<OSSIndexProjectConfigurationRule>();

        public ConcurrentDictionary<OSSIndexArtifact, Exception> GetProjectConfigurationRulesExceptions { get; protected set; }

        public bool PackageSourceInitialized { get; protected set; }

        public Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> ProjectConfigurationRulesEvaluations = new Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>();

        public Dictionary<string, object> ApplicationOptions { get; set; } = new Dictionary<string, object>();

        public bool PrintConfiguration { get; protected set; } = false;

        public bool ListConfigurationRules { get; protected set; } = false;

        public bool OnlyLocalRules { get; protected set; } = false;

        public bool AppDevMode { get; protected set; }

        public string OSUser { get; protected set; }

        public SecureString OSPass { get; protected set; }

        public string AppUser { get; protected set; }

        public SecureString AppPass { get; protected set; }

        #endregion

        #region Methods
        public override AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            try
            {
                this.GetModules();
                this.GetVersion();
            }
            catch (Exception e)
            {
                if (e is NotImplementedException && e.TargetSite.Name == "GetVersion")
                {
                    this.AuditEnvironment.Debug("{0} application does not implement standalone GetVersion method.", this.ApplicationLabel);
                }
                else if (e.TargetSite.Name == "GetVersion")
                {
                    this.AuditEnvironment.Error(e, "There was an error scanning the {0} application version.", this.ApplicationLabel);
                    return AuditResult.ERROR_SCANNING_VERSION;
                }
                else
                {
                    this.AuditEnvironment.Error(e, "There was an error scanning the {0} application modules.", this.ApplicationLabel);
                    return AuditResult.ERROR_SCANNING_MODULES;
                }
            }

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
                if (!this.SkipPackagesAudit && !this.PrintConfiguration && this.PackageSourceInitialized && this.PackagesTask.Status == TaskStatus.RanToCompletion)
                {
                    AuditEnvironment.Success("Scanned {0} {1} packages.", this.Packages.Count(), this.PackageManagerLabel);
                }
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

            if (!this.PackageSourceInitialized || this.ListPackages || this.PrintConfiguration || this.Packages.Count() == 0 || (this.SkipPackagesAudit && this.OnlyLocalRules))
            {
                this.ArtifactsTask = this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else if (this.ListArtifacts)
            {
                this.ArtifactsTask = Task.Run(() => this.GetArtifacts(), ct);
            }
            else this.ArtifactsTask = Task.CompletedTask;

            try
            {
                this.ArtifactsTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error("Exception thrown in GetArtifacts task.", ae.InnerException);
                return AuditResult.ERROR_SEARCHING_ARTIFACTS;
            }

            if (!this.PackageSourceInitialized || this.ListPackages || this.PrintConfiguration || this.Packages.Count() == 0 || this.ListArtifacts || this.ListConfigurationRules)
            {
                this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask; ;
            }
            else
            {
                this.VulnerabilitiesTask = Task.Run(() => this.GetVulnerabiltiesApiv2(), ct);
            }

            if (!this.PackageSourceInitialized || this.PrintConfiguration || this.ListPackages || this.Packages.Count() == 0 || this.ListArtifacts || this.SkipPackagesAudit
                || this.OnlyLocalRules || this.ArtifactsWithProjects == null || this.ArtifactsWithProjects.Count() == 0 || !this.ConfigurationInitialised)
            {
                this.GetConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.GetConfigurationRulesTask = Task.Run(() => this.GetConfigurationRules(), ct);
            }

            if (this.ListPackages || this.ListArtifacts || !this.ConfigurationInitialised || this.PrintConfiguration)
            {
                this.GetDefaultConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.GetDefaultConfigurationRulesTask = Task.Run(() => this.GetDefaultConfigurationRules());
            }

            if (this.ListPackages || this.ListArtifacts || this.PrintConfiguration)
            {
                this.GetAnalyzersTask = Task.CompletedTask;
            }
            else if (this.ModulesInitialised && !string.IsNullOrEmpty(this.AnalyzerType))
            {
                this.GetAnalyzersTask = Task.Run(() => this.GetAnalyzers());
            }
            else
            {
                this.GetAnalyzersTask = Task.CompletedTask;
            }

            try
            {
                Task.WaitAll(this.VulnerabilitiesTask, this.GetConfigurationRulesTask, this.GetDefaultConfigurationRulesTask, this.GetAnalyzersTask);
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
                else if (ae.InnerException.TargetSite.Name == "GetConfigurationRules")
                {  
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetDefaultConfigurationRules task.");
                    return AuditResult.ERROR_SCANNING_DEFAULT_CONFIGURATION_RULES;
                }
                else if (ae.InnerException.TargetSite.Name == "GetAnalyzers")
                {  
                    this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetAnalyzers task.");
                    return AuditResult.ERROR_SCANNING_ANALYZERS;                    
                }
                else
                {
                    this.AuditEnvironment.Error(caller, ae.InnerException);
                    return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
                }
            }

            if (this.ListPackages || this.ListArtifacts || this.ListConfigurationRules || this.Vulnerabilities.Count == 0 || this.PrintConfiguration)
            { 
                this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateVulnerabilitiesTask = Task.Run(() => this.EvaluateVulnerabilities(), ct);
            }

            if (this.ListPackages || this.ListArtifacts || this.ListConfigurationRules || this.PrintConfiguration)
            {
                this.EvaluateConfigurationRulesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateConfigurationRulesTask = Task.Run(() => this.EvaluateProjectConfigurationRules(), ct);
            }

            if (this.ListPackages || this.ListArtifacts || this.ListConfigurationRules || this.PrintConfiguration)
            {
                this.GetAnalyzersResultsTask = Task.CompletedTask;
            }
            else if (this.AnalyzersInitialized)
            {
                this.GetAnalyzersResultsTask = Task.Run(() => this.GetAnalyzerResults());
            }
            else
            {
                this.GetAnalyzersResultsTask = Task.CompletedTask;
            }

            try
            {
                Task.WaitAll(this.EvaluateVulnerabilitiesTask, this.EvaluateConfigurationRulesTask, this.GetAnalyzersResultsTask);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in {0} task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_EVALUATING_CONFIGURATION_RULES;
            }
            return AuditResult.SUCCESS;
        }
     
        protected int GetDefaultConfigurationRules()
        {
            this.AuditEnvironment.Info("Loading default configuration rules for {0} application.", this.ApplicationLabel);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AuditFileInfo rules_file = this.HostEnvironment.ConstructFile(Path.Combine(this.DevAuditDirectory, "Rules", this.ApplicationId + "." + "yml"));
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
            this.AuditEnvironment.Info("Got {0} default configuration rule(s) for {1} module(s) from {2}.yml in {3} ms.", rules.Sum(kv => kv.Value.Count()), rules.Keys.Count, this.ApplicationId, sw.ElapsedMilliseconds);
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
                    if (this.AppDevMode && !r.EnableForAppDevelopment)
                    {
                        this.AuditEnvironment.Info("Not evaluating rule \"{0}\" for application development mode.", r.Title);
                        this.DisabledForAppDevModeRules.Add(r);
                    }
                    else if (!string.IsNullOrEmpty(r.XPathTest))
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

        protected virtual async Task GetAnalyzers()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // Just in case clear AlternativeCompiler so it is not set to Roslyn or anything else by 
            // the CS-Script installed (if any) on the host OS
            CSScript.GlobalSettings.UseAlternativeCompiler = null;
            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
            CSScript.CacheEnabled = false; //Script caching is broken on Mono: https://github.com/oleg-shilo/cs-script/issues/10
            CSScript.KeepCompilingHistory = true;
            DirectoryInfo analyzers_dir = new DirectoryInfo(Path.Combine(this.DevAuditDirectory, "Analyzers", this.AnalyzerType));
            if (!analyzers_dir.Exists)
            {
                this.AnalyzersInitialized = false;
                return;
            }
            this.AnalyzerScripts = analyzers_dir.GetFiles("*.cs", SearchOption.AllDirectories).ToList();
            foreach (FileInfo f in this.AnalyzerScripts)
            {
                string script = string.Empty;
                using (FileStream fs = f.OpenRead())
                using (StreamReader r = new StreamReader(fs))
                {
                    script = await r.ReadToEndAsync();
                }
                try
                {
                    ByteCodeAnalyzer ba = (ByteCodeAnalyzer)await CSScript.Evaluator.LoadCodeAsync(script, this.HostEnvironment.ScriptEnvironment,
                        this.Modules, this.Configuration, this.ApplicationOptions);
                    this.Analyzers.Add(ba);
                    this.HostEnvironment.Info("Loaded {0} analyzer from {1}.", ba.Name, f.FullName);
                }
                catch (csscript.CompilerException ce)
                {
                    HostEnvironment.Error("Compiler error(s) compiling analyzer {0}.", f.FullName);
                    IDictionaryEnumerator en = ce.Data.GetEnumerator();
                    while (en.MoveNext())
                    {
                        List<string> v = (List<string>)en.Value;
                        if (v.Count > 0)
                        {
                            if ((string)en.Key == "Errors")
                            {
                                HostEnvironment.ScriptEnvironment.Error(v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                            else if ((string)en.Key == "Warnings")
                            {
                                HostEnvironment.ScriptEnvironment.Warning(v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                            else
                            {
                                HostEnvironment.ScriptEnvironment.Error("{0} : {1}", en.Key, v.Aggregate((s1, s2) => s1 + Environment.NewLine + s2));
                            }
                        }
                    }
                    throw ce;
                }
            }
            sw.Stop();
            if (this.AnalyzerScripts.Count == 0)
            {
                this.HostEnvironment.Info("No {0} analyzers found in {1}.", this.AnalyzerType, analyzers_dir.FullName);
                return;
            }
            else if (this.AnalyzerScripts.Count > 0 && this.Analyzers.Count > 0)
            {
                if (this.Analyzers.Count < this.AnalyzerScripts.Count)
                {
                    this.HostEnvironment.Warning("Failed to load {0} of {1} analyzer(s).", this.AnalyzerScripts.Count - this.Analyzers.Count, this.AnalyzerScripts.Count);
                }
                this.HostEnvironment.Success("Loaded {0} out of {1} analyzer(s) in {2} ms.", this.Analyzers.Count, this.AnalyzerScripts.Count, sw.ElapsedMilliseconds);
                this.AnalyzersInitialized = true;
                return;
            }
            else
            {
                this.HostEnvironment.Error("Failed to load {0} analyzer(s).", this.AnalyzerScripts.Count);
                return;
            }
        }

        protected async Task GetAnalyzerResults()
        {
            this.AnalyzerResults = new List<ByteCodeAnalyzerResult>(this.Analyzers.Count);
            foreach (ByteCodeAnalyzer a in this.Analyzers)
            {
                this.HostEnvironment.Status("{0} analyzing.", a.Name);
                ByteCodeAnalyzerResult ar = new ByteCodeAnalyzerResult() { Analyzer = a };
                try
                {
                    ar = await a.Analyze();
                }
                catch (AggregateException ae)
                {
                    ar.Exceptions = ae.InnerExceptions.ToList();
                    ar.Succeded = false;
                }
                finally
                {
                    this.AnalyzerResults.Add(ar);
                }

            }
            return;
        }

        protected string CombinePath(params string[] paths)
        {
            if (paths == null || paths.Count() == 0)
            {
                throw new ArgumentOutOfRangeException("paths", "paths must be non-null or at least length 1.");
            }
            else if (paths.Count() == 1 && paths[0].StartsWith("@"))
            {
                string p = paths[0].Substring(1);
                paths = new string[] { "@", p };
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

        #region Fields
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
