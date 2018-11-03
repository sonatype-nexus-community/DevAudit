using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    [Serializable]
    public class DataSourceInfo
    {
        #region Constructors
        public DataSourceInfo(string name, string url, string description, string license = null, string copyright = null)
        {
            this.Name = name;
            this.Url = url;
            this.Description = description;
            this.License = license;
            this.Copyright = copyright;
        }

        public DataSourceInfo() {}
        #endregion

        #region Properties
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string Copyright { get; set; }
        #endregion
    }
}
