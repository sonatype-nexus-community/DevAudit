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
            if (this.AuditOptions.ContainsKey("WithVulners") || this.DataSources.Count == 0)
            {
                if (this.DataSources.Count == 0)
                {
                    this.HostEnvironment.Info("Using Vulners as default package vulnerabilities data source for Dpkg package source.");
                }
                if (this.AuditOptions.ContainsKey("OSName"))
                {
                    this.DataSourceOptions.Add("OSName", this.AuditOptions["OSName"]);
                }
                if (this.AuditOptions.ContainsKey("OSVersion"))
                {
                    this.DataSourceOptions.Add("OSVersion", this.AuditOptions["OSVersion"]);
                }
                this.DataSources.Add(new VulnersDataSource(this, DataSourceOptions));
            }
        }
        #endregion

        #region Overriden properties and methods
        public override string PackageManagerId { get { return "dpkg"; } }

        public override string PackageManagerLabel { get { return "dpkg"; } }

        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            Stopwatch sw = new Stopwatch();
            List<Package> packages = new List<Package>();
            string command = @"dpkg-query";
            string arguments = @"-W -f  '${package} ${version} ${architecture}|'";
            Regex process_output_pattern = new Regex(@"^(\S+)\s(\S+)\s(\S+)$", RegexOptions.Compiled);
            AuditEnvironment.ProcessExecuteStatus process_status;
                string process_output, process_error;
            sw.Start();
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
            return true; //Vulners data source version matching done on server
        }
        #endregion
    }
}

