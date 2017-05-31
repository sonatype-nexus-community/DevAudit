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
			base(package_source_options, message_handler) { }
		#endregion

		#region Public properties
		public override string PackageManagerId { get { return "yum"; } }

		public override string PackageManagerLabel { get { return "Yum"; } }
		#endregion

		#region Overriden methods
		public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
		{
			List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject> ();
			string command = @"yum";
			string arguments = @"list installed";
			Regex process_output_pattern = new Regex (@"^(\S+)\s+(\S+)", RegexOptions.Compiled);
			AuditEnvironment.ProcessExecuteStatus process_status;
			string process_output, process_error;
			if (this.AuditEnvironment.Execute (command, arguments, out process_status, out process_output, out process_error)) {
				string[] p = process_output.Split ("\n".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < p.Count (); i++) {
					if (p [i].StartsWith ("Loaded plugins"))
						continue;
					Match m = process_output_pattern.Match (p [i].TrimStart ());
					if (!m.Success) {
						throw new Exception ("Could not parse yum command output row: " + i.ToString ()
						+ "\n" + p [i]);
					} else {
						packages.Add (new OSSIndexQueryObject ("rpm", m.Groups [1].Value, m.Groups [2].Value, ""));
					}

				}
			} else {
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
	}
}

