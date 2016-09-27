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
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "dpkg"; } }

        public override string PackageManagerLabel { get { return "dpkg"; } }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            string command = @"dpkg-query";
            string arguments = @"-W -f  '${package} ${version}\n'";
            Regex process_output_pattern = new Regex(@"^(\S+)\s(\S+)$", RegexOptions.Compiled);
            AuditEnvironment.ProcessExecuteStatus process_status;
                string process_output, process_error;
            if (AuditEnvironment.Execute(command, arguments, out process_status, out process_output, out process_error))
            {
                string[] p = process_output.Split("\n".ToCharArray());
                for (int i = 0; i < p.Count(); i++)
                {
                        Match m = process_output_pattern.Match(p[i].Trim());
                        if (!m.Success)
                        {
                            throw new Exception("Could not parse dpkg command output row: " + i.ToString()
                                + "\n" + p[i]);
                        }
                        else
                        {
                            packages.Add(new OSSIndexQueryObject("dpkg", m.Groups[1].Value, m.Groups[2].Value, ""));
                        }

                    }
                }
                else
                {
                    throw new Exception(string.Format("Error running {0} {1} command in host environment: {2}.", command,
                        arguments, process_error));
                }
                return packages;

            
        }

        public DpkgPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(package_source_options, message_handler) { }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
    }
}

