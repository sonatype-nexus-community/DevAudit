using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class AuditReporter
    {
        #region Abstract methods
        public abstract Task<bool> ReportPackageSourceAudit();

        protected abstract void PrintMessage(string format, params object[] args);
        protected abstract void PrintMessage(ConsoleColor color, string format, params object[] args);
        protected abstract void PrintMessageLine(string format);
        protected abstract void PrintMessageLine(string format, params object[] args);
        protected abstract void PrintMessageLine(ConsoleColor color, string format, params object[] args);
        #endregion

        #region Constructors
        public AuditReporter(PackageSource target)
        {
            AuditOptions = target.AuditOptions;
            AuditEnvironment = target.AuditEnvironment;
            Source = target as PackageSource;
            Application = target as Application;
            Server = target as ApplicationServer;
        }
        #endregion

        #region Properties
        protected Dictionary<string, object> AuditOptions { get; set; }
        protected AuditEnvironment AuditEnvironment { get; set; }
        protected PackageSource Source { get; set; }
        protected Application Application { get; set; }
        protected ApplicationServer Server { get; set; }
        #endregion

        #region Methods
        protected void BuildPackageSourceAuditReport()
        {
            int total_vulnerabilities = Source.Vulnerabilities.Sum(v => v.Value != null ? v.Value.Count(pv => pv.CurrentPackageVersionIsInRange) : 0);
            PrintMessageLine(ConsoleColor.White, "\nPackage Source Audit Results\n============================");
            PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilit{2} found in {1} package source audit.\n", total_vulnerabilities, Source.PackageManagerLabel, total_vulnerabilities == 0 || total_vulnerabilities > 1 ? "ies" : "y");
            int packages_count = Source.Vulnerabilities.Count;
            int packages_processed = 0;
            foreach (var pv in Source.Vulnerabilities.OrderByDescending(sv => sv.Value.Count(v => v.CurrentPackageVersionIsInRange)))
            {
                OSSIndexQueryObject package = pv.Key;
                List<OSSIndexApiv2Vulnerability> package_vulnerabilities = pv.Value;
                PrintMessage(ConsoleColor.White, "[{0}/{1}] {2}", ++packages_processed, packages_count, package.Name);
                if (package_vulnerabilities.Count() == 0)
                {
                    PrintMessage(" no known vulnerabilities.");
                }
                else if (package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange) == 0)
                {
                    PrintMessage(" {0} known vulnerabilit{1}, 0 affecting installed package version(s).", package_vulnerabilities.Count(), package_vulnerabilities.Count() > 1 ? "ies" : "y");
                }
                else
                {
                    PrintMessage(ConsoleColor.Red, " [VULNERABLE] ");
                    PrintMessage(" {0} known vulnerabilities, ", package_vulnerabilities.Count());
                    PrintMessageLine(ConsoleColor.Magenta, " {0} affecting installed package version(s): [{1}]", package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange), package_vulnerabilities.Where(v => v.CurrentPackageVersionIsInRange).Select(v => v.Package.Version).Distinct().Aggregate((s1, s2) => s1 + "," + s2));
                    var matched_vulnerabilities = package_vulnerabilities.Where(v => v.CurrentPackageVersionIsInRange).ToList();
                    int matched_vulnerabilities_count = matched_vulnerabilities.Count;
                    int c = 0;
                    matched_vulnerabilities.ForEach(v =>
                    {
                        PrintMessage(ConsoleColor.White, "--[{0}/{1}] ", ++c, matched_vulnerabilities_count);
                        PrintMessageLine(ConsoleColor.Red, "{0} ", v.Title.Trim());
                        PrintMessageLine(ConsoleColor.White, "  --Description: {0}", v.Description.Trim());
                        PrintMessage(ConsoleColor.White, "  --Affected versions: ");
                        PrintMessageLine(ConsoleColor.Red, "{0}", string.Join(", ", v.Versions.ToArray()));
                    });
                }
                PrintMessageLine("");
            }
        }
        #endregion
    }
}
