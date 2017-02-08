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
            if (!Directory.Exists("/hostroot"))
            {
                throw new Exception(string.Format("The host root directory is not mounted on the DevAudit Docker image at {0}.", "/hostroot"));
            }           
        }
        #endregion

        #region Overriden methods
        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return base.ConstructDirectory("/hostroot" + dir_path);
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            return base.ConstructFile("/hostroot" + file_path);
        }

        public override bool FileExists(string file_path)
        {
            return base.FileExists("/hostroot" + file_path);
        }

        public override bool DirectoryExists(string dir_path)
        {
            return base.DirectoryExists("/hostroot" + dir_path);
        }
        public override bool Execute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            return base.Execute("chroot", "/hostroot " + command + " " + arguments, out process_status, out process_output, out process_error);
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText (List<AuditFileInfo> files)
        {
            List<AuditFileInfo> f2 = files.Select(f => this.ConstructFile("/hostroot" + f.FullName)).ToList();
            return base.ReadFilesAsText(f2);
        }

        #endregion

        #region Methods
        public FileInfo GetFileAsLocal(string remote_path, string local_path)
        {
            return new FileInfo("/hostroot/" + remote_path);   
        }

        public DirectoryInfo GetDirectoryAsLocal(string remote_path, string local_path)
        {
            return new DirectoryInfo("/hostroot/" + remote_path);
        }
        #endregion

    }
}

