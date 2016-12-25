using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class MVC5Application : NetFx4Application
    {
        #region Constructors
        public MVC5Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) 
            : base(application_options, message_handler)
        { }
        #endregion
    }
}
