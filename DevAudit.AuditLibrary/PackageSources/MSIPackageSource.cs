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
    public class MSIPackageSource : PackageSource, IOperatingSystemPackageSource
    { 
        #region Overriden members
        public override string PackageManagerId { get { return "msi"; } }

        public override string PackageManagerLabel { get { return "MSI"; } }

        public override string DefaultPackageManagerConfigurationFile { get { return string.Empty; } }

        //Get list of installed programs from 3 registry locations.
        public override IEnumerable<Package> GetPackages(params string[] o)
        {
            var wmi = AuditEnvironment.Execute("wmic", "product get name,version", out var process_status, out var process_output, out var process_error);
            if (process_status != AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                throw new Exception("The wmic command did not execute successfully.");
            }
            var lines = process_output.Split(AuditEnvironment.LineTerminator.ToCharArray());
            var regex = new Regex(@"(\S.+)\s+\d(\S+)", RegexOptions.Compiled);
            foreach (var s in lines.Skip(1))
            {
                var m = regex.Match(s);
            }
            throw new NotImplementedException();
        }

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion

        #region Constuctors
        public MSIPackageSource(Dictionary<string, object> package_source_options, EventHandler<EnvironmentEventArgs> message_handler) : base(package_source_options, message_handler) { }
        #endregion
    }
}
