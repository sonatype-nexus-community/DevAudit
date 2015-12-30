using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinAudit.AuditLibrary
{
    public abstract class PackageSource
    {
        #region Public properties
        public abstract OSSIndexHttpClient HttpClient { get;  }

        public abstract string PackageManagerId { get; }

        public abstract string PackageManagerLabel { get; }

        public IEnumerable<OSSIndexQueryObject> Packages { get; set; }

        public IEnumerable<OSSIndexArtifact> Artifacts { get; set; }

        public ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities { get; set; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>>();

        public Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask
        {
            get
            {
                if (_GetPackagesTask == null)
                {

                    _GetPackagesTask = Task<IEnumerable<OSSIndexQueryObject>>.Run(() => this.Packages = this.GetPackages());
                }
                return _GetPackagesTask;
            }
        }
 
        public Task<IEnumerable<OSSIndexArtifact>> GetArtifactsTask
        {
            get
            {
                if (_GetProjectsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
                    IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == 0).SelectMany(g => g);
                    _GetProjectsTask = Task<IEnumerable<OSSIndexArtifact>>.Run(async () =>
                this.Artifacts = await this.HttpClient.SearchAsync(this.PackageManagerId, f));
                }
                return _GetProjectsTask;
            }
        }

        public Task<IEnumerable<OSSIndexProjectVulnerability>>[] GetVulnerabilitiesTask
        {
            get
            {
                if (_GetVulnerabilitiesTask == null)
                {
                    Func<Task<IEnumerable<OSSIndexProjectVulnerability>>> getFunc = async () =>
                    {
                        OSSIndexProject p = await this.HttpClient.GetProjectForIdAsync("284089289");
                        return this.Vulnerabilities.AddOrUpdate(p.Id.ToString(),
                            await this.HttpClient.GetVulnerabilitiesForIdAsync(p.Id.ToString()), (k, v) => v);
                    };

                    List<Task<IEnumerable<OSSIndexProjectVulnerability>>> tasks =
                        new List<Task<IEnumerable<OSSIndexProjectVulnerability>>>(this.Artifacts.Count(p => !string.IsNullOrEmpty(p.ProjectId)));
                    this.Artifacts.ToList().Where(p => !string.IsNullOrEmpty(p.ProjectId)).ToList()
                        .ForEach(p => tasks.Add(Task<IEnumerable<OSSIndexProject>>
                        .Run(async () => await this.HttpClient.GetProjectForIdAsync(p.ProjectId))
                        .ContinueWith(async (antecedent) => (this.Vulnerabilities.AddOrUpdate(antecedent.Result.Id.ToString(),
                            await this.HttpClient.GetVulnerabilitiesForIdAsync(antecedent.Result.Id.ToString()), (k, v) => v)), TaskContinuationOptions.OnlyOnRanToCompletion)
                            .Unwrap()));
                    this._GetVulnerabilitiesTask = tasks.ToArray(); ;
                }
                return this._GetVulnerabilitiesTask;
            }
        }
        #endregion

        #region Public abstract methods
        public abstract IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o);
        #endregion

        #region Private fields
        private Task<IEnumerable<OSSIndexArtifact>> _GetProjectsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _GetPackagesTask;
        private Task<IEnumerable<OSSIndexProjectVulnerability>>[] _GetVulnerabilitiesTask;
        #endregion
    }
}
