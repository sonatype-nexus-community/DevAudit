using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DevAudit.AuditLibrary
{
    public class YumPackageSource : PackageSource
    {
        #region Constructors
        public YumPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : 
            base(package_source_options, message_handler)
        {
            if (this.AuditOptions.ContainsKey("WithVulners") || this.DataSources.Count == 0)
            {
                if (this.DataSources.Count == 0)
                {
                    this.HostEnvironment.Info("Using Vulners as default package vulnerabilities data source for Yum package source.");
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

        #region Public properties
        public override string PackageManagerId { get { return "yum"; } }

        public override string PackageManagerLabel { get { return "Yum"; } }
        #endregion

        #region Overriden methods
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            List<Package> packages = new List<Package> ();
            string command = @"yum";
            string arguments = @"list installed";
            Regex process_output_pattern = new Regex (@"^(\S+)\s+(\S+)", RegexOptions.Compiled);
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output, process_error;
            if (this.AuditEnvironment.Execute (command, arguments, out process_status, out process_output, out process_error))
            {
                string[] p = process_output.Split ("\n".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < p.Count (); i++) {
                    if (p [i].StartsWith("Loaded plugins")) continue;
                    if (p[i].StartsWith("Installed Packages")) continue;
                    Match m = process_output_pattern.Match (p [i].TrimStart ());
                    if (!m.Success)
                    {
                        throw new Exception ("Could not parse yum command output row: " + i.ToString ()
                        + "\n" + p [i]);
                    }
                    else
                    {
                        string[] name = m.Groups[1].Value.Split('.');
                        packages.Add (new Package ("rpm", name[0], m.Groups[2].Value, null, null, name[1]));
                    }

                }
            }
            else
            {
                throw new Exception (string.Format ("Error running {0} {1} command in audit environment: {2} {3}.", command,
                    arguments, process_error, process_output));
            }
            return packages;
        }


        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return false; //Vulners provides package version-matching.
        }
        #endregion
    }
}

