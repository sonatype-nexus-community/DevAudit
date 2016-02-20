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
            FileNotFound = -1,
            Success = 0,
            Error = 1
        }

        public static bool Execute(string command, string arguments, 
            out ProcessStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null)
        {
            int? process_exit_code = null;
            string process_out = "";
            string process_err = "";
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
                    process_out += e.Data + Environment.NewLine;
                    if (OutputDataReceived != null) OutputDataReceived(e.Data);
                }
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_err += e.Data + Environment.NewLine;
                    if (OutputErrorReceived != null) OutputErrorReceived(e.Data);
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
                    process_error = e.Message;
                    process_output = "";
                    return false;
                }

            }
            finally
            {
                p.Dispose();
            }
            process_output = process_out;
            process_error = process_err;
            if (!string.IsNullOrEmpty(process_error) || (process_exit_code.HasValue && process_exit_code.Value != 0))
            {
                process_status = ProcessStatus.Error;
                return false;
            }
            else
            {
                process_status = ProcessStatus.Success;
                return true;
            }
        }
    }
}
