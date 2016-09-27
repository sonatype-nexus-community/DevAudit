using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
namespace DevAudit.AuditLibrary
{
    public enum EventMessageType
    {
        SUCCESS = 0,
        ERROR = 1,
        INFO = 2,
        WARNING = 3,
        DEBUG = 4
    }

    public struct CallerInformation
    {
        public string Name;
        public string File;
        public int LineNumber;

        public CallerInformation(string name, string file, int line_number)
        {
            this.Name = name;
            this.File = file;
            this.LineNumber = line_number;
        }
    }

    public abstract class AuditEnvironment
    {
        public enum ProcessExecuteStatus
        {
            Unknown = -99,
            FileNotFound = -1,
            Completed = 0,
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

        #region Abstract properties and methods
        public abstract bool FileExists(string file_path);
        public abstract bool DirectoryExists(string dir_path);
        public abstract bool Execute(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
        public abstract AuditFileInfo ConstructFile(string file_path);
        public abstract AuditDirectoryInfo ConstructDirectory(string dir_path);
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

        public string PathSeparator { get; protected set; } = string.Empty;

        public string ProcessOutput
        {
            get
            {
                return this.ProcessOutputSB.ToString();
            }
        }

        public OperatingSystem OS { get; protected set; }       
        #endregion

        #region Constructors
        public AuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, OperatingSystem os)
        {
            this.OS = os;
            if (OS.Platform == PlatformID.Win32NT)
            {
                this.LineTerminator = "\r\n";
                this.PathSeparator = "\\";
            }
            else
            {
                this.LineTerminator = "\n";
                this.PathSeparator = "/";
            }
            this.MessageHandler = message_handler;
        }
        #endregion

        #region Protected and private properties
        protected StringBuilder ProcessOutputSB = new StringBuilder();
        protected StringBuilder ProcessErrorSB = new StringBuilder();
        protected string LineTerminator { get; set; }
        #endregion

        #region Internal methods
        internal void Message(EventMessageType message_type, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(message_type, message_format, message));
        }

        internal void Info(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, message_format, message));
        }

        internal void Error(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, message_format, message));
        }

        internal void Error(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.ERROR, message_format, message));
        }


        internal void Success(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.SUCCESS, message_format, message));
        }

        internal void Warning(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.WARNING, message_format, message));
        }

        internal void Debug(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.DEBUG, message_format, message));
        }

        internal void Debug(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.DEBUG, message_format, message));
        }

        internal CallerInformation Here([CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation c;
            c.Name = memberName;
            c.File = fileName;
            c.LineNumber = lineNumber;
            return c;
        }
        #endregion

    }
}
