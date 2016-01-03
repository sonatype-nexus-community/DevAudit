using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Semver;

using Microsoft.Win32;


namespace WinAudit.AuditLibrary
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
                return packages;
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

        public override Func<string, string, bool> PackageVersionInRange { get; } = (range, compare_to_range) =>
        {
            /*
            Regex parse_ex = new Regex(@"^(?<range>~+|<+=?|>+=?)?" +
                @"(?<ver>(\d+)" +
                @"(\.(\d+))?" +
                @"(\.(\d+))?" +
                @"(\-([0-9A-Za-z\-\.]+))?" +
                @"(\+([0-9A-Za-z\-\.]+))?)$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            Match m = parse_ex.Match(range);
            if (!m.Success)
            {
                throw new ArgumentException("Could not parse package range: " + range + ".");
            }
            string range_version_str = "";
            string op = "";
            string[] legal_ops = { "", "~", "<", "<=", ">=", ">" };
            SemVersion range_version = null;
            SemVersion compare_to_range_version = null;
            op = m.Groups[1].Value;
            if (!legal_ops.Contains(op))
                throw new ArgumentException("Could not parse package version range operator: " + op + ".");
            range_version_str = m.Groups[2].Value;
            if (!SemVersion.TryParse(m.Groups[2].Value, out range_version))
            {
                throw new ArgumentException("Could not parse package semantic version: " + m.Groups[2].Value + ".");
            }
            if (!SemVersion.TryParse(compare_to_range, out compare_to_range_version))
            {
                throw new ArgumentException("Could not parse comparing semantic version: " + compare_to_range + ".");
            }
            switch (op)
            {
                case "":
                    return compare_to_range_version == range_version;
                case "<":
                    return compare_to_range_version < range_version;
                case "<=":
                    return compare_to_range_version < range_version;
                case ">":
                    return compare_to_range_version > range_version;
                case ">=":
                    return compare_to_range_version >= range_version;
                //case "~":
                //if (range_version.Major != compare_to_range_version.Major) return false;
                //major matches
                //else if (range_version.Minor == 0 && compare_to_range_version > 0) return true;

                //else if (range_version.Minor == 0 && range_version.Patch == 0 && compare_to_range_version.Patch > 0) return true;
                //else if (range_version.Patch == )
                default:
                    throw new Exception("Unimplemented range operator: " + op + ".");
            }*/
            return (range == compare_to_range);
        };

    }
}
