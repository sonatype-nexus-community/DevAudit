using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class ApplicationServer : Application
    {
        #region Public abstract properties
        public abstract string ServerId { get; }
        public abstract string ServerLabel { get; }
        public abstract Dictionary<string, string> OptionalFileLocations { get; }
        public abstract Dictionary<string, string> OptionalDirectoryLocations { get; }
        #endregion

        #region Public abstract methods
        public abstract string GetVersion();
        #endregion

        #region Public properties
        public string DefaultConfigurationFile { get; protected set; }

        public FileInfo ConfigurationFile
        {
            get
            {
                return (FileInfo)this.ApplicationFileSystemMap["ConfigurationFile"];
            }
        }

        public string Version { get; set; }

        public Dictionary<string, object> ServerOptions { get; set; } = new Dictionary<string, object>();
        #endregion

        #region Constructors
        public ApplicationServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, message_handler)
        {
            if (ReferenceEquals(server_options, null)) throw new ArgumentNullException("server_options");
            this.ServerOptions = server_options;  
        }

        public ApplicationServer(Dictionary<string, object> server_options, Dictionary<PlatformID, string[]> default_configuration_file_path, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, message_handler)
        {
            if (default_configuration_file_path == null) throw new ArgumentNullException("default_configuration_file");
            InitialiseConfigurationFile(default_configuration_file_path);
        }

        public ApplicationServer(Dictionary<string, object> server_options, string[] default_configuration_file_path, EventHandler<EnvironmentEventArgs> message_handler = null) : base(server_options, message_handler)
        {
            if (default_configuration_file_path == null) throw new ArgumentNullException("default_configuration_file");
            InitialiseConfigurationFile(default_configuration_file_path);
        }


        #endregion

        #region Protected methods
        protected void InitialiseConfigurationFile(string[] default_configuration_file_path)
        {
            this.DefaultConfigurationFile = CombinePath(default_configuration_file_path);
            if (!this.ServerOptions.ContainsKey("ConfigurationFile") && !string.IsNullOrEmpty(this.DefaultConfigurationFile))
            {
                if (this.AuditEnvironment.FileExists(this.DefaultConfigurationFile))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", new AuditFileInfo(this.AuditEnvironment, this.DefaultConfigurationFile));
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
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", new AuditFileInfo(this.AuditEnvironment, cf));
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
