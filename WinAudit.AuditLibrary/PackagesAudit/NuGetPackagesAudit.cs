using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinAudit.AuditLibrary
{
    public class NuGetPackagesAudit : IPackagesAudit
    {
        public OSSIndexHttpClient HttpClient { get; set; }

        public string PackageManagerId { get { return "nuget"; } }

        public string PackageManagerLabel { get { return "NuGet"; } }

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

        public IEnumerable<OSSIndexQueryResultObject> Projects { get; set; }

        public ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities { get; set; }

        public Task<IEnumerable<OSSIndexQueryResultObject>> GetProjectsTask
        {
            get
            {
                if (_GetProjectsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 100).ToArray();
                    IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == 0).SelectMany(g => g);
                        _GetProjectsTask = Task<IEnumerable<OSSIndexQueryResultObject>>.Run(async () =>
                    this.Projects = await this.HttpClient.SearchAsync("nuget", f));
                }
                return _GetProjectsTask;
            }
        }

        public Task<IEnumerable<OSSIndexProjectVulnerability>>[] GetVulnerabilitiesTask
        {                       
            get
            {
                List<Task<IEnumerable<OSSIndexProjectVulnerability>>> tasks = 
                    new List<Task<IEnumerable<OSSIndexProjectVulnerability>>>(this.Projects.Count(p => !string.IsNullOrEmpty(p.ProjectId)));
                this.Projects.ToList().Where(p => !string.IsNullOrEmpty(p.ProjectId)).ToList()
                    .ForEach(p => tasks.Add(Task<IEnumerable<OSSIndexProjectVulnerability>>
                    .Run(async () => await this.HttpClient.GetVulnerabilitiesForIdAsync(p.ProjectId) )));
                return tasks.ToArray();
            }
        }
            //Get NuGet packages from reading packages.config
        public IEnumerable<OSSIndexQueryObject> GetPackages(string packages_config_location = null)
        {
            string file = string.IsNullOrEmpty(packages_config_location) ? AppDomain.CurrentDomain.BaseDirectory + @"\packages.config.example" : packages_config_location;
            if (!File.Exists(file))
                throw new ArgumentException("Invalid file location or file not found: " + file);
            try
            {
                XElement root = XElement.Load(file);
                IEnumerable<OSSIndexQueryObject> packages =
                    from el in root.Elements("package")
                    select new OSSIndexQueryObject("nuget", el.Attribute("id").Value, el.Attribute("version").Value, "");
                return packages;
            }
            catch (XmlException e)
            {
                throw new Exception("XML exception thrown parsing file: " + file, e);
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception thrown attempting to get packages from file: "
                    + file, e);
            }

        }

        #region Constructors
        public NuGetPackagesAudit()
        {
            this.HttpClient = new OSSIndexHttpClient("1.0");            
        }
        #endregion

        #region Private fields
        private Task<IEnumerable<OSSIndexQueryResultObject>> _GetProjectsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _GetPackagesTask;
        #endregion

    }
}
