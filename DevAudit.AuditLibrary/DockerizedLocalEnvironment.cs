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
using System.Threading;
using System.Threading.Tasks;


namespace DevAudit.AuditLibrary
{
    public class DockerizedLocalEnvironment : AuditEnvironment
    {
        #region Constructors
        public DockerizedLocalEnvironment(EventHandler<EnvironmentEventArgs> message_handler) : base(message_handler, Environment.OSVersion, null)
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

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("LocalEnvironment");
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
                bool r = this.LocalExecute("chroot", " /hostroot " + command + " " + arguments, out process_status, out process_output, out process_error);
                this.Debug("Execute returned {2} for {0}. Output: {1}. Error:{3}", "chroot /hostroot " + command + " " + arguments, process_output, r, process_error);
                return r;
            }
            else
            {
                bool r = this.LocalExecute(command, arguments, out process_status, out process_output, out process_error);
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
                return !process_output.Contains("no such file or directory") && (process_output.Contains("regular file") || process_output.Contains("symbolic link"));
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
                return !process_output.Contains("no such file or directory") && (process_output.Contains("directory") || process_output.Contains("symbolic link"));
            }

            else
            {
                this.Debug(caller, "Execute returned true for stat {0}. Output: {1}. Error: {2}.", dir_path, process_output, process_error);
                return false;
            }
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            CallerInformation here = this.Here();
            Dictionary<AuditFileInfo, string> results = new Dictionary<AuditFileInfo, string>(files.Count);
            object results_lock = new object();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int read_count = 0;
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length;
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (_f, state) =>
            {
                string process_output = "";
                string process_error = "";
                ProcessExecuteStatus process_status;
                bool r = this.Execute("cat", _f.FullName, out process_status, out process_output, out process_error);
                if (r)
                {
                    if (process_output.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
                    {
                        process_output = process_output.Remove(0, lastIndexOfUtf8);
                    }
                    if (process_output == string.Format("cat: {0}: No such file or directory", _f.FullName))
                    {
                        this.Error(here, "File {0} does not exist.", _f.FullName);
                    }
                    else
                    {   
                        lock (results_lock)
                        {
                            results.Add(_f, process_output);
                        }
                        Interlocked.Increment(ref read_count);
                        Debug(here, string.Format("Read {1} chars from local file {0}.", _f.FullName, process_output.Length), files.Count, read_count);
                    }
                }
                else
                {
                    Error(here, "Could not read {0} as text. Command returned: {1} {2}", _f.FullName, process_output, process_error);
                }
            });
            sw.Stop();
            Success("Read text for {0} out of {1} files in {2} ms.", results.Count(r => r.Value.Length > 0), results.Count(), sw.ElapsedMilliseconds);
            return results;
        }

        #endregion

        #region Properties
        public bool HostRootIsMounted { get; private set; }
        #endregion

        #region Methods
        public bool LocalExecute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            FileInfo cf = new FileInfo(command);
            int? process_exit_code = null;
            StringBuilder process_out_sb = new StringBuilder();
            StringBuilder process_err_sb = new StringBuilder();
            ProcessStartInfo psi = new ProcessStartInfo(command);
            psi.Arguments = arguments;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            if (cf.Exists)
            {
                psi.WorkingDirectory = cf.Directory.FullName;
            }
            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_out_sb.AppendLine(e.Data);
                    OutputDataReceived?.Invoke(e.Data);
                }
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_err_sb.AppendLine(e.Data);
                    OutputErrorReceived?.Invoke(e.Data);
                }

            };
            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                process_exit_code = p.ExitCode;
                p.Close();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    process_status = ProcessExecuteStatus.FileNotFound;
                    process_err_sb.AppendLine(e.Message);
                    return false;
                }
            }
            finally
            {
                process_output = process_out_sb.ToString();
                process_error = process_err_sb.ToString();
                p.Dispose();
            }

            if ((process_exit_code.HasValue && process_exit_code.Value != 0))
            {
                process_status = ProcessExecuteStatus.Error;
                return false;
            }
            else if ((process_exit_code.HasValue && process_exit_code.Value == 0))
            {
                process_status = ProcessExecuteStatus.Completed;
                return true;
            }
            else
            {
                process_status = ProcessExecuteStatus.Unknown;
                return false;

            }
        }
        #endregion
    }
}