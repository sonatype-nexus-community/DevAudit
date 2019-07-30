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
    public class SshAuditEnvironment : AuditEnvironment, IOperatingSystemEnvironment
    {
        #region Constructors
        public SshAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string client, string host_name, int port, string user, object pass, string keyfile, OperatingSystem os, LocalEnvironment host_environment) 
            : base(message_handler, os, host_environment)
        {
            ConnectionInfo ci;
            Info("Connecting to {0}:{1}...", host_name, port);
            if (string.IsNullOrEmpty(keyfile))
            {
                ci = new ConnectionInfo(host_name, port, user, new PasswordAuthenticationMethod(user, ToInsecureString(pass)));
            }
            else if (!string.IsNullOrEmpty(keyfile) && pass != null)
            {
                ci = new ConnectionInfo(host_name, port, user, new PrivateKeyAuthenticationMethod(user, 
                    new PrivateKeyFile[] { new PrivateKeyFile(keyfile, ToInsecureString(pass)) }));
            }
            else 
            {
                ci = new ConnectionInfo(host_name, port, user, new PrivateKeyAuthenticationMethod(user));
            }
            SshClient = new SshClient(ci);
            SshClient.ErrorOccurred += SshClient_ErrorOccurred;
            SshClient.HostKeyReceived += SshClient_HostKeyReceived;
            SshClient.ConnectionInfo.AuthenticationBanner += Ci_AuthenticationBanner;
            Stopwatch sw = new Stopwatch();
            sw.Start();
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
                sw.Stop();
            }
            if (!SshClient.IsConnected || !SshClient.ConnectionInfo.IsAuthenticated)
            {
                Error("Failed to connect or authenticate to {0}", host_name);
                return;
            }
            this.IsConnected = true;
            this.User = user;
            this.HostName = host_name;
            this.ssh_client_pass = pass;
            Success("Connected to {0} in {1} ms.", host_name, sw.ElapsedMilliseconds);
            string tmp_dir = Environment.OSVersion.Platform == PlatformID.Win32NT ? Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User) : "/tmp";
            if (!string.IsNullOrEmpty(tmp_dir) && Directory.Exists(tmp_dir))
            {
                this.WorkDirectory = new DirectoryInfo(Path.Combine(tmp_dir, "devaudit-work", this.GetTimestamp()));
            }
            else
            {
                Warning("Could not get value of temporary directory from environment. The work directory wll be created in the DevAudit root directory.");
                this.WorkDirectory = new DirectoryInfo(Path.Combine("work",  this.GetTimestamp()));
            }
            if (!this.WorkDirectory.Exists)
            {
                this.WorkDirectory.Create();
                this.WorkDirectory.Refresh();
                Debug("Created work directory {0}.", this.WorkDirectory.FullName);
            }
            Info("Using work directory: {0}.", this.WorkDirectory.FullName);
            
        }
        #endregion

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("SshAuditEnvironment");
        public override int MaxConcurrentExecutions { get; } = 0;
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
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            SshCommand ls_cmd = SshClient.RunCommand("stat " + file_path);
            sw.Stop();
            if (!string.IsNullOrEmpty(ls_cmd.Result))
            {
                
                Debug("stat {0} returned {1} in {2} ms.", file_path, ls_cmd.Result, sw.ElapsedMilliseconds);
                return true;
            }
            else
            {
                Debug("stat {0} returned {1} in {2} ms.", file_path, ls_cmd.Error, sw.ElapsedMilliseconds);
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

        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Dictionary<string, string> env = null,
            Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            process_status = ProcessExecuteStatus.Unknown;
            process_output = "";
            process_error = ""; 
            if (env != null && env.Count > 0)
            {
                StringBuilder vars = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in env)
                {
                    vars.AppendFormat("{0}={1} ", kv.Key, kv.Value); 
                }
                command = vars.ToString() + command;
            }
            SshCommand cmd = this.SshClient.CreateCommand(command + " " + arguments);
            Debug("Executing command {0} {1}.", command, arguments);
            Stopwatch cs = new Stopwatch();
            cs.Start();
            CommandAsyncResult result;
            try
            {
                result = cmd.BeginExecute(new AsyncCallback(SshCommandAsyncCallback), new KeyValuePair<SshCommand, Stopwatch>(cmd, cs)) as CommandAsyncResult;
                cmd.EndExecute(result);
            }
            catch (SshConnectionException sce)
            {
                Error(caller, sce, "SSH connection error attempting to execute {0} {1}.", command, arguments);
                return false;
            }
            catch (SshOperationTimeoutException te)
            {
                Error(caller, te, "SSH connection timeout attempting to execute {0} {1}.", command, arguments);
                return false;
            }
            catch (SshException se)
            {
                Error(caller, se, "SSH error attempting to execute {0} {1}.", command, arguments);
                return false;
            }
            catch (Exception e)
            {
                Error(caller, e, "Error attempting to execute over SSH {0} {1}.", command, arguments);
                return false;
            }
            KeyValuePair<SshCommand, Stopwatch> s = (KeyValuePair<SshCommand, Stopwatch>) result.AsyncState;
            process_output = s.Key.Result.Trim();
            process_error = s.Key.Error.Trim();
            if (s.Value.IsRunning) s.Value.Stop();
            process_output = cmd.Result.Trim();
            process_error = process_output + cmd.Error.Trim();
            if (cmd.ExitStatus == 0)
            {
                Debug(caller, "Execute {0} returned zero exit code. Output: {1}.", command + " " + arguments, process_output);
                process_status = ProcessExecuteStatus.Completed;
                cmd.Dispose();
                return true;
            }
            else
            {
                process_status = ProcessExecuteStatus.Error;
                Debug(caller, "Execute {0} returned non-zero exit code {2}. Error: {1}.", command + " " + arguments, process_error, cmd.ExitStatus);
                cmd.Dispose();
                return false;
            }
        }

        public override bool ExecuteAsUser(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, string user, SecureString password, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            process_status = ProcessExecuteStatus.Unknown;
            process_output = "";
            process_error = "";
            string c;
            if (password == null)
            {
                c = string.Format("-n -u {0} -s {1} {2}", user, command, arguments);
                return this.Execute("sudo", c, out process_status, out process_output, out process_error);
            }
            StringBuilder shell_data = new StringBuilder();
            ShellStream stream = this.SshClient.CreateShellStream("dumb", 0, 0, 800, 600, 1024, new Dictionary<TerminalModes, uint> { { TerminalModes.ECHO, 0 } });
            stream.DataReceived += (s, d) => shell_data.Append(Encoding.UTF8.GetString(d.Data));
            c = string.Format("PAGER=cat su -c \"echo CMD_START && {0} {1} && echo CMD_SUCCESS || echo CMD_ERROR\" {2} || echo CMD_ERROR", command, arguments, user);
            byte[] b = Encoding.UTF8.GetBytes(c + this.LineTerminator);
            Stopwatch cs = new Stopwatch();
            cs.Start();
            IAsyncResult wr = stream.BeginWrite(b, 0, b.Length, new AsyncCallback(SshStreamWriteAsyncCallback), new KeyValuePair<string, ShellStream>(c, stream));
            stream.EndWrite(wr);
            bool got_password_prompt = false;
            ExpectAction[] got_password_prompt_action =
           {
               new ExpectAction("Password:", (o) =>
               {
                   b = Encoding.UTF8.GetBytes(ToInsecureString(password) + LineTerminator);
                   got_password_prompt = true;
                   wr = stream.BeginWrite(b, 0, b.Length, new AsyncCallback(SshStreamWriteAsyncCallback), new KeyValuePair<string, ShellStream>(c, stream));
               })
            };
            cs.Restart();
            IAsyncResult er = stream.BeginExpect(new TimeSpan(0, 0, 5), new AsyncCallback(SshExpectAsyncCallback), new KeyValuePair<string, Stopwatch>(c, cs), got_password_prompt_action);
            stream.EndExpect(er);
            if (!got_password_prompt)
            {
                process_status = ProcessExecuteStatus.Error;
                Error(caller, "Unexpected response from server attempting to execute {0}: {1}", c, shell_data);
                return false;
            }
            stream.EndWrite(wr);
            bool cmd_success = false;
            string cmd_output = string.Empty;
            ExpectAction[] cmd_actions =
            {
               new ExpectAction("CMD_ERROR", (o) =>
               {
                   cmd_output = o.Replace("CMD_ERROR", string.Empty);
                   cmd_success = false;
               }),
               new ExpectAction("CMD_SUCCESS", (o) =>
               {
                   cmd_output = o.Replace("CMD_SUCCESS", string.Empty).Replace("CMD_START", string.Empty);
                   cmd_success = true;
               }),
            };
            er = stream.BeginExpect(new TimeSpan(0, 0, 5), new AsyncCallback(SshExpectAsyncCallback), new KeyValuePair<string, Stopwatch>(c, cs), cmd_actions);
            stream.EndExpect(er);
            if (!cmd_success)
            {
                process_status = ProcessExecuteStatus.Error;
                Debug(caller, "Execute {0} {1} returned non-zero exit code. Output: {2}.", command, arguments, cmd_output);
                return false;
            }
            else
            {
                Debug(caller, "Execute {0} {1} returned zero exit code. Output: {2}.", command, arguments, cmd_output);
                process_status = ProcessExecuteStatus.Completed;
                process_output = cmd_output.Trim('\r', '\n');
                return true;
            }
        }

        public List<Tuple<string, ProcessExecuteStatus, string, string>> ExecuteMany(List<Tuple<string, string>> commands)
        {
            CallerInformation caller = this.Here();
            if (!this.IsConnected) throw new InvalidOperationException("The SSH session is not connected.");
            List<Tuple<string, ProcessExecuteStatus, string, string>> results = new List<Tuple<string, ProcessExecuteStatus, string, string>>(commands.Count);
            object results_lock = new object();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Parallel.ForEach(commands, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (_c, state) =>
            {
                string process_output = string.Empty;
                string process_error = string.Empty;
                SshCommand cmd = this.SshClient.CreateCommand(_c.Item1 + " " + _c.Item2);
                Stopwatch cs = new Stopwatch();
                cs.Start();
                CommandAsyncResult result = cmd.BeginExecute(new AsyncCallback(SshCommandAsyncCallback), new KeyValuePair<SshCommand, Stopwatch>(cmd, cs)) as CommandAsyncResult;
                cmd.EndExecute(result);
                KeyValuePair<SshCommand, Stopwatch> s = (KeyValuePair<SshCommand, Stopwatch>) result.AsyncState;
                process_output = s.Key.Result.Trim();
                process_error = s.Key.Error.Trim();
                if (s.Value.IsRunning) s.Value.Stop();
                if (process_output!= string.Empty)
                {
                    lock (results_lock)
                    {
                        results.Add(new Tuple<string, ProcessExecuteStatus, string, string>(_c.Item1, ProcessExecuteStatus.Completed, process_output, process_error));
                    }
                    Debug(caller, "Execute {0} completed with {1} {2}.", s.Key.Result.Length, cmd.CommandText, process_output, process_error);
                }
                else
                {
                    lock (results_lock)
                    {
                        results.Add(new Tuple<string, ProcessExecuteStatus, string, string>(_c.Item1, ProcessExecuteStatus.Error, process_output, process_error));
                    }
                    Debug(caller, "Execute {0} did not complete successfully: {1} {2}.", s.Key.Result.Length, cmd.CommandText, process_output, process_error);
                }
            });
            return results;
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            CallerInformation here = this.Here();
            Dictionary<AuditFileInfo, string> results = new Dictionary<AuditFileInfo, string>(files.Count);
            object results_lock = new object();
            Stopwatch sw = new Stopwatch();
            sw.Start();
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
            sw.Stop();
            Success("Read text for {0} out of {1} files in {2} ms.", results.Count(r => r.Value.Length > 0), results.Count(), sw.ElapsedMilliseconds);
            return results;
        }


        #endregion

        #region Properties
        public string HostName { get; private set; }
        public string User { get; private set; }
        public bool UsePageant { get; private set; }
        public bool UseSshAgent { get; private set; }
        public string HostKey { get; private set; }
        public bool IsConnected { get; private set; }
        public string LastEvent { get; set; }
        public TimeSpan NetworkConnectTimeout { get; private set; } = new TimeSpan(0, 0, 5);
        #endregion

        #region Methods
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
            sw.Restart();
            SshAuditFileInfo dir_archive_file = new SshAuditFileInfo(this, dir_archive_filename);
            LocalAuditFileInfo lf = dir_archive_file.GetAsLocalFile();
            sw.Stop();
            if (lf == null)
            {
                Error(here, "Failed to get archive file {0} as local file.", dir_archive_filename);
                return null;
            }
            Info("Downloaded archive file {0} in to local file {1} in {2} ms.", dir_archive_file.FullName, lf.FullName, sw.ElapsedMilliseconds);
            cs = new SshCommandSpawanble(this.SshClient.CreateCommand(string.Format("rm {0} && echo Succeded || echo Failed", dir_archive_filename)));
            cmd_session = Expect.Spawn(cs, this.LineTerminator);
            r = cmd_session.Expect.RegexEither("Succeded", null, "Failed", null);
            if (r[0].IsMatch)
            {
                Debug("Deleted archive file {0} from remote server.", dir_archive_file.FullName);
            }
            else
            {
                Debug("Failed to delete archive file {0} from remote server. It is safe to delete this file manually.", dir_archive_file.FullName);
            }
            sw.Restart();
            try
            {
                using (Stream fs = File.OpenRead(lf.FullName))
                {
                    ReaderFactory.Open(fs).WriteAllToDirectory(this.WorkDirectory.FullName,
                        new SharpCompress.Common.ExtractionOptions()
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
       
        internal ScpClient CreateScpClient([CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            ScpClient c = new ScpClient(this.HostName, this.User, ToInsecureString(this.ssh_client_pass));
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

        private void Ci_AuthenticationBanner(object sender, AuthenticationBannerEventArgs e)
        {
            if (e.BannerMessage.ToLower().Contains("ubuntu"))
            {
                this.OSName = "ubuntu";
            }
            else if (e.BannerMessage.ToLower().Contains("debian"))
            {
                this.OSName = "debian";
            }
            else if (e.BannerMessage.ToLower().Contains("redhat"))
            {
                this.OSName = "redhat";
            }
            else if (e.BannerMessage.ToLower().Contains("centos"))
            {
                this.OSName = "centos";
            }
        }

        private void ReadFilesAsText_ErrorOccurred(object sender, ExceptionEventArgs e)
        {
            Error("An error occurred attempting to read a file as text: {0}", e.Exception);
        }

        private void SshCommandAsyncCallback(IAsyncResult r)
        {
            CommandAsyncResult car = r as CommandAsyncResult;
            KeyValuePair<SshCommand, Stopwatch> cas = (KeyValuePair<SshCommand, Stopwatch>) car.AsyncState;
            if (car.IsCompleted)
            {
                cas.Value.Stop();
                Debug("Completed execution of {0} with {1} bytes received in {2} ms.", cas.Key.CommandText, car.BytesReceived, cas.Value.ElapsedMilliseconds);
            }
        }

        private void SshStreamWriteAsyncCallback(IAsyncResult r)
        {
            KeyValuePair<string, ShellStream> cas = (KeyValuePair<string, ShellStream>) r.AsyncState;
            if (r.IsCompleted)
            {
                cas.Value.Flush();
                Debug("Completed stream operation for write {0}.", cas.Key);
            }
        }

        private void SshExpectAsyncCallback(IAsyncResult r)
        {
            ExpectAsyncResult ear  = r as ExpectAsyncResult;
            KeyValuePair<string, Stopwatch> cas = (KeyValuePair<string, Stopwatch>)r.AsyncState;
            if (r.IsCompleted)
            {
                cas.Value.Stop();
                Debug("Completed expect operation {0} in {1} ms.", cas.Key, cas.Value.ElapsedMilliseconds);
            }
        }
        #endregion

        #region Fields
        ExpectNet.Session SshSession { get; set; }
        SshClient SshClient;
        object ssh_client_pass;
        List<ScpClient> scp_clients = new List<ScpClient>();
        private bool IsDisposed = false;
        #endregion

        #region Disposer and Finalizer
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
                        if (!ReferenceEquals(this.SshClient, null))
                        {
                            this.SshClient.HostKeyReceived -= SshClient_HostKeyReceived;
                            this.SshClient.ErrorOccurred -= SshClient_ErrorOccurred;
                            this.SshClient.Dispose();
                            this.SshClient = null;
                        }
                    }
                    // Release all unmanaged resources here 
                    // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                    if (this.WorkDirectory != null)
                    {
                        if (this.WorkDirectory.Exists)
                        {
                            this.WorkDirectory.Delete(true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("Exception thrown during disposal of Ssh audit environment.", e);
            }
            finally
            {
                this.IsDisposed = true;
            }
            base.Dispose(isDisposing);
        }

        ~SshAuditEnvironment()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
