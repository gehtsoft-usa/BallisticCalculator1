using BallisticCalculator.Serialization;
using System.Xml;

namespace BallisticCalculator.Test
{
    public class SerializerRoundtrip
    {
        BallisticXmlSerializer serializer = new BallisticXmlSerializer();
        BallisticXmlDeserializer deserializer = new BallisticXmlDeserializer();

        internal XmlElement Serialize(object value) => serializer.Serialize(value);

        internal T Deserialize<T>(XmlElement element) where T : class => deserializer.Deserialize(element, typeof(T)) as T;
    }
}

