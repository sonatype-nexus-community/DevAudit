using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditEnvironment
    {
        public enum ProcessExecuteStatus
        {
            Unknown = -99,
            FileNotFound = -1,
            Success = 0,
            Error = 1
        }

        #region Events
        public event EventHandler<EnvironmentEventArgs> MessageHandler;
        public event EventHandler<DataReceivedEventArgs> OutputDataReceivedHandler;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceivedHandler;

        protected virtual void OnMessage(EnvironmentEventArgs e)
        {
            MessageHandler?.Invoke(this, e);
        }

        protected virtual void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                ProcessOutputSB.AppendLine(e.Data);
                OutputDataReceivedHandler?.Invoke(this, e);
            }
        }

        protected virtual void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                ProcessErrorSB.AppendLine(e.Data);
                ErrorDataReceivedHandler?.Invoke(this, e);
            }
        }
        #endregion

        #region Abstract properties
        public abstract bool FileExists(string file_path);
        public abstract bool DirectoryExists(string dir_path);
        public abstract bool Execute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null);
        #endregion

        #region Public properties
        public bool IsWindows
        {
            get
            {
                if (this.OS != null && this.OS.Platform == PlatformID.Win32NT)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsUnix
        {
            get
            {
                if (this.OS != null && this.OS.Platform == PlatformID.Unix)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public string ProcessOutput
        {
            get
            {
                return this.ProcessOutputSB.ToString();
            }
        }

        public OperatingSystem OS { get; protected set; }       
        #endregion

        #region Protected and private properties
        protected StringBuilder ProcessOutputSB = new StringBuilder();
        protected StringBuilder ProcessErrorSB = new StringBuilder();
        protected string LineTerminator { get; set; }
        #endregion

        #region Constructors
        public AuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, OperatingSystem os)
        {
            this.OS = os;
            if (OS.Platform == PlatformID.Win32NT)
            {
                this.LineTerminator = "\r\n";
            }
            else
            {
                this.LineTerminator = "\n";
            }
            this.MessageHandler = message_handler;
        }
        #endregion

        #region Protected and private methods
        protected void Message(EventMessageType message_type, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(message_type, message_format, message));
        }

        protected void Info(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, "[INFO] " + message_format, message));
        }

        protected void Error(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, "[ERROR] " + message_format, message));
        }

        protected void Success(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.SUCCESS, "[SUCCESS] " + message_format, message));
        }

        protected void Warning(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.WARNING, "[WARN] " + message_format, message));
        }
        #endregion

    }
}
