using Gehtsoft.Measurements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BallisticCalculator.Serialization
{

    /// <summary>
    /// Serializer
    /// </summary>
    public class BallisticXmlSerializer
    {
        /// <summary>
        /// The property returns the document associated with the serializer
        /// </summary>
        public XmlDocument Document { get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BallisticXmlSerializer() : this(new XmlDocument())
        {
        }

        /// <summary>
        /// Constructor for an existing document
        /// </summary>
        /// <param name="document"></param>
        public BallisticXmlSerializer(XmlDocument document)
        {
            Document = document;
        }

        /// <summary>
        /// Serialize the value into an element
        /// </summary>
        /// <param name="value">The value to be serialized</param>
        /// <param name="forceName">The forced element name</param>
        /// <returns></returns>
        public XmlElement Serialize(object value, string forceName = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (forceName != null && string.IsNullOrWhiteSpace(forceName))
                throw new ArgumentException("The forced name of the attribute must not be an empty string if specified", nameof(forceName));

            var elementAttribute = value.GetType().GetCustomAttribute<BXmlElementAttribute>();

            if (elementAttribute == null)
                throw new ArgumentException($"Type {value.GetType().Name} must have {nameof(BXmlElementAttribute)}", nameof(value));

            if (string.IsNullOrEmpty(elementAttribute.Name))
                throw new ArgumentException($"The attribute {nameof(BXmlElementAttribute)} of type {value.GetType().Name} must have an non-empty Name value", nameof(value));

            XmlElement element = Document.CreateElement(forceName ?? elementAttribute.Name);
            if (forceName != null && forceName != elementAttribute.Name)
                AddAttribute(element, "data-type", elementAttribute.Name, null);

            SerializeTo(value, element, null);

            return element;
        }

        private void SerializeTo(object value, XmlElement element, string attributePrefix = null)
        {
            foreach (var property in value.GetType().GetProperties())
            {
                var propertyAttribute = property.GetCustomAttribute<BXmlPropertyAttribute>();

                if (propertyAttribute == null)
                    continue;

                object propertyValue = property.GetValue(value);

                if (propertyValue == null)
                {
                    if (propertyAttribute.Optional)
                        continue;
                    throw new InvalidOperationException($"The not optional property {value.GetType().FullName}.{property.Name} is null");
                }

                SerializeProperty(propertyValue, element, attributePrefix, property, propertyAttribute, propertyValue);
            }
        }

        private void SerializeProperty(object value, XmlElement element, string attributePrefix, PropertyInfo property, BXmlPropertyAttribute propertyAttribute, object propertyValue)
        {
            if ((!propertyAttribute.ChildElement || propertyAttribute.FlattenChild) && string.IsNullOrEmpty(propertyAttribute.Name))
                throw new InvalidOperationException($"The property {value.GetType().FullName}.{property.Name} must have the Name property specified in {nameof(BXmlPropertyAttribute)} in order to save flatten children, collections or attributes");

            if ((propertyAttribute.ChildElement || propertyAttribute.Collection) && !string.IsNullOrEmpty(attributePrefix))
                throw new InvalidOperationException($"The type {value.GetType().FullName}.{property.Name} is saved as a flatten value but contains child elements and/or collections");

            if (propertyAttribute.ChildElement)
                SerializeProperty_Child(element, propertyAttribute, propertyValue);
            else if (propertyAttribute.Collection)
                SerializeProperty_Collection(element, propertyAttribute, propertyValue);
            else
                AddAttribute(element, value, propertyValue, property, propertyAttribute, attributePrefix);
        }

        private void SerializeProperty_Child(XmlElement element, BXmlPropertyAttribute propertyAttribute, object propertyValue)
        {
            string forceName1 = null;

            if (!string.IsNullOrEmpty(propertyAttribute.Name))
                forceName1 = propertyAttribute.Name;

            if (propertyAttribute.FlattenChild)
                SerializeTo(propertyValue, element, propertyAttribute.Name);
            else
                element.AppendChild(Serialize(propertyValue, forceName1));
        }

        private void SerializeProperty_Collection(XmlElement element, BXmlPropertyAttribute propertyAttribute, object propertyValue)
        {
            var collectionElement = Document.CreateElement(propertyAttribute.Name);

            foreach (object o in (propertyValue as IEnumerable))
                collectionElement.AppendChild(Serialize(o));

            element.AppendChild(collectionElement);
        }

        class AddAttributeAction
        {
            internal Func<Type, bool> Probe { get; }

            internal Func<Type, object, string> Value { get; }

            internal AddAttributeAction(Func<Type, bool> probe, Func<Type, object, string> value)
            {
                Probe = probe;
                Value = value;
            }
        }

        private static readonly AddAttributeAction[] gAddAttributeActions = new AddAttributeAction[]
        {
            new AddAttributeAction(
                (type) => SerializerTools.IsTypeMeasurement(type),
                (type, value) => type.GetProperty("Text").GetValue(value).ToString()
            ),

            new AddAttributeAction(
                (type) => type.IsEnum,
                (type, value) => value.ToString()
            ),

            new AddAttributeAction(
                (type) => type == typeof(double),
                (type, value) => ((double)value).ToString(CultureInfo.InvariantCulture)
            ),

            new AddAttributeAction(
                (type) => type == typeof(float),
                (type, value) => ((float)value).ToString(CultureInfo.InvariantCulture)
            ),

            new AddAttributeAction(
                (type) => type == typeof(int),
                (type, value) => ((int)value).ToString(CultureInfo.InvariantCulture)
            ),

            new AddAttributeAction(
                (type) => type == typeof(bool),
                (type, value) => ((bool)value) ? "true" : "false"
            ),

            new AddAttributeAction(
                (type) => type == typeof(DateTime),
                (type, value) => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss")
            ),

            new AddAttributeAction(
                (type) => type == typeof(TimeSpan),
                (type, value) => ((TimeSpan)value).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
            ),

            new AddAttributeAction(
                (type) => type == typeof(string),
                (type, value) => (string)value
            ),
            
            new AddAttributeAction(
                (type) => type == typeof(BallisticCoefficient),
                (type, value) => ((BallisticCoefficient)value).ToString(CultureInfo.InvariantCulture)
            ),          
        };

        /// <summary>
        /// Serializes a primitive value and adds it as an attribute
        /// </summary>
        /// <param name="element"></param>
        /// <param name="targetObject"></param>
        /// <param name="value"></param>
        /// <param name="property"></param>
        /// <param name="propertyAttribute"></param>
        /// <param name="attributePrefix"></param>
        private void AddAttribute(XmlElement element, object targetObject, object value, PropertyInfo property, BXmlPropertyAttribute propertyAttribute, string attributePrefix)
        {
            var type = SerializerTools.RemoveNullabilityFromType(property.PropertyType);

            for (int i = 0; i < gAddAttributeActions.Length; i++)
                if (gAddAttributeActions[i].Probe(type))
                {
                    AddAttribute(element, propertyAttribute.Name,
                        gAddAttributeActions[i].Value(type, value),
                        attributePrefix);
                    return;
                }

            throw new InvalidOperationException($"The type {type.FullName} of the property {targetObject.GetType().FullName}.{property.Name} is not supported to save as an attribute value");
        }

        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="attributePrefix"></param>
        internal static void AddAttribute(XmlElement element, string name, string value, string attributePrefix)
        {
            if (!string.IsNullOrEmpty(attributePrefix))
                name = $"{attributePrefix}-{name}";

            var attribute = element.OwnerDocument.CreateAttribute(name);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }

        /// <summary>
        /// Serializes the object to a stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="stream"></param>
        public static void SerializeToStream<T>(T value, Stream stream)
        {
            var xmlSeralizer = new BallisticXmlSerializer();
            xmlSeralizer.Document.AppendChild(xmlSeralizer.Serialize(value));
            xmlSeralizer.Document.Save(stream);
        }

        /// <summary>
        /// Serializes the object to the specified file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="fileName"></param>
        public static void SerializeToFile<T>(T value, string fileName)
        {
            using var file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            SerializeToStream<T>(value, file);
        }
    }
}
