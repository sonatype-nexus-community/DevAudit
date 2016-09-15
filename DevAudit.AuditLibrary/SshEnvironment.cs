using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
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
        public string HostKey { get; private set; }
        public bool IsConnected { get; private set; }
        public string LastEvent { get; set; }
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
            Info("Password: ", ToInsecureString(pass));
            string ssh_command = Environment.OSVersion.Platform == PlatformID.Win32NT ? "plink.exe" : "ssh";
            string ssh_arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? string.Format("-v -ssh -l {0} -pw \"{1}\" -sshlog plink_ssh.log {2}", user,
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
                Info("Plink log file exists, overwriting.");
                SshSession.Send("y");
            };

            Action<string> ConnectedToServer = (o) =>
            {
                Info("Connected to host {0}.", host_name);
            };

            Action<string> FailedConnectToServer = (o) =>
            {
                Info("Failed to connect to host {0}.", host_name);
                this.IsConnected = false;
                return;
            };

            Action<string> GotHostKeyFingerprint = (o) =>
            {
                string lt = Environment.OSVersion.Platform == PlatformID.Win32NT ? "\r\n" : "\n";
                Match m = Regex.Match(o, "Host key fingerprint is:" + lt + "([\\w\\-\\d\\s\\:]+)" + lt);
                if (m.Success && m.Groups.Count == 2)
                {
                    this.HostKey = m.Groups[1].Value;
                    Success("Host key: {0}", this.HostKey);
                }
                else
                {
                    SshSession.Expect.Contains(lt, (hk) =>
                    {
                        Match nm = Regex.Match(hk, "([\\w\\-\\d\\s\\:]+)" + lt);
                        if (nm.Success && nm.Groups.Count == 2)
                        {
                            this.HostKey = nm.Groups[1].Value;
                            Success("Host key: {0}", this.HostKey);
                        }
                        else
                        {
                            throw new Exception("Could not parse host key from output: " + o + hk);
                        }

                    });
                }
            };

            Action<string> ServerKeyNotCached = (o) =>
            {
                Warning("Server key not cached. The host key is not trusted.");
                SshSession.Send("n");
            };

            Action<string> AccessGranted = (o) =>
            {
                this.HostName = host_name;
                this.IsConnected = true;
            };

            Action<string> AccessDenied = (o) =>
            {
                if (o.Contains("Password authentication failed"))
                {
                    this.IsConnected = false;
                    Error("The user name or password is incorrect.");
                    Error("Could not connect to host {0}.", host_name);
                }
            };

            SshSession.Expect.Contains("The session log file \"plink_ssh.log\" already exists.", LogFileExists);
            if (!SshSession.Expect.Contains("Using SSH protocol version 2", ConnectedToServer, 5000).IsMatch)
            {
                Error("Failed to connect to host {0}.", host_name);
                this.IsConnected = false;
                Error("Failed to initialise SSH audit environment.");
                return;
            }
            if (!SshSession.Expect.Contains("Host key fingerprint is:", GotHostKeyFingerprint, 5000).IsMatch)
            {
                throw new Exception("Failed to get host key.");
            }
            SshSession.Expect.Contains("Store key in cache?", ServerKeyNotCached);
            if (SshSession.Expect.Contains("Access granted", AccessGranted, 5000).IsMatch)
            {
                return;
            }
            else
            {
                if (SshSession.Expect.Contains("Access denied", AccessDenied).IsMatch)
                {
                    Error("Failed to initialise SSH audit environment.");
                    return;
                }
                else
                {
                    this.IsConnected = false;
                    Error("Could not connect to host {0}.", host_name);
                    Error("Failed to initialise SSH audit environment.");
                    return;
                }

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
