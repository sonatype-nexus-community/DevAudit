using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using DevAudit.AuditLibrary;
using DevAudit.AuditLibrary.Serializers.JUnit;

namespace DevAudit.AuditLibrary.Serializers
{
    public class JUnitXmlSerializer
    {
        StringBuilder VulnText = new StringBuilder();

        void PrintAuditResultMultiLineField(int indent, string field, string value)
        {
            StringBuilder sb = VulnText;
            sb.Append(' ', indent);
            sb.Append("--");
            sb.Append(field);
            sb.AppendLine(":");
            string[] lines = value.Split(Environment.NewLine.ToCharArray());
            string last = null;
            foreach (string l in lines)
            {
                string line = l.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                sb.Append(' ', indent * 2);
                if (string.IsNullOrEmpty(last))
                {
                    sb.Append("--");
                }
                else
                {
                    sb.Append("  ");
                }
                sb.AppendLine(line);
                last = line;
            }
        }

        public JUnitXmlSerializer(PackageSource source, string time, string outputFile)
        {

            var testSuites = new testsuites();
            var testSuite = new testsuite();
            testSuite.name = string.Format("DevAudit {0} package source audit.", source.PackageManagerLabel);
            testSuite.package = "org.ossindex.devaudit";
            testSuite.time = time;
            testSuite.tests = source.Vulnerabilities.Count.ToString();
            testSuite.failures = source.Vulnerabilities.Where(v => v.Value.Count > 0).Count().ToString();
            testSuite.testcase = new testcase[source.Vulnerabilities.Count];
            int tcount = 0;
            foreach (var pv in source.Vulnerabilities.OrderByDescending(sv => sv.Value.Count(v => v.PackageVersionIsInRange)))
            {
                IPackage package = pv.Key;
                List<IVulnerability> package_vulnerabilities = pv.Value;
                var tc = new testcase();
                tc.name = package.Name;
                tc.classname = package.Name;
                tc.assertions = "1";
                if (package_vulnerabilities.Count() == 0)
                {
                    continue;
                }
                else if (package_vulnerabilities.Count(v => v.PackageVersionIsInRange) == 0)
                {
                    continue;
                }
                else
                {
                    var matched_vulnerabilities = package_vulnerabilities.Where(v => v.PackageVersionIsInRange).ToList();
                    int matched_vulnerabilities_count = matched_vulnerabilities.Count;
                    tc.failure = new failure[matched_vulnerabilities_count];
                    int c = 0;
                    foreach (var v in matched_vulnerabilities)
                    { 
                        failure f = new failure();
                        f.message = "Package vulnerable";
                        VulnText.AppendFormat("--[{0}/{1}] ", (c + 1), matched_vulnerabilities_count);
                        VulnText.AppendFormat("{0} ", v.Title.Trim());
                        PrintAuditResultMultiLineField(2, "Description", v.Description.Trim().Replace("\n", "").Replace(". ", "." + Environment.NewLine));
                        VulnText.AppendFormat("{0}", string.Join(", ", v.Versions.ToArray()));
                        if (v.CVE != null && v.CVE.Count() > 0)
                        {
                            VulnText.AppendFormat("  --CVE(s): {0}", string.Join(", ", v.CVE.ToArray()));
                        }
                        if (!string.IsNullOrEmpty(v.Reporter))
                        {
                            VulnText.AppendFormat("  --Reporter: {0} ", v.Reporter.Trim());
                        }
                        if (!string.IsNullOrEmpty(v.CVSS.Score))
                        {
                            VulnText.AppendFormat("  --CVSS Score: {0}. Vector: {1}", v.CVSS.Score, v.CVSS.Vector);
                        }
                        if (v.Published != DateTime.MinValue)
                        {
                            VulnText.AppendFormat("  --Date published: {0}", v.Published.ToShortDateString());
                        }
                        if (!string.IsNullOrEmpty(v.Id))
                        {
                            VulnText.AppendFormat("  --Id: {0}", v.Id);
                        }
                        if (v.References != null && v.References.Count() > 0)
                        {
                            if (v.References.Count() == 1)
                            {
                                VulnText.AppendFormat("  --Reference: {0}", v.References[0]);
                            }
                            else
                            {
                                VulnText.AppendFormat("  --References:");
                                for (int i = 0; i < v.References.Count(); i++)
                                {
                                    VulnText.AppendFormat("    - {0}", v.References[i]);
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(v.DataSource.Name))
                        {
                            VulnText.AppendFormat("  --Provided by: {0}", v.DataSource.Name);
                        }
                        f.Text = VulnText.ToString().Split(Environment.NewLine.ToCharArray());
                        tc.failure[c++] = f;
                        VulnText.Clear();
                    }
                }
                testSuite.testcase[tcount++] = tc;
            }
            testSuites.testsuite = new testsuite[] { testSuite };
            
            XmlSerializer xs = new XmlSerializer(typeof(testsuites));
            xs.Serialize(File.CreateText(outputFile), testSuites);
        }
    }
}
