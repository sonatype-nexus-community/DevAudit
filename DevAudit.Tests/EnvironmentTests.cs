using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public abstract class EnvironmentTests
    {
        #region Properties
        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected IDeveloperPackageSource DPS { get; }
        #endregion

    }
}
