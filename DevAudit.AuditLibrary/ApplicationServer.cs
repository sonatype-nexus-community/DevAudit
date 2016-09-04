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
        public ApplicationServer() { } 

        public ApplicationServer(Dictionary<string, object> server_options) : base(server_options)
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

            /*
            if (!this.ServerOptions.ContainsKey("RootDirectory"))
            {
                throw new ArgumentException(string.Format("The root server directory was not specified."), "server_options");
            }
            else if (!Directory.Exists((string)this.ServerOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root server directory {0} was not found.", this.ServerOptions["RootDirectory"]), "server_options");
            }
            else
            {
                this.ServerFileSystemMap.Add("RootDirectory", new DirectoryInfo((string)this.ServerOptions["RootDirectory"]));
            }

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
                    this.ServerFileSystemMap.Add("ConfigurationFile", new FileInfo(cf));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file was not specified and the default configuration file {0} could not be found.", cf), "server_options");
                }
            }
            else 
            {
                string cf = (string) this.ServerOptions["ConfigurationFile"];
                if (cf.StartsWith("@"))
                {
                    cf = Path.Combine(this.RootDirectory.FullName, cf.Substring(1));
                }
                if (File.Exists(cf))
                {
                    this.ServerFileSystemMap.Add("ConfigurationFile", new FileInfo(cf));
                }
                else
                {
                    throw new ArgumentException(string.Format("The server configuration file {0} was not found.", cf), "server_options");
                }           
            }

            foreach (KeyValuePair<string, string> f in RequiredFileLocations)
            {
                string fn = f.Value;
                if (f.Value.StartsWith("@"))
                {
                    fn = Path.Combine(this.RootDirectory.FullName, f.Value.Substring(1));
                }
                if (!this.ServerOptions.ContainsKey(f.Key))
                {
                    if (string.IsNullOrEmpty(f.Value))
                    {
                        throw new ArgumentException(string.Format("The required server file {0} was not specified and no default path exists.", f), "server_options");
                    }
                    else
                    {
                        if (!File.Exists(fn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required server file {1} does not exist.",
                                fn, f.Key), "RequiredFileLocations");
                        }
                        else
                        {
                            this.ServerFileSystemMap.Add(f.Key, new FileInfo(fn));
                        }
                    }

                }
                else
                {
                    fn = (string) ServerOptions[f.Key];
                    if (fn.StartsWith("@"))
                    {
                        fn = Path.Combine(this.RootDirectory.FullName, fn.Substring(1));
                    }
                    if (!File.Exists(fn))
                    {
                        throw new ArgumentException(string.Format("The required server file {0} was not found.", f), "server_options");
                    }
                    else
                    {
                        this.ServerFileSystemMap.Add(f.Key, new FileInfo(fn));
                    }
                }
            }

            foreach (KeyValuePair<string, string> d in RequiredDirectoryLocations)
            {
                string dn = d.Value;
                if (dn.StartsWith("@"))
                {
                    dn = Path.Combine(this.RootDirectory.FullName, dn.Substring(1));
                }
                if (!this.ServerOptions.ContainsKey(d.Key))
                {
                    if (string.IsNullOrEmpty(d.Value))
                    {
                        throw new ArgumentException(string.Format("The required server directory {0} was not specified and no default path exists.", d.Key), "server_options");
                    }
                    else
                    {
                        if (!Directory.Exists(dn))
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required server directory {1} does not exist.",
                                d.Key, dn), "RequiredDirectoryLocations");
                        }
                        else
                        {
                            this.ServerFileSystemMap.Add(d.Key, new DirectoryInfo(dn));
                        }
                    }

                }
                else
                {
                    dn = (string) ServerOptions[d.Key];
                    if (dn.StartsWith("@"))
                    {
                        dn = Path.Combine(this.RootDirectory.FullName, dn.Substring(1));
                    }

                    if (!Directory.Exists(dn))
                    {
                        throw new ArgumentException(string.Format("The required server directory {0} was not found.", dn), "server_options");
                    }
                    else
                    {
                        this.ServerFileSystemMap.Add(d.Key, new DirectoryInfo(dn));
                    }
                }
            }
            */
        }
        #endregion

        #region Private fields
        private Task<Dictionary<string, object>> _ConfigurationTask;
        #endregion
    }
}
