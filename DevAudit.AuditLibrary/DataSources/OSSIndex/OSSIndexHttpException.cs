using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexHttpException : Exception
    {
        public string RequestParameter { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public HttpRequestMessage Request { get; set; }
        public OSSIndexHttpException(string request_parameter, HttpStatusCode status_code, string reason_phrase, HttpRequestMessage request) 
            : base("HTTP error code was received or did not receieve expected HTTP response.")
        {
            this.RequestParameter = request_parameter;
            this.StatusCode = status_code;
            this.ReasonPhrase = reason_phrase;
            this.Request = request;
        }

    }
}
