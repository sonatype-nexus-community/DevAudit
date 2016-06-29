using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Versatile;

namespace DevAudit.AuditLibrary
{
    public class NuGetPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "nuget"; } }

        public override string PackageManagerLabel { get { return "NuGet"; } }

        public NuGetPackageSource() : base() { }
        public NuGetPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options)
        {
            if (string.IsNullOrEmpty(this.PackageManagerConfigurationFile))
            {
                if 
                    (!File.Exists("packages.config")) throw new ArgumentException("Could not find the file " + "packages.config" + ".");
                else
                {
                    this.PackageManagerConfigurationFile = @"packages.config";
                }
            }      
        }

       

        //Get NuGet packages from reading packages.config
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            try
            {
                XElement root = XElement.Load(this.PackageManagerConfigurationFile);
                IEnumerable<OSSIndexQueryObject> packages =
                    from el in root.Elements("package")
                    select new OSSIndexQueryObject("nuget", el.Attribute("id").Value, el.Attribute("version").Value, "");
                return packages;
            }
            catch (XmlException e)
            {
                throw new Exception("XML exception thrown parsing file: " + this.PackageManagerConfigurationFile, e);
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception thrown attempting to get packages from file: "
                    + this.PackageManagerConfigurationFile, e);
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
    }
}
