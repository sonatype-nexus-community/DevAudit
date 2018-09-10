using System.Collections.Generic;

namespace DevAudit.AuditLibrary
{
    internal class OSSIndexApiv3Query
    {
        public List<string> coordinates { get; set; }

        public OSSIndexApiv3Query()
        {
            coordinates = new List<string>();
        }

        public List<string> getCoordinates()
        {
            return coordinates;
        }

        public void addCoordinate(string coord)
        {
            coordinates.Add(coord);
        }
    }
}