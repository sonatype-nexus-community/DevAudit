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
using Alpheus.IO;
namespace DevAudit.AuditLibrary
{
    public class SshAuditEnvironment : AuditEnvironment
    {
        #region Public properties
        public string HostName { get; private set; }
        public bool UsePageant { get; private set; }
        public bool UseSshAgent { get; private set; }
        public string HostKey { get; private set; }
        public bool IsConnected { get; private set; }
        public string LastEvent { get; set; }
        public int NetwrokConnectTimeout { get; private set; } = 3000;
        #endregion

        private Session SshSession { get; set; }
        private Action<string> FileFound;
        private Action<string> FileNotFound;
        private Action<string> DirectoryFound;
        private Action<string> DirectoryNotFound;

        #region Overriden members     
        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new SshAuditFileInfo(this, file_path);
        }

        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return new SshAuditDirectoryInfo(this, dir_path);
        }

        public override bool FileExists(string file_path)
        {
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            this.SshSession.Send.String("ls " + file_path + LineTerminator);
            List<IResult> result = this.SshSession.Expect.ContainsEither(file_path, FileFound, "No such file or directory", FileNotFound, 6000, 5, true);
            if (result[0].IsMatch)
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
            this.SshSession.Send.String("stat " + dir_path + LineTerminator);
            List<IResult> result = this.SshSession.Expect.ContainsEither("directory", FileFound, "No such file or directory", FileNotFound, 6000, 5, true);
            if (result[0].IsMatch)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null)
        {
            CallerInformation here = Here();
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            process_status = ProcessExecuteStatus.Unknown;
            process_output = "";
            process_error = "";
            if (this.SshSession.Send.Command(command + " " + arguments, out process_output, 5000))
            {
                Debug(here, "Send command {0} returned true, output: {1}", command + " " + arguments, process_output);
                process_status = ProcessExecuteStatus.Completed;
                return true;
            }
            else
            {
                process_status = ProcessExecuteStatus.Error;
                return false;
            }
        }
        #endregion

        public SshAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, OperatingSystem os, string host_name) : base(message_handler, os)
        {
            this.HostName = host_name;
            FileFound = (o) =>
            {
                Debug("ls returned file exists.");
            };
            FileNotFound = (o) =>
            {
                Debug("ls returns file does not exist.");
            };
            DirectoryFound = (o) =>
            {
                Debug("stat returned directory exists.");
            };
            DirectoryNotFound = (o) =>
            {
                Debug("stat returns directory does not exist.");
            };
        }

        public SshAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string host_name, string user, object pass, OperatingSystem os) : this(message_handler, os, host_name)
        {
            string ssh_command = Environment.OSVersion.Platform == PlatformID.Win32NT ? "plink.exe" : "ssh";
            string ssh_arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? string.Format("-v -ssh -2 -l {0} -pw \"{1}\" -sshlog plink_ssh.log {2}", user,
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
            SshSession = Expect.Spawn(s, this.LineTerminator);
            Action<string> LogFileExists = (o) =>
            {
                Info("Plink log file exists, overwriting.");
                SshSession.Send.Char('y');
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
                SshSession.Send.Char('n');
            };

            Action<string> AccessGranted = (o) =>
            {
                this.HostName = host_name;
                this.IsConnected = true;
                Success("Password authentication succeded.");
                Success("Connected to host {0}.", host_name);
                return;
            };

            Action<string> PasswordAuthenticationFailed = (o) =>
            {
                if (o.Contains("Password authentication failed"))
                {
                    this.IsConnected = false;
                    Error("The user name or password is incorrect. Could not connect to host {0}.", host_name);
                }
            };

            Action<string> AccessDenied = (o) =>
            {
                this.IsConnected = false;
                Error("Unknown error in authentication. Access denied.");
                return;
            };

            SshSession.Expect.Contains("The session log file \"plink_ssh.log\" already exists.", LogFileExists);
            if (!SshSession.Expect.Contains("Using SSH protocol version 2", ConnectedToServer, 1000, 10).IsMatch)
            {
                Error("Failed to connect to host {0}.", host_name);
                this.IsConnected = false;
                Error("Failed to initialise SSH audit environment.");
                return;
            }
            if (!SshSession.Expect.Contains("Host key fingerprint is:", GotHostKeyFingerprint, 1000, 10).IsMatch)
            {
                throw new Exception("Failed to get host key.");
            }
            SshSession.Expect.Contains("Store key in cache?", ServerKeyNotCached);
            List<IResult> access = SshSession.Expect.ContainsEither("Access granted", AccessGranted, "Password authentication failed", PasswordAuthenticationFailed, 5000);
            if (access[0].IsMatch)
            {
                this.IsConnected = true;
                Success("SSH audit environment initalised."); return;
            }
            else if (access[1].IsMatch || SshSession.Expect.Contains("Access denied", AccessDenied, 100, 5).IsMatch)
            {
                this.IsConnected = false;
                Error("Access denied to host {0}.", host_name);
                Error("Failed to initialise SSH audit environment.");
                return;
            }
            else throw new Exception("Could not parse SSH command output.");
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
