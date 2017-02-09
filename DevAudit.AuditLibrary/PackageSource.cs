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
    public abstract class PackageSource : AuditTarget
    {
        #region Constructors
        public PackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler)
        {
            this.PackageSourceOptions = this.AuditOptions;
            if (this.PackageSourceOptions.ContainsKey("File"))
            {
                this.PackageManagerConfigurationFile = (string)this.PackageSourceOptions["File"];
                if (!this.AuditEnvironment.FileExists(this.PackageManagerConfigurationFile)) throw new ArgumentException("Could not find the file " + this.PackageManagerConfigurationFile + ".", "package_source_options");
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

        #region Abstract properties
        public abstract string PackageManagerId { get; }
        public abstract string PackageManagerLabel { get; }
        #endregion

        #region Abstract methods
        public abstract IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o);
        public abstract bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version);
        #endregion

        #region Properties
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

        public OSSIndexHttpClient HttpClient { get; protected set; } = new OSSIndexHttpClient("2.0");
                
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

        public virtual Func<List<OSSIndexQueryObject>, List<OSSIndexQueryObject>> VulnerabilitiesQueryTransform { get; } = (packages) =>
        {
            List<OSSIndexQueryObject> o = packages;
            foreach (OSSIndexQueryObject p in o)
            {
                OSSIndexQueryObject package = new OSSIndexQueryObject(p.PackageManager, p.Name, "*", p.Group);
            }
            return o.ToList();
        };
    

        public virtual Func<List<OSSIndexApiv2Result>, List<OSSIndexApiv2Result>> VulnerabilitiesResultsTransform { get; } = (results) =>
        {
            List<OSSIndexApiv2Result> o = results;
            foreach (OSSIndexApiv2Result r in o)
            {
                if (string.IsNullOrEmpty(r.PackageManager) || string.IsNullOrEmpty(r.PackageName))
                {
                    throw new Exception("Did not receive expected fields for result with id: " + r.Id);
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(r.PackageManager, r.PackageName, r.PackageVersion, "");
                    r.Package = package;
                    if (r.Vulnerabilities != null)
                    {
                        r.Vulnerabilities.ForEach(v => v.Package = package);
                    }
                    else
                    {
                        r.Vulnerabilities = new List<OSSIndexApiv2Vulnerability>();
                    }
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

        public Dictionary<OSSIndexQueryObject, List<OSSIndexApiv2Vulnerability>> Vulnerabilities
        {
            get
            {
                return this._Vulnerabilities;
            }
        }

        public ConcurrentDictionary<OSSIndexArtifact, Exception> GetVulnerabilitiesExceptions { get; protected set; }

        public Task PackagesTask { get; protected set; } 

        public Task ArtifactsTask { get; protected set; }

        public Task VulnerabilitiesTask { get; protected set; }

        public Task EvaluateVulnerabilitiesTask { get; protected set; }

        #region Cache stuff
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
        
        #endregion

        #region Methods
        public virtual Task GetPackagesTask(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            if (this.SkipPackagesAudit)
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

        public virtual AuditResult Audit(CancellationToken ct)
        {
            CallerInformation caller = this.AuditEnvironment.Here();

            this.GetPackagesTask(ct);
            try
            {
                this.PackagesTask.Wait();
                if (!this.SkipPackagesAudit) AuditEnvironment.Success("Scanned {0} {1} packages.", this.Packages.Count(), this.PackageManagerLabel);
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae, "Exception thrown in {0} method in GetPackages task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_SCANNING_PACKAGES;
            }
            
            if (this.ListPackages || this.Packages.Count() == 0)
            {
                this.ArtifactsTask = this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else if (this.ListArtifacts)
            {
                this.ArtifactsTask = Task.Run(() => this.GetArtifacts(), ct);
            }
            else
            {
                this.ArtifactsTask = Task.CompletedTask;
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
            if (this.ListPackages || this.Packages.Count() == 0 || this.ListArtifacts)
            {
                this.VulnerabilitiesTask = this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else
            {
                this.VulnerabilitiesTask = Task.Run(() => this.GetVulnerabiltiesApiv2(), ct);
            }
            try
            {
                this.VulnerabilitiesTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in GetVulnerabilities task");
                return AuditResult.ERROR_SEARCHING_VULNERABILITIES;
            }
          
          
            if (this.Vulnerabilities.Count == 0)
            {
                this.EvaluateVulnerabilitiesTask = Task.CompletedTask;
            }
            else
            {
                this.EvaluateVulnerabilitiesTask = Task.Run(() => this.EvaluateVulnerabilities(), ct);
            }
            try
            {
                this.EvaluateVulnerabilitiesTask.Wait();
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae.InnerException, "Exception thrown in {0} task.", ae.InnerException.TargetSite.Name);
                return AuditResult.ERROR_EVALUATING_VULNERABILITIES;
            }
            return AuditResult.SUCCESS;
        }
        
        protected Tuple<int, int> GetArtifacts()
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            this.AuditEnvironment.Status("Searching OSS Index for artifacts for {0} packages.", this.Packages.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            object artifacts_lock = new object();
            int package_count = 0, artifact_count = 0;
            int i = 0;
            IGrouping<int, OSSIndexQueryObject>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<OSSIndexQueryObject>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            List<Task> tasks = new List<Task>();
            foreach (IEnumerable<OSSIndexQueryObject> q in queries)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    IEnumerable<OSSIndexArtifact> artifacts = await this.HttpClient.SearchAsync(this.PackageManagerId, q, this.ArtifactsTransform);
                    lock (artifacts_lock)
                    {
                        var aa = AddArtifiact(q, artifacts);
                        package_count += aa.Key.Count();
                        artifact_count += aa.Value.Count();
                    }
                }, q, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                this.AuditEnvironment.Error(caller, "Exception thrown waiting for tasks,", e);
            }
            finally
            {
                sw.Stop();
            }
            if (artifact_count > 0)
            {
                this.AuditEnvironment.Success("Found {0} artifacts on OSS Index in {1} ms.", artifact_count, sw.ElapsedMilliseconds);
            }
            else
            {
                this.AuditEnvironment.Warning("Found 0 artifacts on OSS Index in {0} ms.", sw.ElapsedMilliseconds);
            }
            return new Tuple<int, int>(package_count, artifact_count);
        }

        protected void GetVulnerabilties()
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            this.AuditEnvironment.Status("Searching OSS Index for vulnerabilities for {0} artifacts.", this.Artifacts.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.GetVulnerabilitiesExceptions = new ConcurrentDictionary<OSSIndexArtifact, Exception>(5, this.Artifacts.Count());
            List<Task> tasks = new List<Task>();
            foreach (var artifact in this.ArtifactsWithProjects)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    OSSIndexProject project = await this.HttpClient.GetProjectForIdAsync(artifact.ProjectId);
                    List<OSSIndexPackageVulnerability> package_vulnerabilities = await this.HttpClient.GetPackageVulnerabilitiesAsync(artifact.PackageId);

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
                        this.AddPackageVulnerability(artifact.Package, package_vulnerabilities);
                        this.AddProjectVulnerability(project, project_vulnerabilities);
                        this.AuditEnvironment.Debug("Found {0} project vulnerabilities and {1} package vulnerabilities for package artifact {2}.", project_vulnerabilities.Count(), package_vulnerabilities.Count, artifact.PackageName);
                    }
                    catch (AggregateException ae)
                    {
                        this.GetVulnerabilitiesExceptions.TryAdd(artifact, ae.InnerException);
                    }
                    catch (Exception e)
                    {
                        this.GetVulnerabilitiesExceptions.TryAdd(artifact, e.InnerException);
                    }
                }, artifact, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae, "Exception thrown waiting for Http task to complete in {0}.", ae.InnerException.TargetSite.Name);
            }
            finally
            {
                sw.Stop();
            }
            this.AuditEnvironment.Success("Found {0} total vulnerabilities for {1} artifacts in {2} ms with errors searching vulnerabilities for {3} artifacts.", this.PackageVulnerabilities.Sum(kv => kv.Value.Count()) + this.ProjectVulnerabilities.Sum(kv => kv.Value.Count()), this.ArtifactsWithProjects.Count(), sw.ElapsedMilliseconds, this.GetVulnerabilitiesExceptions.Count);
        }

        protected void GetVulnerabiltiesApiv2()
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            this.AuditEnvironment.Status("Searching OSS Index for vulnerabilities for {0} packages.", this.Packages.Count());
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int i = 0;
            IGrouping<int, OSSIndexQueryObject>[] packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
            IEnumerable<OSSIndexQueryObject>[] queries = packages_groups.Select(group => packages_groups.Where(g => g.Key == group.Key).SelectMany(g => g)).ToArray();
            List<Task> tasks = new List<Task>();
            foreach (IEnumerable<OSSIndexQueryObject> q in queries)
            {
                Task t = Task.Factory.StartNew(async (o) =>
                {
                    try
                    { 
                        List<OSSIndexApiv2Result> results = await this.HttpClient.SearchVulnerabilitiesAsync(q, this.VulnerabilitiesResultsTransform);
                        foreach (OSSIndexApiv2Result r in results)
                        {
                            if (r.Vulnerabilities != null && r.Vulnerabilities.Count > 0)
                            {
                                this.AddVulnerability(r.Package, r.Vulnerabilities);
                            }
                        }
                    }            
                    catch (Exception e)
                    {
                        if (e is OSSIndexHttpException)
                        {
                            this.AuditEnvironment.Error(caller, e, "An HTTP error occured attempting to query the OSS Index API for the following {1} packages: {0}.",
                                q.Select(query => query.Name).Aggregate((q1, q2) => q1 + "," + q2), this.PackageManagerLabel);
                        }
                        else
                        {
                            this.AuditEnvironment.Error(caller, e, "An error occurred attempting to query the OSS Index API for the following {1} packages: {0}.",
                                q.Select(query => query.Name).Aggregate((q1, q2) => q1 + "," + q2), this.PackageManagerLabel);

                        }
                    }
                }, i, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                tasks.Add(t);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                this.AuditEnvironment.Error(caller, ae, "Exception thrown waiting for SearchVulnerabilitiesAsync task to complete in {0}.", ae.InnerException.TargetSite.Name);
            }
            finally
            {
                sw.Stop();
            }
            if (this.Vulnerabilities.Sum(pv => pv.Value.Count()) > 0)
            {
                this.AuditEnvironment.Success("Found {0} vulnerabilities for {1} package(s) on OSS Index in {2} ms.", this.Vulnerabilities
                    .Sum(pv => pv.Value.Count()), this.Packages.Count(), sw.ElapsedMilliseconds);
            }
            else
            {
                this.AuditEnvironment.Warning("Found 0 vulnerabilities for {0} package(s) on OSS Index in {1} ms.", 
                    this.Packages.Count(), sw.ElapsedMilliseconds);
            }
            return;

        }

        protected void EvaluateProjectVulnerabilities()
        {
            if (this.ProjectVulnerabilities.Count == 0 || this.ProjectVulnerabilities.Sum(kv => kv.Value.Count()) == 0) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.ProjectVulnerabilities.AsParallel().ForAll((pv) =>
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
            sw.Stop();
            this.AuditEnvironment.Info("Evaluated {0} project vulnerabilities in {1} ms.", this.ProjectVulnerabilities.Sum(kv => kv.Value.Count()), sw.ElapsedMilliseconds);
        }

        protected void EvaluatePackageVulnerabilities()
        {
            if (this.PackageVulnerabilities.Count == 0 || this.PackageVulnerabilities.Sum(kv => kv.Value.Count()) == 0) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.PackageVulnerabilities.AsParallel().ForAll((pv) =>
            {
                pv.Value.AsParallel().ForAll((vulnerability) =>
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
            sw.Stop();
            this.AuditEnvironment.Info("Evaluated {0} package vulnerabilities in {1} ms.", this.PackageVulnerabilities
                .Sum(pv => pv.Value.Count())  , sw.ElapsedMilliseconds);
        }

        protected void EvaluateVulnerabilities()
        {
            if (this.Vulnerabilities.Count == 0 || this.Vulnerabilities.Sum(kv => kv.Value.Count()) == 0) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            this.Vulnerabilities.AsParallel().ForAll((pv) =>
            {
                pv.Value.AsParallel().ForAll((vulnerability) =>
                {
                    try
                    {
                        List<OSSIndexQueryObject> packages = this.Packages.Where(p => p.PackageManager == vulnerability.Package.PackageManager && p.Name == vulnerability.Package.Name).ToList();
                        foreach (OSSIndexQueryObject p in packages)
                        {
                            if (vulnerability.Versions.Any(version => !string.IsNullOrEmpty(version) && this.IsVulnerabilityVersionInPackageVersionRange(version, p.Version)))
                            {
                                vulnerability.CurrentPackageVersionIsInRange = true;
                                vulnerability.Package = p;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.AuditEnvironment.Warning("Error determining vulnerability version range ({0}) in package version range ({1}): {2}.",
                            vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), pv.Key.Version, e.Message);
                    }
                });
            });
            sw.Stop();
            this.AuditEnvironment.Info("Evaluated {0} vulnerabilities with {1} matches to package version in {2} ms.", this.Vulnerabilities
                .Sum(pv => pv.Value.Count()), this.Vulnerabilities
                .Sum(pv => pv.Value.Count(v => v.CurrentPackageVersionIsInRange)), sw.ElapsedMilliseconds);
        }

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

        private void AddVulnerability(OSSIndexQueryObject package, List<OSSIndexApiv2Vulnerability> vulnerabilities)
        {
            lock (vulnerabilities_lock)
            {
                this._Vulnerabilities.Add(package, vulnerabilities);
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

        #region Fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object(), project_vulnerabilities_cache_lock = new object(), artifact_project_lock = new object();
        private BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>
            _ProjectVulnerabilitiesCache = null;
        private Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery =
            new Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>();
        private Dictionary<OSSIndexArtifact, OSSIndexProject> _ArtifactProject = new Dictionary<OSSIndexArtifact, OSSIndexProject>();
        private Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>> _VulnerabilitiesForPackage =
            new Dictionary<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>>();
        private Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> _VulnerabilitiesForProject =
            new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>();
        private Dictionary<OSSIndexQueryObject, List<OSSIndexApiv2Vulnerability>> _Vulnerabilities = new Dictionary<OSSIndexQueryObject, List<OSSIndexApiv2Vulnerability>>();
        #endregion

    }
}
