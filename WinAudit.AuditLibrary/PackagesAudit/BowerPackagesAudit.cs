using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinAudit.AuditLibrary
{
    public class BowerPackagesAudit : IPackagesAudit
    {
        public OSSIndexHttpClient HttpClient { get; set; }

        public string PackageManagerId { get { return "bower"; } }

        public string PackageManagerLabel { get { return "Bower"; } }

        public Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask
        { get
            {
                if (_GetPackagesTask == null)
                {

                    _GetPackagesTask = Task<IEnumerable<OSSIndexQueryObject>>.Run(() => this.Packages = this.GetPackages());
                }
                return _GetPackagesTask;
            }
        }
        public IEnumerable<OSSIndexQueryObject> Packages { get; set; }

        public IEnumerable<OSSIndexArtifact> Artifacts { get; set; }

        public ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities { get; set; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>>();

        public Task<IEnumerable<OSSIndexArtifact>> GetArtifactsTask
        {
            get
            {
                if (_GetProjectsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 10).ToArray();
                    IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == 1).SelectMany(g => g);
                        _GetProjectsTask = Task<IEnumerable<OSSIndexArtifact>>.Run(async () =>
                    this.Artifacts = await this.HttpClient.SearchAsync("bower", f));
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
                    List<Task<IEnumerable<OSSIndexProjectVulnerability>>> tasks =
                        new List<Task<IEnumerable<OSSIndexProjectVulnerability>>>(this.Artifacts.Count(p => !string.IsNullOrEmpty(p.ProjectId)));
                    this.Artifacts.ToList().Where(p => !string.IsNullOrEmpty(p.ProjectId)).ToList()
                        .ForEach(p => tasks.Add(Task<IEnumerable<OSSIndexProjectVulnerability>>
                        .Run(async () => this.Vulnerabilities.AddOrUpdate(p.ProjectId, await this.HttpClient.GetVulnerabilitiesForIdAsync(p.ProjectId),
                        (k, v) => v))));
                    this._GetVulnerabilitiesTask = tasks.ToArray(); ;
                }
                return this._GetVulnerabilitiesTask;
            }
        }

        //Get bower packages from reading packages.json
        public IEnumerable<OSSIndexQueryObject> GetPackages(string file_name = @".\bower.json")
        {
            if (!File.Exists(file_name)) file_name = @".\bower.json.example";
            using (JsonTextReader r = new JsonTextReader(new StreamReader(
                        file_name)))
            {
                JObject json = (JObject)JToken.ReadFrom(r);
                JObject dependencies = (JObject)json["dependencies"];
                return dependencies.Properties().Select(d => new OSSIndexQueryObject("bower", d.Name, d.Value.ToString(), ""));
            }
        }

        #region Constructors
        public BowerPackagesAudit()
        {
            this.HttpClient = new OSSIndexHttpClient("1.1");            
        }
        #endregion

        #region Private fields
        private Task<IEnumerable<OSSIndexArtifact>> _GetProjectsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _GetPackagesTask;
        private Task<IEnumerable<OSSIndexProjectVulnerability>>[] _GetVulnerabilitiesTask;
        #endregion

    }
}
