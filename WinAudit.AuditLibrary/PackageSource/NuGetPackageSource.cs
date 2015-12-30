using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinAudit.AuditLibrary
{
    public class NuGetPackagesAudit : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.0");

        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return "NuGet"; } }

        //Get NuGet packages from reading packages.config
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            string packages_config_location = "";
            string file = string.IsNullOrEmpty(packages_config_location) ? AppDomain.CurrentDomain.BaseDirectory + @"\packages.config.example" : packages_config_location;
            if (!File.Exists(file))
                throw new ArgumentException("Invalid file location or file not found: " + file);
            try
            {
                XElement root = XElement.Load(file);
                IEnumerable<OSSIndexQueryObject> packages =
                    from el in root.Elements("package")
                    select new OSSIndexQueryObject("nuget", el.Attribute("id").Value, el.Attribute("version").Value, "");
                return packages;
            }
            catch (XmlException e)
            {
                throw new Exception("XML exception thrown parsing file: " + file, e);
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception thrown attempting to get packages from file: "
                    + file, e);
            }

        }
    }
}
