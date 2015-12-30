using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace WinAudit.AuditLibrary
{
    public class OSSIndexHttpClient
    {
        public string ApiVersion { get; set; }

        public OSSIndexHttpClient(string api_version)
        {
            this.ApiVersion = api_version;
        }
                             
        public async Task<IEnumerable<OSSIndexArtifact>> Search(string package_manager, OSSIndexQueryObject package)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = await client.GetAsync("v" + this.ApiVersion + "/search/artifact/" +
                    string.Format("{0}/{1}/{2}", package_manager, package.Name, package.Version, package.Vendor));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return await Task.Factory.StartNew<IEnumerable<OSSIndexArtifact>>(() =>
                    { return JsonConvert.DeserializeObject<IEnumerable<OSSIndexArtifact>>(r); });
                }
                else
                {
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<IEnumerable<OSSIndexArtifact>> SearchAsync(string package_manager, IEnumerable<OSSIndexQueryObject> packages)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = await client.PostAsync("v" + this.ApiVersion + "/search/artifact/" + package_manager,
                    new StringContent(JsonConvert.SerializeObject(packages),Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexArtifact> results = JsonConvert.DeserializeObject<List<OSSIndexArtifact>>(r);
                    results.ForEach(result =>
                    {
                        if (!string.IsNullOrEmpty(result.ProjectId) && string.IsNullOrEmpty(result.SCMId))
                        {
                            result.SCMId = result.ProjectId;
                        }
                        else if (string.IsNullOrEmpty(result.ProjectId) && !string.IsNullOrEmpty(result.SCMId))
                        {
                            result.ProjectId = result.SCMId;
                        }

                    })
                    ;
                    return results;
                }
                else
                {
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<OSSIndexProject> GetProjectForIdAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = this.ApiVersion == "1.0" ? 
                    await client.GetAsync(string.Format("v" + this.ApiVersion + "/scm/{0}", id)) : await client.GetAsync(string.Format("v" + this.ApiVersion + "/project/{0}", id));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<OSSIndexProject>>(r).FirstOrDefault();
                }
                else
                {
                    throw new OSSIndexHttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }


        }
    
        public async Task<IEnumerable<OSSIndexProjectVulnerability>> GetVulnerabilitiesForIdAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = this.ApiVersion == "1.0" ?
                    await client.GetAsync(string.Format("v" + this.ApiVersion + "/scm/{0}/vulnerabilities", id)) : await client.GetAsync(string.Format("v" + this.ApiVersion + "/project/{0}/vulnerabilities", id)); if (response.IsSuccessStatusCode)
                {
                    string r = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<IEnumerable<OSSIndexProjectVulnerability>>(r);
                }
                else
                {
                    throw new OSSIndexHttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage); 
                }
            }

        
        }
    }
}
