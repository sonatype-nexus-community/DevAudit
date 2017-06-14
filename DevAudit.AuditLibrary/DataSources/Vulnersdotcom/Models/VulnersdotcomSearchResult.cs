using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{

    public class VulnersdotcomSearchResult
    {
        public string result { get; set; }
        public VulnersdotcomData data { get; set; }
    }

    public class VulnersdotcomData
    {
        public VulnersdotcomSearch[] search { get; set; }
        public object exactMatch { get; set; }
        public object references { get; set; }
        public int total { get; set; }
    }

    public class VulnersdotcomSearch
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public object _score { get; set; }
        public Vulnersdotcom_Source _source { get; set; }
        public VulnersdotcomHighlight highlight { get; set; }
        public long[] sort { get; set; }
        public string flatDescription { get; set; }
    }

    public class Vulnersdotcom_Source
    {
        public DateTime lastseen { get; set; }
        public object[] references { get; set; }
        public string description { get; set; }
        public int edition { get; set; }
        public string reporter { get; set; }
        public DateTime published { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string objectVersion { get; set; }
        public string bulletinFamily { get; set; }
        public VulnersdotcomAffectedsoftware[] affectedSoftware { get; set; }
        public string[] cvelist { get; set; }
        public DateTime modified { get; set; }
        public string id { get; set; }
        public string href { get; set; }
        public VulnersdotcomCvss cvss { get; set; }
        public string hash { get; set; }
        public string vhref { get; set; }
    }

    public class VulnersdotcomCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }

    public class VulnersdotcomAffectedsoftware
    {
        public string name { get; set; }
        public string version { get; set; }
        public string _operator { get; set; }
    }

    public class VulnersdotcomHighlight
    {
        public string[] historybulletinreporter { get; set; }
        public string[] bulletinFamily { get; set; }
        public string[] historybulletintype { get; set; }
        public string[] historybulletinbulletinFamily { get; set; }
        public string[] reporter { get; set; }
        public string[] affectedSoftwarename { get; set; }
        public string[] historybulletinaffectedSoftwarename { get; set; }
        public string[] type { get; set; }
    }

}
