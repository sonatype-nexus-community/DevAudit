using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class HostEnvironment
    {
        public enum ProcessStatus
        {
            Unknown = -99,
            FileNotFound = -1,
            Success = 0,
            Error = 1
        }

        public static bool Execute(string command, string arguments, 
            out ProcessStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null)
        {
            int? process_exit_code = null;
            StringBuilder process_out_sb = new StringBuilder();
            StringBuilder process_err_sb = new StringBuilder();
            ProcessStartInfo psi = new ProcessStartInfo(command);
            psi.Arguments = arguments;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
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
                    process_status = ProcessStatus.FileNotFound;
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
                process_status = ProcessStatus.Error;
                return false;
            }
            else if ((process_exit_code.HasValue && process_exit_code.Value == 0))
            {
                process_status = ProcessStatus.Success;
                return true;
            }
            else
            {
                process_status = ProcessStatus.Unknown;
                return false;

            }
        }
    }
}
