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

namespace DevAudit.AuditLibrary
{
    public class ChocolateyPackageSource : PackageSource
    {
        #region Overriden properties
        public override string PackageManagerId { get { return "chocolatey"; } }

        public override string PackageManagerLabel { get { return "Chocolatey"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return string.Empty; } }
        #endregion

        #region Overriden methods
        //run and parse output from choco list -lo command.
        public override IEnumerable<Package> GetPackages(params string[] o)
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
            List<Package> packages = new List<Package>();
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
                            packages.Add(new Package("chocolatey", output[0], output[1], ""));
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

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constructors
        public ChocolateyPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options,message_handler) {}
        #endregion

    }
}
