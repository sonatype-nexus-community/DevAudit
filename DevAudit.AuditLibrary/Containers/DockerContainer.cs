using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class DockerContainer : Container
    {
        #region Constructors
        public DockerContainer(Dictionary<string, object> container_options, EventHandler<EnvironmentEventArgs> message_handler) : base(container_options, message_handler)
        {
            
        }
        #endregion

        #region Properties

        #endregion
    }
}
