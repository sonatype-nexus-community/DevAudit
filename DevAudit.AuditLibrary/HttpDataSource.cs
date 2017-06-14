using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevAudit.AuditLibrary
{
    public abstract class HttpDataSource : IDataSource
    {
        #region Constructors
        public HttpDataSource(AuditTarget target, AuditEnvironment host_env, Dictionary<string, object> datasource_options)
        {
            this.DataSourceOptions = datasource_options;
            this.HostEnvironment = host_env;
            this.Target = target;
            if (this.DataSourceOptions.ContainsKey("HttpsProxy"))
            {
                HttpsProxy = (Uri)this.DataSourceOptions["HttpsProxy"];
            }
        }
        #endregion

        #region Abstract methods
        public abstract Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages);
        public abstract Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages);
        public abstract bool IsEligibleForTarget(AuditTarget target);
        #endregion

        #region Abstract properties
        public abstract int MaxConcurrentSearches { get; }
        #endregion

        #region Properties
        public AuditTarget Target { get; }
        public Dictionary<string, object> DataSourceOptions { get; protected set; }
        protected AuditEnvironment HostEnvironment { get; set; }
        public bool DataSourceInitialised { get; protected set; } = false;
        public Uri ApiUrl { get; protected set; }
        public Uri HttpsProxy { get; protected set; }
        #endregion

        #region Methods
        protected virtual HttpClient CreateHttpClient()
        {
            if (ApiUrl == null) throw new InvalidOperationException("The ApiUrl property is not initialised.");
            HttpClient client;
            if (this.HttpsProxy == null)
            {
                client = new HttpClient();
            }
            else
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(this.HttpsProxy, false),
                    UseProxy = true,
                };
                client = new HttpClient(handler);
            }
            client.BaseAddress = ApiUrl;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
            return client;
        }
        #endregion
    }
}
