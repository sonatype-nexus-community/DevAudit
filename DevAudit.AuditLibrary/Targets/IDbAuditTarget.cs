using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.XPath;
namespace DevAudit.AuditLibrary
{
    public interface IDbAuditTarget
    {
        XPathNodeIterator ExecuteDbQueryToXml(object[] args);
        bool DetectServerDataDirectory();
        AuditDirectoryInfo ServerDataDirectory { get; }
    }
}
