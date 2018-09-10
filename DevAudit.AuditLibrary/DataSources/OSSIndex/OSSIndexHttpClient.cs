using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexHttpClient
    {
        #region Properties
        public string ApiVersion { get; set; }

        public Uri HttpsProxy { get; set; } = null;

        private string HOST = "https://ossindex.net/";
        //private String HOST = "http://localhost:8080/";
        #endregion

        #region Constructors
        public OSSIndexHttpClient(string api_version)
        {
            this.ApiVersion = api_version;
        }
        public OSSIndexHttpClient(string host, string api_version)
        {
            this.HOST = host;
            this.ApiVersion = api_version;
        }

        #endregion

        #region Methods                     
        public async Task<IEnumerable<OSSIndexArtifact>> SearchAsync(string package_manager, Package package, Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform)
        {
            string api_version = "1.1";
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("v" + api_version + "/search/artifact/" +
                    string.Format("{0}/{1}/{2}", package_manager, package.Name, package.Version, package.Vendor));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexArtifact> artifacts = JsonConvert.DeserializeObject<List<OSSIndexArtifact>>(r);
                    if (artifacts.Count() == 0 || transform == null)
                    {
                        return artifacts;
                    }
                    else
                    {
                        return transform(artifacts);
                    }
                }
                else
                {
                    throw new HttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<IEnumerable<OSSIndexArtifact>> SearchAsync(string package_manager, IEnumerable<Package> packages, 
            Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform)
        {
            string api_version = "1.1";
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync("v" + api_version + "/search/artifact/" + package_manager,
                    new StringContent(JsonConvert.SerializeObject(packages),Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexArtifact> artifacts = JsonConvert.DeserializeObject<List<OSSIndexArtifact>>(r);
                    if (artifacts.Count() == 0 || transform == null)
                    {
                        return artifacts;
                    }
                    else
                    {
                        return transform(artifacts);
                    }
                }
                else
                {
                    throw new HttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<OSSIndexProject> GetProjectForIdAsync(string id)
        {
            string api_version = "1.1";
            if (string.IsNullOrEmpty(id))throw new ArgumentNullException("Project id.");
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + api_version + "/project/{0}", id));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<OSSIndexProject>>(r).FirstOrDefault();
                }
                else
                {
                    throw new HttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<List<OSSIndexPackageVulnerability>> GetPackageVulnerabilitiesAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("Package id.");
            string api_version = "1.1";
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + api_version + "/package/{0}/vulnerabilities", id)); 
                    
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<OSSIndexPackageVulnerability>>(r);
                }
                else
                {
                    throw new HttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }

        }

        public async Task<IEnumerable<OSSIndexProjectVulnerability>> GetVulnerabilitiesForIdAsync(string id)
        {
            string api_version = "1.1";
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + api_version + "/project/{0}/vulnerabilities", id));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexProjectVulnerability> result = JsonConvert.DeserializeObject<List<OSSIndexProjectVulnerability>>(r);
                    result.ForEach(v =>
                    {
                        v.ProjectId = id;
                        v.Title = HttpUtility.HtmlDecode(v.Title);
                        v.Summary = HttpUtility.HtmlDecode(v.Summary);
                    }); ;

                    return result;
                }
                else
                {
                    throw new HttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<IEnumerable<ConfigurationRule>> GetConfigurationRulesForIdAsync(string id)
        {
            string api_version = "1.1";
            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + api_version + "/project/{0}/rules", id));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<ConfigurationRule> result = JsonConvert.DeserializeObject<List<ConfigurationRule>>(r);
                    result.ForEach(v =>
                    {
                        v.ProjectId = id;
                        v.Title = HttpUtility.HtmlDecode(v.Title);
                        v.Summary = HttpUtility.HtmlDecode(v.Summary);
                    }); ;

                    return result;
                }
                else
                {
                    throw new HttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<List<OSSIndexApiv2Result>> SearchVulnerabilitiesAsync(IEnumerable<Package> packages, 
            Func<List<OSSIndexApiv2Result>, List<OSSIndexApiv2Result>> transform)
        {
            string server_api_version = this.ApiVersion;
            IEnumerable<Package> packages_for_query = packages.Select(p => new Package(p.PackageManager, p.Name, "*", string.Empty, p.Group));

            using (HttpClient client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync("v" + server_api_version + "/package",
                    new StringContent(JsonConvert.SerializeObject(packages_for_query), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexApiv2Result> results = JsonConvert.DeserializeObject<List<OSSIndexApiv2Result>>(r);
                    if (results != null && results.Count > 0 && transform != null)
                    {
                        return transform(results);
                    }
                    else
                    {
                        return results;
                    }
                }
                else
                {
                    throw new HttpException("packages", response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        private HttpClient CreateHttpClient()
        {
            HttpClient client;
            if (this.HttpsProxy == null)
            {
                client = new HttpClient();
            }
            else
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(this.HttpsProxy, false),
                    UseProxy = true,
                };
                client = new HttpClient(handler);
            }
            client.BaseAddress = new Uri(@HOST);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
            return client;
        }
        #endregion

    }
}
