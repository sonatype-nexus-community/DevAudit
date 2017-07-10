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

using ExpectNet;
using Renci.SshNet;
using Renci.SshNet.Common;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace DevAudit.AuditLibrary
{
    public class SshDockerAuditEnvironment : SshAuditEnvironment, IContainerEnvironment
    {
        #region Constructors
        public SshDockerAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string client, string host_name, int port, string user, object pass, string keyfile, string container, OperatingSystem os, LocalEnvironment host_environment) 
            : base(message_handler, client, host_name, port, user, pass, keyfile, os, host_environment)
        {
            ProcessExecuteStatus process_status;
            string process_output, process_error;
            bool container_exists = false;
            bool container_running = false;
            bool r = base.Execute("docker", "ps -a", out process_status, out process_output, out process_error);          
            if (r)
            {
                string[] p = process_output.Split(this.LineTerminator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < p.Count(); i++)
                {
                    if (string.IsNullOrEmpty(p[i]) || string.IsNullOrWhiteSpace(p[i]))
                        continue;
                    if (p[i].Trim().StartsWith(container) || p[i].Trim().EndsWith(container))
                    {
                        container_exists = true;
                        if (p[i].Contains("Up "))
                        {
                            container_running = true;
                        }
                        break;
                    }
                }
                if (container_exists)
                {
                    this.Container = container;
                    this.ContainerRunning = container_running;
                    this.Success("Found Docker container with id or name {0}.", this.Container);
                    this.GetOSName();
                    this.GetOSVersion();
                }
                else this.Error("The Docker container with name or id {0} does not exist.", container);
            }
            else
            {
                this.Error("Error executing command docker ps -a: {0}.", process_error);
            }
        }
        #endregion

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("SshDockerAuditEnvironment");
        #endregion

        #region Overriden methods
        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new SshDockerAuditFileInfo(this, file_path);
        }

        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return new SshDockerAuditDirectoryInfo(this, dir_path);
        }

        public override bool FileExists(string file_path)
        {
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            string output;
            bool ls_result = this.ExecuteCommand("stat ", file_path, out output);
            if (ls_result)
            {
                
                return true;
            }
            else
            {
                return false;
            }

        }

        public override bool DirectoryExists(string dir_path)
        {
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            string output;
            bool ls_result = this.ExecuteCommand("stat ", dir_path, out output);
            if (ls_result)
            {

                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Dictionary<string, string> env = null, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (env != null && env.Count > 0)
            {
                StringBuilder vars = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in env)
                {
                    vars.AppendFormat("{0}={1} ", kv.Key, kv.Value);
                }
                command = vars.ToString() + command;
            }
            string docker_exec_command = string.Format("exec {0} {1} {2}", this.Container, command, arguments);
            return base.Execute("docker", docker_exec_command, out process_status, out process_output, out process_error);

        }
       

        public override bool ExecuteAsUser(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, string user, SecureString password, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (password == null)
            {
                string c = string.Format("-c \"echo CMD_START && {0} {1} && echo CMD_SUCCESS || echo CMD_ERROR\" {2} || echo CMD_ERROR", command, arguments, user);
                return this.Execute("su", c, out process_status, out process_output, out process_error);
            }
            else
            {
                Error("Executing commands as a different operating system user with a required password in a Docker container environment is not currently supported.");
                process_error = string.Empty;
                process_output = string.Empty;
                process_status = ProcessExecuteStatus.Error;
                return false;
            }
        }
    

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Properties
        public string Container { get; protected set; }
        public bool ContainerRunning { get; protected set; }
        #endregion

        #region Methods
        public bool ExecuteCommandInContainer(string command, string arguments, out string process_output)
        {
            return this.ExecuteCommand(command, arguments, out process_output);
        }

        public Tuple<bool, bool> GetContainerStatus(string container_id)
        {
            bool r;
            string process_output;
            bool container_exists = false, container_running = false;
            r = this.ExecuteCommand("docker", "ps -a", out process_output);
            if (r)
            {
                string[] p = process_output.Split(this.HostEnvironment.LineTerminator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < p.Count(); i++)
                {
                    if (string.IsNullOrEmpty(p[i]) || string.IsNullOrWhiteSpace(p[i]))
                    {
                        continue;
                    }
                    if (p[i].Trim().StartsWith(container_id) || p[i].Trim().EndsWith(container_id))
                    {
                        container_exists = true;
                        if (p[i].Contains("Up "))
                        {
                            container_running = true;
                        }
                        break;
                    }
                }
                return new Tuple<bool, bool>(container_exists, container_running);
            }
            else
            {
                this.HostEnvironment.Error("Could not get status of container {0}. Error: {1}", container_id, process_output);
                return new Tuple<bool, bool>(false, false);
            }

        }
        #endregion

        #region Disposer and Finalizer
        ~SshDockerAuditEnvironment()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
