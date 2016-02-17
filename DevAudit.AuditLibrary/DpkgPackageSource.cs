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
			string ps_command = @"dpkg-query";
			string process_error = "";
			string[] process_output;
			int process_output_lines = 0;
			ProcessStartInfo psi = new ProcessStartInfo(ps_command);
			psi.Arguments = @"-W -f '${package} ${version}\\n'";
			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			Process p = new Process();
			p.EnableRaisingEvents = true;
			p.StartInfo = psi;
			List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
			Regex process_output_pattern = new Regex(@"^(\S+)\s(\S+)$", RegexOptions.Compiled);
			p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
			{
				if (!String.IsNullOrEmpty(e.Data))
				{
					process_output = e.Data.Split("\n".ToCharArray());
					process_output_lines+=process_output.Count();
					for (int i =0; i < process_output.Count(); i++)
					{
						Match m = process_output_pattern.Match(process_output[i].TrimStart());
						if (!m.Success)
						{
							throw new Exception("Could not parse dpkg command output row: " + process_output_lines.ToString()
								+ "\n" + process_output[i]);
						}
						else
						{
							packages.Add(new OSSIndexQueryObject("dpkg", m.Groups[1].Value, m.Groups[2].Value, ""));
						}
					}
				};
			};
			p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
			{
				if (!String.IsNullOrEmpty(e.Data))
				{
					process_error += e.Data + Environment.NewLine;
					p.CancelOutputRead();
					p.CancelOutputRead();
					throw new Exception(string.Format("dpkg-query wrote to stderror:{0}.", e.Data));
				}

			};
			try
			{
				p.Start();
			}
			catch (Win32Exception e)
			{
                if (e.Message == "The system cannot find the file specified")
                {
                    throw new Exception("dpkg is not installed.", e);
                }
               
			}
			finally 
			{
                p.Dispose();
			}
			p.BeginErrorReadLine();
			p.BeginOutputReadLine();
			p.WaitForExit();
			p.Close();
			return packages;
		}

		public DpkgPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) { }

		public DpkgPackageSource() : base() { }

		public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
		{
			List<OSSIndexArtifact> o = artifacts.ToList();
			foreach (OSSIndexArtifact a in o)
			{
				if (a.Search == null || a.Search.Count() != 4)
				{
					throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
						a.PackageId + " project id: " + a.ProjectId + ".");
				}
				else
				{
					OSSIndexQueryObject package = new OSSIndexQueryObject(a.PackageManager, a.Search[1], a.Search[3], "");
					a.Package = package;
				}
			}
			return o;
		};

		public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
		{
			return vulnerability_version == package_version;
		}
	}
}

