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
    public class OneGetPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "oneget"; } }

        public override string PackageManagerLabel { get { return "OneGet"; } }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
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
                        p.CancelOutputRead();
                        p.CancelOutputRead();
                        throw new Exception("Error running Get-Package Powershell command (OneGet is not installed on this computer).");
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

        public OneGetPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) { }

        public OneGetPackageSource() : base() { }

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
                a.GetProjectId = () =>
                {
                    string project_id = a.ProjectId;
                    return project_id;
                };
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.PackageManager, a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }

    }
}
