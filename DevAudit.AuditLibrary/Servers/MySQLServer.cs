using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"mysqld", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "mysqld", this.Version) }}
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
                this.Version = v0.Substring(0, v0.IndexOf(" "));
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


        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
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

        #region Methods
        public XPathNodeIterator ExecuteDbQueryToXml(object[] args)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            XmlDocument queryXml = new XmlDocument();
            if (string.IsNullOrEmpty(this.AppUser))
            {
                this.AuditEnvironment.Error(caller, "The MySQL user was not specified in the audit options so database queries cannot be executed.");
                queryXml.LoadXml("<error>No User</error>");
                return queryXml.CreateNavigator().Select("/");
            }
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
            string output = string.Empty, error = string.Empty;
            string mysql_args = this.AuditEnvironment.IsWindows ? string.Format("--user={0}\t--password={1}\t-X\t--execute={2}", this.AppUser, this.AppPass, mysql_query)
                : string.Format("--user={0} --password={1} -X --execute=\"{2}\"", this.AppUser, this.AppPass, mysql_query);
            if (this.AuditEnvironment.Execute("mysql", mysql_args, out status, out output, out error))
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
                    queryXml.LoadXml(string.Format("<error><![CDATA[{0}]]><error>", output));
                    return queryXml.CreateNavigator().Select("/");
                }
            }
            else
            {
                this.AuditEnvironment.Error(caller, "Could not execute database query \"{0}\" on MySQL server. Error: {1} {2}", mysql_query, error, output);
                queryXml.LoadXml(string.Format("<error><![CDATA[{0}\n{1]]]><error>", error, output));
                return queryXml.CreateNavigator().Select("/");
            }
        }
        #endregion
    }
}
