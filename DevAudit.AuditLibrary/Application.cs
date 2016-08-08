using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class Application : PackageSource
    {
        #region Public abstract properties
        public abstract string ApplicationId { get; }

        public abstract string ApplicationLabel { get; }
        
        public abstract Dictionary<string, string> RequiredFileLocations { get; }

        public abstract Dictionary<string, string> RequiredDirectoryLocations { get; }
        #endregion

        #region Public abstract methods
        public abstract Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules();
        public abstract Dictionary<string, object> GetConfiguration();
        #endregion

        #region Public properties
        public Dictionary<string, FileSystemInfo> ApplicationFileSystemMap { get; } = new Dictionary<string, FileSystemInfo>();

        public DirectoryInfo RootDirectory
        {
            get
            {
                return (DirectoryInfo)this.ApplicationFileSystemMap["RootDirectory"];
            }
        }

        public Dictionary<string, IEnumerable<OSSIndexQueryObject>> Modules { get; set; }

        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, object> ApplicationOptions { get; set; } = new Dictionary<string, object>();

        public Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> ModulesTask
        {
            get
            {
                if (_ModulesTask == null)
                {
                    _ModulesTask = Task.Run(() => this.GetModules());
                }
                return _ModulesTask;
            }
        }

        public Task<Dictionary<string, object>> ConfigurationTask
        {
            get
            {
                if (_ConfigurationTask == null)
                {
                    _ConfigurationTask = Task.Run(() => this.GetConfiguration());
                }
                return _ConfigurationTask;
            }
        }

        #endregion

        #region Constructors
        public Application() { }

        public Application(Dictionary<string, object> application_options)
        {
            if (ReferenceEquals(application_options, null)) throw new ArgumentNullException("application_options");
            this.ApplicationOptions = application_options;
            if (!this.ApplicationOptions.ContainsKey("RootDirectory"))
            {
                //.ApplicationFileSystemMap.Add("RootDirectory", new DirectoryInfo(Directory.GetCurrentDirectory()));
                throw new ArgumentException(string.Format("The root application directory was not specified."), "application_options");
            }
            else if (!Directory.Exists((string) this.ApplicationOptions["RootDirectory"]))
            {
                throw new ArgumentException(string.Format("The root application directory {0} was not found.", this.ApplicationOptions["RootDirectory"]), "application_options");
            }
            else
            {
                this.ApplicationFileSystemMap.Add("RootDirectory", new DirectoryInfo((string)this.ApplicationOptions["RootDirectory"]));
            }

            foreach (string f in RequiredFileLocations.Keys)
            {
                if (!this.ApplicationOptions.ContainsKey(f))
                {
                    if (string.IsNullOrEmpty(RequiredFileLocations[f]))
                    {
                        throw new ArgumentException(string.Format("The required application file {0} was not specified and no default path exists.", f), "application_options");
                    }
                    else
                    {
                        if (this.RootDirectory.GetFiles(RequiredFileLocations[f]).FirstOrDefault() == null)
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application file {1} does not exist.",
                                RequiredFileLocations[f], f), "RequiredFileLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(f, this.RootDirectory.GetFiles(RequiredFileLocations[f]).First());
                        }
                    }

                }
                else if (this.RootDirectory.GetFiles((string) ApplicationOptions[f]).FirstOrDefault() == null)
                {
                    throw new ArgumentException(string.Format("The required application file {0} was not found.", f), "application_options");
                }
                else
                {
                    this.ApplicationFileSystemMap.Add(f, this.RootDirectory.GetFiles((string) ApplicationOptions[f]).First());
                }
            }

            foreach (string d in RequiredDirectoryLocations.Keys)
            {
                if (!this.ApplicationOptions.ContainsKey(d))
                {
                    if (string.IsNullOrEmpty(RequiredDirectoryLocations[d]))
                    {
                        throw new ArgumentException(string.Format("The required application Directory {0} was not specified and no default path exists.", d), "application_options");
                    }
                    else
                    {
                        if (this.RootDirectory.GetDirectories(RequiredDirectoryLocations[d]).FirstOrDefault() == null)
                        {
                            throw new ArgumentException(string.Format("The default path {0} for required application directory {1} does not exist.",
                                RequiredDirectoryLocations[d], d), "RequiredDirectoryLocations");
                        }
                        else
                        {
                            this.ApplicationFileSystemMap.Add(d, this.RootDirectory.GetDirectories(RequiredDirectoryLocations[d]).First());
                        }
                    }

                }
                else if (this.RootDirectory.GetDirectories((string)ApplicationOptions[d]).FirstOrDefault() == null)
                {
                    throw new ArgumentException(string.Format("The required application Directory {0} was not found.", d), "application_options");
                }
                else
                {
                    this.ApplicationFileSystemMap.Add(d, this.RootDirectory.GetDirectories((string)ApplicationOptions[d]).First());
                }
            }
        }
        #endregion

        #region Static methods
        public static List<FileInfo> RecursiveFolderScan(DirectoryInfo dir, string pattern)
        {
            List<FileInfo> results = new List<FileInfo>();
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                results.AddRange(RecursiveFolderScan(d, pattern));
            }
            results.AddRange(dir.GetFiles(pattern));
            return results;
        }

        public static string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }
        #endregion

        #region Private fields
        private Task<Dictionary<string, IEnumerable<OSSIndexQueryObject>>> _ModulesTask;
        private Task<Dictionary<string, object>> _ConfigurationTask;
        #endregion

        #region Private methods

        #endregion

    }
}
