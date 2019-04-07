using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Sprache;
using Versatile;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public abstract class EnvironmentTests
    {
        #region Constructors
        public EnvironmentTests() {}
        #endregion

        #region Abstract Properties
        protected abstract AuditEnvironment Env {get; }

        protected abstract List<string> FilesToConstruct { get; }

        protected abstract List<string> FilesToTestExistence { get; }

        protected abstract Dictionary<string, string> FilesToRead { get; }
        #endregion

        #region Abstract Tests
    
        #endregion

        #region Properties
        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected List<PackageSource> Sources {get; } = new List<PackageSource>();

        protected static LocalEnvironment HostEnvironment = new LocalEnvironment(EnvironmentMessageHandler);
        #endregion

        #region Methods
        protected static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e) { }
        #endregion

        #region Tests
        [Fact]
        public void CanConstructFiles()
        {
            Assert.All(FilesToConstruct, f => Assert.NotNull(Env.ConstructFile(f)));
        }

        [Fact]
        public void CanTestFilesExist()
        {
            Assert.All(FilesToTestExistence, f => Assert.True(Env.ConstructFile(f).Exists));
        }


        [Fact]
        public void CanReadFiles()
        {
            Assert.All(FilesToRead, f => Assert.True(Env.ConstructFile(f.Key).ReadAsText().Contains(f.Value)));

        }

        [Fact]
        public virtual void CanGetPackages()
        {
            Assert.All(Sources, s => Assert.NotEmpty(s.GetPackages()));
        }

        [Fact]
        public virtual void CanGetVulnerabilities()
        {
            Assert.All(Sources, s => Assert.Equal(AuditTarget.AuditResult.SUCCESS, s.Audit(Cts.Token)));
        }
        #endregion
    }
}