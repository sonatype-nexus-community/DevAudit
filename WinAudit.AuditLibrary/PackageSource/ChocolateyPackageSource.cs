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

using Semver;

using Microsoft.Win32;

namespace WinAudit.AuditLibrary
{
    public class ChocolateyPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "chocolatey"; } }

        public override string PackageManagerLabel { get { return "Chocolatey"; } }

        //run and parse output from choco list -lo command.
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            string choco_command = @"C:\ProgramData\chocolatey\choco.exe";
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
            try
            {
                p.Start();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    throw new Exception("Chocolatey is not installed on this computer or is not on the current PATH.", e);
                }
            }
            p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                p.Close();
                        
            return packages;
        }

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
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


        public override Func<string, string, bool> PackageVersionInRange { get; } = (range, compare_to_range) =>
        {
            /*
                Regex parse_ex = new Regex(@"^(?<range>~+|<+=?|>+=?)?" +
                    @"(?<ver>(\d+)" +
                    @"(\.(\d+))?" +
                    @"(\.(\d+))?" +
                    @"(\-([0-9A-Za-z\-\.]+))?" +
                    @"(\+([0-9A-Za-z\-\.]+))?)$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
                Match m = parse_ex.Match(range);
                if (!m.Success)
                {
                    throw new ArgumentException("Could not parse package range: " + range + ".");
                }
                string range_version_str = "";
                string op = "";
                string[] legal_ops = { "", "~", "<", "<=", ">=", ">" };
                SemVersion range_version = null;
                SemVersion compare_to_range_version = null;
                op = m.Groups[1].Value;
                if (!legal_ops.Contains(op))
                    throw new ArgumentException("Could not parse package version range operator: " + op + ".");
                range_version_str = m.Groups[2].Value;
                if (!SemVersion.TryParse(m.Groups[2].Value, out range_version))
                {
                    throw new ArgumentException("Could not parse package semantic version: " + m.Groups[2].Value + ".");
                }
                if (!SemVersion.TryParse(compare_to_range, out compare_to_range_version))
                {
                    throw new ArgumentException("Could not parse comparing semantic version: " + compare_to_range + ".");
                }
                switch (op)
                {
                    case "":
                        return compare_to_range_version == range_version;
                    case "<":
                        return compare_to_range_version < range_version;
                    case "<=":
                        return compare_to_range_version < range_version;
                    case ">":
                        return compare_to_range_version > range_version;
                    case ">=":
                        return compare_to_range_version >= range_version;
                    //case "~":
                    //if (range_version.Major != compare_to_range_version.Major) return false;
                    //major matches
                    //else if (range_version.Minor == 0 && compare_to_range_version > 0) return true;

                    //else if (range_version.Minor == 0 && range_version.Patch == 0 && compare_to_range_version.Patch > 0) return true;
                    //else if (range_version.Patch == )
                    default:
                        throw new Exception("Unimplemented range operator: " + op + ".");
                }*/
            return (range == compare_to_range);
        };

        public ChocolateyPackageSource() : base() { }
        public ChocolateyPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) {}


    }
}
