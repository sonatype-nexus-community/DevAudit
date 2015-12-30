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

        //run and parse output from choco list -lo command.
        public IEnumerable<OSSIndexQueryObject> GetChocolateyPackages(string choco_command = "")
        {
            if (string.IsNullOrEmpty(choco_command)) choco_command = @"C:\ProgramData\chocolatey\choco.exe";
            string process_output = "", process_error = "";
            ProcessStartInfo psi = new ProcessStartInfo(choco_command);
            psi.Arguments = @"list -lo";
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                string first = @"(\d+)\s+packages installed";
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_output += e.Data + Environment.NewLine;
                    Match m = Regex.Match(e.Data.Trim(), first);
                    if (m.Success)
                    {
                        return;
                    }
                    else
                    {
                        string[] output = e.Data.Trim().Split(' ');
                        if ((output == null) || (output != null) && (output.Length != 2))
                        {
                            throw new Exception("Could not parse output from choco command: " + e.Data);
                        }
                        else
                        {
                            packages.Add(new OSSIndexQueryObject("chocolatey", output[0], output[1], ""));
                        }
                    }

                };
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_error += e.Data + Environment.NewLine;
                };
            };
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
            p.Close();
            return packages;
        }

        public IEnumerable<OSSIndexQueryObject> GetOneGetPackages()
        {
            string ps_command = @"powershell";
            string process_output = "", process_error = "";
            int process_output_lines = 0;
            ProcessStartInfo psi = new ProcessStartInfo(ps_command);
            psi.Arguments = @"-NoLogo -NonInteractive -OutputFormat Text -Command Get-Package | Select-Object -Property Name,Version,ProviderName | Format-Table -AutoSize | Out-String -Width 1024";
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            bool init_columns = false;
            string columns_header_pattern = @"(Name\s*)(Version\s*)(ProviderName\s*)";
            string columns_format = @"^(.{n})(.{v})(.*)$";
            string columns_pattern = "";
            int name_col_pos = 0, version_col_pos = 0, provider_name_col_pos = 0,
            name_col_length = 0, version_col_length = 0, provider_name_col_length = 0;
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
             if (!String.IsNullOrEmpty(e.Data))
                {
                    process_output += e.Data + Environment.NewLine;
                    process_output_lines++;
                    if (process_output_lines == 1)
                    {
                        Match m = Regex.Match(e.Data.TrimStart(), columns_header_pattern);
                        if (!m.Success)
                        {
                            throw new Exception("Could not parse Powershell command output table header: " + e.Data.Trim());
                        }
                        else
                        {
                            name_col_pos = m.Groups[1].Index;
                            name_col_length = m.Groups[1].Length;
                            version_col_pos = m.Groups[2].Index;
                            version_col_length = m.Groups[2].Length;
                            provider_name_col_pos = m.Groups[3].Index;
                            provider_name_col_length = m.Groups[3].Length;
                            columns_pattern = columns_format
                                .Replace("n", name_col_length.ToString())
                                .Replace("v", version_col_length.ToString())
                                .Replace("p", provider_name_col_length.ToString());
                            init_columns = true;
                        }
                    }
                    else if (process_output_lines == 2) return;

                    else
                    {
                        if (!init_columns)
                            throw new Exception("Powershell command output parser not initialised at line " + process_output_lines.ToString());
                        Match m = Regex.Match(e.Data.TrimStart(), columns_pattern);
                        if (!m.Success)
                        {
                            throw new Exception("Could not parse Powershell command output table row: " + process_output_lines.ToString()
                                + "\n" + e.Data.TrimStart());
                        }
                        else
                        {
                            packages.Add(new OSSIndexQueryObject(m.Groups[3].Value.Trim(), m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim(), ""));
                        }
                    }
                };
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_error += e.Data + Environment.NewLine;
                    if (e.Data.Contains("Get-Package : The term 'Get-Package' is not recognized as the name of a cmdlet, function, script file, or operable"))
                    {
                        p.Kill();
                        throw new Exception("Error running Get-Package Powershell command (OneGet may not be installed on this computer):" + e.Data);
                    }
                };
            };
            try
            {
                p.Start();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    throw new Exception("Powershell is not installed on this computer or is not on the current PATH.", e);
                }
            }
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
            p.Close();
            return packages;
        }



        public IEnumerable<OSSIndexQueryObject> GetBowerPackages(string file_name=@".\bower.json")
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
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }
        public async Task<IEnumerable<OSSIndexQueryResultObject>> SearchOSSIndexAsync(string package_manager, IEnumerable<OSSIndexQueryObject> packages)
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
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }


        
        public async Task<IEnumerable<OSSIndexProjectVulnerability>> GetOSSIndexVulnerabilitiesForId(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"https://ossindex.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "WinAudit");
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + this.ApiVersion + "/scm/{0}/vulnerabilities", id));
                if (response.IsSuccessStatusCode)
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
