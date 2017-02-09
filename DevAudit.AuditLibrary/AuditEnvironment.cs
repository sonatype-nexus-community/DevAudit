using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;
namespace DevAudit.AuditLibrary
{
    #region Types
    public enum EventMessageType
    {
        SUCCESS = 0,
        ERROR = 1,
        INFO = 2,
        WARNING = 3,
        STATUS = 4,
        PROGRESS = 5,
        DEBUG = 6,
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

    public struct OperationProgress
    {
        public string Operation;
        public int Total;
        public int Complete;
        public TimeSpan? Time;

        public OperationProgress(string op, int total, int complete, TimeSpan? time)
        {
            this.Operation = op;
            this.Total = total;
            this.Complete = complete;
            this.Time = time;
        }
    }
    #endregion

    public abstract class AuditEnvironment : IDisposable
    {
        #region Types
        public enum ProcessExecuteStatus
        {
            Unknown = -99,
            FileNotFound = -1,
            Completed = 0,
            Error = 1
        }
        #endregion

        #region Events
        public event EventHandler<EnvironmentEventArgs> MessageHandler;
        public event EventHandler<DataReceivedEventArgs> OutputDataReceivedHandler;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceivedHandler;

        protected virtual void OnMessage(EnvironmentEventArgs e)
        {
            lock (message_lock)
            {
                MessageHandler?.Invoke(this, e);
            }
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
        public abstract Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files);
        protected abstract TraceSource TraceSource { get; set; }
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

        public bool IsMonoRuntime
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
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

        public LocalEnvironment HostEnvironment { get; protected set; }
    
        public DirectoryInfo WorkDirectory { get; protected set; }
        #endregion

        #region Public methods
        public string GetTimestamp ()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString();
        }

        #endregion

        #region Constructors
        public AuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, OperatingSystem os, LocalEnvironment host_environment)
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
            this.HostEnvironment = host_environment;
        }
        #endregion

        #region Protected properties
        protected StringBuilder ProcessOutputSB = new StringBuilder();
        protected StringBuilder ProcessErrorSB = new StringBuilder();
        //protected Stopwatch Stopwatch { get; } = new Stopwatch();
        #endregion

        #region Protected fields
        protected object message_lock = new object();
        #endregion

        #region Internal methods
        internal void Message(EventMessageType message_type, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(message_type, message_format, message));
        }

        internal void Info(string message_format, params object[] message)
        {
            TraceSource.TraceInformation(message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, message_format, message));
        }

        internal void Error(string message_format, params object[] message)
        {
            TraceSource.TraceEvent(TraceEventType.Error, 0, message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, message_format, message));
        }

        internal void Error(CallerInformation caller, string message_format, params object[] message)
        {   
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.ERROR, message_format, message));
        }

        internal void Error(Exception e)
        {
            OnMessage(new EnvironmentEventArgs(e));
        }

        internal void Error(CallerInformation caller, Exception e)
        {
            OnMessage(new EnvironmentEventArgs(caller, e));
        }

        internal void Error(Exception e, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(e);
        }

        internal void Error(CallerInformation caller, Exception e, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(caller, e);
        }

        internal void Error(AggregateException ae)
        {
            if (ae.InnerExceptions != null && ae.InnerExceptions.Count >= 1)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    Error(e);
                }
            }
        }

        internal void Error(AggregateException ae, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(ae);
        }

        internal void Error(CallerInformation caller, AggregateException ae, string message_format, params object[] message)
        {
            Error(caller, message_format, message);
            if (ae.InnerExceptions != null && ae.InnerExceptions.Count >= 1)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    Error(caller, e);
                }
            }
        }

        internal void Success(string message_format, params object[] message)
        {
            TraceSource.TraceEvent(TraceEventType.Information, 0, message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.SUCCESS, message_format, message));
        }

        internal void Warning(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.WARNING, message_format, message));
        }

        internal void Status(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.STATUS, message_format, message));
        }

        internal void Progress(string operation, int total, int complete, TimeSpan? time = null)
        {
            OnMessage(new EnvironmentEventArgs(new OperationProgress(operation, total, complete, time)));
        }

        internal void Debug(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.DEBUG, message_format, message));
        }

        internal void Debug(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.DEBUG, message_format, message));
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

        #region Internal properties
        internal string LineTerminator { get; set; }
        #endregion

        #region Disposer and Finalizer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true); // This object will be cleaned up by the Dispose method. // Therefore, you should call GC.SupressFinalize to // take this object off the finalization queue // and prevent finalization code for this object // from executing a second time. // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
        }

        protected virtual void Dispose(bool isDisposing)
        {         
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
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
                    }
                    // Release all unmanaged resources here 
                    // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }

        ~AuditEnvironment()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
