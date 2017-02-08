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
        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return this.HostRootIsMounted ? base.ConstructDirectory(Path.Combine("/hostroot", dir_path)) : base.ConstructDirectory(dir_path);
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            return this.HostRootIsMounted ? base.ConstructFile(Path.Combine("/hostroot", file_path)) : base.ConstructFile(file_path);
        }

        public override bool FileExists(string file_path)
        {
            return this.HostRootIsMounted ? File.Exists(Path.Combine("/hostroot", file_path)) : File.Exists(file_path);
        }

        public override bool DirectoryExists(string dir_path)
        {
            return this.HostRootIsMounted ? Directory.Exists(Path.Combine("/hostroot", dir_path)) : Directory.Exists(dir_path);
        }

        public override bool Execute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!HostRootIsMounted)
            {
                throw new InvalidOperationException("The Docker host root directory is not mounted at /hostroot so chroot for executables is not possible.\n Mount the host root using the the -v /:/hostroot:ro docker run  option.");
            }

            if (command.Contains("/hostroot"))
            {
                command = command.Replace("/hostroot", string.Empty);
            }
            if (arguments.Contains("/hostroot"))
            {
                arguments = arguments.Replace("/hostroot", string.Empty);
            }

            return base.Execute("chroot", " /hostroot "+ command + " " + arguments, out process_status, out process_output, out process_error);
        }
        #endregion

        #region Properties
        public bool HostRootIsMounted { get; private set; }
        #endregion
    }
}

