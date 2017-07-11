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
        #region Abstract properties
        public abstract string ServerId { get; }
        public abstract string ServerLabel { get; }
        #endregion

        #region Constructors
        public ApplicationServer(Dictionary<string, object> server_options, Dictionary<PlatformID, string[]> default_binary_file_path, Dictionary<PlatformID, string[]> default_configuration_file_path, Dictionary<string, string[]> RequiredFilePaths, Dictionary<string, string[]> RequiredDirectoryPaths, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(server_options, RequiredFilePaths, RequiredDirectoryPaths, message_handler)
        {
            this.ServerOptions = server_options;
            if (this.ServerOptions.ContainsKey("ServerSkipPackagesAudit"))
            {
                this.SkipPackagesAudit = true;
            }
            if (default_binary_file_path == null) throw new ArgumentNullException("default_binary_file_path");
            if (default_configuration_file_path == null) throw new ArgumentNullException("default_configuration_file_path");
            if (this.ApplicationBinary == null)
            {
                DetectServerBinaryFile(default_binary_file_path);
            }
            if (this.ServerOptions.ContainsKey("ConfigurationFile"))
            {
                string cf = this.CombinePath((string)this.ServerOptions["ConfigurationFile"]);
                if (this.AuditEnvironment.FileExists(cf))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", this.AuditEnvironment.ConstructFile(cf));
                    this.AuditEnvironment.Info("Using {0} configuration file {1}.", this.ApplicationLabel, cf);
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file {0} was not found.", cf), "server_options");
                }
            }
            else
            {
                DetectConfigurationFile(default_configuration_file_path);
            }
            if (!this.ApplicationFileSystemMap.ContainsKey("ConfigurationFile"))
            {
                throw new ArgumentException(string.Format("Could not auto-detect configuration file for {0} and the configuration file was not specified.", this.ApplicationLabel));
            }
            if (this is IDbAuditTarget)
            {
                bool d = (this as IDbAuditTarget).DetectServerDataDirectory();
                {
                    if (!d)
                    {
                        this.AuditEnvironment.Warning("Failed to auto-detect {0} database server data directory.", this.ApplicationLabel);
                    }
                }
            }
            
        }
        #endregion

        #region Properties
        public override string ApplicationId => this.ServerId;

        public override string ApplicationLabel => this.ServerLabel;

        public override string PackageManagerId { get { return "ossi"; } }

        public override string PackageManagerLabel => this.ServerLabel;

        public string DefaultConfigurationFilePath { get; protected set; }
      
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

        #region Methods
        protected virtual void DetectServerBinaryFile(Dictionary<PlatformID, string[]> autodetect_binary_file_path)
        {
            if (autodetect_binary_file_path.Keys.Contains(this.AuditEnvironment.OS.Platform))
            {
                this.FindServerBinaryFile(autodetect_binary_file_path[this.AuditEnvironment.OS.Platform]);
            }
        }

        protected virtual void DetectConfigurationFile(Dictionary<PlatformID, string[]> default_configuration_file_path)
        {
            if (default_configuration_file_path.Keys.Contains(this.AuditEnvironment.OS.Platform))
            {
                this.FindConfigurationFile(default_configuration_file_path[this.AuditEnvironment.OS.Platform]);
            }
        }

        protected void FindServerBinaryFile(string[] default_binary_file_path)
        {
            if (this.ApplicationBinary == null)
            {
                if (default_binary_file_path.Length > 1 && default_binary_file_path.First() == "which")
                {
                    string search_path = this.CombinePath(default_binary_file_path.Skip(1).ToArray());
                    string file = this.WhichServerFile(search_path);
                    if (!string.IsNullOrEmpty(file))
                    {
                        this.ApplicationBinary = this.AuditEnvironment.ConstructFile(file);
                    }

                }
                else if (default_binary_file_path.Length > 1 && default_binary_file_path.First() == "find")
                {
                    string search_path = this.CombinePath(default_binary_file_path.Skip(1).ToArray());
                    string file = this.FindServerFile(search_path).FirstOrDefault();
                    if (!string.IsNullOrEmpty(file))
                    {
                        this.ApplicationBinary = this.AuditEnvironment.ConstructFile(file);
                    }

                }
                else
                {
                    string file = this.CombinePath(default_binary_file_path);
                    if (this.AuditEnvironment.FileExists(file))
                    {
                        this.ApplicationBinary = this.AuditEnvironment.ConstructFile(file);
                    }
                }
                if (this.ApplicationBinary != null)
                {
                    this.AuditEnvironment.Success("Auto-detected {0} server binary at {1}.", this.ApplicationLabel, this.ApplicationBinary.FullName);
                }
            }
        }

        protected void FindConfigurationFile(string[] default_configuration_file_path)
        {
            if (!this.ServerOptions.ContainsKey("ConfigurationFile"))
            {
                if (default_configuration_file_path.Length > 1 && default_configuration_file_path.First() == "which")
                {
                    string search_path = this.CombinePath(default_configuration_file_path.Skip(1).ToArray());
                    string file = this.WhichServerFile(search_path);
                    if (!string.IsNullOrEmpty(file))
                    {
                        this.DefaultConfigurationFilePath = file;
                    }

                }
                else if (default_configuration_file_path.Length > 1 && default_configuration_file_path.First() == "find")
                {
                    string search_path = this.CombinePath(default_configuration_file_path.Skip(1).ToArray());
                    string file = this.FindServerFile(search_path).FirstOrDefault();
                    if (!string.IsNullOrEmpty(file))
                    {
                        this.DefaultConfigurationFilePath = file;
                    }

                }
                else
                {
                    this.DefaultConfigurationFilePath = this.CombinePath(default_configuration_file_path);
                }
            }
            if (!this.ServerOptions.ContainsKey("ConfigurationFile") && !string.IsNullOrEmpty(this.DefaultConfigurationFilePath))
            {
                if (this.AuditEnvironment.FileExists(this.DefaultConfigurationFilePath))
                {
                    this.AuditEnvironment.Success("Auto-detected {0} server configuration file {1}.", this.ApplicationLabel, this.DefaultConfigurationFilePath);
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", this.AuditEnvironment.ConstructFile(this.DefaultConfigurationFilePath));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file was not specified and the default configuration file {0} could not be found.", this.DefaultConfigurationFilePath), "server_options");
                }
            }
        }

        protected string WhichServerFile(string path)
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            if (this.AuditEnvironment.Execute("which", path, out process_status, out process_output, out process_error))
            {
                this.AuditEnvironment.Debug("WhichServerFile({0}) returned {1}.", path, process_output);
                return process_output;
            }
            else
            {
                this.AuditEnvironment.Debug("WhichServerFile({0}) returned null.", path); ;
                return string.Empty;
            }
        }

        protected string[] FindServerFile(string path)
        {
            if (this.AuditEnvironment.OS.Platform == PlatformID.Win32NT)
            {
                string process_output;
                string args = string.Format("/ -wholename '{0}'", path);
                bool r = this.AuditEnvironment.ExecuteCommand("find", args, out process_output, false);
                if (r || (!string.IsNullOrEmpty(process_output)))
                {
                    string[] f = process_output.Split(this.AuditEnvironment.LineTerminator.ToCharArray()).Where(o => !o.Contains("Permission denied")).ToArray();
                    this.AuditEnvironment.Debug("FindServerFile({0}) returned {1}.", path, f.Aggregate((f1, f2) => f1 + " " + f2));
                    return f;
                }
                else
                {
                    this.AuditEnvironment.Debug("FindServerFile({0}) returned null.", path);
                    return new string[] { };
                }
            }
            else
            {
                AuditEnvironment.ProcessExecuteStatus process_status;
                string process_output;
                string process_error;
                string args = string.Format("/ -wholename '{0}'", path);
                bool r = this.AuditEnvironment.Execute("find", args, out process_status, out process_output, out process_error);
                if (r || (!string.IsNullOrEmpty(process_output)))
                {
                    string[] files = process_output.Split(this.AuditEnvironment.LineTerminator.ToCharArray()).Where(o => !o.Contains("Permission denied")).ToArray();
                    this.AuditEnvironment.Debug("FindServerFile({0}) returned {1}.", path, files.Aggregate((f1, f2) => f1 + " " + f2));
                    return files;
                }
                else
                {
                    this.AuditEnvironment.Debug("FindServerFile({0}) returned null.", path);
                    return new string[] { };
                }
            }
        }
        #endregion

        #region Fields
        #endregion
    }
}
