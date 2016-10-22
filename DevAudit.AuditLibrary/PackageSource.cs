using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Sprache;

namespace DevAudit.AuditLibrary
{
    public abstract class PackageSource : AuditTarget, IDisposable
    {
        #region Constructors
        public PackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler)
        {
            this.PackageSourceOptions = this.AuditOptions;
            if (this.PackageSourceOptions.ContainsKey("File"))
            {
                this.PackageManagerConfigurationFile = (string)this.PackageSourceOptions["File"];
                if (!this.AuditEnvironment.FileExists(this.PackageManagerConfigurationFile)) throw new ArgumentException("Could not find the file " + this.PackageManagerConfigurationFile + ".");
            }
            else
            {
                this.PackageManagerConfigurationFile = "";
            }

            if (this.PackageSourceOptions.ContainsKey("ListPackages"))
            {
                this.ListPackages = true;
            }

            if (this.PackageSourceOptions.ContainsKey("ListArtifacts"))
            {
                this.ListArtifacts = true;
            }

            if (this.PackageSourceOptions.ContainsKey("SkipPackagesAudit"))
            {
                this.SkipPackagesAudit = true;
            }

            #region Cache option
            if (this.PackageSourceOptions.ContainsKey("Cache") && (bool)this.PackageSourceOptions["Cache"] == true)
            {
                this.ProjectVulnerabilitiesCacheEnabled = true;
                if (this.PackageSourceOptions.ContainsKey("CacheFile") && !string.IsNullOrEmpty((string)this.PackageSourceOptions["CacheFile"]))
                {
                    this.ProjectVulnerabilitiesCacheFile = (string)this.PackageSourceOptions["CacheFile"];
                }
                else
                {
                    this.ProjectVulnerabilitiesCacheFile = AppDomain.CurrentDomain.BaseDirectory + "DevAudit-net.cache";
                }
                if (this.PackageSourceOptions.ContainsKey("CacheTTL") && !string.IsNullOrEmpty((string)this.PackageSourceOptions["CacheTTL"]))

                {
                    int cache_ttl;
                    if (Int32.TryParse((string)this.PackageSourceOptions["CacheTTL"], out cache_ttl))
                    {
                        if (cache_ttl > 60 * 24 * 30) throw new ArgumentOutOfRangeException("The value for the cache ttl is too large: " + this.PackageSourceOptions["CacheTTL"] + ".");
                        this.ProjectVulnerabilitiesCacheTTL = TimeSpan.FromMinutes(cache_ttl);
                    }
                    else
                        throw new ArgumentOutOfRangeException("The value for the cache ttl is not an integer: " + (string)this.PackageSourceOptions["CacheTTL"] + ".");
                }
                else
                {
                    this.ProjectVulnerabilitiesCacheTTL = TimeSpan.FromMinutes(180);
                }
                if (this.PackageSourceOptions.ContainsKey("CacheDump"))
                {
                    this.ProjectVulnerabilitiesCacheDump = true;
                }
                else
                {
                    this.ProjectVulnerabilitiesCacheDump = false;
                }
                this.ProjectVulnerabilitiesCacheInitialiseTask =
                    Task<BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>>.Run(() =>
                    {
                        return this.InitialiseProjectVulnerabilitiesCache(this.ProjectVulnerabilitiesCacheFile); //Assembly.GetExecutingAssembly().Location + "win-audit.cache");
                    });
            }
            else
            {
                this.ProjectVulnerabilitiesCacheEnabled = false;
            }
            #endregion
        }
        #endregion

        #region Public properties
        public abstract OSSIndexHttpClient HttpClient { get; }

        public abstract string PackageManagerId { get; }

        public abstract string PackageManagerLabel { get; }

        #region Cache stuff
        public bool ProjectVulnerabilitiesCacheEnabled { get; set; }

        public TimeSpan ProjectVulnerabilitiesCacheTTL { get; set; }
        
