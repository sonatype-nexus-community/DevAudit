using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using ExpectNet;

namespace DevAudit.AuditLibrary
{
    public class SshEnvironment : AuditEnvironment
    {
        #region Public properties
        public string HostName { get; private set; }
        public bool UsePageant { get; private set; }
        public bool UseSshAgent { get; private set; }
        #endregion

        private Session SshSession { get; set; }

        #region Overriden members     
        public override bool IsWindows
        {
            get
            {
                return false;
            }
        }

        public override bool IsUnix
        {
            get
            {
                return true;
            }
        }
        public override bool FileExists(string file_path)
        {
            throw new NotImplementedException();
        }

        public override bool DirectoryExists(string dir_path)
        {
            throw new NotImplementedException();
        }

        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null)
        {
            throw new NotImplementedException();
        }
        #endregion
        public bool Spawn(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null)
        {
            throw new NotImplementedException();
        }

        public SshEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string host_name) : base(message_handler)
        {
            this.HostName = host_name;         
        }

        public SshEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string host_name, string user, object pass) : this(message_handler, host_name)
        {
            
            string ssh_command = Environment.OSVersion.Platform == PlatformID.Win32NT ? "plink.exe" : "ssh";
            string ssh_arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? string.Format("-ssh -l {0} -pw {1} -sshlog plink_ssh.log {2}", user, 
                ToInsecureString(pass), host_name) : "";
            ProcessStartInfo psi = new ProcessStartInfo(ssh_command, ssh_arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;
            ProcessSpawnable s = new ProcessSpawnable(p);
            SshSession = Expect.Spawn(s);
            Action<string> LogFileExists = (o) =>
            {
                OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, "Plink log file exists, overwriting."));
                SshSession.Send("y");
            };
            Action<string> ConnectedToServer = (o) =>
            {
                OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, "Connected to server."));
                SshSession.Send("n");
            };
            Action<string> ServerKeyNotCached = (o) =>
            {
                OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, "server key not cached."));
                SshSession.Send("n");
            };
            WaitAndContinueSession("The session log file \"plink_ssh.log\" already exists.", LogFileExists);
            WaitAndContinueSession("The server's host key is not cached in the registry.", ServerKeyNotCached);
            
            WaitAndContinueSession("Using SSH protocol version 2", ConnectedToServer, 2500);
            
        }


        public bool WaitAndContinueSession(string expected, Action<string> handler, int timeout = 100)
        {
            if (this.SshSession == null) throw new InvalidOperationException("The current Expect Session is null.");
            this.SshSession.Timeout = timeout;
            try
            {
                this.SshSession.Expect(expected, new ExpectedHandlerWithOutput(handler));
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        public string ToInsecureString(object o)
        {
            SecureString s = o as SecureString;
            if (s == null) throw new ArgumentException("Object is not of type SecureString.", "o");
            string r = string.Empty;
            IntPtr ptr = Marshal.SecureStringToBSTR(s);
            try
            {
                r = Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
            return r;
        }


    }
}
