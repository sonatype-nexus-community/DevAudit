/*
Copyright (c) 2015, OSS Index
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the 
following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
in the documentation and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from 
this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32;
using Newtonsoft.Json;

namespace WinAudit.AuditLibrary
{
    public class Audit
    {
        public string ApiVersion { get; set; }

        public Audit(string api_version)
        {
            this.ApiVersion = api_version;
        }
        
        //Get list of installed programs from 3 registry locations.
        public IEnumerable<OSSIndexQueryObject> GetMSIPackages()
        {
            RegistryKey k = null;
            try
            {
                RegistryPermission perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                IEnumerable <OSSIndexQueryObject> packages_query =
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string) k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string) k.OpenSubKey(sn).GetValue("DisplayVersion"), 
                        (string) k.OpenSubKey(sn).GetValue("Publisher"));
                List<OSSIndexQueryObject> packages = packages_query
                    .Where(p => !string.IsNullOrEmpty(p.Name))
                    .ToList<OSSIndexQueryObject>();

                perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                packages_query = 
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string) k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string) k.OpenSubKey(sn).GetValue("DisplayVersion"), (string) k.OpenSubKey(sn).GetValue("Publisher"));
                packages.AddRange(packages_query.Where(p => !string.IsNullOrEmpty(p.Name)));

                perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                packages_query =
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string)k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string)k.OpenSubKey(sn).GetValue("DisplayVersion"), (string)k.OpenSubKey(sn).GetValue("Publisher"));
                packages.AddRange(packages_query.Where(p => !string.IsNullOrEmpty(p.Name)));
                return packages;
            }

            catch (SecurityException se)
            {
                throw new Exception("Security exception thrown reading registry key: " + (k == null ? "" : k.Name + "\n" + se.Message), se);
            }
            catch (Exception e)
            {
                throw new Exception("Exception thrown reading registry key: " + (k == null ? "" : k.Name), e);
            }

            finally
            {
                k = null;
            }


        }

        public async Task<IEnumerable<OSSIndexQueryResultObject>> SearchOSSIndex(string package_manager, OSSIndexQueryObject package)
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
                    return await Task.Factory.StartNew<IEnumerable<OSSIndexQueryResultObject>>(() =>
                    { return JsonConvert.DeserializeObject<IEnumerable<OSSIndexQueryResultObject>>(r); });
                }
                else
                {
                    throw new Exception("HTTP request did not return success.\nReason: " + response.ReasonPhrase);
                }
            }
        }
        public async Task<IEnumerable<OSSIndexQueryResultObject>> SearchOSSIndex(string package_manager, IEnumerable<OSSIndexQueryObject> packages)
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
                    return await Task.Factory.StartNew<IEnumerable<OSSIndexQueryResultObject>>(()=>
                    { return JsonConvert.DeserializeObject<IEnumerable<OSSIndexQueryResultObject>>(r); });
                }
                else
                {
                    throw new Exception("HTTP request did not return success.\nReason: " + response.ReasonPhrase);
                }
            }
        }
        


        public IEnumerable<OSSIndexProjectVulnerability> GetVulnerabilityForSCMId(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = client.GetAsync(string.Format("v1.0/scm/{0}/vulnerabilities", id)).Result;
                if (response.IsSuccessStatusCode)
                {
                    string r = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<IEnumerable<OSSIndexProjectVulnerability>>(r);
                }
                else
                {
                    throw new Exception("HTTP request did not return success.\nReason: " + response.ReasonPhrase);
                }
            }

        
        }
    }
}
