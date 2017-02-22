using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class DockerizedLocalEnvironment : LocalEnvironment
    {
        #region Constructors
        public DockerizedLocalEnvironment(EventHandler<EnvironmentEventArgs> message_handler) : base(message_handler)
        {
            if (Directory.Exists("/hostroot"))
            {
                this.HostRootIsMounted = true;
                //throw new Exception(string.Format("The host root directory is not mounted on the DevAudit Docker image at {0}.", "/hostroot"));
            }
            else
            {
                this.Warning("The Docker host root directory is not mounted on the DevAudit Docker image at /hostroot so no chroot for executables is possible.");
            }
        }
        #endregion

        #region Overriden methods
        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new DockerizedLocalAuditFileInfo(this, file_path);
        }

        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return new DockerizedLocalAuditDirectoryInfo(this, dir_path);
        }

        public override bool Execute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.HostRootIsMounted)
            {
                bool r = base.Execute("chroot", " /hostroot " + command + " " + arguments, out process_status, out process_output, out process_error);
                this.Debug("Execute returned {2} for {0}. Output: {1}. Error:{3}", "chroot /hostroot " + command + " " + arguments, process_output, r, process_error);
                return r;
            }
            else
            {
                bool r = base.Execute(command, arguments, out process_status, out process_output, out process_error);
                this.Debug("Execute returned {2} for {0}. Output: {1}. Error {3}.", "chroot /hostroot " + command + " " + arguments, process_output, r, process_error);
                return r;
            }
        }

        public override bool FileExists(string file_path)
        {
            CallerInformation caller = this.Here();
            string process_output = "";
            string process_error = "";
            ProcessExecuteStatus process_status;
            if (this.Execute("stat", file_path, out process_status, out process_output, out process_error))
            {
                this.Debug(caller, "Execute returned true for stat {0}. Output: {1}. Error: {2}.", file_path, process_output, process_error);
                return !process_output.Contains("no such file or directory") && process_output.Contains("regular file");
            }

            else
            {
                this.Debug(caller, "Execute returned false for stat {0}. Output: {1}. Error: {2}.", file_path, process_output, process_error);
                return false;
            }

        }

        public override bool DirectoryExists(string dir_path)
        {
            CallerInformation caller = this.Here();
            string process_output = "";
            string process_error = "";
            ProcessExecuteStatus process_status;
            if (this.Execute("stat", dir_path, out process_status, out process_output, out process_error))
            {

                this.Debug(caller, "Execute returned true for stat {0}. Output: {1}. Error: {2}.", dir_path, process_output, process_error);
                return !process_output.Contains("no such file or directory") && process_output.Contains("directory");
            }

            else
            {
                this.Debug(caller, "Execute returned true for stat {0}. Output: {1}. Error: {2}.", dir_path, process_output, process_error);
                return false;
            }
        }
        #endregion

        #region Properties
        public bool HostRootIsMounted { get; private set; }
        #endregion
    }
}