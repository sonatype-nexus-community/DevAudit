using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

using Versatile;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class MySQLServer : ApplicationServer, IDbAuditTarget
    {
        #region Constructors
        public MySQLServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, 
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix,  new string[] { "find", "@", "*bin", "mysqld" } },
                { PlatformID.Win32NT, new string[] { "@", "bin", "mysqld.exe" } }
            },
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "find", "@", "etc", "*", "mysql", "my.cnf" } },
                { PlatformID.Win32NT, new string[] { "@", "my.ini" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["mysqld"] = this.ApplicationBinary;
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override void DetectConfigurationFile(Dictionary<PlatformID, string[]> default_configuration_file_path)
        {
            bool set_config_from_process_cmdline = false;
            bool set_config_from_env = false;
            if (this.AuditEnvironment.IsUnix)
            {
                List<ProcessInfo> processes = this.AuditEnvironment.GetAllRunningProcesses();
                if (processes != null && processes.Any(p => p.CommandLine.Contains("mysqld") && (p.CommandLine.Contains("--user") || p.CommandLine.Contains("--datadir") || p.CommandLine.Contains("--basedir"))))
                {
                    ProcessInfo process = processes.Where(p => p.CommandLine.Contains("mysqld") && (p.CommandLine.Contains("--user") || p.CommandLine.Contains("--datadir") || p.CommandLine.Contains("--basedir"))).First();
                    Match m = Regex.Match(process.CommandLine, @"--defaults-file=(\S+)");
                    if (m.Success)
                    {
                        string f = m.Groups[1].Value;
                        AuditFileInfo cf = this.AuditEnvironment.ConstructFile(f);
                        if (cf.Exists)
                        {
                            this.AuditEnvironment.Success("Auto-detected {0} server configuration file at {1}.", this.ApplicationLabel, cf.FullName);
                            this.ApplicationFileSystemMap.Add("ConfigurationFile", cf);
                            set_config_from_process_cmdline = true;
                        }
                    }
                }
                if (!set_config_from_process_cmdline)
                {
                    Dictionary<string, string> env = this.AuditEnvironment.GetEnvironmentVars();
                    if (env != null)
                    {
                        if (env.ContainsKey("MY_CNF"))
                        {
                            if (!set_config_from_process_cmdline)
                            {
                                AuditFileInfo cf = this.AuditEnvironment.ConstructFile(env["MY_CNF"]);
                                if (cf.Exists)
                                {
                                    this.AuditEnvironment.Success("Auto-detected {0} server configuration file at {1}.", this.ApplicationLabel, cf.FullName);
                                    this.ApplicationFileSystemMap.Add("ConfigurationFile", cf);
                                    set_config_from_env = true;
                                }
                            }
                        }
                    }
                }
                if (!(set_config_from_process_cmdline || set_config_from_env))
                {
                    base.DetectConfigurationFile(default_configuration_file_path);
                }
            }
        }

        protected override Dictionary<string, IEnumerable<Package>> GetModules()
        {
            Dictionary<string, IEnumerable<Package>> m = new Dictionary<string, IEnumerable<Package>>
            {
                {"mysqld", new List<Package> {new Package(this.PackageManagerId, "mysqld", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized =  this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override string GetVersion()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(this.ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            sw.Stop();
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                string v0 = process_output.Substring(process_output.IndexOf("Ver") + 4);
                string v1 = v0.Substring(0, v0.IndexOf(" "));
                this.Version = new string(v1.TakeWhile(v => char.IsDigit(v) || v == '.').ToArray());
                this.VersionInitialised = true;
                this.AuditEnvironment.Success("Got {0} version {1} in {2} ms.", this.ApplicationLabel, this.Version, sw.ElapsedMilliseconds);
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        protected override IConfiguration GetConfiguration()
        {
            MySQL mysql = new MySQL(this.ConfigurationFile, this.AlpheusEnvironment);
            ;
            if (mysql.ParseSucceded)
            {
                this.Configuration = mysql;
                this.ConfigurationInitialised = true;
            }
            else
            {
                this.AuditEnvironment.Error("Could not parse configuration from {0}.", mysql.FullFilePath);
                if (mysql.LastParseException != null) this.AuditEnvironment.Error(mysql.LastParseException);
                if (mysql.LastIOException != null) this.AuditEnvironment.Error(mysql.LastIOException);
                this.Configuration = null;
                this.ConfigurationInitialised = false;
            }
            return this.Configuration;
        }


        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            return this.ModulePackages["mysqld"];
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            string message = "";
            bool r = NuGetv2.RangeIntersect(vulnerability_version, package_version, out message);
            if (!r && !string.IsNullOrEmpty(message))
            {
                throw new Exception(message);
            }
            else return r;
        }
        #endregion

        #region Properties
        public AuditDirectoryInfo ServerDataDirectory { get; protected set; }
        #endregion

        #region Methods
        public bool DetectServerDataDirectory()
        {
            bool set_data_from_process_cmdline = false;
            bool set_data_from_config = false;
            if (this.AuditEnvironment.IsUnix)
            {
                List<ProcessInfo> processes = this.AuditEnvironment.GetAllRunningProcesses();
                if (processes != null && processes.Any(p => p.CommandLine.Contains("mysqld") && p.CommandLine.Contains("--datadir")))
                {
                    ProcessInfo process = processes.Where(p => p.CommandLine.Contains("mysqld") && p.CommandLine.Contains("--datadir")).First();
                    Match m = Regex.Match(process.CommandLine, @"--datadir=(\S+)\s+");
                    if (m.Success)
                    {
                        string d = m.Groups[1].Value;
                        AuditDirectoryInfo df = this.AuditEnvironment.ConstructDirectory(d);
                        if (df.Exists)
                        {
                            this.AuditEnvironment.Success("Auto-detected {0} server data directory at {1}.", this.ApplicationLabel, df.FullName);
                            this.ServerDataDirectory = df;
                            this.ApplicationFileSystemMap.Add("Data", df);
                            set_data_from_process_cmdline = true;
                        }
                    }
                }
                if (!set_data_from_process_cmdline)
                {
                    string t = this.ConfigurationFile.ReadAsText();
                    Match m3 = Regex.Match(t, @"[^#]?datadir\s?=\s?(\S+)");
                    if (m3.Success)
                    {
                        AuditDirectoryInfo df = this.AuditEnvironment.ConstructDirectory(m3.Groups[1].Value);
                        if (df.Exists)
                        {
                            this.AuditEnvironment.Success("Auto-detected {0} server data directory at {1}.", this.ApplicationLabel, df.FullName);
                            this.ServerDataDirectory = df;
                            this.ApplicationFileSystemMap.Add("Data", df);
                            set_data_from_config = true;
                        }
                    }
                }
                return set_data_from_process_cmdline || set_data_from_config;        
            }
            else
            {
                return false;
            }
                
        }

        public XPathNodeIterator ExecuteDbQueryToXml(object[] args)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            XmlDocument queryXml = new XmlDocument();
            string mysql_query;
            string mysql_db;
            if (args.Count() == 1)
            {
                mysql_query = (string)args[0];
            }
            else
            {
                mysql_db = (string)args[0];
                mysql_query = (string)args[1];
            }
            AuditEnvironment.ProcessExecuteStatus status = AuditEnvironment.ProcessExecuteStatus.Unknown;
            string output = string.Empty, error = string.Empty, mysql_args;
            if (this.AuditEnvironment.IsUnix)
            {
                mysql_query = mysql_query.Replace("'", "\\'");
                if (string.IsNullOrEmpty(this.AppUser))
                {
                    mysql_args = string.Format("-X --execute=$\'{0}\'", mysql_query);
                }
                else if (AppPass == null)
                {
                    mysql_args = string.Format("--user={0} -X --execute=$\'{1}\'", this.AppUser, mysql_query);
                }
                else
                {
                    mysql_args = string.Format("--user={0} --password={1} -X --execute=$\'{2}\'", this.AppUser, this.AuditEnvironment.ToInsecureString(this.AppPass), mysql_query);
                }
            }
            else if (this.AuditEnvironment is WinRmAuditEnvironment)
            {
                if (string.IsNullOrEmpty(this.AppUser))
                {
                    mysql_args = string.Format("-X\t--execute={0}", mysql_query);
                }
                else if (AppPass == null)
                {
                    mysql_args = string.Format("-X\t--user={0}\t--execute={1}", this.AppUser, mysql_query);
                }
                else
                {
                    mysql_args = string.Format("--user={0}\t--password={1}\t-X\t--execute={1}", this.AppUser, this.AuditEnvironment.ToInsecureString(this.AppPass), mysql_query);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(this.AppUser))
                {
                    mysql_args = string.Format("-X\t--execute=\"{0}\"", mysql_query);
                }
                else if (AppPass == null)
                {
                    mysql_args = string.Format("--user={0}\t-X\t--execute=\"{1}\"", this.AppUser, mysql_query);
                }
                else
                {
                    mysql_args = string.Format("--user={0}\t--password={1}\t-X\t--execute=\"{1}\"", this.AppUser, this.AuditEnvironment.ToInsecureString(this.AppPass), mysql_query);
                }
            }
            bool r;
            if (string.IsNullOrEmpty(this.OSUser))
            {
                r = this.AuditEnvironment.Execute("mysql", mysql_args, out status, out output, out error);
            }
            else
            {
                r = this.AuditEnvironment.ExecuteAsUser("mysql", mysql_args, out status, out output, out error, this.OSUser, this.OSPass);
            }
            if (r)
            {
                if (!output.StartsWith("ERROR"))
                {
                    this.AuditEnvironment.Debug(caller, "MySQL query \"{0}\" returned: {1}", mysql_query, output);
                    queryXml.LoadXml(output);
                    return queryXml.CreateNavigator().Select("/");
                }
                else
                {
                    this.AuditEnvironment.Error(caller, "Could not execute database query \"{0}\" on MySQL server. Server returned: {1}", mysql_query, output);
                    queryXml.LoadXml(string.Format("<error><![CDATA[{0}]]></error>", output));
                    return queryXml.CreateNavigator().Select("/");
                }
            }
            else
            {
                this.AuditEnvironment.Error(caller, "Could not execute database query \"{0}\" on MySQL server. Error: {1} {2}", mysql_query, error, output);
                queryXml.LoadXml(string.Format("<error><![CDATA[{0}\n{1}]]></error>", error, output));
                return queryXml.CreateNavigator().Select("/");
            }
        }
        #endregion
    }
}
