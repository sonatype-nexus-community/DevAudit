using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DevAudit.AuditLibrary
{
    public class SshAuditEnvironment : AuditEnvironment
    {
        #region Public methods
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
        #endregion

        #region Public properties
        public string HostName { get; private set; }
        public bool UsePageant { get; private set; }
        public bool UseSshAgent { get; private set; }
        public string HostKey { get; private set; }
        public bool IsConnected { get; private set; }
        public string LastEvent { get; set; }
        public int NetwrokConnectTimeout { get; private set; } = 3000;
        #endregion

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("SshAuditEnvironment");
        #endregion

        #region Overriden methods     
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
            Stopwatch.Reset();
            Stopwatch.Start();
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            SshCommand ls_cmd = SshClient.RunCommand("stat " + file_path);
            if (!string.IsNullOrEmpty(ls_cmd.Result))
            {
                Stopwatch.Stop();
                Debug("ls {0} returned {1}. Time elapsed: {2} ms.", file_path, ls_cmd.Result, Stopwatch.ElapsedMilliseconds);
                return true;
            }
            else
            {
                Debug("ls {0} returned {1}.", file_path, ls_cmd.Error);
                return false;
            }

        }

        public override bool DirectoryExists(string dir_path)
        {
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            SshCommand stat_cmd = SshClient.RunCommand("stat " + dir_path);
            if (!string.IsNullOrEmpty(stat_cmd.Result))
            {
                Debug("stat {0} returned {1}.", dir_path, stat_cmd.Result);
                return true;
            }
            else
            {
                Debug("stat {0} returned {1}.", dir_path, stat_cmd.Error);
                return false;
            }            
        }

        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            process_status = ProcessExecuteStatus.Unknown;
            process_output = "";
            process_error = "";
            SshCommand ssh_command = this.SshClient.RunCommand(command + " " + arguments);
            process_output = ssh_command.Result.Trim();
            process_error = ssh_command.Error.Trim();
            if (!string.IsNullOrEmpty(ssh_command.Result))
            {
                Debug(caller, "Command {0} completed successfully, output: {1}", command + " " + arguments, process_output);
                process_status = ProcessExecuteStatus.Completed;
                return true;
            }
            else
            {
                process_status = ProcessExecuteStatus.Error;
                Debug(caller, "Send command {0} did not complete successfully, output: {1}", command + " " + arguments, process_error);
                return false;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            try
            {
                if (!this.IsDisposed)
                {
                    // Explicitly set root references to null to expressly tell the GarbageCollector 
                    // that the resources have been disposed of and its ok to release the memory 
                    // allocated for them.
                    
                    if (isDisposing)
                    {
                        // Release all managed resources here 
                        // Need to unregister/detach yourself from the events. Always make sure 
                        // the object is not null first before trying to unregister/detach them! 
                        // Failure to unregister can be a BIG source of memory leaks 
                        //if (someDisposableObjectWithAnEventHandler != null)
                        //{ someDisposableObjectWithAnEventHandler.SomeEvent -= someDelegate; 
                        //someDisposableObjectWithAnEventHandler.Dispose(); 
                        //someDisposableObjectWithAnEventHandler = null; } 
                        // If this is a WinForm/UI control, uncomment this code 
                        //if (components != null) //{ // components.Dispose(); //} } 
                        // Release all unmanaged resources here 
                        // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                        if (!ReferenceEquals(this.SshClient, null))
                        {
                            this.SshClient.HostKeyReceived -= SshClient_HostKeyReceived;
                            this.SshClient.ErrorOccurred -= SshClient_ErrorOccurred;
                            this.SshClient.Dispose();
                            this.SshClient = null;
                        }
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }

        }
        #endregion

        #region Constructors
        public SshAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string client, string host_name, string user, object pass, OperatingSystem os) : base(message_handler, os)
        {
            if (client == "openssh")
            {
                InitialiseOpenSshSession(host_name, user, pass, os);
            }
            else if (client == "plink")
            {
                InitialiseOpenSshSession(host_name, user, pass, os);
            }
            else
            {
                InitialiseSshSession(host_name, user, pass, os);
            }
        }
        #endregion

        #region Private methods
        public void InitialisePlinkSesion(string host_name, string user, object pass, OperatingSystem os)
        {
            this.HostName = host_name;
            FileFound = (o) =>
            {
                Debug(this.Here(), "ls returned file exists.");
            };
            FileNotFound = (o) =>
            {
                Debug(this.Here(), "ls returns file does not exist.");
            };
            DirectoryFound = (o) =>
            {
                Debug(this.Here(), "stat returned directory exists.");
            };
            DirectoryNotFound = (o) =>
            {
                Debug(this.Here(), "stat returns directory does not exist.");
            };

            string ssh_command = Environment.OSVersion.Platform == PlatformID.Win32NT ? "plink.exe" : "ssh";
            string ssh_arguments = Environment.OSVersion.Platform == PlatformID.Win32NT ? string.Format("-v -ssh -2 -l {0} -pw \"{1}\" -sshlog plink_ssh.log {2}", user,
                ToInsecureString(pass), host_name) : "";
            ProcessStartInfo psi = new ProcessStartInfo(ssh_command, ssh_arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;
            ProcessSpawnable s = new ProcessSpawnable(p);
            SshSession = Expect.Spawn(s, this.LineTerminator);
            Action<IResult> LogFileExists = (o) =>
            {
                Info("Plink log file exists, overwriting.");
                SshSession.Send.Char('y');
            };

            Action<IResult> ConnectedToServer = (o) =>
            {
                Info("Connected to host {0}.", host_name);
            };

            Action<IResult> FailedConnectToServer = (o) =>
            {
                Info("Failed to connect to host {0}.", host_name);
                this.IsConnected = false;
                return;
            };

            Action<IResult> GotHostKeyFingerprint = (match) =>
            {
                string lt = Environment.OSVersion.Platform == PlatformID.Win32NT ? "\r\n" : "\n";
                Match m = Regex.Match((string)match.Result, "Host key fingerprint is:" + lt + "([\\w\\-\\d\\s\\:]+)" + lt);
                if (m.Success && m.Groups.Count == 2)
                {
                    this.HostKey = m.Groups[1].Value;
                    Success("Host key: {0}", this.HostKey);
                }
                else
                {
                    SshSession.Expect.Contains(lt, (hk) =>
                    {
                        Match nm = Regex.Match((string)hk.Result, "([\\w\\-\\d\\s\\:]+)" + lt);
                        if (nm.Success && nm.Groups.Count == 2)
                        {
                            this.HostKey = nm.Groups[1].Value;
                            Success("Host key: {0}", this.HostKey);
                        }
                        else
                        {
                            throw new Exception("Could not parse host key from output: " + nm.Value);
                        }

                    });
                }
            };

            Action<IResult> ServerKeyNotCached = (o) =>
            {
                Warning("Server key not cached. The host key is not trusted.");
                SshSession.Send.Char('n');
            };

            Action<IResult> AccessGranted = (o) =>
            {
                this.HostName = host_name;
                this.IsConnected = true;
                Success("Password authentication succeded.");
                Success("Connected to host {0}.", host_name);
                return;
            };

            Action<IResult> PasswordAuthenticationFailed = (match) =>
            {

                string o = (string)match.Result;
                if (o.Contains("Password authentication failed"))
                {
                    this.IsConnected = false;
                    Error("The user name or password is incorrect. Could not connect to host {0}.", host_name);
                }
            };

            Action<IResult> AccessDenied = (o) =>
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
        public void InitialiseOpenSshSession(string host_name, string user, object pass, OperatingSystem os)
        {
            #region Create actions for client responses
            Action<IResult> ConnectionEstablished = (result) =>
            {
                Info("Connected to host {0}.", host_name);
            };
            Action<IResult> FailedToConnect = (result) =>
            {
                Error(Here(), "Failed to connect to host {0}. SSH output: {1}", host_name, (string) result.Text);
            };
            Action<IResult> HostKeyReceived = (result) =>
            {
                Match m = result.Result as Match;
                Success("Received host key of type {0} with fingerprint {1}", m.Groups[1].Value, m.Groups[2].Value);
            };
            Action<IResult> FailedToReceiveHostKey = (result) =>
            {
                Match m = result.Result as Match;
                Error(Here(), "Failed to receive host key. SSH output: {0}", (string) result.Result);
            };
            Action<IResult> HostKnown = (result) =>
            {
                Info("Host {0} is know and matches the host key.", host_name);
            };
            Action<IResult> HostNotKnown = (result) =>
            {
                Warning("Host key not known. The host key is not trusted. The host fingerprint will be added to your known_hosts file.");
                //SshSession.Send.String("yes", true);
         
            };
            #endregion

            string ssh_command = Environment.OSVersion.Platform == PlatformID.Win32NT ? "openssh-win32\\ssh.exe" : "ssh";
            string ssh_arguments = string.Format("-v -l {0} {1}", user, host_name);
            ProcessStartInfo psi = new ProcessStartInfo(ssh_command, ssh_arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += OnOutputDataReceived;
            p.ErrorDataReceived += OnErrorDataReceived;
            ProcessSpawnable s = new ProcessSpawnable(p);
            SshSession = Expect.Spawn(s, this.LineTerminator);
            IResult r = SshSession.Expect.Regex("Reading configuration data (\\S+)", null, 300);
            if (r.IsMatch)
            {
                Match m = (Match) r.Result;
                Info("Using OpenSSH configuration from {0}.", m.Groups[1].Value);
            }
            if (!SshSession.Expect.ContainsElse("Connection established", ConnectionEstablished, FailedToConnect, 6000).IsMatch)
            {
                this.IsConnected = false;
                return;
            } 
            if (!SshSession.Expect.RegexElse("Server host key: ([a-zA-Z0-9\\-]+)\\s+([a-zA-Z0-9\\-\\:]+)", HostKeyReceived, FailedToReceiveHostKey, 6000).IsMatch)
            {
                this.IsConnected = false;
                return;
            }
            List<IResult> ok = SshSession.Expect.ContainsEither(string.Format("is known", host_name), HostKnown, "can't be established", HostNotKnown, 6000);
          
        }

        private void InitialiseSshSession(string host_name, string user, object pass, OperatingSystem os)
        {
            Info("Connecting to {0}...", host_name);
            ConnectionInfo ci = new ConnectionInfo(host_name, user, new PasswordAuthenticationMethod(user, ToInsecureString(pass)));
            SshClient = new SshClient(ci);
            SshClient.ErrorOccurred += SshClient_ErrorOccurred;
            SshClient.HostKeyReceived += SshClient_HostKeyReceived;
            Stopwatch.Reset();
            Stopwatch.Start();
            try
            {
                SshClient.Connect();
            }
            catch (SshConnectionException ce)
            {
                Error("Connection error connecting to {0} : {1}", host_name, ce.Message);
                return;
            }
            catch (SshAuthenticationException ae)
            {
                Error("Authentication error connecting to {0} : {1}", host_name, ae.Message);
                return;
            }
            catch(System.Net.Sockets.SocketException se)
            {
                Error("Socket error connecting to {0} : {1}", host_name, se.Message);
                return;
            }
            finally
            {
                Stopwatch.Stop();
            }
            this.IsConnected = true;
            Success("Connected to {0}. Time elapsed: {1} ms.", host_name, Stopwatch.ElapsedMilliseconds);
        }

        public static string ByteArrayToHexString(byte[] Bytes)
        {
            StringBuilder Result = new StringBuilder(Bytes.Length * 2);
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in Bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }
        #endregion

        #region Event handlers
        private void SshClient_ErrorOccurred(object sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {
            Error(e.Exception);
            this.IsConnected = false;
            return;
        }

        private void SshClient_HostKeyReceived(object sender, Renci.SshNet.Common.HostKeyEventArgs e)
        {
            Info("Host key fingerprint is: {0} {1}.", e.HostKeyName, BitConverter.ToString(e.FingerPrint).Replace('-', ':').ToLower());
        }

        #endregion

        #region Private fields
        private ExpectNet.Session SshSession { get; set; }
        SshClient SshClient;
        Action<IResult> FileFound;
        Action<IResult> FileNotFound;
        Action<IResult> DirectoryFound;
        Action<IResult> DirectoryNotFound;
        private bool IsDisposed;
        #endregion

    }
}
