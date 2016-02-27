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
		public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

		public override string PackageManagerId { get { return "yum"; } }

		public override string PackageManagerLabel { get { return "Yum"; } }

		public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
		{
			List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject> ();
			if (this.UseDockerContainer) {
				Docker.ProcessStatus process_status;
				string process_output, process_error;
				if (Docker.ExecuteInContainer (this.DockerContainerId, @"yum list installed", out process_status, out process_output, out process_error)) {
					string[] p = process_output.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);

					for (int i = 0; i < p.Count (); i++) {
						string[] k = p [i].Split ("|".ToCharArray ());
						packages.Add (new OSSIndexQueryObject ("dpkg", k [0], k [1]));
					}
				} else {
					throw new Exception (string.Format ("Error running {0} command on docker container {1}: {2}", @"dpkg-query -W -f ${Package}'${Version}\n",
						this.DockerContainerId, process_error));
				}
				return packages;
			} else {
				string command = @"yum";
				string arguments = @"list installed";
				Regex process_output_pattern = new Regex (@"^(\S+)\s(\S+)$", RegexOptions.Compiled);
				HostEnvironment.ProcessStatus process_status;
				string process_output, process_error;
				if (HostEnvironment.Execute (command, arguments, out process_status, out process_output, out process_error)) {
					string[] p = process_output.Split ("\n".ToCharArray ());
					for (int i = 0; i < p.Count (); i++) {
						Match m = process_output_pattern.Match (p [i].TrimStart ());
						if (!m.Success) {
							throw new Exception ("Could not parse dpkg command output row: " + i.ToString ()
							+ "\n" + p [i]);
						} else {
							packages.Add (new OSSIndexQueryObject ("dpkg", m.Groups [1].Value, m.Groups [2].Value, ""));
						}

					}
				} else {
					throw new Exception (string.Format ("Error running {0} {1} command in host environment: {2}.", command,
						arguments, process_error));
				}
				return packages;

			}
		}

		public YumPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) { }

		public YumPackageSource() : base() { }

		public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
		{
			return vulnerability_version == package_version;
		}

	}
}

