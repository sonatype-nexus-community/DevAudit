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
            if (!AuditOptions.ContainsKey("IQServerUrl") || !AuditOptions.ContainsKey("IQServerUser") || !AuditOptions.ContainsKey("IQServerPass"))
            {
                throw new ArgumentException("The IQServerUrl, IQServerUser, and IQServerPass audit options must all be present.");
            }
            IQServerUrl = (Uri)AuditOptions["IQServerUrl"];
            IQServerUser = (string)AuditOptions["IQServerUser"];
            IQServerPass = (string)AuditOptions["IQServerPass"];

            if (AuditOptions.ContainsKey("IQServerAppId"))
            {
                IQServerAppId = (string)AuditOptions["IQServerAppId"];
            }
            else
            {
                IQServerAppId = Path.GetDirectoryName(Directory.GetCurrentDirectory()).Split(Path.DirectorySeparatorChar).First();
                AuditEnvironment.Info("Using the current directory name {0} as the IQ Server app id.", IQServerAppId);
            }
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
