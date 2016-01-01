using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinAudit.AuditLibrary
{
    public abstract class PackageSource
    {
        #region Public properties
        public abstract OSSIndexHttpClient HttpClient { get;  }

        public abstract string PackageManagerId { get; }

        public abstract string PackageManagerLabel { get; }

        public string PackageManagerConfigurationFile { get; set; }

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
                return this._ArtifactsForQuery.Values.SelectMany(a=> a);
            }
        }

        public Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities
        {
            get
            {
                return _VulnerabilitiesForProject;
            }
        }

        public Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask
        {
            get
            {
                if (_GetPackagesTask == null)
                {
                    _GetPackagesTask = Task<IEnumerable<OSSIndexQueryObject>>.Run(() => this.Packages = 
                    this.GetPackages().GroupBy(x => new { x.Name, x.Version, x.Vendor }).Select(y => y.First()));
                }
                return _GetPackagesTask;
            }
        }
 
        public IEnumerable<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>> GetArtifactsTask
        {
            get
            {
                if (_GetArtifactsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
                    _GetArtifactsTask = new List<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>>(packages_groups.Count());
                    for (int index = 0; index < packages_groups.Count(); index++)
                    {
                        IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == index).SelectMany(g => g).ToList();
                        Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>> t
                            = Task<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>>.Factory.StartNew(async (o) =>
                            {
                                IEnumerable<OSSIndexQueryObject> query = o as IEnumerable<OSSIndexQueryObject>;
                                return AddArtifiact(query, await this.HttpClient.SearchAsync(this.PackageManagerId, query));
                            }, f, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
                        _GetArtifactsTask.Add(t);
                        
                    } 
                }
                return this._GetArtifactsTask;
            }
        }

        public List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> GetVulnerabilitiesTask
        {
            get
            {
                if (_GetVulnerabilitiesTask == null)
                {
                    this._GetVulnerabilitiesTask =
                        new List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>>
                            (this.Artifacts.Count(a => !string.IsNullOrEmpty(a.ProjectId)));
                    this.Artifacts.ToList().Where(a => !string.IsNullOrEmpty(a.ProjectId)).ToList()
                        .ForEach(p => this._GetVulnerabilitiesTask.Add(Task<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>>
                            .Factory.StartNew(async (o) =>
                                {
                                    OSSIndexArtifact artifact = o as OSSIndexArtifact;
                                    OSSIndexProject project = await this.HttpClient.GetProjectForIdAsync(artifact.ProjectId);
                                    IEnumerable<OSSIndexProjectVulnerability> v = await this.HttpClient.GetVulnerabilitiesForIdAsync(project.Id.ToString());
                                    return this.AddVulnerability(project, v);
                                },
                                p, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap()));
                                
                     
                }
                return this._GetVulnerabilitiesTask;
            }
        }
        #endregion

        #region Public abstract methods
        public abstract IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o);
        #endregion

        #region Constructors
        #endregion

        #region Private fields
        private readonly object artifacts_lock = new object(), vulnerabilities_lock = new object();
        private Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>> _ArtifactsForQuery = 
            new Dictionary<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>();
        private List<Task<KeyValuePair<IEnumerable<OSSIndexQueryObject>, IEnumerable<OSSIndexArtifact>>>> _GetArtifactsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _GetPackagesTask;
        private Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> _VulnerabilitiesForProject =
            new Dictionary<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>();
        private List<Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>> _GetVulnerabilitiesTask;
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
                return new KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>(project, vulnerability);
            }
        }

        #endregion
    }
}
