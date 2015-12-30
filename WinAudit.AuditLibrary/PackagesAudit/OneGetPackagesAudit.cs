using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;


namespace WinAudit.AuditLibrary
{
    public class OneGetPackagesAudit : IPackagesAudit
    {
        public OSSIndexHttpClient HttpClient { get; set; }

        public string PackageManagerId { get { return "oneget"; } }

        public string PackageManagerLabel { get { return "OneGet"; } }

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

        public Task<IEnumerable<OSSIndexQueryResultObject>> GetProjectsTask
        {
            get
            {
                if (_GetProjectsTask == null)
                {
                    int i = 0;
                    IEnumerable<IGrouping<int, OSSIndexQueryObject>> packages_groups = this.Packages.GroupBy(x => i++ / 10).ToArray();
                    IEnumerable<OSSIndexQueryObject> f = packages_groups.Where(g => g.Key == 1).SelectMany(g => g);
                        _GetProjectsTask = Task<IEnumerable<OSSIndexQueryResultObject>>.Run(async () =>
                    this.Projects = await this.HttpClient.SearchAsync("oneget", f));
                }
                return _GetProjectsTask;
            }
        }

        public ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities { get; set; } = new System.Collections.Concurrent.ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>>();

        public Task<IEnumerable<OSSIndexProjectVulnerability>>[] GetVulnerabilitiesTask
        {
            get
            {
                if (_GetVulnerabilitiesTask == null)
                {
                    List<Task<IEnumerable<OSSIndexProjectVulnerability>>> tasks =
                        new List<Task<IEnumerable<OSSIndexProjectVulnerability>>>(this.Projects.Count(p => !string.IsNullOrEmpty(p.ProjectId)));
                    this.Projects.ToList().Where(p => !string.IsNullOrEmpty(p.ProjectId)).ToList()
                        .ForEach(p => tasks.Add(Task<IEnumerable<OSSIndexProjectVulnerability>>
                        .Run(async () => this.Vulnerabilities.AddOrUpdate(p.ProjectId, await this.HttpClient.GetVulnerabilitiesForIdAsync(p.ProjectId),
                        (k, v) => v))));
                    this._GetVulnerabilitiesTask = tasks.ToArray(); ;
                }
                return this._GetVulnerabilitiesTask;
            }
        }

        public IEnumerable<OSSIndexQueryObject> GetPackages()
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

        #region Constructors
        public OneGetPackagesAudit()
        {
            this.HttpClient = new OSSIndexHttpClient("1.1");            
        }
        #endregion

        #region Private fields
        private Task<IEnumerable<OSSIndexQueryResultObject>> _GetProjectsTask;
        private Task<IEnumerable<OSSIndexQueryObject>> _GetPackagesTask;
        private Task<IEnumerable<OSSIndexProjectVulnerability>>[] _GetVulnerabilitiesTask;
        #endregion

    }
}
