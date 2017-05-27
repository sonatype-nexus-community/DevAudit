using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        #region Abstract methods
        public abstract Task<IArtifact> SearchArtifacts(List<IPackage> packages);
        public abstract Task<List<IVulnerability>> SearchVulnerabilities(List<IPackage> packages);
        #endregion

        #region Constructors
        public HttpDataSource(AuditEnvironment host_env, Dictionary<string, object> datasource_options)
        {
            if (!datasource_options.ContainsKey("ApiUrl")) throw new ArgumentException("The ApiUrl option is not specified.");
            this.DataSourceOptions = datasource_options;
            this.HostEnvironment = host_env;
            string api_url_s = (string)this.DataSourceOptions["ApiUrl"];
            Uri api_url = null;
            if (!Uri.TryCreate(api_url_s, UriKind.Absolute, out api_url))
            {
                this.HostEnvironment.Error("Could not create Uri from {0}.", api_url_s);
                this.DataSourceInitialised = false;
                return;
            }
            else
            {
                this.ApiUrl = api_url;
            }
            if (this.DataSourceOptions.ContainsKey("HttpsProxy"))
            {
                HttpsProxy = (Uri)this.DataSourceOptions["HttpsProxy"];
            }
        }
        #endregion

        #region Properties
        public Dictionary<string, object> DataSourceOptions { get; protected set; }
        protected AuditEnvironment HostEnvironment { get; set; }
        public bool DataSourceInitialised { get; protected set; } = false;
        public Uri ApiUrl { get; protected set; }
        public Uri HttpsProxy { get; protected set; }
        public ConcurrentDictionary<IPackage, Exception> SearchArtifactsExceptions = new ConcurrentDictionary<IPackage, Exception>();
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
