using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSharpTest.Net.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace DevAudit.AuditLibrary
{
    public class BsonSerializer<T> : ISerializer<T>
    {
        JsonSerializer s = new JsonSerializer();
        
        public T ReadFrom(Stream stream)
        {
            using (BsonReader reader = new BsonReader(stream))
            {
                reader.CloseInput = false;
                return s.Deserialize<T>(reader);
            }
        }
        public void WriteTo(T value, Stream stream)
        {
            using (BsonWriter writer = new BsonWriter(stream))
            {
                writer.CloseOutput = false;
                s.Serialize(writer, value);
            }
        }
    }
}
