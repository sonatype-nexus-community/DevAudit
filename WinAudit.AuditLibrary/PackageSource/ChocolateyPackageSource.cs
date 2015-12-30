using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
    public class ChocolateyPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "chocolatey"; } }

        public override string PackageManagerLabel { get { return "Chocolatey"; } }

        //run and parse output from choco list -lo command.
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            string choco_command = "";
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

    }
}
