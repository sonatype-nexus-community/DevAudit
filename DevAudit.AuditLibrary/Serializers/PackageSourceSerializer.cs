using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary.Serializers
{
    public class PackageSourceSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PackageSource val = (PackageSource)value;

            writer.WriteStartObject();
            writer.WritePropertyName("Packages Audited");
            serializer.Serialize(writer, val.Vulnerabilities.Values.Count());
            writer.WritePropertyName("Vulnerabilities Found");
            int total_vulnerabilities = val.Vulnerabilities.Sum(v => v.Value != null ? v.Value.Count(pv => pv.PackageVersionIsInRange) : 0);
            serializer.Serialize(writer, total_vulnerabilities);
            writer.WritePropertyName("Packages");
            writer.WriteStartArray();
            foreach (var vul in val.Vulnerabilities)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Package");
                serializer.Serialize(writer, vul.Key);
                writer.WritePropertyName("Vulnerabilities");
                serializer.Serialize(writer, vul.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(PackageSource).IsAssignableFrom(objectType);
        }
    }
}
