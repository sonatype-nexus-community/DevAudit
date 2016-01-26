using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;

namespace WinAudit.AuditLibrary
{
    public abstract class PackageSource
    {
        #region Public properties
        public abstract OSSIndexHttpClient HttpClient { get; }

        public abstract string PackageManagerId { get; }

        public abstract string PackageManagerLabel { get; }

        public string PackageManagerConfigurationFile { get; set; }

        public bool ProjectVulnerabilitiesCacheEnabled { get; set; }

        public TimeSpan ProjectVulnerabilitiesCacheTTL { get; set; }

        public IEnumerable<Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>> ProjectVulnerabilitiesCacheItems
        {
            get
            {
                if (ProjectVulnerabilitiesCacheEnabled)
                {
                    IEnumerable<string> cache_keys = ProjectVulnerabilitiesCache
                        .Keys
                        .Where(k => DateTime.UtcNow.Subtract(GetProjectVulnerabilitiesCacheEntry(k).Item2) > this.ProjectVulnerabilitiesCacheTTL);
                    
                    return 
                        from pvc in this.ProjectVulnerabilitiesCache
                        join k in cache_keys
                        on pvc.Key equals k
                        select pvc.Value;
                }
                else
                {
                    return null;
                }
            }
        }
        
        public Dictionary<string, object> PackageSourceOptions { get; set; } = new Dictionary<string, object>();

        public IEnumerable<OSSIndexQueryObject> Packages { get; set; }

        public Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> ArtifactsForQuery
        {
            get
            {
                return _ArtifactsForQuery;
            }
        }

        public IEnumerable<OSSIndexArtifact> Artifacts
        {
            get
            {
                return this._ArtifactsForQuery.Values.SelectMany(a => a);
            }
        }

        public abstract Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; }

        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities
        {
            get
            {
                return _VulnerabilitiesForProject;
            }
        }

        public Task<IEnumerable<OSSIndexQueryObject>> PackagesTask
        {
            get
            {
                if (_PackagesTask == null)
                {
                    _PackagesTask = Task<IEnumerable<OSSIndexQueryObject>>.Run(() => this.Packages =
                    this.GetPackages().GroupBy(x => new { x.Name, x.Version, x.Vendor }).Select(y => y.First()));
                }
                return _PackagesTask;
            }
        }

        public IEnumerable<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>> ArtifactsTask
        {
            get
            {
                if (_ArtifactsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
                    _ArtifactsTask = new List<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>,
                        IEnumerable<OSSIndexArtifact>>>>(packages_groups.Count());
                    for (int index = 0; index < packages_groups.Count(); index++)
                    {
                        IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == index).SelectMany(g => g).ToList();
                        Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>> t
                            = Task<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>>.Factory
                                .StartNew(async (o) =>
                                {
                                    IEnumerable<OSSIndexQueryObject> query = o as IEnumerable<OSSIndexQueryObject>;
                                    return AddArtifiact(query, await this.HttpClient.SearchAsync(this.PackageManagerId, query,
                                        this.ArtifactsTransform));
                                }, f, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                        _ArtifactsTask.Add(t);

                    }
                }
                return this._ArtifactsTask;
            }
        }

        public List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> VulnerabilitiesTask
        {
            get
            {
                if (_VulnerabilitiesTask == null)
                {
                    this._VulnerabilitiesTask =
                        new List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>>
                            (this.Artifacts.Count(a => !string.IsNullOrEmpty(a.ProjectId)));
                    this.Artifacts.ToList().Where(a => !string.IsNullOrEmpty(a.ProjectId)).ToList()
                        .ForEach(p => this._VulnerabilitiesTask.Add(Task<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>>
                            .Factory.StartNew(async (o) =>
                                {
                                    OSSIndexArtifact artifact = o as OSSIndexArtifact;
                                    OSSIndexProject project = await this.HttpClient.GetProjectForIdAsync(artifact.ProjectId);
                                    project.Package = artifact.Package;
                                    IEnumerable<OSSIndexProjectVulnerability> v = await this.HttpClient.GetVulnerabilitiesForIdAsync(project.Id.ToString());
                                    return this.AddVulnerability(project, v);

                                },
                                p, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap()));


                }
                return this._VulnerabilitiesTask;
            }
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

        #region Constructors
        public PackageSource() { }

        public PackageSource(Dictionary<string, object> package_source_options)
        {
            this.PackageSourceOptions = package_source_options;

            if (this.PackageSourceOptions.ContainsKey("File"))
            {
                this.PackageManagerConfigurationFile = (string)this.PackageSourceOptions["File"];
                if (!File.Exists(this.PackageManagerConfigurationFile)) throw new ArgumentException("Could not find the file " + this.PackageManagerConfigurationFile + ".");
            }
            else
            {
                this.PackageManagerConfigurationFile = "";
            }

            if (this.PackageSourceOptions.ContainsKey("Cache") && (bool) this.PackageSourceOptions["Cache"] == true)
            {
                this.ProjectVulnerabilitiesCacheEnabled = true;
                if (this.PackageSourceOptions.ContainsKey("CacheFile") && !string.IsNullOrEmpty((string) this.PackageSourceOptions["CacheFile"]))
                {
                    this.ProjectVulnerabilitiesCacheFile = (string) this.PackageSourceOptions["CacheFile"];
                }
                else
                {
                    this.ProjectVulnerabilitiesCacheFile = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache";
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
        }
        #endregion

        #region Private fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object(), project_vulnerabilities_cache_lock = new object();
        private BPlusTree<string, Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>
            _ProjectVulnerabilitiesCache = null;
        private Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery =
            new Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>();
        private List<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>> _ArtifactsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _PackagesTask;
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

        private KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>
            AddVulnerability(OSSIndexProject project, IEnumerable<OSSIndexProjectVulnerability> vulnerability)
        {
            lock (vulnerabilities_lock)
            {
                this._VulnerabilitiesForProject.Add(project, vulnerability);
                if (this.ProjectVulnerabilitiesCacheEnabled)
                {
                    this.ProjectVulnerabilitiesCache[GetProjectVulnerabilitiesCacheKey(project.Id)] = new Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>
                        (project, vulnerability);
                }
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
        #endregion
    }
}
