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
		#region Overriden properties
		public override string PackageManagerId { get { return "rpm"; } }

		public override string PackageManagerLabel { get { return "RPM"; } }
		#endregion

		#region Overriden methods
		public override IEnumerable<Package> GetPackages(params string[] o)
		{
			List<Package> packages = new List<Package>();
			string command = @"rpm";
			string arguments = @"-qa --qf ""%{NAME} %{VERSION}\n""";
			Regex process_output_pattern = new Regex (@"^(\S+)\s(\S+)", RegexOptions.Compiled);
			AuditEnvironment.ProcessExecuteStatus process_status;
			string process_output, process_error;
			if (this.AuditEnvironment.Execute (command, arguments, out process_status, out process_output, out process_error)) 
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
						packages.Add (new Package("rpm", m.Groups [1].Value, m.Groups [2].Value));
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
			return vulnerability_version == package_version;
		}
		#endregion

		#region Constructors
		public RpmPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler) { }
		#endregion
	}
}

