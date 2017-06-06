using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;

using Versatile;
using Alpheus;
using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class PostgreSQLServer : ApplicationServer, IDbAuditTarget
    {
        #region Constructors
        public PostgreSQLServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, 
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] {"find", "@", "*", "bin", "postgres" } },
                { PlatformID.Win32NT, new string[] { "@", "bin", "postgres.exe" } }
            }, 
            new Dictionary<PlatformID, string[]>() {
                { PlatformID.Unix, new string[] { "find", "@", "var", "lib", "*sql", "*", "data", "postgresql.conf" } },
                { PlatformID.Win32NT, new string[] { "@", "data", "postgresql.conf" } }
            }, 
            new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["postgres"] = this.ApplicationBinary;
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "postgresql"; } }

        public override string ServerLabel { get { return "PostgreSQL"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override string GetVersion()
        {
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed && (process_output.Contains("postgres (PostgreSQL) ") || process_error.Contains("postgres (PostgreSQL) ")))
            {
                if (!string.IsNullOrEmpty(process_error) && string.IsNullOrEmpty(process_output))
                {
                    process_output = process_error;
                }
                this.Version = process_output.Substring("postgres (PostgreSQL) ".Length).Trim();
                this.VersionInitialised = true;
                this.AuditEnvironment.Success("Got {0} server version {1}.", this.ApplicationLabel, this.Version);
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute command {0} successfully or could not parse command output. Process output: {1}.\nProcess error: {2}.", ApplicationBinary.Name, process_output, process_error));
            }
        }

        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"postgres", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "postgres", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized = this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override IConfiguration GetConfiguration()
        {
            PostgreSQL pgsql = new PostgreSQL(this.ConfigurationFile, this.AlpheusEnvironment);
            if (pgsql.ParseSucceded)
            {
                this.Configuration = pgsql;
                this.ConfigurationInitialised = true;
                this.AuditEnvironment.Success(this.ConfigurationStatistics);
                if (this.AuditEnvironment.OS.Platform == PlatformID.Unix)
                {
                    string auto_file_path = this.FindServerFile(this.CombinePath("@", "var", "lib", "*sql", "*", "postgresql.auto.conf")).FirstOrDefault();
                    if (!string.IsNullOrEmpty(auto_file_path))
                    {
                        AuditFileInfo auto_config_file = this.AuditEnvironment.ConstructFile(auto_file_path);
                        this.AuditEnvironment.Info("Found PostgreSQL server auto configuration file {0}.", auto_config_file.FullName);
                        PostgreSQL auto_pgsql = new PostgreSQL(auto_config_file);
                        if (auto_pgsql.ParseSucceded)
                        {
                            pgsql.XmlConfiguration.Root.Element("Values").Add(auto_pgsql.XmlConfiguration.Root.Element("Values").Descendants());
                            this.AuditEnvironment.Success("Merged configuration from {0}.", auto_pgsql.FullFilePath);
                        }
                    }
                }
                this.Configuration.XmlConfiguration.Root.Add(new XAttribute("Version", this.Version));
            }
            else
            {
                this.AuditEnvironment.Error("Could not parse configuration from {0}.", pgsql.FullFilePath);
                if (pgsql.LastParseException != null) this.AuditEnvironment.Error(pgsql.LastParseException);
                if (pgsql.LastIOException != null) this.AuditEnvironment.Error(pgsql.LastIOException);
                this.Configuration = null;
                this.ConfigurationInitialised = false;
            }
            return this.Configuration;
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised)
            {
                throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            }
            else
            {
                return this.GetModules()["postgres"];
            }
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

        public XPathNodeIterator ExecuteDbQueryToXml(object[] args)
        {
            CallerInformation caller = this.AuditEnvironment.Here();
            XmlDocument queryXml = new XmlDocument();
            bool execute_as_os_user = !string.IsNullOrEmpty(this.OSUser);
            bool execute_as_app_user = !string.IsNullOrEmpty(this.AppUser);
            string pgsql_query;
            string pgsql_db = string.Empty;
            if (args.Count() == 1)
            {
                pgsql_query = (string)args[0];
            }
            else
            {
                pgsql_db = (string)args[0];
                pgsql_query = (string)args[1];
            }
            AuditEnvironment.ProcessExecuteStatus status = AuditEnvironment.ProcessExecuteStatus.Unknown;
            string output = string.Empty, error = string.Empty;
            string pgsql_cmd, pgsql_args;
            if (this.AuditEnvironment.IsWindows)
            {
                pgsql_cmd = CombinePath("@", "bin", "psql.exe");
                if (!execute_as_app_user)
                {

                    if (string.IsNullOrEmpty(pgsql_db))
                    {
                        pgsql_args = string.Format("-w -H -c \"{0}\"", pgsql_query);
                    }
                    else
                    {
                        pgsql_args = string.Format("-w -H -d {0} -c \"{1}\"", pgsql_db, pgsql_query);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(pgsql_db))
                    {
                        pgsql_args = string.Format("-U {0} -w -H -c \"{1}\"", this.AppUser, pgsql_query);
                    }
                    else
                    {
                        pgsql_args = string.Format("-U {0} -w -H -d {1} -c \"{2}\"", this.AppUser, pgsql_db, pgsql_query);
                    }

                }
            }
            else
            {
                pgsql_cmd = "psql";
                if (!execute_as_app_user)
                {

                    if (string.IsNullOrEmpty(pgsql_db))
                    {
                        pgsql_args = string.Format("-w -H -c \'{0}\'", pgsql_query);
                    }
                    else
                    {
                        pgsql_args = string.Format("-w -H -d {0} -c \'{1}\'", pgsql_db, pgsql_query);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(pgsql_db))
                    {
                        pgsql_args = string.Format("-U {0} -w -H -c \'{1}\'", this.AppUser, pgsql_query);
                    }
                    else
                    {
                        pgsql_args = string.Format("-U {0} -w -H -d {1} -c \'{2}\'", this.AppUser, pgsql_db, pgsql_query);
                    }

                }


            }
            bool result = execute_as_os_user ? this.AuditEnvironment.ExecuteAsUser(pgsql_cmd, pgsql_args, out status, out output, out error, this.OSUser, this.OSPass) 
                : this.AuditEnvironment.Execute(pgsql_cmd, pgsql_args, out status, out output, out error, 
                this.AppPass != null ? new Dictionary<string, string> { { "PGPASSWORD", this.AuditEnvironment.ToInsecureString(this.AppPass) } } : null);

            if (result)
            {
                if (!output.StartsWith("ERROR"))
                {
                    this.AuditEnvironment.Debug(caller, "PGSQL query \"{0}\" returned: {1}", pgsql_query, output);
                    return ConvertPGSQLHtml("<root>" + output + "</root>").CreateNavigator().Select("/");
                }
                else
                {
                    this.AuditEnvironment.Error(caller, "Could not execute database query \"{0}\" on PGSQL server. Server returned: {1}", pgsql_query, output);
                    queryXml.LoadXml(string.Format("<error><![CDATA[{0}]]><error>", output));
                    return queryXml.CreateNavigator().Select("/");
                }
            }
            else
            {
                this.AuditEnvironment.Error(caller, "Could not execute command {0} {1}. Error: {2} {3}", pgsql_cmd, pgsql_query, error, output);
                queryXml.LoadXml(string.Format("<error><![CDATA[{0}\n{1]]]><error>", error, output));

                return queryXml.CreateNavigator().Select("/");
            }
        }

        protected XDocument ConvertPGSQLHtml(string html)
        {
            XDocument h = XDocument.Parse(WebUtility.HtmlDecode(html));
            XDocument x = XDocument.Parse("<resultSet></resultSet>");
            List<string> field_names = h.Root.Element("table").Elements("tr").First()
                .Elements("th").Select(e => e.Value).ToList();
            foreach (XElement e in h.Root.Element("table").Elements("tr").Skip(1))
            {
                XElement row = new XElement("row");
                List<XElement> field_values = e.Elements("td").ToList();
                for  (int i = 0; i < field_values.Count; i++)
                {
                    XElement field = new XElement("field", field_values[i].Value);
                    field.Add(new XAttribute("name", field_names[i]));
                    row.Add(field);
                }

                x.Root.Add(row);
            }
                
            return x;
        }
        #endregion
    }
}
