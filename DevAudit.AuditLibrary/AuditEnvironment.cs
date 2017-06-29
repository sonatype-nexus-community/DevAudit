using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Security;
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
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, Dictionary<string, string> EnvironmentVariables = null, 
            Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
        public abstract bool ExecuteAsUser(string command, string arguments,
            out ProcessExecuteStatus process_status, out string process_output, out string process_error, string user, SecureString password, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
        public abstract AuditFileInfo ConstructFile(string file_path);
        public abstract AuditDirectoryInfo ConstructDirectory(string dir_path);
        public abstract Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files);
        public abstract int MaxConcurrentExecutions { get; }
        protected abstract TraceSource TraceSource { get; set; }
        #endregion

        #region Properties
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

        public string OSName { get; set; }

        public string OSVersion { get; set; }

        public LocalEnvironment HostEnvironment { get; protected set; }
    
        public DirectoryInfo WorkDirectory { get; protected set; }

        protected StringBuilder ProcessOutputSB = new StringBuilder();

        protected StringBuilder ProcessErrorSB = new StringBuilder();

        internal string LineTerminator { get; set; }

        #endregion

        #region Methods
        public bool ExecuteCommand(string command, string arguments, out string output, bool report_errors = true, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation caller = new CallerInformation(memberName, fileName, lineNumber);
            ProcessExecuteStatus process_status = ProcessExecuteStatus.Unknown;
            string process_output, process_error;
            bool r = this.Execute(command, arguments, out process_status, out process_output, out process_error);
            if (r)
            {
                output = process_output.Trim();
                Debug(caller, "The command {0} {1} executed successfully. Output: {2}", command, arguments, output);
                return true;
            }
            else
            {
                output = process_output + process_error;
                if (report_errors)
                {
                    Error("The command {0} {1} did not execute successfully. Error: {2}", command, arguments, output);
                }
                else
                {
                    Debug("The command {0} {1} did not execute successfully. Error: {2}", command, arguments, output);
                }
                return false;
            }
        }

        public virtual string GetOSName()
        {
            if (!string.IsNullOrEmpty(this.OSName)) return this.OSName;
            CallerInformation here = Here();
            string cmd = "", args = "";
            if (this.IsUnix)
            {
                cmd = "cat";
                args = "/etc/*release";
                string output;
                if (this.ExecuteCommand(cmd, args, out output, false) || !string.IsNullOrEmpty(output))
                {
                    if (output.ToLower().Contains("ubuntu"))
                    {
                        this.OSName = "ubuntu";
                    }
                    else if (output.ToLower().Contains("debian"))
                    {
                        this.OSName = "debian";
                    }
                    else if (output.ToLower().Contains("centos"))
                    {
                        this.OSName = "centos";
                    }
                    else if (output.ToLower().Contains("suse linux"))
                    {
                        this.OSName = "suse";
                    }
                    else if (output.ToLower().Contains("red hat enterprise linux"))
                    {
                        this.OSName = "rhel";
                    }
                    else if (output.ToLower().Contains("oracle linux server"))
                    {
                        this.OSName = "oraclelinux";
                    }
                }
                if (string.IsNullOrEmpty(this.OSName))
                {
                    cmd = "lsb_release";
                    args = "-a";
                    if (this.ExecuteCommand(cmd, args, out output, false))
                    {
                        if (output.ToLower().Contains("ubuntu"))
                        {
                            this.OSName = "ubuntu";
                        }
                        else if (output.ToLower().Contains("debian"))
                        {
                            this.OSName = "debian";
                        }
                        else if (output.ToLower().Contains("centos"))
                        {
                            this.OSName = "centos";
                        }
                        else if (output.ToLower().Contains("suse linux"))
                        {
                            this.OSName = "suse";
                        }
                        else if (output.ToLower().Contains("oracle linux"))
                        {
                            this.OSName = "oracle";
                        }

                        else if (output.ToLower().Contains("red hat enterprise linux"))
                        {
                            this.OSName = "rhel";
                        }
                    }
                    if (string.IsNullOrEmpty(this.OSName))
                    {
                        cmd = "stat";
                        args = "/etc/oracle-release";
                        if (this.ExecuteCommand(cmd, args, out output, false))
                        {
                            this.OSName = "oraclelinux";
                        }
                        else
                        {
                            cmd = "stat";
                            args = "/etc/centos-release";
                            if (this.ExecuteCommand(cmd, args, out output, false))
                            {
                                this.OSName = "centos";
                            }
                            else
                            {
                                cmd = "stat";
                                args = "/etc/redhat-release";
                                if (this.ExecuteCommand(cmd, args, out output, false))
                                {
                                    this.OSName = "rhel";
                                }
                                else
                                {
                                    Error("GetOSName() failed.");
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(this.OSName))
                {
                    Success("Detected operating system of environment is {0}.", this.OSName);
                }
                else
                {
                    Warning("GetOSName() failed. Falling back to unix");
                    this.OSName = "unix";
                }

            }
            return this.OSName;
        }

        public virtual string GetOSVersion()
        {
            if (!string.IsNullOrEmpty(this.OSVersion)) return this.OSVersion;
            CallerInformation here = Here();
            string cmd = "", args = "", version = "";
            if (this.IsUnix)
            {
                if (this.OSName == "ubuntu")
                {
                    cmd = "lsb_release";
                    args = "-sr ";
                    string output;
                    if (this.ExecuteCommand(cmd, args, out output, false))
                    {
                        version = output;
                        Debug(here, "GetOSVersion() returned {0}.", version);
                    }
                    else
                    {
                        cmd = "bash";
                        args = "-c \"cat /etc/*release | grep -m 1 DISTRIB_RELEASE | cut -d '=\' -f2 && test \\${PIPESTATUS[0]} -eq 0\"";
                        if (this.ExecuteCommand(cmd, args, out output, false) && !string.IsNullOrEmpty(output))
                        {
                            version = output.Replace("Release:\t", string.Empty);
                            Debug(here, "GetOSVersion() returned {0}.", version);
                        }
                        else
                        {
                            Error("GetOSVersion() failed.");
                        }
                    }

                }
                else if (this.OSName == "debian")
                {
                    cmd = "cat";
                    args = "/etc/debian_version";
                    string output;
                    if (this.ExecuteCommand(cmd, args, out output))
                    {
                        version = output.Trim();
                        Debug(here, "GetOSVersion() returned {0}.", version);
                    }
                    else
                    {
                        Error("GetOSVersion() failed.");
                    }
                }
                else if (this.OSName == "centos")
                {
                    cmd = "bash";
                    args = "-c \"cat /etc/centos-release | cut -d' ' -f4 && test \\${PIPESTATUS[0]} -eq 0\"";
                    string output;
                    if (this.ExecuteCommand(cmd, args, out output,false))
                    {
                        version = output.Trim();
                        Debug(here, "GetOSVersion() returned {0}.", version);
                    }
                    else
                    {
                        cmd = "awk";
                        args = "'NR==1{print $3}' /etc/issue";
                        if (this.ExecuteCommand(cmd, args, out output, false))
                        {
                            version = output.Trim();
                            Debug(here, "GetOSVersion() returned {0}.", version);
                        }
                        else
                        {
                            Error("GetOSVersion() failed.");
                        }
                    }
                }
                else if (this.OSName == "oraclelinux")
                {
                    string output;
                    cmd = "cat";
                    args = "/etc/oracle-release";
                    if (this.ExecuteCommand(cmd, args, out output, false) && !string.IsNullOrEmpty(output))
                    {
                        version = output.Replace("Oracle Linux Server release ", string.Empty).Split('.').FirstOrDefault();
                    }
                    else
                    {
                        Error("GetOSVersion() failed.");
                    }
                }
                if (!string.IsNullOrEmpty(version))
                {
                    this.OSVersion = version;
                    Success("Detected operating system version of environment is {0}.", this.OSVersion);
                }
            }
            return this.OSVersion;
        }

        public virtual string GetEnvironmentVar(string name)
        {
            CallerInformation here = Here();
            string var = "", cmd = "", args = "";
            if (this.IsWindows)
            {
                var = "%" + name + "%";
                cmd = "powershell";
                args = "(Get-Childitem env:" + name + ").Value";
            }
            else
            {
                var = "$" + name;
                cmd = "echo";
                args = var;
            }
            string output;
            if (this.ExecuteCommand(cmd, args, out output))
            {
                Debug(here, "GetEnvironmentVar({0}) returned {1}.", name, output);
                return output;
            }
            else
            {
                Error("GetEnvironmentVar({0}) failed.", var);
                return string.Empty;
            }
        }


        public virtual string GetUnixFileMode(string path, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            CallerInformation here = Here();
            if (this.IsWindows)
            {
                Error(here, "This method is not implemented in a Windows environment.");
                return string.Empty;
            }
            else
            {
                string output;
                if (this.ExecuteCommand("find", string.Format("{0} -prune -printf '%m'", path), out output))
                {
                    Debug(here, "GetUnixFileMode({0}) returned {1}.", path, output);
                    return output;
                }
                else
                {
                    Debug(here, "GetUnixFileMode({0}) failed.", path);
                    return string.Empty;
                }
            }
        }


        public string GetTimestamp ()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString();
        }

        public SecureString ToSecureString(string s)
        {
            SecureString r = new SecureString();
            foreach (char c in s)
            {
                r.AppendChar(c);
            }
            r.MakeReadOnly();
            return r;
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

        [DebuggerStepThrough]
        internal void Message(EventMessageType message_type, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(message_type, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Info(string message_format, params object[] message)
        {
            TraceSource.TraceInformation(message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.INFO, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Error(string message_format, params object[] message)
        {
            TraceSource.TraceEvent(TraceEventType.Error, 0, message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.ERROR, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Error(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.ERROR, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Error(Exception e)
        {
            OnMessage(new EnvironmentEventArgs(e));
        }

        [DebuggerStepThrough]
        internal void Error(CallerInformation caller, Exception e)
        {
            OnMessage(new EnvironmentEventArgs(caller, e));
        }

        [DebuggerStepThrough]
        internal void Error(Exception e, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(e);
        }

        [DebuggerStepThrough]
        internal void Error(CallerInformation caller, Exception e, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(caller, e);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        internal void Error(AggregateException ae, string message_format, params object[] message)
        {
            Error(message_format, message);
            Error(ae);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        internal void Success(string message_format, params object[] message)
        {
            TraceSource.TraceEvent(TraceEventType.Information, 0, message_format, message);
            OnMessage(new EnvironmentEventArgs(EventMessageType.SUCCESS, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Warning(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.WARNING, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Status(string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(EventMessageType.STATUS, message_format, message));
        }

        [DebuggerStepThrough]
        internal void Progress(string operation, int total, int complete, TimeSpan? time = null)
        {
            OnMessage(new EnvironmentEventArgs(new OperationProgress(operation, total, complete, time)));
        }

        [DebuggerStepThrough]
        internal void Debug(CallerInformation caller, string message_format, params object[] message)
        {
            OnMessage(new EnvironmentEventArgs(caller, EventMessageType.DEBUG, message_format, message));
        }

        [DebuggerStepThrough]
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

        #region Fields
        protected object message_lock = new object();
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
