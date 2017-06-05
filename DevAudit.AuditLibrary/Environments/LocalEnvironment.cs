using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;


using ExpectNet;
namespace DevAudit.AuditLibrary
{
    public class LocalEnvironment : AuditEnvironment
    {
        #region Constructors
        public LocalEnvironment(EventHandler<EnvironmentEventArgs> message_handler) : base(message_handler, Environment.OSVersion, null)
        {
            this.ScriptEnvironment = new ScriptEnvironment(this);
        }
        public LocalEnvironment() : base(null, Environment.OSVersion, null) { }
        #endregion

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("LocalEnvironment");
        #endregion

        #region Overriden methods
        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return new LocalAuditDirectoryInfo(this, dir_path);
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new LocalAuditFileInfo(this, file_path);
        }

        public override bool FileExists(string file_path)
        {
            return File.Exists(file_path);
        }

        public override bool DirectoryExists(string dir_path)
        {
            return Directory.Exists(dir_path);
        }

        public override bool Execute(string command, string arguments, 
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
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
                    process_err_sb.AppendLine (e.Message);
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

        public override bool ExecuteAsUser(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, string user, SecureString password, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            if (this.OS.Platform == PlatformID.Win32NT)
            {
                string domain_name = Environment.UserDomainName;
                if (user.Contains("\\"))
                {
                    string[] u = user.Split('\\');
                    domain_name = u[0];
                    user = u[1];
                }
                FileInfo cf = new FileInfo(command);
                int? process_exit_code = null;
                StringBuilder process_out_sb = new StringBuilder();
                StringBuilder process_err_sb = new StringBuilder();
                ProcessStartInfo psi = new ProcessStartInfo(command);
                psi.UserName = user;
                psi.Password = password;
                psi.Domain = domain_name;
                psi.Arguments = arguments;
                psi.CreateNoWindow = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardInput = true;
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
            else
            {
                Error("Executing commands as a different operating system user in the local environment is not suppported on *nix. Use the su command to run DevAudit as the required operating system user.");
                process_error = string.Empty;
                process_output = string.Empty;
                process_status = ProcessExecuteStatus.Error;
                return false;
                /*
                string args = string.Format("-c \"echo CMD_START && {0} {1} && echo CMD_SUCCESS || echo CMD_ERROR\" {2} || echo CMD_ERROR", command, arguments, user);
                ProcessStartInfo psi = new ProcessStartInfo("su");
                psi.Arguments = args;
                psi.CreateNoWindow = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardInput = true;
                psi.UseShellExecute = false;
                Process p = new Process();
                p.EnableRaisingEvents = false;
                p.StartInfo = psi;
                ProcessSpawnable s = new ProcessSpawnable(p);
                Session cmd_session = Expect.Spawn(s, this.LineTerminator);
                IResult r = cmd_session.Expect.Contains("Password:", null);
                if (r.IsMatch)
                {
                    s.Write(ToInsecureString(password) + this.LineTerminator);
                }
                else
                {
                    if (!p.HasExited) p.Close();
                    p.Dispose();
                    process_status = ProcessExecuteStatus.Error;
                    process_output = r.Text;
                    process_error = string.Empty;
                    Error(caller, "Unexpected response from server attempting to execute su {0}: {1}", args, process_output);
                    return false;
                }
                List<IResult> cmd_result = cmd_session.Expect.ContainsEither("CMD_SUCCESS", null, "CMD_ERROR", null);
                if (!p.HasExited) p.Close();
                p.Dispose();
                if (cmd_result.First().IsMatch)
                {
                    process_status = ProcessExecuteStatus.Completed;
                    string o = (string)cmd_result.First().Result;
                    process_output = o.Replace("CMD_START", string.Empty).Replace("CMD_SUCCESS", string.Empty);
                    process_error = string.Empty;
                    return true;
                }
                else
                {
                    process_status = ProcessExecuteStatus.Error;
                    string o = (string)cmd_result.Last().Result;
                    process_output = o.Replace("CMD_ERROR", string.Empty);
                    process_error = string.Empty;
                    return false;
                }
            }*/
                /*
                CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
                List<string> args = arguments.Split('\t').ToList();
                string cmd = command;
                foreach (string a in args)
                {
                    cmd += " \"" + a + "\"";
                }
                cmd = "\"" + cmd + "\"";
                string shell_uri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";
                ICollection<PSObject> result = null;
                PSCredential credential = new PSCredential(user, pass);
                WSManConnectionInfo ci = new WSManConnectionInfo() { Credential = credential };
                using (Runspace r = RunspaceFactory.CreateRunspace(ci))
                {
                    r.Open();
                    using (PowerShell ps = PowerShell.Create())
                    {
                        ps.Runspace = r;
                        ps.AddCommand("cmd.exe");
                        ps.AddParameter("/c", cmd);
                        result = ps.Invoke();

                    }
                }
                */

            }

        }
        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            CallerInformation here = this.Here();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Dictionary<AuditFileInfo, string> results = new Dictionary<AuditFileInfo, string>(files.Count);
            object results_lock = new object();
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (_f, state) =>
            {
                LocalAuditFileInfo _lf = _f as LocalAuditFileInfo;
                string text = _lf.ReadAsText();
                if (text != string.Empty)
                {
                    lock (results_lock)
                    {
                        results.Add(_f, text);
                    }
                }
     
            });
            sw.Stop();
            Info("Read text for {0} out of {1} files in {2} ms.", results.Count(r => r.Value.Length > 0), results.Count, sw.ElapsedMilliseconds);
            return results;
        }
        #endregion

        #region Public properties
        public ScriptEnvironment ScriptEnvironment { get; protected set; }
        public bool IsDockerContainer { get; internal set; }
        #endregion

    }
}
