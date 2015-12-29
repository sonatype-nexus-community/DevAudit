using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinAudit.AuditLibrary;

namespace WinAudit.CommandLine
{
    interface IPackagesAudit
    {
        #region Public properties
        Audit PackagesAudit { get;  }

        string PackageManagerId { get; }

        string PackageManagerLabel { get; }

        Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask { get; }

        IEnumerable<OSSIndexQueryObject> Packages { get; set; }

        Task<IEnumerable<OSSIndexQueryResultObject>> GetProjectsTask { get;}

        IEnumerable<OSSIndexQueryResultObject> Projects { get; set; }
        #endregion

    }
}
