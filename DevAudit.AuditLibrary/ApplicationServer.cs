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

        public abstract string DefaultConfigurationFile { get; }
        #endregion

        #region Public abstract methods
        public abstract string GetVersion();
        #endregion

        #region Public properties
        //public Dictionary<string, FileSystemInfo> ServerFileSystemMap { get; } = new Dictionary<string, FileSystemInfo>();

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
           
            if (!this.ServerOptions.ContainsKey("ConfigurationFile") && string.IsNullOrEmpty(this.DefaultConfigurationFile))
            {
                throw new ArgumentException(string.Format("The server configuration file was not specified and no default configuration file can be used."), "server_options");
            }
            else if (!this.ServerOptions.ContainsKey("ConfigurationFile") && !string.IsNullOrEmpty(this.DefaultConfigurationFile))
            {
                string cf;
                if (this.DefaultConfigurationFile.First() == '@')
                {
                    cf = Path.Combine(this.RootDirectory.FullName, this.DefaultConfigurationFile.Substring(1));
                }
                else
                {
                    cf = this.DefaultConfigurationFile;
                }
                if (File.Exists(cf))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", new FileInfo(cf));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file was not specified and the default configuration file {0} could not be found.", cf), "server_options");
                }
            }
            else
            {
                string cf = (string)this.ServerOptions["ConfigurationFile"];
                if (cf.StartsWith("@"))
                {
                    cf = Path.Combine(this.RootDirectory.FullName, cf.Substring(1));
                }
                if (File.Exists(cf))
                {
                    this.ApplicationFileSystemMap.Add("ConfigurationFile", new FileInfo(cf));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file {0} was not found.", cf), "server_options");
                }
            }           
        }
        #endregion

        #region Private fields
        private Task<Dictionary<string, object>> _ConfigurationTask;
        #endregion
    }
}
