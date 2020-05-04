using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Octokit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using DevAudit.AuditLibrary.Reporters.Models;

namespace DevAudit.AuditLibrary
{
    public class IQServerReporter : AuditReporter
    {
        #region Constructors
        public IQServerReporter(PackageSource source) : base(source) 
        {
            if (!AuditOptions.ContainsKey("IQServerUrl") || !AuditOptions.ContainsKey("IQServerUser") || !AuditOptions.ContainsKey("IQServerPass") || !AuditOptions.ContainsKey("IQServerAppId"))
            {
                throw new ArgumentException("The IQServerUrl, IQServerUser, IQServerPass, IQServerAppId audit options must all be present.");
            }
            IQServerUrl = (Uri)AuditOptions["IQServerUrl"];
            IQServerUser = (string)AuditOptions["IQServerUser"];
            IQServerPass = (string)AuditOptions["IQServerPass"];
            IQServerAppId = (string)AuditOptions["IQServerAppId"];
            HttpClient = CreateHttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{IQServerUser}:{IQServerPass}");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(byteArray));
            AuditEnvironment.Info("Authenticating with IQ Server user {0}.", IQServerUser);
        }
        #endregion

        #region Properties
        protected Uri IQServerUrl { get; set; }

        protected string IQServerAppId { get; set; }

        protected string IQServerInternalAppId { get; set; }

        protected string IQServerUser { get; set; }

        protected string IQServerPass { get; set; }

        protected Uri HttpsProxy { get; set; }

        protected HttpClient HttpClient { get; }

        protected StringBuilder SBom { get; } = new StringBuilder();
        #endregion

        #region Methods
        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    
        protected virtual HttpClient CreateHttpClient()
        {
            WebRequestHandler handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = IQServerUrl;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("user-agent", string.Format("DevAudit/3.5 (+https://github.com/OSSIndex/DevAudit)"));
            return client;
        }
        #endregion

        #region Overriden methods
        public override async Task<bool> ReportPackageSourceAudit()
        {
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var r = await HttpClient.GetStringAsync("/api/v2/applications?publicId=" + IQServerAppId);
            var apps = JsonConvert.DeserializeObject<IQServerApplications>(r);
            if (apps.Applications.Length == 0)
            {
                AuditEnvironment.Error("The app id {0} does not exist on the IQ Server at {1}.", IQServerAppId, IQServerUrl.ToString());
                return false;
            }
            IQServerInternalAppId = apps.Applications.First().Id;
            SBom.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<bom xmlns=\"http://cyclonedx.org/schema/bom/1.1\" version=\"1\" serialNumber=\"urn:uuid:{0}\"\nxmlns:v=\"http://cyclonedx.org/schema/ext/vulnerability/1.0\">\n\t<components>\n",
                Guid.NewGuid().ToString("D"));
            int packages_count = Source.Vulnerabilities.Count;
            int packages_processed = 0;
            int c = 0;
            foreach (var pv in Source.Vulnerabilities.OrderByDescending(sv => sv.Value.Count(v => v.PackageVersionIsInRange)))
            {
                IPackage package = pv.Key;
                List<IVulnerability> package_vulnerabilities = pv.Value;
                var purl = string.IsNullOrEmpty(package.Group) ? string.Format("pkg:nuget/{0}@{1}", package.Name, package.Version) : string.Format("pkg:nuget/{0}/{1}@{2}", package.Group, package.Name, package.Version); 
                SBom.AppendLine("\t\t<component type =\"library\">");
                if (!string.IsNullOrEmpty(package.Group))
                {
                    SBom.AppendFormat("\t\t\t<group>{0}</group>\n", package.Group);
                }
                SBom.AppendFormat("\t\t\t<name>{0}</name>\n", package.Name);
                SBom.AppendFormat("\t\t\t<version>{0}</version>\n", package.Version);
                SBom.AppendFormat("\t\t\t<purl>{0}</purl>\n", purl);
                if ((package_vulnerabilities.Count() != 0) && (package_vulnerabilities.Count(v => v.PackageVersionIsInRange) > 0))
                {
                    SBom.AppendLine("\t\t\t<v:vulnerabilities>");
                    var matched_vulnerabilities = package_vulnerabilities.Where(v => v.PackageVersionIsInRange).ToList();
                    int matched_vulnerabilities_count = matched_vulnerabilities.Count;
                    
                    matched_vulnerabilities.ForEach(v =>
                    {
                        SBom.AppendFormat("\t\t\t\t<v:vulnerability ref=\"{0}\">\n", purl);
                        SBom.AppendFormat("\t\t\t\t\t<v:id>{0}</v:id>\n", v.CVE != null && v.CVE.Length > 0 ? v.CVE.First() : v.Id);
                        SBom.AppendLine("\t\t\t\t</v:vulnerability>");
                        c++;
                    });
                    SBom.AppendLine("\t\t\t</v:vulnerabilities>");
                }
                SBom.AppendLine("\t\t</component>");
                packages_processed++;
            }
            SBom.AppendLine("\t</components>\n</bom>");
            AuditEnvironment.Debug("SBOM:\n{0}", SBom.ToString());
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            //HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            var req = string.Format("/api/v2/scan/applications/{0}/sources/devaudit", IQServerInternalAppId);
            var content = new StringContent(SBom.ToString(), Encoding.UTF8, "application/xml");
            var response = await HttpClient.PostAsync(req, content);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            string json = await response.Content.ReadAsStringAsync();
            AuditEnvironment.Debug("JSON response from IQ Server at {0}: {1}", IQServerUrl.ToString(), json);
            var status = JsonConvert.DeserializeObject<IQServerStatus>(json);
            AuditEnvironment.Info("IQ Server report complete. {0} components with {1} vulnerabilities submitted. Scan status is at: {2}{3}", packages_count, c, IQServerUrl.ToString(), status.statusUrl);
            return true;
        }

        protected override void PrintMessage(ConsoleColor color, string format, params object[] args)
        {
            
        }

        protected override void PrintMessage(string format, params object[] args)
        {
            
        }

        protected override void PrintMessageLine(ConsoleColor color, string format, params object[] args)
        {
            
        }

        protected override void PrintMessageLine(string format, params object[] args)
        {
           
        }

        protected override void PrintMessageLine(string format)
        {
           
        }
        #endregion

    }
}
