using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;
namespace DevAudit.Tests
{
    public abstract class HttpClientTests
    {
        protected abstract OSSIndexHttpClient http_client { get; }

        [Fact]
        public abstract Task CanSearch();

        [Fact]
        public abstract Task CanGetProject();

        [Fact]
        public abstract Task CanGetVulnerabilityForId();
    }
}
