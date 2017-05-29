using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using Naos.WinRM;

namespace DevAudit.AuditLibrary
{
    public class WinRmAuditEnvironment : AuditEnvironment
    {
        #region Constructors
        public WinRmAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, IPAddress address, string user, SecureString pass, LocalEnvironment host_environment) : base(message_handler, new OperatingSystem(PlatformID.Win32NT, new Version(0, 0)), host_environment)
        {
            try
            {
                this.HostEnvironment.Status("Connecting to Windows host address {0}", address.ToString());
                this.Manager = new MachineManager(address.ToString(), user, pass);
                ICollection<dynamic> r = this.Manager.RunScript(@"{ Get-WMIObject Win32_OperatingSystem -ComputerName . | select-object Description, Caption,OSArchitecture, ServicePackMajorVersion
                                                                Get-WmiObject -Class Win32_ComputerSystem -Property Name

                }");
                if (r == null || r.Count < 2)
                {
                    this.HostEnvironment.Error("Could not remotely execute PowerShell command on Windows host address {0}.", address.ToString());
                    this.IsConnected = false;
                }
                else
                {
                    PSObject os = r.First();
                    PSObject name = r.Last();
                    this.OSCaption = (string)os.Properties["Caption"].Value;
                    this.ComputerName = (string)name.Properties["Name"].Value;
                    this.IsConnected = true;
                    this.HostEnvironment.Success("Connected to Windows host address {0}. Computer name: {1}. Windows version: {2}.", this.Manager.IpAddress, this.ComputerName, this.OSCaption);
                    
                }
            }
            catch (TrustedHostMissingException te)
            {
                this.HostEnvironment.Error(te, "Error connecting to Windows host address {0}. You must add the host address to the TrustedHosts list of the WinRM client for your computer", address.ToString());
                this.IsConnected = false;
            }
            catch (Exception e)
            {
                this.HostEnvironment.Error(e, "Error connecting to Windows host address {0}.", address.ToString());
                this.IsConnected = false;
                return;
            }
        }

        public WinRmAuditEnvironment(EventHandler<EnvironmentEventArgs> message_handler, string host, string user, SecureString pass, LocalEnvironment host_environment) : base(message_handler, new OperatingSystem(PlatformID.Win32NT, new Version(0, 0)), host_environment)
        {
            this.Manager = new MachineManager(host, user, pass);
            this.IsConnected = true;
        }
        #endregion

        #region Overriden properties
        protected override TraceSource TraceSource { get; set; } = new TraceSource("WinRmAuditEnvironment");
        public bool IsConnected { get; protected set; }
        #endregion

        #region Overriden methods
        public override bool Execute(string command, string arguments, out ProcessExecuteStatus process_status, out string process_output, out string process_error, Action<string> OutputDataReceived = null, Action<string> OutputErrorReceived = null, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {

            process_output = string.Empty;
            process_error = string.Empty;
            try
            {
                process_output = this.Manager.RunCmd(command, arguments.Split('\t').ToList());
                process_status = ProcessExecuteStatus.Completed;
                return true;
            }
            catch (Exception e)
            {
                process_error = e.Message;
                process_status = ProcessExecuteStatus.Error;
                return false;
            }
        }
 
        public override bool FileExists(string file_path)
        {
            ICollection<dynamic> r = this.RunPSScript("{ param($path) Test-Path $path -pathType leaf}", new string[] { file_path });
            if (r == null)
            {
                Error("Could not test file {0} exists on {1}.", file_path, this.Manager.IpAddress);
                return false;
            }
            else 
            {
                PSObject o = r.First();
                return (bool) o.BaseObject;
            }
        }

        public override bool DirectoryExists(string dir_path)
        {
            ICollection<dynamic> r = this.RunPSScript("{ param($path) Test-Path $path -pathType container}", new string[] { dir_path });
            if (r == null)
            {
                Error("Could not test directory {0} exists on {1}.", dir_path, this.Manager.IpAddress);
                return false;
            }
            else
            {
                PSObject o = r.First();
                return (bool)o.BaseObject;
            }
        }

        public override AuditFileInfo ConstructFile(string file_path)
        {
            return new WinRmAuditFileInfo(this, file_path);
        }

        public override AuditDirectoryInfo ConstructDirectory(string dir_path)
        {
            return new WinRmAuditDirectoryInfo(this, dir_path);
        }

        public override Dictionary<AuditFileInfo, string> ReadFilesAsText(List<AuditFileInfo> files)
        {
            Dictionary<AuditFileInfo, string> results = new Dictionary<AuditFileInfo, string>(files.Count);
            object results_lock = new object();
            Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, (f) =>
            {
               ICollection<dynamic> r = this.RunPSScript("{ param($file) Get-Content -Path $file | Out-String}", new string[] { f.FullName });
               if (r == null)
               {
                   Warning("Could not read file {0} on {1} as text.", f.FullName, this.Manager.IpAddress);
                   results.Add(f, string.Empty);
               }
               else
               {
                    PSObject o = r.First();
                    lock (results_lock)
                    {
                        results.Add(f, (string)o.BaseObject);
                    }
               }
            });
            return results;

        }
        #endregion

        #region Properties
        public MachineManager Manager { get; protected set; }
        public string ComputerName { get; protected set; }
        public string OSCaption { get; protected set; }
        #endregion

        #region Methods
        public ICollection<dynamic> RunPSScript(string script, bool quiet_exceptions = false, List<string> args = null)
        {
            try
            {
                ICollection<dynamic> r = this.Manager.RunScript(script, args?.Select(a => a as object).ToList());
                if (r != null)
                {
                    Debug("Executed PowerShell script {0} {1} on {2}.", script, args?.Aggregate((s1, s2) => s1 + s2), this.Manager.IpAddress);
                    return r;
                }
                else
                {
                    if (!quiet_exceptions)
                    {
                        Error("Could not execute PowerShell script {0} {1} on {2}.", script, args?.Aggregate((s1, s2) => s1 + s2), this.Manager.IpAddress);
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                if (!quiet_exceptions)
                {

                    Error(e, "Could not execute PowerShell script {0} {1} on {2}.", script, args?.Aggregate((s1, s2) => s1 + s2), this.Manager.IpAddress);
                }
                return null;
            }
        }

        public ICollection<dynamic> RunPSScript(string script, bool quiet_exceptions, string[] args)
        {
            return this.RunPSScript(script, quiet_exceptions, args.ToList());     
        }

        public ICollection<dynamic> RunPSScript(string script, string[] args)
        {
            return this.RunPSScript(script, false, args.ToList());
        }

        public FileInfo GetFileAsLocal(string remote_path, string local_path)
        {
            throw new NotImplementedException();
        }

        public DirectoryInfo GetDirectoryAsLocal(string remote_path, string local_path)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