        public IEnumerable<Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>> ProjectVulnerabilitiesCacheItems
        {
            get
            {
                if (ProjectVulnerabilitiesCacheEnabled)
                {
                    IEnumerable<string> alive_cache_keys =
                        from cache_key in ProjectVulnerabilitiesCache.Keys
                        where DateTime.UtcNow.Subtract(GetProjectVulnerabilitiesCacheEntry(cache_key).Item2) < this.ProjectVulnerabilitiesCacheTTL
                        join artifact in ArtifactsWithProjects
                        on GetProjectVulnerabilitiesCacheEntry(cache_key).Item1 equals artifact.ProjectId
                        select cache_key;                    
                    return
                        from pvc in this.ProjectVulnerabilitiesCache
                        join k in alive_cache_keys
                        on pvc.Key equals k
                        select pvc.Value;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool ProjectVulnerabilitiesCacheDump { get; set; }
        
        public IEnumerable<string> ProjectVulnerabilitiesCacheKeys
        {
            get
            {
                return this.ProjectVulnerabilitiesCache.Keys;
            }
        }
        public IEnumerable<string> ProjectVulnerabilitiesExpiredCacheKeys { get; set; }
                    
        public IEnumerable<OSSIndexArtifact> CachedArtifacts
        {
            get
            {
                return
                   from cache_key in ProjectVulnerabilitiesCache.Keys
                   where DateTime.UtcNow.Subtract(GetProjectVulnerabilitiesCacheEntry(cache_key).Item2) < this.ProjectVulnerabilitiesCacheTTL
                   join artifact in ArtifactsWithProjects
                   on GetProjectVulnerabilitiesCacheEntry(cache_key).Item1 equals artifact.ProjectId
                   select artifact;
            }
        }
        #endregion
        public Dictionary<string, object> PackageSourceOptions { get; set; } = new Dictionary<string, object>();

        public bool ListPackages { get; protected set; } = false;

        public bool ListArtifacts { get; protected set; } = false;

        public bool SkipPackagesAudit { get; protected set; } = false;

        public string PackageManagerConfigurationFile { get; set; }

        public IEnumerable<OSSIndexQueryObject> Packages { get; protected set; }

        public Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> ArtifactsForQuery
        {
            get
            {
                return _ArtifactsForQuery;
            }
        }

        public Dictionary<OSSIndexArtifact, OSSIndexProject> ArtifactProject
        {
            get
            {
                return this._ArtifactProject;
            }
        }
        
        public IEnumerable<OSSIndexArtifact> Artifacts
        {
            get
            {
                return this._ArtifactsForQuery.Values.SelectMany(a => a).Where(a => string.IsNullOrEmpty(a.Version) || (!string.IsNullOrEmpty(a.Version) 
                    && this.IsVulnerabilityVersionInPackageVersionRange(a.Version, a.Package.Version)));//.GroupBy(a => new { a.PackageName}).Select(d => d.First());
            }
        }


        public virtual Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts;
            foreach (OSSIndexArtifact a in o)
            {
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o.ToList();
        };

        public List<OSSIndexArtifact> ArtifactsWithProjects
        {
            get
            {
                return this.Artifacts.Where(a => !string.IsNullOrEmpty(a.ProjectId)).ToList();                    
            }
        }

        public Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>> PackageVulnerabilities
        {
            get
            {
                return _VulnerabilitiesForPackage;
            }
        }

        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> ProjectVulnerabilities
        {
            get
            {
                return _VulnerabilitiesForProject;
            }
        }

        public ConcurrentDictionary<OSSIndexArtifact, Exception> GetVulnerabilitiesExceptions { get; protected set; } 

        public Task PackagesTask { get; protected set; } 

        public Task ArtifactsTask { get; protected set; }

        public Task VulnerabilitiesTask { get; protected set; }

        public Task EvaluateVulnerabilitiesTask { get; protected set; }

        #endregion

        #region Public methods
        public virtual AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            Task get_packages_task = null, get_artifacts_task = null, get_vulnerabilities_task = null, evaluate_vulnerabilities_task = null;
            if (this.SkipPackagesAudit)
            {
                get_packages_task = get_artifacts_task = get_vulnerabilities_task = evaluate_vulnerabilities_task = Task.CompletedTask;
                this.Packages = new List<OSSIndexQueryObject>();
            }
            else
            {
                this.AuditEnvironment.Status("Scanning {0} packages.", this.PackageManagerLabel);
                get_packages_task = Task.Run(() => this.Packages = this.GetPackages(), ct);
            }
            try
            {
                get_packages_task.Wait();
                if (!this.SkipPackagesAudit) AuditEnvironment.Success("Scanned {0} {1} packages.", this.Packages.Count(), this.PackageManagerLabel);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error("Exception thrown in GetPackages task.", ae.InnerException);
                return AuditResult.ERROR_SCANNING_PACKAGES;
            }
            this.PackagesTask = get_packages_task;

            if (this.ListPackages || this.Packages.Count() == 0)
            {
                get_artifacts_task = evaluate_vulnerabilities_task = evaluate_vulnerabilities_task = Task.CompletedTask;
            }
            else
            {
                get_artifacts_task = Task.Run(() => this.GetArtifacts(), ct);
            }
            try
            {
                get_artifacts_task.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error("Exception thrown in GetArtifacts task.", ae.InnerException);
                return AuditResult.ERROR_SEARCHING_ARTIFACTS;
            }
            this.ArtifactsTask = get_artifacts_task; 

            if (this.ListArtifacts || this.ArtifactsWithProjects.Count == 0)
            {
                get_vulnerabilities_task = evaluate_vulnerabilities_task = Task.CompletedTask;
            }
            else
            {
                get_vulnerabilities_task = Task.Run(() => this.GetVulnerabilties(), ct);
            }
            try
            {
                get_vulnerabilities_task.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetVulnerabilities task");
                return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
            }
            this.VulnerabilitiesTask = get_vulnerabilities_task;
            
            if (this.PackageVulnerabilities.Count == 0 && this.ProjectVulnerabilities.Count == 0)
            {
                evaluate_vulnerabilities_task = Task.CompletedTask;
            }
            else
            {
                evaluate_vulnerabilities_task = Task.WhenAll(Task.Run(() => this.EvaluateProjectVulnerabilities(), ct), Task.Run(() => this.EvaluatePackageVulnerabilities(), ct));
            }
            try
            {
                evaluate_vulnerabilities_task.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in {0} task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_EVALUATING_VULNERABILITIES;
            }
            this.EvaluateVulnerabilitiesTask = evaluate_vulnerabilities_task;
            return AuditResult.SUCCESS;
        }

        public async Task<bool> AuditAsync()
        {
            Tuple<int, int> package_artifact_count = await this.GetArtifactsAsync();
            if (package_artifact_count.Item1 > 0 && package_artifact_count.Item2 > 0)
            {
                return true;
            }
            else
            {
                this.AuditEnvironment.Warning("No OSS Index artifacts found, exiting {0} packagee audit.");
                return false;
            }
        }

        #endregion

        #region Protected methods
        protected Tuple<int, int> GetArtifacts()
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            this.AuditEnvironment.Status("Searching OSS Index for artifacts for {0} packages.", this.Packages.Count());
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            object artifacts_lock = new object();
            int package_count = 0, artifact_count = 0;
            int i = 0;
            IGrouping<int, OSSIndexQueryObject>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<OSSIndexQueryObject>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            Parallel.ForEach(queries, new ParallelOptions() { MaxDegreeOfParallelism = 10, TaskScheduler = TaskScheduler.Default }, (q) =>
            {
                try
                {
                    IEnumerable<OSSIndexArtifact> artifacts = this.HttpClient.SearchAsync(this.PackageManagerId, q, this.ArtifactsTransform).Result;
                    lock (artifacts_lock)
                    {
                        var aa = AddArtifiact(q, artifacts);
                        package_count += aa.Key.Count();
                        artifact_count += aa.Value.Count();
                    }
                }
                catch (AggregateException ae)
                {
                    this.AuditEnvironment.Error(caller, ae, "Exception thrown attempting to search for artifacts.");
                }
            });
            sw.Stop();
            this.AuditEnvironment.Success("Found {0} artifacts from OSS Index in {1} ms.", artifact_count, sw.ElapsedMilliseconds);
            return new Tuple<int, int>(package_count, artifact_count);
        }

        protected async Task<Tuple<int, int>> GetArtifactsAsync()
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            object artifacts_lock = new object();
            int package_count = 0, artifact_count = 0;
            int i = 0;
            IGrouping<int, OSSIndexQueryObject>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            foreach (IGrouping<int, OSSIndexQueryObject> group in packages_groups)
            {
                IEnumerable<OSSIndexQueryObject> query = packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g);
                try
                {
                    IEnumerable<OSSIndexArtifact> artifacts = await this.HttpClient.SearchAsync(this.PackageManagerId, query, this.ArtifactsTransform).ConfigureAwait(false);
                    var aa = AddArtifiact(query, artifacts);
                    package_count += aa.Key.Count();
                    artifact_count += aa.Value.Count();
                }
                catch (AggregateException ae)
                {
                    this.AuditEnvironment.Error(caller, ae, "Exception thrown attempting to search for artifacts.");
                }
            }
            this.AuditEnvironment.Info("Got {0} artifacts for {1} modules or plugins in {2} ms.", artifact_count, package_count, this.Stopwatch.ElapsedMilliseconds);
            return new Tuple<int, int>(package_count, artifact_count);
        }


        protected void GetVulnerabilties()
        {
            this.AuditEnvironment.Status("Searching OSS Index for vulnerabilities for {0} artifacts.", this.Artifacts.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.GetVulnerabilitiesExceptions = new ConcurrentDictionary<OSSIndexArtifact, Exception>(5, this.Artifacts.Count());
            Parallel.ForEach(this.ArtifactsWithProjects, new ParallelOptions() { MaxDegreeOfParallelism = 30, TaskScheduler = TaskScheduler.Default },(artifact) =>
            {
                List<OSSIndexPackageVulnerability> package_vulnerabilities = null;//this.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId).Result;
                OSSIndexProject project = null; // this.HttpClient.GetProjectForIdAsync(artifact.ProjectId).Result;

                try
                {
                    Parallel.Invoke(() => package_vulnerabilities = this.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId).Result, () => project = this.HttpClient.GetProjectForIdAsync(artifact.ProjectId).Result);
                }
                catch (AggregateException e)
                {
                    this.GetVulnerabilitiesExceptions.TryAdd(artifact, e.InnerException);
                }
                project.Artifact = artifact;
                project.Package = artifact.Package;
                lock (artifact_project_lock)
                {
                    if (!ArtifactProject.Keys.Any(a => a.ProjectId == project.Id.ToString()))
                    {
                        this._ArtifactProject.Add(Artifacts.Where(a => a.ProjectId == project.Id.ToString()).First(), project);
                    }
                }
                IEnumerable<OSSIndexProjectVulnerability> project_vulnerabilities = null;
                try
                {
                    project_vulnerabilities = this.HttpClient.GetVulnerabilitiesForIdAsync(project.Id.ToString()).Result;
                }
                catch (AggregateException ae)
                {
                    this.GetVulnerabilitiesExceptions.TryAdd(artifact, ae.InnerException);
                }
                this.AddPackageVulnerability(artifact.Package, package_vulnerabilities);
                this.AddProjectVulnerability(project, project_vulnerabilities);
                this.AuditEnvironment.Info("Found {0} project vulnerabilities and {1} package vulnerabilities for package artifact {2}.", project_vulnerabilities.Count(), package_vulnerabilities.Count, artifact.PackageName);
            });
            sw.Stop();
            this.AuditEnvironment.Success("Found {0} total vulnerabilities for {1} artifacts in {2} ms with errors searching vulnerabilities for {3} artifacts.", this.PackageVulnerabilities.Count + this.ProjectVulnerabilities.Count, this.ArtifactsWithProjects.Count(), sw.ElapsedMilliseconds, this.GetVulnerabilitiesExceptions.Count);
        }

        protected async Task GetVulnerabiltiesAsync()
        {
            this.AuditEnvironment.Status("Searching OSS Index for vulnerabilities for {0} artifacts.", this.Artifacts.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.GetVulnerabilitiesExceptions = new ConcurrentDictionary<OSSIndexArtifact, Exception>(5, this.Artifacts.Count());
            foreach( var artifact in this.ArtifactsWithProjects) 
            {
                List<OSSIndexPackageVulnerability> package_vulnerabilities = await this.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId);
                OSSIndexProject project = await this.HttpClient.GetProjectForIdAsync(artifact.ProjectId);
                project.Artifact = artifact;
                project.Package = artifact.Package;
                lock (artifact_project_lock)
                {
                    if (!ArtifactProject.Keys.Any(a => a.ProjectId == project.Id.ToString()))
                    {
                        this._ArtifactProject.Add(Artifacts.Where(a => a.ProjectId == project.Id.ToString()).First(), project);
                    }
                }
                IEnumerable<OSSIndexProjectVulnerability> project_vulnerabilities = null;
                try
                {
                    project_vulnerabilities = await this.HttpClient.GetVulnerabilitiesForIdAsync(project.Id.ToString());
                }
                catch (AggregateException ae)
                {
                    this.GetVulnerabilitiesExceptions.TryAdd(artifact, ae.InnerException);
                }
                this.AddPackageVulnerability(artifact.Package, package_vulnerabilities);
                this.AddProjectVulnerability(project, project_vulnerabilities);
                this.AuditEnvironment.Info("Found {0} project vulnerabilities and {1} package vulnerabilities for package artifact {2}.", project_vulnerabilities.Count(), package_vulnerabilities.Count, artifact.PackageName);
            }
            sw.Stop();
            this.AuditEnvironment.Success("Found {0} total vulnerabilities for {1} artifacts in {2} ms with errors searching vulnerabilities for {3} artifacts.", this.PackageVulnerabilities.Count + this.ProjectVulnerabilities.Count, this.ArtifactsWithProjects.Count(), sw.ElapsedMilliseconds, this.GetVulnerabilitiesExceptions.Count);
        }

        protected void EvaluateProjectVulnerabilities()
        {
            Parallel.ForEach(this.ProjectVulnerabilities, (pv) =>
            {
                Parallel.ForEach(pv.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v), (vulnerability) =>
                {
                   try
                   {
                       if (vulnerability.Versions.Any(version => !string.IsNullOrEmpty(version) && this.IsVulnerabilityVersionInPackageVersionRange(version, pv.Key.Package.Version)))
                       {
                           vulnerability.CurrentPackageVersionIsInRange = true;

                       }
                   }
                   catch (Exception e)
                   {
                       this.AuditEnvironment.Warning("Error determining vulnerability version range ({0}) in package version range ({1}). Message: {2}",
                           vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), pv.Key.Package.Version, e.Message);
                   }
                });
            });
        }

        protected void EvaluatePackageVulnerabilities()
        {
            Parallel.ForEach(this.PackageVulnerabilities, (pv) =>
            {
                Parallel.ForEach(pv.Value, (vulnerability) =>
                {
                    try
                    {
                        if (vulnerability.Versions.Any(version => !string.IsNullOrEmpty(version) && this.IsVulnerabilityVersionInPackageVersionRange(version, pv.Key.Version)))
                        {
                            vulnerability.CurrentPackageVersionIsInRange = true;
                        }
                    }
                    catch (Exception e)
                    {
                        this.AuditEnvironment.Error(e, "Error determining vulnerability version range ({0}) in package version range ({1}).",
                            vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), pv.Key.Version);
                    }
                });
            });
        }

        #endregion

        #region Public abstract methods
        public abstract IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o);
        public abstract bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version);
        #endregion
        
        #region Private properties
        private string ProjectVulnerabilitiesCacheFile { get; set; }

        private Task<BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> ProjectVulnerabilitiesCacheInitialiseTask { get; set; }

        private BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>> ProjectVulnerabilitiesCache
        {
            get
            {
                if (this._ProjectVulnerabilitiesCache == null)
                {
                    this.ProjectVulnerabilitiesCacheInitialiseTask.Wait();
                    this._ProjectVulnerabilitiesCache = this.ProjectVulnerabilitiesCacheInitialiseTask.Result;

                }
                return this._ProjectVulnerabilitiesCache;
            }
        }
        #endregion

        #region Private fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object(), project_vulnerabilities_cache_lock = new object(), artifact_project_lock = new object();
        private BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>
            _ProjectVulnerabilitiesCache = null;
        private Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery =
            new Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>();
        private Dictionary<OSSIndexArtifact, OSSIndexProject> _ArtifactProject = new Dictionary<OSSIndexArtifact, OSSIndexProject>();
        private List<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>> _ArtifactsTask;
        private Task<Tuple<int, int>> _ArtifactsTask2;
        private Task<IEnumerable<OSSIndexQueryObject>> _PackagesTask;
        private Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>> _VulnerabilitiesForPackage =
            new Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>();
        private Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> _VulnerabilitiesForProject =
            new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>();
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> _VulnerabilitiesTask;
        #endregion
     
        #region Private methods
        private async Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>> AddArtifiactAsync(IEnumerable<OSSIndexQueryObject> query, Task<IEnumerable<OSSIndexArtifact>> artifact_task)
        {
            var artifact = await artifact_task;
            lock (artifacts_lock)
            {
                this._ArtifactsForQuery.Add(query, artifact);
                return new KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>(query, artifact);
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

        private KeyValuePair<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>
           AddPackageVulnerability(OSSIndexQueryObject package, IEnumerable<OSSIndexPackageVulnerability> vulnerability)
        {
            lock (vulnerabilities_lock)
            {
                this._VulnerabilitiesForPackage.Add(package, vulnerability);
                return new KeyValuePair<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>(package, vulnerability);
            }
        }

        private KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>
            AddProjectVulnerability(OSSIndexProject project, IEnumerable<OSSIndexProjectVulnerability> vulnerability)
        {
            lock (vulnerabilities_lock)
            {
                this._VulnerabilitiesForProject.Add(project, vulnerability);
                
                return new KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>(project, vulnerability);
            }
        }

        private BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>> InitialiseProjectVulnerabilitiesCache(string file)
        {
            lock (project_vulnerabilities_cache_lock)
            {
                BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>.OptionsV2 cache_file_options =
                    new BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>.OptionsV2(PrimitiveSerializer.String,
                    new BsonSerializer<Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>());
                cache_file_options.CalcBTreeOrder(4, 128);
                cache_file_options.CreateFile = CreatePolicy.IfNeeded;
                cache_file_options.FileName = file;
                cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
                var c = new BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>(cache_file_options);
                c.EnableCount();
                IEnumerable<string> expired_cache_keys =
                    from cache_key in c.Keys
                    where DateTime.UtcNow.Subtract(GetProjectVulnerabilitiesCacheEntry(cache_key).Item2) >= this.ProjectVulnerabilitiesCacheTTL
                    join artifact in ArtifactsWithProjects
                    on GetProjectVulnerabilitiesCacheEntry(cache_key).Item1 equals artifact.ProjectId
                    select cache_key;
                this.ProjectVulnerabilitiesExpiredCacheKeys = expired_cache_keys;
                foreach (string k in expired_cache_keys)
                {
                    if (!c.Remove(k)) throw new Exception("Error removing expired cache item with key: " + k + ".");
                }
                return c;
            }
                        
        }

        private string GetProjectVulnerabilitiesCacheKey(long project_id)
        {
            return project_id.ToString() + "_" + DateTime.UtcNow.Ticks.ToString();
        }

        private Tuple<string, DateTime> GetProjectVulnerabilitiesCacheEntry(string cache_key)
        {
            string[] result = cache_key.Split('_');
            if (result.Length != 2) throw new ArgumentException("Invalid cache key.");
            string id = result[0];
            long ticks = 0;
            Int64.TryParse(result[1], out ticks);
            if (ticks == 0) throw new ArgumentException("Invalid cache key: could not parse ticks value " + result[1] + ".");
            return new Tuple<string, DateTime>(id, new DateTime(ticks));
        }

        private int DeleteProjectVulnerabilitiesExpired()
        {
            if (ProjectVulnerabilitiesCacheEnabled)
            {
                IEnumerable<string> expired_cache_keys =
                    from cache_key in ProjectVulnerabilitiesCache.Keys
                    where DateTime.UtcNow.Subtract(GetProjectVulnerabilitiesCacheEntry(cache_key).Item2) >= this.ProjectVulnerabilitiesCacheTTL
                    join artifact in ArtifactsWithProjects
                    on GetProjectVulnerabilitiesCacheEntry(cache_key).Item1 equals artifact.ProjectId
                    select cache_key;
                this.ProjectVulnerabilitiesExpiredCacheKeys = expired_cache_keys;
                foreach (string k in expired_cache_keys)
                {
                    if (!ProjectVulnerabilitiesCache.Remove(k)) throw new Exception("Error removing expired cache item with key: " + k + ".");
                }
                return expired_cache_keys.Count();
                    
            }
            else
            {
                throw new Exception("Project vulnerabilities cache is not enabled.");
            }
            
        }
        #endregion

        #region Disposer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true); // This object will be cleaned up by the Dispose method. // Therefore, you should call GC.SupressFinalize to // take this object off the finalization queue // and prevent finalization code for this object // from executing a second time. // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
            try
            {
                if (!this.IsDisposed)
                {
                    // Explicitly set root references to null to expressly tell the GarbageCollector 
                    // that the resources have been disposed of and its ok to release the memory 
                    // allocated for them. 
                    if (isDisposing)
                    {
                        // Release all managed resources here 
                        // Need to unregister/detach yourself from the events. Always make sure 
                        // the object is not null first before trying to unregister/detach them! 
                        // Failure to unregister can be a BIG source of memory leaks 
                        //if (someDisposableObjectWithAnEventHandler != null)
                        //{ someDisposableObjectWithAnEventHandler.SomeEvent -= someDelegate; 
                        //someDisposableObjectWithAnEventHandler.Dispose(); 
                        //someDisposableObjectWithAnEventHandler = null; } 
                        // If this is a WinForm/UI control, uncomment this code 
                        //if (components != null) //{ // components.Dispose(); //} } 
                        // Release all unmanaged resources here 
                        // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 

                        /*
                        if (PackagesTask.Status == TaskStatus.RanToCompletion ||
                            PackagesTask.Status == TaskStatus.Faulted || PackagesTask.Status == TaskStatus.Canceled)
                        {

                            PackagesTask.Dispose();
                        }
                        if (_ArtifactsTask != null)
                        {
                            foreach (Task t in this._ArtifactsTask.Where(t => t.Status == TaskStatus.RanToCompletion ||
                            t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled))
                            {
                                t.Dispose();
                            }
                            this._ArtifactsTask = null;
                        }

                        if (_VulnerabilitiesTask != null)
                        {
                            foreach (Task t in this._VulnerabilitiesTask.Where(t => t.Status == TaskStatus.RanToCompletion ||
                         t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled))
                            {
                                t.Dispose();
                            }
                            this._VulnerabilitiesTask = null;
                        }
                        if (this.ProjectVulnerabilitiesCacheInitialiseTask != null)
                        {
                            this.ProjectVulnerabilitiesCacheInitialiseTask.Dispose();
                            this.ProjectVulnerabilitiesCacheInitialiseTask = null;
                        }
                        if (this._ProjectVulnerabilitiesCache != null)
                        {
                            this.ProjectVulnerabilitiesCache.Dispose();
                            this._ProjectVulnerabilitiesCache = null;
                        }
                        */
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }
        #endregion
    }
}
