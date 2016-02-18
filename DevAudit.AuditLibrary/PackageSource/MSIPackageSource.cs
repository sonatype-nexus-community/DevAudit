using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;


namespace DevAudit.AuditLibrary
{
    public class MSIPackageSource : PackageSource
    {
        public override OSSIndexHttpClient HttpClient { get; } = new OSSIndexHttpClient("1.1");

        public override string PackageManagerId { get { return "msi"; } }

        public override string PackageManagerLabel { get { return "MSI"; } }

        //Get list of installed programs from 3 registry locations.
        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            RegistryKey k = null;
            try
            {
                RegistryPermission perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                IEnumerable<OSSIndexQueryObject> packages_query =
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string)k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string)k.OpenSubKey(sn).GetValue("DisplayVersion"),
                        (string)k.OpenSubKey(sn).GetValue("Publisher"));
                List<OSSIndexQueryObject> packages = packages_query
                    .Where(p => !string.IsNullOrEmpty(p.Name))
                    .ToList<OSSIndexQueryObject>();

                perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                packages_query =
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string)k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string)k.OpenSubKey(sn).GetValue("DisplayVersion"), (string)k.OpenSubKey(sn).GetValue("Publisher"));
                packages.AddRange(packages_query.Where(p => !string.IsNullOrEmpty(p.Name)));

                perm = new RegistryPermission(RegistryPermissionAccess.Read, @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                perm.Demand();
                k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                packages_query =
                    from sn in k.GetSubKeyNames()
                    select new OSSIndexQueryObject("msi", (string)k.OpenSubKey(sn).GetValue("DisplayName"),
                        (string)k.OpenSubKey(sn).GetValue("DisplayVersion"), (string)k.OpenSubKey(sn).GetValue("Publisher"));
                packages.AddRange(packages_query.Where(p => !string.IsNullOrEmpty(p.Name)));
                return packages.GroupBy(p => new { p.Name, p.Version, p.Vendor }).SelectMany(p => p).ToList();
            }

            catch (SecurityException se)
            {
                throw new Exception("Security exception thrown reading registry key: " + (k == null ? "" : k.Name + "\n" + se.Message), se);
            }
            catch (Exception e)
            {
                throw new Exception("Exception thrown reading registry key: " + (k == null ? "" : k.Name), e);
            }

            finally
            {
                k = null;
            }
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }

        public MSIPackageSource() : base() { }
        public MSIPackageSource(Dictionary<string, object> package_source_options) : base(package_source_options) { }

        public override Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> ArtifactsTransform { get; } = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
               if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    OSSIndexQueryObject package = new OSSIndexQueryObject(a.PackageManager, a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

    }
}
