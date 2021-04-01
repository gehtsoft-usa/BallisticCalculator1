using Gehtsoft.Measurements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

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

                if ((!propertyAttribute.ChildElement || propertyAttribute.FlattenChild) && string.IsNullOrEmpty(propertyAttribute.Name))
                    throw new InvalidOperationException($"The property {value.GetType().FullName}.{property.Name} must have the Name property specified in {nameof(BXmlPropertyAttribute)} in order to save flatten children, collections or attributes");

                if ((propertyAttribute.ChildElement || propertyAttribute.Collection) && !string.IsNullOrEmpty(attributePrefix))
                    throw new InvalidOperationException($"The type {value.GetType().FullName}.{property.Name} is saved as a flatten value but contains child elements and/or collections");

                if (propertyAttribute.ChildElement)
                {
                    string forceName1 = null;

                    if (!string.IsNullOrEmpty(propertyAttribute.Name))
                        forceName1 = propertyAttribute.Name;

                    if (propertyAttribute.FlattenChild)
                        SerializeTo(propertyValue, element, propertyAttribute.Name);
                    else
                        element.AppendChild(Serialize(propertyValue, forceName1));
                }
                else if (propertyAttribute.Collection)
                {
                    var collectionElement = Document.CreateElement(propertyAttribute.Name);

                    foreach (object o in (propertyValue as IEnumerable))
                        collectionElement.AppendChild(Serialize(o));

                    element.AppendChild(collectionElement);
                }
                else
                {
                    AddAttribute(element, value, propertyValue, property, propertyAttribute, attributePrefix);
                }
            }
        }

        /// <summary>
        /// Serializes a primitive value and adds it as an attribute
        /// </summary>
        /// <param name="element"></param>
        /// <param name="targetObject"></param>
        /// <param name="propertyValue"></param>
        /// <param name="property"></param>
        /// <param name="propertyAttribute"></param>
        /// <param name="attributePrefix"></param>
        private void AddAttribute(XmlElement element, object targetObject, object propertyValue, PropertyInfo property, BXmlPropertyAttribute propertyAttribute, string attributePrefix)
        {
            var propertyType = property.PropertyType;
            var propertyType1 = Nullable.GetUnderlyingType(propertyType);
            if (propertyType1 != null && propertyType1 != propertyType)
                propertyType = propertyType1;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Measurement<>))
            {
                var textProperty = propertyType.GetProperty("Text");
                AddAttribute(element, propertyAttribute.Name, (string)textProperty.GetValue(propertyValue), attributePrefix);
            }
            else if (propertyType.IsEnum)
            {
                var textValue = propertyValue.ToString();
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(double))
            {
                var textValue = ((double)propertyValue).ToString(CultureInfo.InvariantCulture);
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(float))
            {
                var textValue = ((float)propertyValue).ToString(CultureInfo.InvariantCulture);
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(int))
            {
                var textValue = ((int)propertyValue).ToString(CultureInfo.InvariantCulture);
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(bool))
            {
                var textValue = (bool)propertyValue ? "true" : "false";
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(DateTime))
            {
                var textValue = ((DateTime)propertyValue).ToString("yyyy-MM-dd HH:mm:ss");
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(TimeSpan))
            {
                var textValue = ((TimeSpan)propertyValue).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else if (propertyType == typeof(string))
            {
                AddAttribute(element, propertyAttribute.Name, (string)propertyValue, attributePrefix);
            }
            else if (propertyType == typeof(BallisticCoefficient))
            {
                var textValue = ((BallisticCoefficient)propertyValue).ToString(CultureInfo.InvariantCulture);
                AddAttribute(element, propertyAttribute.Name, textValue, attributePrefix);
            }
            else
            {
                throw new InvalidOperationException($"The type {propertyType.FullName} of the property {targetObject.GetType().FullName}.{property.Name} is not supported to save as an attribute value");
            }
        }

        /// <summary>
        /// Adds an attribute
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="attributePrefix"></param>
        private void AddAttribute(XmlElement element, string name, string value, string attributePrefix)
        {
            if (!string.IsNullOrEmpty(attributePrefix))
                name = $"{attributePrefix}-{name}";

            var attribute = Document.CreateAttribute(name);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }
    }
}
