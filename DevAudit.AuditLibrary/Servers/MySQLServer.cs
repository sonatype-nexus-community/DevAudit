using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IniParser.Parser;
using IniParser.Model;
using IniParser.Model.Configuration;

namespace DevAudit.AuditLibrary
{
    public class MySQLServer : ApplicationServer
    {
        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override string ApplicationId { get { return "mysql"; } }

        public override string ApplicationLabel { get { return "MySQL"; } }

        public override Dictionary<string, string> RequiredDirectoryLocations { get; } = new Dictionary<string, string>()
        {
            { "bin", "bin" }
        };

        public override Dictionary<string, string> RequiredFileLocations { get; } = new Dictionary<string, string>()
        {
            { "mysql", Path.Combine("bin", "mysql.exe") },
            
        };

        public override Dictionary<string, string> OptionalDirectoryLocations { get; } = new Dictionary<string, string>();

        public override Dictionary<string, string> OptionalFileLocations { get; } = new Dictionary<string, string>();

        public override string PackageManagerId { get { return "mysql"; } }

        public override string PackageManagerLabel { get { return "MySQL"; } }

        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string DefaultConfigurationFile { get; } = "my.ini";
        #endregion

        #region Public properties
        public DirectoryInfo MySQLBin
        {
            get
            {
                return this.RootDirectory.GetDirectories("bin").First();
            }
        }

        public FileInfo MySQLExe
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    return this.MySQLBin.GetFiles("mysql").First();
                }
                else
                {
                    return this.MySQLBin.GetFiles("mysql.exe").First();
                }
            }
        }

        #endregion
        #region Overriden methods
        public override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"mysqld", new List<OSSIndexQueryObject> {new OSSIndexQueryObject("mysql", "mysqld", this.Version) }}
            };
            this.Modules = m;
            return this.Modules;
        }

        public override string GetVersion()
        {
            HostEnvironment.ProcessStatus process_status;
            string process_output;
            string process_error;
            HostEnvironment.Execute(MySQLExe.FullName, "-V", out process_status, out process_output, out process_error);
            if (process_status == HostEnvironment.ProcessStatus.Success)
            {
                this.Version = process_output.Substring(process_output.IndexOf("Ver"));
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", MySQLExe.Name, process_error));
            }
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            IniParserConfiguration ini_parser_cfg = new IniParserConfiguration();
            ini_parser_cfg.CommentString = "#";
            ini_parser_cfg.AllowDuplicateKeys = true;
            ini_parser_cfg.OverrideDuplicateKeys = true;
            IniDataParser ini_parser = new IniDataParser(ini_parser_cfg);
            IniData data = null;
            using (StreamReader r = new StreamReader(this.ConfigurationFile.OpenRead()))
            {
                data = ini_parser.Parse(r.ReadToEnd());            
            }
            foreach (KeyData d in data.Global)
            {
                if (d.Value.First() == '"') d.Value = d.Value.Remove(0, 1);
                if (d.Value.Last() == '"') d.Value = d.Value.Remove(d.Value.Length - 1, 1);
                this.Configuration.Add(d.KeyName, d.Value);
            }
            foreach(SectionData s in data.Sections)
            {
                foreach(KeyData k in s.Keys)
                {
                    this.Configuration.Add(s.SectionName + ">" + k.KeyName, k.Value);
                }
            }
            return this.Configuration;
        }

        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            return this.Modules["mysqld"];
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        public MySQLServer(Dictionary<string, object> server_options) : base(server_options) {}
    }
}
