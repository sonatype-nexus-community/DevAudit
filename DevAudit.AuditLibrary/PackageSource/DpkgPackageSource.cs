using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DevAudit.AuditLibrary
{
    public class DpkgPackageSource : PackageSource
    {
        public override string PackageManagerId { get { return "dpkg"; } }

        public override string PackageManagerLabel { get { return "dpkg"; } }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            string command = @"dpkg-query";
            string arguments = @"-W -f  '${package} ${version}|'";
            Regex process_output_pattern = new Regex(@"^(\S+)\s(\S+)$", RegexOptions.Compiled);
            AuditEnvironment.ProcessExecuteStatus process_status;
                string process_output, process_error;
            this.Stopwatch.Restart();
            if (AuditEnvironment.Execute(command, arguments, out process_status, out process_output, out process_error))
            {
                string[] p = process_output.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < p.Count(); i++)
                {
                    if (string.IsNullOrEmpty(p[i]) || string.IsNullOrWhiteSpace(p[i])) continue;
                    Match m = process_output_pattern.Match(p[i].Trim());
                    if (!m.Success)
                    {
                        Stopwatch.Stop();
                        this.AuditEnvironment.Error(this.AuditEnvironment.Here(), "Could not parse dpkg command output at row {0}: {1}.", i, p[i]);
                        throw new Exception(string.Format("Could not parse dpkg command output at row {0}: {1}.", i, p[i]));
                    }
                    else
                    {
                        packages.Add(new OSSIndexQueryObject("dpkg", m.Groups[1].Value, m.Groups[2].Value, ""));
                    }

                }
                this.Stopwatch.Stop();
                this.AuditEnvironment.Success("Retrieved {0} packages from {1} package manager in {2} ms.", packages.Count, this.PackageManagerLabel, this.Stopwatch.ElapsedMilliseconds);
            }
            else
            {
                Stopwatch.Stop();
				throw new Exception(string.Format("Error running {0} {1} command in audit environment: {2} {3}.", command,
                    arguments, process_error, process_output));
            }
            return packages;            
        }

        public DpkgPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler) { }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
    }
}

