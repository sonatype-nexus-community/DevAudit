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
    public class SshAuditEnvironment : AuditEnvironment
    {
        #region Public methods
        public FileInfo GetFileAsLocal(string remote_path, string local_path)
        {
            CallerInformation here = this.Here();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ScpClient c = this.CreateScpClient();
            c.BufferSize = 16 * 16384;
            if (c == null) return null;
            try
            {
                FileInfo f = new FileInfo(local_path);            
                c.Download(remote_path, f);
                sw.Stop();
                Debug(here, "Downloaded remote file {0} to {1} via SCP in {2} ms.", remote_path, f.FullName, sw.ElapsedMilliseconds);
                return f;
               
            }
            catch (Exception e)
            {
                Error("Exception thrown attempting to download file {0} from {1} to {2} via SCP.", remote_path, this.HostName, remote_path);
                Error(here, e);
                return null;
            }
            finally
            {
                this.DestroyScpClient(c);
                if (sw.IsRunning) sw.Stop();
            }
        }

        public DirectoryInfo GetDirectoryAsLocal(string remote_path, string local_path)
        {
            CallerInformation here = this.Here();
            Stopwatch sw = new Stopwatch();
            string dir_archive_filename = string.Format("_devaudit_{0}.tgz", this.GetTimestamp());
            SshCommandSpawanble cs = new SshCommandSpawanble(this.SshClient.CreateCommand(string.Format("tar -czf {0} -C {1} . && stat {0} || echo Failed", dir_archive_filename, remote_path)));
            sw.Start();
            ExpectNet.Session cmd_session = Expect.Spawn(cs, this.LineTerminator);
            List<IResult> r = cmd_session.Expect.RegexEither("Size:\\s+([0-9]+)", null, "Failed", null);
            sw.Stop();
            long dir_archive_size;
            cs.Dispose();
            if (r[0].IsMatch)
            {
                Match m = r[0].Result as Match;
                dir_archive_size = long.Parse(m.Groups[1].Value);
                Debug(here, "Archive file {0} created with size {1} bytes in {2} ms.", dir_archive_filename, dir_archive_size, sw.ElapsedMilliseconds);
            }
            else
            {
                Error(here, "Archive file {0} could not be created, command output: {1}", dir_archive_filename, r[1].Text);
                return null;
            }
            sw.Reset();
            sw.Start();
            SshAuditFileInfo dir_archive_file = new SshAuditFileInfo(this, dir_archive_filename);
            LocalAuditFileInfo lf = dir_archive_file.GetAsLocalFile();
            sw.Stop();
            if (lf == null)
            {
                Error(here, "Failed to get archive file {0} as local file.", dir_archive_filename);
                return null;
            }
            Info("Downloaded archive file {0} in to local file {1} in {2} ms.", dir_archive_file.FullName,  lf.FullName, sw.ElapsedMilliseconds);
            Debug("Extracting archive file {0} to work directory {1}.", lf.FullName, this.WorkDirectory.FullName);
            sw.Restart();
            try
            {
                using (Stream fs = File.OpenRead(lf.FullName))
                {
                    ReaderFactory.Open(fs).WriteAllToDirectory(this.WorkDirectory.FullName,
                        new ExtractionOptions()
                        {
                            Overwrite = true,
                            ExtractFullPath = true
                        });
                }
                /*
                ArchiveFactory.WriteToDirectory(lf.FullName, , new ExtractionOptions()
                {
                    Overwrite = true,

                });*/
                sw.Stop();
                Debug(here, "Extracting archive file {0} to work directory {1} in {2} ms.", lf.FullName, this.WorkDirectory.FullName, sw.ElapsedMilliseconds);
                return this.WorkDirectory;
            }
            catch (Exception e)
            {
                Error(here, e);
                return null;
            }
            finally
            {
                if (sw != null && sw.IsRunning) sw.Stop();
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
        #endregion

        #region Public properties
        public string HostName { get; private set; }
        public string User { get; private set; }
        public bool UsePageant { get; private set; }
        public bool UseSshAgent { get; private set; }
        public string HostKey { get; private set; }
        public bool IsConnected { get; private set; }
        public string LastEvent { get; set; }
        public int NetworkConnectTimeout { get; private set; } = 3000;
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
            Stopwatch.Stop();
            if (!string.IsNullOrEmpty(ls_cmd.Result))
            {
                
                Debug("ls {0} returned {1} in {2} ms.", file_path, ls_cmd.Result, Stopwatch.ElapsedMilliseconds);
                return true;
            }
            else
            {
                Debug("ls {0} returned {1} in {2} ms.", file_path, ls_cmd.Error, Stopwatch.ElapsedMilliseconds);
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

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            CallerInformation here = this.Here();
            Dictionary<AuditFileInfo, string> results = new Dictionary<AuditFileInfo, string>(files.Count);
            object results_lock = new object();
            this.Stopwatch.Reset();
            this.Stopwatch.Start();
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (_f, state) => 
            {
                SshCommand cmd = this.SshClient.CreateCommand("cat " + _f.FullName);
                Stopwatch cs = new Stopwatch();
                cs.Start();
                CommandAsyncResult result = cmd.BeginExecute(new AsyncCallback(SshCommandAsyncCallback), new KeyValuePair<SshCommand, Stopwatch> (cmd, cs)) as CommandAsyncResult;
                cmd.EndExecute(result); 
                KeyValuePair<SshCommand, Stopwatch> s = (KeyValuePair<SshCommand, Stopwatch>) result.AsyncState;
                if (s.Key.Result != string.Empty)
                {
                    lock (results_lock)
                    {
                        results.Add(_f, s.Key.Result);
                    }
                    if (s.Value.IsRunning) s.Value.Stop();
                    Debug(here, "Read {0} chars from {1}.", s.Key.Result.Length, _f.FullName);
                    Progress("Read environment files", files.Count, 3, s.Value.Elapsed);
                }                    
                else
                {
                    Error(here, "Could not read {0} as text. Command returned: {1}", _f.FullName, s.Key.Error);
                }
                s.Key.Dispose();
                cs = null;
            });
            this.Stopwatch.Stop();
            Success("Read text for {0} out of {1} files in {2} ms.", results.Count(r => r.Value.Length > 0), results.Count(), Stopwatch.ElapsedMilliseconds);
            return results;
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
        public SshAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string client, string host_name, string user, object pass, OperatingSystem os, LocalEnvironment host_environment) : base(message_handler, os, host_environment)
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
                catch (System.Net.Sockets.SocketException se)
                {
                    Error("Socket error connecting to {0} : {1}", host_name, se.Message);
                    return;
                }
                finally
                {
                    Stopwatch.Stop();
                }
                this.IsConnected = true;
                this.User = user;
                this.HostName = host_name;
                this.pass = pass;
                Success("Connected to {0} in {1} ms.", host_name, Stopwatch.ElapsedMilliseconds);
                this.WorkDirectory = new DirectoryInfo("work" + this.PathSeparator + this.GetTimestamp());
                if (!this.WorkDirectory.Exists)
                {
                    this.WorkDirectory.Create();
                    Debug("Created work directory {0}.", this.WorkDirectory.FullName);
                }
                Info("Using work directory: {0}.", this.WorkDirectory.FullName);
            }
        }
        #endregion

        #region Internal methods
        internal ScpClient CreateScpClient([CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            ScpClient c = new ScpClient(this.HostName, this.User, ToInsecureString(this.pass));
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
                c.Connect();
                
                c.ErrorOccurred += ScpClient_ErrorOccurred;
                c.Downloading += ScpClient_Downloading;
                this.scp_clients.Add(c);
            }
            catch (SshConnectionException ce)
            {
                Error(caller, "Connection error connecting to {0} : {1}", this.HostName, ce.Message);
                return null;
            }
            catch (SshAuthenticationException ae)
            {
                Error(caller, "Authentication error connecting to {0} : {1}", this.HostName, ae.Message);
                return null;
            }
            catch (System.Net.Sockets.SocketException se)
            {
                Error(caller, "Socket error connecting to {0} : {1}", this.HostName, se.Message);
                return null;
            }
            finally
            {
                sw.Stop();
            }
            Debug(caller, "Created SCP connection to {0} in {1} ms.", this.HostName, sw.ElapsedMilliseconds);
            return c;
        }

        internal void DestroyScpClient(ScpClient c, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            if (!scp_clients.Contains(c)) throw new ArgumentException("The ScpClient does not exist in the scp_clients dictionary.");
            if (c.IsConnected) c.Disconnect();
            c.ErrorOccurred -= ScpClient_ErrorOccurred;
            c.Downloading -= ScpClient_Downloading;
            c.Dispose();
            scp_clients.Remove(c);
            Debug(caller, "Destroyed SCP connection to {0}.", this.HostName);
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

        private void ScpClient_Downloading(object sender, ScpDownloadEventArgs e)
        {
            Debug("Scp client downloaded {0} of {1} bytes for file {2}.", e.Downloaded, e.Size, e.Filename);
        }

        private void ScpClient_ErrorOccurred(object sender, ExceptionEventArgs e)
        {
            ScpClient c = sender as ScpClient;
            Error("Scp client threw an exception.");
            Error(e.Exception);
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

        private void ReadFilesAsText_ErrorOccurred(object sender, ExceptionEventArgs e)
        {
            Error("An error occurred attempting to read a file as text: {0}", e.Exception);
        }

        private void SshCommandAsyncCallback(IAsyncResult r)
        {
            CommandAsyncResult car = r as CommandAsyncResult;
            KeyValuePair<SshCommand, Stopwatch> cas = (KeyValuePair<SshCommand, Stopwatch>)car.AsyncState;
            //Debug("Read {0} bytes for execution of {1}.", car.BytesReceived, cas.Key.CommandText);
            if (car.IsCompleted)
            {
                cas.Value.Stop();
                Debug("Completed execution of {0} with {1} bytes received in {2} ms.", cas.Key.CommandText, car.BytesReceived, cas.Value.ElapsedMilliseconds);
            }
        }
        #endregion

        #region Private fields
        ExpectNet.Session SshSession { get; set; }
        SshClient SshClient;
        object pass;
        List<ScpClient> scp_clients = new List<ScpClient>();
        Action<IResult> FileFound;
        Action<IResult> FileNotFound;
        Action<IResult> DirectoryFound;
        Action<IResult> DirectoryNotFound;
        private bool IsDisposed;
        #endregion
    }
}
