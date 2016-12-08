using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class ApplicationServer : Application
    {
        #region Constructors
        public ApplicationServer(Dictionary<string, object> server_options, Dictionary<PlatformID, string[]> default_configuration_file_path, Dictionary<string, string[]> RequiredFilePaths, Dictionary<string, string[]> RequiredDirectoryPaths, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, RequiredFilePaths, RequiredDirectoryPaths, message_handler)
        {
            this.ServerOptions = server_options;
            if (default_configuration_file_path == null) throw new ArgumentNullException("default_configuration_file_path");
            InitialiseConfigurationFile(default_configuration_file_path);
        }

        public ApplicationServer(Dictionary<string, object> server_options, string[] default_configuration_file_path, Dictionary<string, string[]> RequiredFilePaths, Dictionary<string, string[]> RequiredDirectoryPaths, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, RequiredFilePaths, RequiredDirectoryPaths, message_handler)
        {
            this.ServerOptions = server_options;
            if (default_configuration_file_path == null) throw new ArgumentNullException("default_configuration_file_path");
            InitialiseConfigurationFile(default_configuration_file_path);
        }
        #endregion

        #region Public abstract properties
        public abstract string ServerId { get; }
        public abstract string ServerLabel { get; }
        #endregion

        #region Public properties
        public override string ApplicationId => this.ServerId;

        public override string ApplicationLabel => this.ServerLabel;

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel => this.ServerLabel;

        public string DefaultConfigurationFile { get; protected set; }
      
        public AuditFileInfo ConfigurationFile
        {
            get
            {
                return (AuditFileInfo)this.ApplicationFileSystemMap["ConfigurationFile"];
            }
        }
        public Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public Dictionary<string, object> ServerOptions { get; set; } = new Dictionary<string, object>();
        #endregion

        #region Public methods
        #endregion

        #region Protected methods
        protected void InitialiseConfigurationFile(string[] default_configuration_file_path)
        {
            this.DefaultConfigurationFile = CombinePath(default_configuration_file_path);
            if (!this.ServerOptions.ContainsKey("ConfigurationFile") && !string.IsNullOrEmpty(this.DefaultConfigurationFile))
            {
                if (this.AuditEnvironment.FileExists(this.DefaultConfigurationFile))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", this.AuditEnvironment.ConstructFile(this.DefaultConfigurationFile));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file was not specified and the default configuration file {0} could not be found.", this.DefaultConfigurationFile), "server_options");
                }
            }
            else
            {
                string cf = CombinePath((string)this.ServerOptions["ConfigurationFile"]);
                if (this.AuditEnvironment.FileExists(cf))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", this.AuditEnvironment.ConstructFile(cf));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file {0} was not found.", cf), "server_options");
                }
            }
        }

        protected void InitialiseConfigurationFile(Dictionary<PlatformID, string[]> default_configuration_file_path)
        {
            if (!default_configuration_file_path.Keys.Contains(this.AuditEnvironment.OS.Platform))
            {
                throw new ArgumentException("No default configuration file for current audit environment specified.");
            }
            this.InitialiseConfigurationFile(default_configuration_file_path[this.AuditEnvironment.OS.Platform]);
        }
        #endregion

        #region Private fields
        private Task<Dictionary<string, object>> _ConfigurationTask;
        #endregion
    }
}
