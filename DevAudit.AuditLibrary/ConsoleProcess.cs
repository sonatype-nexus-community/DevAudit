using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Medallion.Shell;

namespace DevAudit.AuditLibrary
{
    public class InteractiveConsoleProcess
    {
        public enum ExitStatus
        {
            CommandNotFound = -1,
            Success = 0,
            Error = 1
        }

        public ExitStatus ProcessExitStatus { get; private set; }
        
        public bool ProcessExited
        {
            get
            {
                return this.InteractiveProcess.HasExited;
            } 
        }

        public StringBuilder ProcessOutput { get; private set; } = new StringBuilder(1000);

        public StringBuilder ProcessError { get; private set; } = new StringBuilder(1000);

        public string LastProcessOutput { get; private set; }

        public string LastProcessError { get; private set; }

        public string LastProcessInput { get; private set; }

        public bool CanWriteInput
        {
            get
            {
                if (this.ProcessExited) return false;
                else
                {
                    return this.InteractiveProcess.StandardInput.BaseStream.CanWrite;
                }
            }
        }

        public int? ProcessExitCode { get; private set;}

        public Exception ProcessException { get; set; } = null;

        public Action<string> OutputDataReceivedAction { get; set; }

        public Action<string> ErrorDataReceivedAction { get; set; }

        public Process InteractiveProcess { get; private set; }

        private ProcessStartInfo ProcessStartInfo { get; set; } 

        public InteractiveConsoleProcess (string command, string arguments, Action<string> output_data_received)
        {
            this.ProcessStartInfo = new ProcessStartInfo(command, arguments);
            this.ProcessStartInfo.CreateNoWindow = true;
            this.ProcessStartInfo.RedirectStandardError = true;
            this.ProcessStartInfo.RedirectStandardOutput = true;
            this.ProcessStartInfo.UseShellExecute = false;
            this.InteractiveProcess = new Process();
            this.InteractiveProcess.StartInfo = this.ProcessStartInfo;
            this.InteractiveProcess.EnableRaisingEvents = true;
            this.InteractiveProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    this.LastProcessOutput = e.Data;
                    this.ProcessOutput.AppendLine(e.Data);
                    OutputDataReceivedAction?.Invoke(e.Data);
                }
            };
            this.InteractiveProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    this.LastProcessError = e.Data;
                    this.ProcessError.AppendLine(e.Data);
                    ErrorDataReceivedAction?.Invoke(e.Data);
                }
            };
            this.InteractiveProcess.Exited += (object sender, EventArgs e) =>
            {
                this.ProcessExitCode = this.InteractiveProcess.ExitCode;
            };
            try
            {
                this.InteractiveProcess.Start();
                this.InteractiveProcess.BeginErrorReadLine();
                this.InteractiveProcess.BeginOutputReadLine();
                this.InteractiveProcess.WaitForExit();
                this.ProcessExitCode = this.InteractiveProcess.ExitCode;
                this.InteractiveProcess.Close();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    this.ProcessExitStatus = ExitStatus.CommandNotFound;
                    this.ProcessException = e;
                }
            }
            catch (Exception e)
            {
                this.ProcessException = e;
                
                
            }
            finally
            {
                this.InteractiveProcess.Dispose();
            }
            if (this.ProcessError.Length > 0 || (this.ProcessExitCode.HasValue && this.ProcessExitCode.Value != 0))
            {
                this.ProcessExitStatus = ExitStatus.Error;
            }
            else
            {
                this.ProcessExitStatus = ExitStatus.Success;
            }
        }
    }
}
