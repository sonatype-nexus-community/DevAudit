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
        public MVC5Application(Dictionary<string, object> application_options, EventHandler<EnvironmentEventArgs> message_handler) : base(application_options, new Dictionary<string, string[]>()
            {
                { "AppConfig", new string[] { "@", "app.config" } },
            },
            new Dictionary<string, string[]>(), message_handler)
        { }
        #endregion
    }
}
