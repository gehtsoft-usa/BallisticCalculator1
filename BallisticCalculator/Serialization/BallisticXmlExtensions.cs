using System.IO;
using System.Xml;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// Extension classes to simplify serialization invocation
    /// </summary>
    public static class BallisticXmlExtensions
    {
        /// <summary>
        /// Deserialize a ballistic XML object of type `T` from the stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T BallisticXmlDeserialize<T>(this Stream stream)
            where T : class
            => BallisticXmlDeserializer.ReadFromStream<T>(stream);

        /// <summary>
        /// Serialize a ballistic XML object to the stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="stream"></param>
        public static void BallisticXmlSerialize<T>(this T value, Stream stream)
            where T : class
            => BallisticXmlSerializer.SerializeToStream(value, stream);

        /// <summary>
        /// Serialize a ballistic XML object to the file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="fileName"></param>
        public static void BallisticXmlSerialize<T>(this T value, string fileName)
            where T : class
            => BallisticXmlSerializer.SerializeToFile(value, fileName);
    }
}
