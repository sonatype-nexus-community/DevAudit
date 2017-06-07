using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

using Versatile;
using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class MariaDBServer : MySQLServer
    {
        #region Constructors
        public MariaDBServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(server_options, message_handler) {}
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "mariadb"; } }

        public override string ServerLabel { get { return "MariaDB"; } }
        #endregion
        
    }
}
