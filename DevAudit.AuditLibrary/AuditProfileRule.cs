using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;

namespace DevAudit.AuditLibrary
{
    public class AuditProfileRule
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "target")]
        public string Target { get; set; }

        [YamlMember(Alias = "category")]
        public string Category { get; set; }

        [YamlMember(Alias = "match_name")]
        public string MatchName { get; set; }

        [YamlMember(Alias = "match_version")]
        public string MatchVersion { get; set; }
    }
}
