using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Security;
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
            ERROR_SCANNING_VULNERABLE_CREDENTIAL_STORAGE,
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

            #region Initialise host environment
            if (this.AuditOptions.ContainsKey("HostEnvironment"))
            {
                this.HostEnvironment = (LocalEnvironment)this.AuditOptions["HostEnvironment"];
                this.HostEnvironmentInitialised = true;
            }
            else
            {
                this.ControllerMessage = controller_message_handler;
                this.HostEnvironmentMessage = AuditTarget_HostEnvironmentMessageHandler;
                this.HostEnvironment = new LocalEnvironment(this.HostEnvironmentMessage);
                this.HostEnvironment.ScriptEnvironment.MessageHandler += this.AuditTarget_ScriptEnvironmentMessageHandler;
                if (this.AuditOptions.ContainsKey("Dockerized"))
                {
                    this.HostEnvironment.IsDockerContainer = true;
                }
                this.HostEnvironmentInitialised = true;
            }
            #endregion

            #region Initialise audit environment
            if (this.AuditOptions.ContainsKey("AuditEnvironment"))
            {
                this.AuditEnvironment = (AuditEnvironment)this.AuditOptions["AuditEnvironment"];
                this.AuditEnvironmentIntialised = true;
            }
            else
            {
                if (this.AuditOptions.Keys.Contains("DockerContainer") && !this.AuditOptions.Keys.Contains("RemoteHost"))
                {
                    DockerAuditEnvironment docker_environment = new DockerAuditEnvironment(this.HostEnvironmentMessage, (string)this.AuditOptions["DockerContainer"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
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
                        this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                        this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                    }

                }
                else if (this.AuditOptions.Keys.Contains("DockerContainer") && this.AuditOptions.Keys.Contains("RemoteHost"))
                {
                    SshDockerAuditEnvironment ssh_environment = null;
                    if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemoteKeyFile"))
                    {
                        if (this.AuditOptions.Keys.Contains("RemoteKeyPassPhrase"))
                        {
                            ssh_environment = new SshDockerAuditEnvironment(this.HostEnvironmentMessage, "ssh", (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                                (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemoteKeyPassPhrase"], (string)this.AuditOptions["RemoteKeyFile"],
                                (string)this.AuditOptions["DockerContainer"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                        }
                        else
                        {
                            ssh_environment = new SshDockerAuditEnvironment(this.HostEnvironmentMessage, "ssh", (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                                (string)this.AuditOptions["RemoteUser"], null, (string)this.AuditOptions["RemoteKeyFile"],
                                (string)this.AuditOptions["DockerContainer"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                        }
                    }
                    else if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                    {
                        ssh_environment = new SshDockerAuditEnvironment(this.HostEnvironmentMessage, "ssh", (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                            (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"], null,  (string)this.AuditOptions["DockerContainer"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                    }
                    else throw new Exception("Unknown remote host authentication options.");

                    if (ssh_environment.IsConnected)
                    {
                        if (string.IsNullOrEmpty(ssh_environment.Container))
                        {
                            this.AuditEnvironmentIntialised = false;
                            throw new Exception("Failed to initialise audit environment.");
                        }
                        else if (!ssh_environment.ContainerRunning)
                        {
                            this.AuditEnvironmentIntialised = false;
                            throw new Exception("The Docker container is not currently running and DevAudit does not know how to run your container. Ensure your container is running before attempting to" +
                                "audit it.");
                        }
                        else
                        {
                            this.AuditEnvironment = ssh_environment;
                            this.AuditEnvironmentIntialised = true;
                            this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                            this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                            this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                        }
                    }
                    else
                    {
                        ssh_environment = null;
                        this.AuditEnvironmentIntialised = false;
                        throw new Exception("Failed to initialise SSH Docker audit environment.");
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
                            ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessage, client, (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                                (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemoteKeyPassPhrase"], (string)this.AuditOptions["RemoteKeyFile"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                        }
                        else
                        {
                            ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessage, client, (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                                (string)this.AuditOptions["RemoteUser"], null, (string)this.AuditOptions["RemoteKeyFile"], new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                        }
                    }
                    else if (this.AuditOptions.Keys.Contains("RemoteUser") && this.AuditOptions.Keys.Contains("RemotePass"))
                    {
                        ssh_environment = new SshAuditEnvironment(this.HostEnvironmentMessage, client, (string)this.AuditOptions["RemoteHost"], (int)this.AuditOptions["RemoteSshPort"],
                            (string)this.AuditOptions["RemoteUser"], this.AuditOptions["RemotePass"], null, new OperatingSystem(PlatformID.Unix, new Version(0, 0)), this.HostEnvironment);
                    }
                    else throw new Exception("Unknown remote host authentication options.");

                    if (ssh_environment.IsConnected)
                    {
                        this.AuditEnvironment = ssh_environment;
                        this.AuditEnvironmentIntialised = true;
                        this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                        this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                    }
                    else
                    {
                        ssh_environment = null;
                        this.AuditEnvironmentIntialised = false;
                        throw new Exception("Failed to initialise SSH audit environment.");
                    }
                }
                else if (this.AuditOptions.Keys.Contains("WinRmRemoteIp") || this.AuditOptions.Keys.Contains("WinRmRemoteHost"))
                {
                    if (!this.AuditOptions.Keys.Contains("RemoteUser") || !this.AuditOptions.Keys.Contains("RemotePass"))
                    {
                        throw new Exception("A remote user and password must be specified.");
                    }
                    WinRmAuditEnvironment winrm;
                    if (this.AuditOptions.Keys.Contains("WinRmRemoteIp"))
                    {
                        winrm = new WinRmAuditEnvironment(this.HostEnvironmentMessage, this.AuditOptions["WinRmRemoteIp"] as IPAddress,
                            (string)this.AuditOptions["RemoteUser"], (SecureString)this.AuditOptions["RemotePass"], this.HostEnvironment);
                        if (winrm.IsConnected)
                        {
                            this.AuditEnvironment = winrm;
                            this.AuditEnvironmentIntialised = true;
                            this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                            this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                            this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                        }
                        else
                        {
                            winrm = null;
                            this.AuditEnvironmentIntialised = false;
                            throw new Exception("Failed to initialise WinRM audit environment.");
                        }
                    }
                    else
                    {
                        winrm = new WinRmAuditEnvironment(this.HostEnvironmentMessage, (string)this.AuditOptions["WinRmRemoteHost"],
                            (string)this.AuditOptions["RemoteUser"], (SecureString)this.AuditOptions["RemotePass"], this.HostEnvironment);
                    }
                }
                else if (this.AuditOptions.Keys.Contains("RemoteUser") || this.AuditOptions.Keys.Contains("RemotePass"))
                {
                    throw new Exception("A remote host name must be specified.");
                }
                else if (this.AuditOptions.ContainsKey("Dockerized"))
                {
                    this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                    this.AuditEnvironment = new DockerizedLocalEnvironment(this.AuditEnvironmentMessage);
                    this.AuditEnvironmentIntialised = true;
                }
                else if (this.AuditOptions.ContainsKey("GitHubRepoName"))
                {
                    if (!this.AuditOptions.ContainsKey("GitHubRepoOwner") || !this.AuditOptions.ContainsKey("GitHubRepoBranch"))
                    {
                        throw new ArgumentException("A required audit option for the GitHub environment is missing.");
                    }
                    string user_api_token = string.Empty;
                    if (this.AuditOptions.ContainsKey("GitHubToken"))
                    {
                        user_api_token = (string)this.AuditOptions["GitHubToken"];
                    }
                    GitHubAuditEnvironment github_environment = new GitHubAuditEnvironment(this.HostEnvironmentMessage, user_api_token, (string)this.AuditOptions["GitHubRepoOwner"],
                       (string)this.AuditOptions["GitHubRepoName"], (string)this.AuditOptions["GitHubRepoBranch"], this.HostEnvironment);
                    if (github_environment.RepositoryInitialised)
                    {
                        this.AuditEnvironment = github_environment;
                        this.AuditEnvironmentIntialised = true;
                        this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                        this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                    }
                    else
                    {
                        github_environment = null;
                        this.AuditEnvironmentIntialised = false;
                        throw new Exception("Failed to initialise GitHub audit environment.");
                    }
                }
                else if (this.AuditOptions.ContainsKey("GitLabRepoName"))
                {
                    string api_token = string.Empty;
                    if (!this.AuditOptions.ContainsKey("GitLabRepoUrl") || !this.AuditOptions.ContainsKey("GitLabRepoName") || !this.AuditOptions.ContainsKey("GitLabRepoBranch"))
                    {
                        throw new ArgumentException("A required audit option for the GitLab environment is missing.");
                    }
                    GitLabAuditEnvironment GitLab_environment = new GitLabAuditEnvironment(this.HostEnvironmentMessage, api_token, (string)this.AuditOptions["GitLabRepoUrl"],
                       (string)this.AuditOptions["GitLabRepoName"], (string)this.AuditOptions["GitLabRepoBranch"], this.HostEnvironment);
                    if (GitLab_environment.RepositoryInitialised)
                    {
                        this.AuditEnvironment = GitLab_environment;
                        this.AuditEnvironmentIntialised = true;
                        this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                        this.AuditEnvironment.MessageHandler -= HostEnvironmentMessage;
                        this.AuditEnvironment.MessageHandler += this.AuditEnvironmentMessage;
                    }
                    else
                    {
                        GitLab_environment = null;
                        this.AuditEnvironmentIntialised = false;
                        throw new Exception("Failed to initialise audit environment.");
                    }
                }
                else
                {
                    this.AuditEnvironmentMessage = AuditTarget_AuditEnvironmentMessageHandler;
                    this.AuditEnvironment = new LocalEnvironment(this.AuditEnvironmentMessage);
                    this.AuditEnvironmentIntialised = true;
                }
                if (this.AuditOptions.ContainsKey("Profile"))
                {
                    AuditFileInfo pf = this.AuditEnvironment.ConstructFile((string)this.AuditOptions["Profile"]);
                    if (pf.Exists)
                    {
                        this.AuditProfile = new AuditProfile(this.AuditEnvironment, pf);
                    }
                    else
                    {
                        this.AuditEnvironment.Warning("The profile file {0} does not exist. No audit profile will be used.", pf.FullName);
                    }
                }

                if (this.AuditEnvironment is IOperatingSystemEnvironment)
                {
                    this.AuditEnvironment.GetOSName();
                    this.AuditEnvironment.GetOSVersion();
                    if (this.AuditOptions.ContainsKey("OSName"))
                    {
                        this.AuditEnvironment.OSName = (string)this.AuditOptions["OSName"];
                        this.AuditEnvironment.Info("Overriding audit environment OS name to {0}.", this.AuditEnvironment.OSName);
                    }
                    if (this.AuditOptions.ContainsKey("OSVersion"))
                    {
                        this.AuditEnvironment.OSVersion = (string)this.AuditOptions["OSVersion"];
                        this.AuditEnvironment.Info("Overriding audit environment OS version to {0}.", this.AuditEnvironment.OSVersion);
                    }
                }
            }
            #endregion

            #region Setup data source options
            if (this.AuditOptions.ContainsKey("HttpsProxy"))
            {
                this.HostEnvironment.Info("Using proxy {0}.", ((Uri)AuditOptions["HttpsProxy"]).AbsoluteUri);
            }

            if (this.AuditOptions.ContainsKey("DeleteCache"))
            {
                this.DataSourceOptions.Add("DeleteCache", true);
            }

            if (this.AuditOptions.ContainsKey("IgnoreHttpsCertErrors"))
            {
                this.DataSourceOptions.Add("IgnoreHttpsCertErrors", true);
            }

            if (this.AuditOptions.ContainsKey("WithOSSI")) 
            {
                this.DataSources.Add(new OSSIndexApiv3DataSource(this, DataSourceOptions));
            }

            if (this.AuditOptions.ContainsKey("WithVulners"))
            {
                this.DataSources.Add(new VulnersDataSource(this, DataSourceOptions));
            }

            #endregion

            #region Setup audit profile
            if (this.AuditEnvironment.FileExists("devaudit.yml"))
            {
                this.AuditProfile = new AuditProfile(this.AuditEnvironment, this.AuditEnvironment.ConstructFile("devaudit.yml"));
            }

            #endregion
        }

        #region Event Handlers
        private void AuditTarget_HostEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "HOST";
            this.ControllerMessage.Invoke(sender, e);
        }

        private void AuditTarget_AuditEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "AUDIT";
            this.ControllerMessage.Invoke(sender, e);
        }

        private void AuditTarget_ScriptEnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            e.EnvironmentLocation = "SCRIPT";
            this.ControllerMessage.Invoke(sender, e);
        }
        #endregion

        #endregion

        #region Events
        protected event EventHandler<EnvironmentEventArgs> HostEnvironmentMessage;
        protected event EventHandler<EnvironmentEventArgs> AuditEnvironmentMessage;
        protected event EventHandler<EnvironmentEventArgs> ControllerMessage;
        #endregion
        
        #region Properties
        public string DevAuditDirectory = Assembly.GetEntryAssembly() != null ?
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) : Directory.GetCurrentDirectory();

        public Dictionary<string, object> AuditOptions { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, object> DataSourceOptions { get; set; } = new Dictionary<string, object>();

        public AuditProfile AuditProfile { get; set; }

        public bool IsDockerized { get; protected set; }

        public LocalEnvironment HostEnvironment { get; protected set; }

        public AuditEnvironment AuditEnvironment { get; protected set; }

        public bool HostEnvironmentInitialised { get; private set; } = false;

        public bool AuditEnvironmentIntialised { get; private set; } = false;

        public List<IDataSource> DataSources { get; } = new List<IDataSource>();

        public bool UseAsyncMethods { get; private set; } = false;                
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

                        foreach(IDataSource ds in DataSources)
                        {
                            if (ds is HttpDataSource)
                            {
                                HttpDataSource hs = ds as HttpDataSource;
                                hs.Dispose();
                            }
                        }
                        if (AuditEnvironmentMessage != null)
                        {
                            foreach (Delegate d in this.AuditEnvironmentMessage.GetInvocationList())
                            {
                                this.AuditEnvironmentMessage -= (EventHandler<EnvironmentEventArgs>)d;
                            }
                        }

                        if (this.AuditEnvironment != null)
                        {
                            this.AuditEnvironment.Dispose();
                            this.AuditEnvironment = null;
                        }
                        if (this.HostEnvironmentMessage != null)
                        {
                            foreach (Delegate d in this.HostEnvironmentMessage.GetInvocationList())
                            {
                                this.HostEnvironmentMessage -= (EventHandler<EnvironmentEventArgs>)d;
                            }
                        }

                        if (this.HostEnvironment != null)
                        {
                            this.HostEnvironment.Dispose();
                            this.HostEnvironment = null;
                        }
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

        ~AuditTarget()
        {
            Dispose(false);
        }
        #endregion

    }
}
