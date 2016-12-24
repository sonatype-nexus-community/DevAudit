using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditTarget : IDisposable
    {
        #region Enums
        public enum AuditResult
        {
            SUCCESS = 0,
            INVALID_AUDIT_TARGET_OPTIONS,
            ERROR_CREATING_AUDIT_TARGET,
            ERROR_SCANNING_MODULES,
            ERROR_SCANNING_PACKAGES,
            ERROR_SEARCHING_ARTIFACTS,
            ERROR_SEARCHING_VULNERABILITIES,
            ERROR_EVALUATING_VULNERABILITIES,
            ERROR_SCANNING_VERSION,
            ERROR_SCANNING_CONFIGURATION,
            ERROR_SCANNING_DEFAULT_CONFIGURATION_RULES,
            ERROR_SEARCHING_CONFIGURATION_RULES,
            ERROR_EVALUATING_CONFIGURATION_RULES,
            ERROR_SCANNING_PROJECTS,
            ERROR_ANALYZING,
            ERROR_SCANNING_WORKSPACE,
            ERROR_SCANNING_ANALYZERS,
            ERROR_RUNNING_ANALYZERS,
        }
        #endregion

        #region Constructors
        public AuditTarget(Dictionary<string, object> audit_options, EventHandler<EnvironmentEventArgs> controller_message_handler = null)
        {
            if (ReferenceEquals(audit_options, null)) throw new ArgumentNullException("audit_options");
            this.AuditOptions = audit_options;
            this.ControllerMessageHandler = controller_message_handler;
            this.HostEnvironmentMessageHandler = AuditTarget_HostEnvironmentMessageHandler;
            this.HostEnvironment = new LocalEnvironment(this.HostEnvironmentMessageHandler);
            this.HostEnvironment.ScriptEnvironment.MessageHandler += this.AuditTarget_ScriptEnvironmentMessageHandler;
            this.HostEnvironmentInitialised = true;
            if (this.AuditOptions.Keys.Contains("DockerContainer"))
            {
                DockerAuditEnvironment docker_environment = new DockerAuditEnvironment(this.HostEnvironmentMessageHandler, (string)this.AuditOptions["DockerContainer"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                if (string.IsNullOrEmpty(docker_environment.Container))
                {
                    this.AuditEnvironmentIntialised = false;
                    throw new Exception("Failed to initialise audit environment.");
                }
                else if (!docker_environment.ContainerRunning)
                {
                    this.AuditEnvironmentIntialised = false;
                    throw new Exception("The Docker container is not currently running and DevAudit does not know how to run your container. Ensure your container is running before attempting to" +
                        "audit it.");
                }
                else
                {
                    this.AuditEnvironment = docker_environment;
                    this.AuditEnvironmentIntialised = true;
                    this.AuditEnvironmentMessageHandler = AuditTarget_AuditEnvironmentMessageHandler;
                    this.AuditEnvironment.MessageHandler -= HostEnvironmentMessageHandler;
                    this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessageHandler;
                }

            }
            else if (this.AuditOptions.Keys.Contains("RemoteHost"))
            {
                string client;
                SshAuditEnvironment ssh_environment = null;

                if (this.HostEnvironment.OS.Platform == PlatformID.Win32NT)
                {
                    client = this.AuditOptions.Keys.Contains("WindowsUsePlink") ? "plink" : this.AuditOptions.Keys.Contains("WindowsUsePlink") ? "openssh" : "ssh";
                }
                else
                {
                    client = "ssh";
                }

                if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemoteKeyFile"))
                {
                    if (this.AuditOptions.Keys.Contains("RemoteKeyPassPhrase"))
                    {
                        ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessageHandler, client, (string)this.AuditOptions["RemoteHost"],
                            (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemoteKeyPassPhrase"], (string)this.AuditOptions["RemoteKeyFile"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                    }
                    else
                    {
                        ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessageHandler, client, (string)this.AuditOptions["RemoteHost"],
                            (string)this.AuditOptions["RemoteUser"], null, (string)this.AuditOptions["RemoteKeyFile"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                    }
                }
                else if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                {
                    ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessageHandler, client, (string)this.AuditOptions["RemoteHost"],
                        (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"], null, new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                }
                else throw new Exception("Unknown remote host authentication options.");

                if (ssh_environment.IsConnected)
                {
                    this.AuditEnvironment = ssh_environment;
                    this.AuditEnvironmentIntialised = true;
                    this.AuditEnvironmentMessageHandler = AuditTarget_AuditEnvironmentMessageHandler;
                    this.AuditEnvironment.MessageHandler -= HostEnvironmentMessageHandler;
                    this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessageHandler;
                }
                else
                {
                    ssh_environment = null;
                    this.AuditEnvironmentIntialised = false;
                    throw new Exception("Failed to initialise audit environment.");
                }
            }
            else if (this.AuditOptions.Keys.Contains("RemoteUser") || this.AuditOptions.Keys.Contains("RemotePass"))
            {
                throw new Exception("A remote host name must be specified.");
            }
            else
            {
                this.AuditEnvironmentMessageHandler = AuditTarget_AuditEnvironmentMessageHandler;
                this.AuditEnvironment = new LocalEnvironment(this.AuditEnvironmentMessageHandler);
                this.AuditEnvironmentIntialised = true;
            }
        }

        private void AuditTarget_HostEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "HOST";
            this.ControllerMessageHandler.Invoke(sender, e);
        }

        private void AuditTarget_AuditEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "AUDIT";
            this.ControllerMessageHandler.Invoke(sender, e);
        }

        private void AuditTarget_ScriptEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "SCRIPT";
            this.ControllerMessageHandler.Invoke(sender, e);
        }
        #endregion

        #region Events
        protected event EventHandler<EnvironmentEventArgs> HostEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> AuditEnvironmentMessageHandler;
        protected event EventHandler<EnvironmentEventArgs> ControllerMessageHandler;
        #endregion

        #region Properties
        public string Id { get; protected set; }
        public string Label { get; protected set; }
        public Dictionary<string, object> AuditOptions { get; set; } = new Dictionary<string, object>();
        public LocalEnvironment HostEnvironment { get; protected set; }
        public AuditEnvironment AuditEnvironment { get; protected set; }
        public bool HostEnvironmentInitialised { get; private set; } = false;
        public bool AuditEnvironmentIntialised { get; private set; } = false;
        public bool UseAsyncMethods { get; private set; } = false;
        
        internal Stopwatch Stopwatch { get; set; } = new Stopwatch();
        #endregion

        #region Disposer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true); 
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object // from executing a second time. 
            // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
            GC.SuppressFinalize(this);
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
                        // Release all unmanaged resources here 
                        // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 

                        foreach (Delegate d in this.AuditEnvironmentMessageHandler.GetInvocationList())
                        {
                            this.AuditEnvironmentMessageHandler -= (EventHandler<EnvironmentEventArgs>)d;
                        }

                        if (this.AuditEnvironment != null)
                        {
                            this.AuditEnvironment.Dispose();
                            this.AuditEnvironment = null;
                        }
                        foreach (Delegate d in this.HostEnvironmentMessageHandler.GetInvocationList())
                        {
                            this.HostEnvironmentMessageHandler -= (EventHandler<EnvironmentEventArgs>)d;
                        }

                        if (this.HostEnvironment != null)
                        {
                            this.HostEnvironment.Dispose();
                            this.HostEnvironment = null;
                        }
                    }
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }
        #endregion

    }
}
