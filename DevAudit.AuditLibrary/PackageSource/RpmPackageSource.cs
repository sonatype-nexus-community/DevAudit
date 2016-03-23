using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DevAudit.AuditLibrary
{
	public class RpmPackageSource : PackageSource
	{
		public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

		public override string PackageManagerId { get { return "rpm"; } }

		public override string PackageManagerLabel { get { return "RPM"; } }

		public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
		{
			List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject> ();
			if (this.UseDockerContainer) 
			{
				Docker.ProcessStatus process_status;
				string process_output, process_error;
				if (Docker.ExecuteInContainer (this.DockerContainerId, @"rpm -qa --qf ""%{NAME} %{VERSION}\n""", out process_status, out process_output, out process_error))
				{
					string[] p = process_output.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
					Regex process_output_pattern = new Regex (@"^(\S+)\s(\S+)", RegexOptions.Compiled);
					Match m;
					for (int i = 0; i < p.Count (); i++) 
					{
						m = process_output_pattern.Match(p[i]);
						if (!m.Success) {
							throw new Exception ("Could not parse rpm command output row: " + i.ToString() + "\n" + p [i]);
						} 
						else 
						{
							packages.Add (new OSSIndexQueryObject("rpm", m.Groups [1].Value, m.Groups [2].Value));
						}						
					}
				}
				else 
				{
					throw new Exception (string.Format ("Error running {0} command on docker container {1}: {2}", @"rpm -qa --qf ""%{NAME} %{VERSION}\n""",
						this.DockerContainerId, process_error));
				}
				return packages;
			} 
			else 
			{
				string command = @"rpm";
				string arguments = @"-qa --qf ""%{NAME} %{VERSION}\n""";
				Regex process_output_pattern = new Regex (@"^(\S+)\s(\S+)", RegexOptions.Compiled);
				HostEnvironment.ProcessStatus process_status;
				string process_output, process_error;
				if (HostEnvironment.Execute (command, arguments, out process_status, out process_output, out process_error)) 
				{
					string[] p = process_output.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);					
					Match m;
					for (int i = 0; i < p.Count (); i++) 
					{
						m = process_output_pattern.Match(p[i]);
						if (!m.Success) {
							throw new Exception ("Could not parse rpm command output row: " + i.ToString() + "\n" + p [i]);
						} 
						else 
						{
							packages.Add (new OSSIndexQueryObject("rpm", m.Groups [1].Value, m.Groups [2].Value));
						}
					}
				}
				else 
				{
					throw new Exception (string.Format ("Error running {0} {1} command in host environment: {2}.", command,
						arguments, process_error));
				}
				return packages;
			}
		}

		public RpmPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) { }

		public RpmPackageSource() : base() { }

		public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
		{
			return vulnerability_version == package_version;
		}

	}
}

