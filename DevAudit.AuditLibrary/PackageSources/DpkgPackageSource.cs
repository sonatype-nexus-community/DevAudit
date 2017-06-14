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
        #region Constructors
        public DpkgPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler)
        {
            if (this.AuditOptions.ContainsKey("WithVulners") || this.DataSources.Count == 0 && !string.IsNullOrEmpty(this.AuditEnvironment.OSName) &&
                !string.IsNullOrEmpty(this.AuditEnvironment.OSVersion))
            {
                if (this.DataSources.Count == 0)
                {
                    this.HostEnvironment.Info("Using default vulnerabilities data source Vulners.com for Dpkg package source.");
                }
                this.DataSourceOptions.Add("OSName", this.AuditEnvironment.OSName);
                this.DataSourceOptions.Add("OSVersion", this.AuditEnvironment.OSVersion);
                this.DataSources.Add(new VulnersdotcomDataSource(this, this.HostEnvironment, DataSourceOptions));
            }
        }
        #endregion

        #region Overriden properties and methods
        public override string PackageManagerId { get { return "dpkg"; } }

        public override string PackageManagerLabel { get { return "dpkg"; } }

        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            List<Package> packages = new List<Package>();
            string command = @"dpkg-query";
            string arguments = @"-W -f  '${package} ${version} ${architecture}|'";
            Regex process_output_pattern = new Regex(@"^(\S+)\s(\S+)\s(\S+)$", RegexOptions.Compiled);
            AuditEnvironment.ProcessExecuteStatus process_status;
                string process_output, process_error;
            Stopwatch sw = new Stopwatch();
            if (AuditEnvironment.Execute(command, arguments, out process_status, out process_output, out process_error))
            {
                string[] p = process_output.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < p.Count(); i++)
                {
                    if (string.IsNullOrEmpty(p[i]) || string.IsNullOrWhiteSpace(p[i])) continue;
                    Match m = process_output_pattern.Match(p[i].Trim());
                    if (!m.Success)
                    {
                        sw.Stop();
                        this.AuditEnvironment.Error(this.AuditEnvironment.Here(), "Could not parse dpkg command output at row {0}: {1}.", i, p[i]);
                        throw new Exception(string.Format("Could not parse dpkg command output at row {0}: {1}.", i, p[i]));
                    }
                    else
                    {
                        packages.Add(new Package("dpkg", m.Groups[1].Value, m.Groups[2].Value, null, null, m.Groups[3].Value));
                    }

                }
                sw.Stop();
                this.AuditEnvironment.Info("Retrieved {0} packages from {1} package manager in {2} ms.", packages.Count, this.PackageManagerLabel, sw.ElapsedMilliseconds);
            }
            else
            {
                sw.Stop();
                throw new Exception(string.Format("Error running {0} {1} command in audit environment: {2} {3}.", command,
                    arguments, process_error, process_output));
            }
            return packages;            
        }

 
        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
    }
}

