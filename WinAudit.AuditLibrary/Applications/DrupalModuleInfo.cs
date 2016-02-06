using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinAudit.AuditLibrary
{
    public class DrupalModuleInfo
    {
        [YamlIgnore]
        public string ShortName { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "configure")]
        public string Configure { get; set; }

        [YamlMember(Alias = "hidden")]
        public bool Hidden { get; set; }

        [YamlMember(Alias = "required")]
        public bool Required { get; set; }

        [YamlMember(Alias = "package")]
        public string Package { get; set; }

        [YamlMember(Alias = "version")]
        public string Version { get; set; }

        [YamlMember(Alias = "core")]
        public string Core { get; set; }

        [YamlMember(Alias = "dependencies")]
        public List<string> Dependencies { get; set; }

        [YamlMember(Alias = "project")]
        public string Project { get; set; }

        [YamlMember(Alias = "datestamp")]
        public long DateStamp { get; set; }

    }
}
