using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace DevAudit.AuditLibrary
{
	public class DockerAuditEnvironment : AuditEnvironment
	{
		#region Constructors
		public DockerAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string container, OperatingSystem os, LocalEnvironment host_environment) : 
		base(message_handler, os, host_environment)
		{									
			ProcessExecuteStatus process_status;
			string process_output, process_error;
			bool container_exists = false;
			bool container_running = false;
            string command = "docker", arguments = "ps -a";
            bool r;
            if (this.HostEnvironment.IsDockerContainer)
            {
                r = this.HostEnvironment.Execute("chroot", " /hostroot " + command + " " + arguments, out process_status, out process_output, out process_error);
            }
            else
            {
                r = this.HostEnvironment.Execute("docker", "ps -a", out process_status, out process_output, out process_error);
            }
			if (r)
            {
				string[] p = process_output.Split (this.HostEnvironment.LineTerminator.ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
				for (int i = 1; i < p.Count (); i++) {
					if (string.IsNullOrEmpty (p [i]) || string.IsNullOrWhiteSpace (p [i]))
						continue;	
					if (p[i].Trim().StartsWith(container) || p[i].Trim().EndsWith(container)) {
						container_exists = true;					
						if (p [i].Contains ("Up ")) {
							container_running = true;
						}
						break;
					} 
				}
				if (container_exists) {
					this.Container = container;
					this.ContainerRunning = container_running;
					this.HostEnvironment.Success ("Found Docker container with id or name {0}.", this.Container);
				}
				else this.HostEnvironment.Error ("The Docker container with name or id {0} does not exist.", container); 
			} 
			else {
				this.HostEnvironment.Error ("Error executing command docker ps -a: {0}.", process_error); 
			}
		}
		#endregion

		#region Overriden properties
		protected override TraceSource TraceSource { get; set; } = new TraceSource("DockerAuditEnvironment");
		#endregion

		#region Overriden methods
		public override AuditDirectoryInfo ConstructDirectory (string dir_path)
		{
			return new DockerAuditDirectoryInfo(this, dir_path);
		}

		public override AuditFileInfo ConstructFile (string file_path)
		{
			return new DockerAuditFileInfo(this, file_path);
		}

		public override bool Execute (string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
		{
            if (this.HostEnvironment.IsDockerContainer)
            {
                string docker_exec_command = string.Format("/hostroot docker exec {0} {1} {2}", this.Container, command, arguments);
                return this.HostEnvironment.Execute("chroot", docker_exec_command, out process_status, out process_output, out process_error);
            }
            else
            {
                string docker_exec_command = string.Format("exec {0} {1} {2}", this.Container, command, arguments);
                return this.HostEnvironment.Execute("docker", docker_exec_command, out process_status, out process_output, out process_error);
            }
		}

        public override bool ExecuteAsUser(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, string user, SecureString password, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            throw new NotImplementedException();
        }

        public override bool DirectoryExists (string dir_path)
		{
			if (!this.ContainerRunning) throw new InvalidOperationException("The Docker container does not exist or is not running.");
			Stopwatch sw = new Stopwatch();
			sw.Start();
			string stat_command = "stat";
			string process_output;
			string process_error;
			ProcessExecuteStatus process_status;
			bool r = this.Execute (stat_command, dir_path, out process_status, out process_output, out process_error);
			sw.Stop();
			if (r)
			{
				Debug("stat {0} returned {1} in {2} ms.", dir_path, process_output, sw.ElapsedMilliseconds);
				return true;
			}
			else
			{
				Debug("stat {0} returned {1} in {2} ms.", dir_path, process_error, sw.ElapsedMilliseconds);
				return false;
			}
		}

		public override bool FileExists (string file_path)
		{
			if (!this.ContainerRunning) throw new InvalidOperationException("The Docker container does not exist or is not running.");
			Stopwatch sw = new Stopwatch();
			sw.Start();
			string ls_command = "ls";
			string process_output;
			string process_error;
			ProcessExecuteStatus process_status;
			bool r = this.Execute (ls_command, file_path, out process_status, out process_output, out process_error);
			sw.Stop();
			if (r)
			{
				Debug("ls {0} returned {1} in {2} ms.", file_path, process_output, sw.ElapsedMilliseconds);
				return true;
			}
			else
			{
				Debug("ls {0} returned {1} in {2} ms.", file_path, process_error, sw.ElapsedMilliseconds);
				return false;
			}
		}

		public override Dictionary<AuditFileInfo, string> ReadFilesAsText (List<AuditFileInfo> files)
		{
			throw new NotImplementedException ();
		}
			
		#endregion

		#region Public properties
		public string Container { get; protected set; }
		public bool ContainerRunning { get; protected set; }
		#endregion

		#region Public methods
		public FileInfo GetFileAsLocal(string container_path, string local_path)
		{
			CallerInformation here = this.Here();
			Stopwatch sw = new Stopwatch();
			sw.Start();
			string process_output = "", process_error = "";
			ProcessExecuteStatus process_status;
			bool r = this.HostEnvironment.Execute("docker", string.Format("cp {0}:{1} {2}", this.Container, container_path, local_path), out process_status, out process_output, out process_error);
			sw.Stop ();
			if (r) {
				FileInfo f = new FileInfo (local_path);
				if (f.Exists) {
					return f;
				} else {
					this.Error ("docker cp {0}:{1} {2} executed successfully but the file with path {3} does not exist.", this.Container, container_path, local_path, f.FullName);
					return null;
				}
			} 
			else {
				this.Error ("docker cp {0}:{1} {2} did not execute successfully. Output: {3}.", this.Container, container_path, local_path, process_error);
				return null;
			}				
		}

		public DirectoryInfo GetDirectoryAsLocal(string container_path, string local_path)
		{
			throw new NotImplementedException ();
		}
		#endregion
			
	}
}

