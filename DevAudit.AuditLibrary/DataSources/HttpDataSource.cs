using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;
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
    public abstract class HttpDataSource : IDataSource, IDisposable
    {
        #region Constructors
        public HttpDataSource(AuditTarget target, Dictionary<string, object> datasource_options)
        {
            this.DataSourceOptions = datasource_options;
            this.HostEnvironment = target.HostEnvironment;
            this.AuditEnvironment = target.AuditEnvironment;
            this.Target = target;
            if (this.Target.AuditOptions.ContainsKey("HttpsProxy"))
            {
                this.DataSourceOptions.Add("HttpsProxy", (Uri)this.Target.AuditOptions["HttpsProxy"]);
                HttpsProxy = (Uri) this.Target.AuditOptions["HttpsProxy"];
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
        protected AuditEnvironment AuditEnvironment { get; set; }
        public bool DataSourceInitialised { get; protected set; } = false;
        public Uri ApiUrl { get; protected set; }
        public Uri HttpsProxy { get; protected set; }
        public bool Initialised { get; protected set; }
        public DataSourceInfo Info { get; set; } = new DataSourceInfo();
        protected Version LibraryVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version;
        #endregion

        #region Methods
        protected virtual HttpClient CreateHttpClient()
        {
            if (ApiUrl == null) throw new InvalidOperationException("The ApiUrl property is not initialised.");
            WebRequestHandler handler = new WebRequestHandler();
            handler.ServerCertificateValidationCallback += RemoteCertificateValidationCallback;
            if (this.HttpsProxy != null)
            {
                handler.Proxy = new WebProxy(this.HttpsProxy, false);
                handler.UseProxy = true;
            }
            HttpClient client = new HttpClient(handler);
            client.BaseAddress = ApiUrl;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("user-agent", string.Format("DevAudit/{0}.{1} (+https://github.com/OSSIndex/DevAudit)", LibraryVersion.Major, LibraryVersion.Minor));
            return client;
        }

        #region Event handlers
        public bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //Return true if the server certificate is ok
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            //The server did not present a certificate
            else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                this.HostEnvironment.Error("The remote HTTPS server did not present a certificate.");
                return false;
            }
            else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                this.HostEnvironment.Error("The remote HTTPS server certificate name did not match the server name. Subject: {0} Issuer: {1}.",
                        certificate.Subject, certificate.Issuer);
                if (this.DataSourceOptions.ContainsKey("IgnoreHttpsCertErrors"))
                {
                    this.HostEnvironment.Warning("Ignoring remote HTTPS server certificate errors for subject {0} from issuer {1}. This is extremely insecure. You have been warned.",
                        certificate.Subject, certificate.Issuer);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //There is some other problem with the certificate
            else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                foreach (X509ChainStatus item in chain.ChainStatus)
                {
                    if (item.Status == X509ChainStatusFlags.RevocationStatusUnknown || item.Status == X509ChainStatusFlags.OfflineRevocation)
                    {
                        this.HostEnvironment.Error("Certificate status: {0}", item.StatusInformation);
                    }

                    else if (item.Status != X509ChainStatusFlags.NoError)
                    {
                        this.HostEnvironment.Error("Certificate status: {0}", item.StatusInformation);
                    }
                }
                if (this.DataSourceOptions.ContainsKey("IgnoreHttpsCertErrors"))
                {
                    this.HostEnvironment.Warning("Ignoring remote HTTPS server certificate errors for subject {0} from issuer {1}. This is extremely insecure. You have been warned.",
                        certificate.Subject, certificate.Issuer);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                this.HostEnvironment.Error("Unknown certificate error: {0}", sslPolicyErrors.ToString());
                return false;
            }

        }
        #endregion

        #endregion

        #region Disposer and Finalizer
        private bool IsDisposed { get; set; }
        /// <summary> 
        /// /// Implementation of Dispose according to .NET Framework Design Guidelines. 
        /// /// </summary> 
        /// /// <remarks>Do not make this method virtual. 
        /// /// A derived class should not be able to override this method. 
        /// /// </remarks>         
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object // from executing a second time. 
            // Always use SuppressFinalize() in case a subclass // of this type implements a finalizer. GC.SuppressFinalize(this); }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            // TODO If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource. 
            try
            {
                if (!this.IsDisposed)
                {
                    // Explicitly set root references to null to expressly tell the GarbageCollector 
                    // that the resources have been disposed of and its ok to release the memory 
                    // allocated for them. 
                    if (isDisposing)
                    {
                        // Release all managed resources here 
                        // Need to unregister/detach yourself from the events. Always make sure 
                        // the object is not null first before trying to unregister/detach them! 
                        // Failure to unregister can be a BIG source of memory leaks 
                        //if (someDisposableObjectWithAnEventHandler != null)
                        //{ someDisposableObjectWithAnEventHandler.SomeEvent -= someDelegate; 
                        //someDisposableObjectWithAnEventHandler.Dispose(); 
                        //someDisposableObjectWithAnEventHandler = null; } 
                        // If this is a WinForm/UI control, uncomment this code 
                        //if (components != null) //{ // components.Dispose(); //} } 

                    }
                    // Release all unmanaged resources here 
                    // (example) if (someComObject != null && Marshal.IsComObject(someComObject)) { Marshal.FinalReleaseComObject(someComObject); someComObject = null; 
                }
            }
            finally
            {
                this.IsDisposed = true;
            }
        }

        ~HttpDataSource()
        {
            Dispose(false);
        }
        #endregion
    }
}
