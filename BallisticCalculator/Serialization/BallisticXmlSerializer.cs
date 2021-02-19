using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// Serializer/de-seralizer of the ballistic calculator data to XML
    /// </summary>
    public class BallisticXmlSerializer
    {
        private readonly XmlDocument mDocument;

        /// <summary>
        /// The document
        /// </summary>
        public XmlDocument Document { get; }

        /// <summary>
        /// Constructor
        /// 
        /// Default constructor creates a new document
        /// </summary>
        public BallisticXmlSerializer() : this(new XmlDocument())
        {

        }

        /// <summary>
        /// Serializes the value
        /// 
        /// The type of the value must be attributed with <see cref="BXmlElementAttribute"/>
        /// </summary>
        /// <param name="value">The value to be serialized</param>
        /// <returns></returns>
        public XmlElement Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var elementAttribute = value.GetType().GetCustomAttribute<BXmlElementAttribute>();
            if (elementAttribute == null)
                throw new ArgumentException($"The value type {value.GetType()} is not attributed using {nameof(BXmlElementAttribute)}", nameof(value));

            if (string.IsNullOrWhiteSpace(elementAttribute.Name))
                throw new ArgumentException($"The element name associated with the type {value.GetType().FullName} is an empty string", nameof(value));

            var element = CreateElement(elementAttribute.Name);

            var properties = value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                var propertyAttribute = property.GetCustomAttribute<BXmlPropertyAttribute>();
                
                if (propertyAttribute == null)
                    continue;
                
                if (!propertyAttribute.ChildElement && string.IsNullOrWhiteSpace(propertyAttribute.Name))
                    throw new ArgumentException($"The attribute name associated with the property {property.Name} of type {value.GetType().FullName} is an empty string", nameof(value));

                var propertyValue = property.GetValue(value);

                if (propertyValue != null)
                {
                    if (propertyAttribute.ChildElement)
                    {
                        element.AppendChild(Serialize(propertyValue));
                    }
                    else
                    {
                        var propertyType = property.PropertyType;
                        var propertyType1 = Nullable.GetUnderlyingType(propertyType);
                        if (propertyType1 != null && propertyType1 != propertyType)
                            propertyType = propertyType1;

                        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Measurement<>))
                        {
                            var textProperty = propertyType.GetProperty("Text");
                            AddAttribute(element, propertyAttribute.Name, (string)textProperty.GetValue(propertyValue));
                        }
                        else if (propertyType.IsEnum)
                        {
                            var textValue = propertyValue.ToString();
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(double))
                        {
                            var textValue = ((double)propertyValue).ToString(CultureInfo.InvariantCulture);
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(int))
                        {
                            var textValue = ((int)propertyValue).ToString(CultureInfo.InvariantCulture);
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(bool))
                        {
                            var textValue = (bool)propertyValue ? "true" : "false";
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(DateTime))
                        {
                            var textValue = ((DateTime)propertyValue).ToString("yyyy-MM-dd HH:mm:ss");
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(TimeSpan))
                        {
                            var textValue = ((TimeSpan)propertyValue).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else if (propertyType == typeof(string))
                        {
                            AddAttribute(element, propertyAttribute.Name, (string)propertyValue);
                        }
                        else if (propertyType == typeof(BallisticCoefficient))
                        {
                            var textValue = ((BallisticCoefficient)propertyValue).ToString(CultureInfo.InvariantCulture);
                            AddAttribute(element, propertyAttribute.Name, textValue);
                        }
                        else
                        {
                            throw new InvalidOperationException($"The type {propertyType.FullName} of the property {value.GetType().FullName}.{property.Name} is not supported");
                        }
                    }
                }
            }

            return element;
        }

        /// <summary>
        /// Deserialize element of the specified type
        /// </summary>
        /// <param name="type">The expected type of the object</param>
        /// <param name="element">The element name</param>
        /// <param name="forceIfNameDoesNotMatch">Force deserialization even if the name does not match</param>
        /// <returns></returns>
        public object Deserialize(Type type, XmlElement element, bool forceIfNameDoesNotMatch = false)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var elementAttribute = type.GetCustomAttribute<BXmlElementAttribute>();
            
            if (elementAttribute == null)
                throw new ArgumentException($"The type {type.FullName} is not attributed with {nameof(BXmlElementAttribute)}");

            if (!forceIfNameDoesNotMatch && elementAttribute.Name != element.Name)
                throw new ArgumentException($"The element name {element.Name} does not match to the expected name {elementAttribute.Name} specified in the attribute {nameof(BXmlElementAttribute)} assigned to {type.FullName})");

            var r = Activator.CreateInstance(type);

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                var propertyAttribute = property.GetCustomAttribute<BXmlPropertyAttribute>();

                if (propertyAttribute == null)
                    continue;

                if (!propertyAttribute.ChildElement && string.IsNullOrWhiteSpace(propertyAttribute.Name))
                    throw new ArgumentException($"The attribute name associated with the property {property.Name} of type {type.FullName} is an empty string", nameof(type));

                object propertyValue = null;

                if (propertyAttribute.ChildElement)
                {
                    var propertyElementAttribute = property.PropertyType.GetCustomAttribute<BXmlElementAttribute>();
                    if (propertyElementAttribute != null)
                    {
                        foreach (XmlNode childNode in element.ChildNodes)
                        {
                            if (childNode.NodeType == XmlNodeType.Element && childNode.Name == propertyElementAttribute.Name)
                            {
                                propertyValue = Deserialize(property.PropertyType, (XmlElement)childNode);
                                break;
                            }
                        }
                    }
                    
                }
                else
                {

                    if (!string.IsNullOrWhiteSpace(propertyAttribute.Name))
                    {
                        string propertyText = GetAttribute(element, propertyAttribute.Name, null);
                        if (propertyText != null)
                        {
                            var propertyType = property.PropertyType;
                            var propertyType1 = Nullable.GetUnderlyingType(propertyType);
                            if (propertyType1 != null && propertyType1 != propertyType)
                                propertyType = propertyType1;
                            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Measurement<>))
                            {
                                var ci = propertyType.GetConstructor(new Type[] { typeof(string) });
                                if (ci != null)
                                {
                                    try
                                    {
                                        propertyValue = ci.Invoke(new object[] { propertyText });
                                    }
                                    catch (Exception )
                                    {
                                        propertyValue = null;
                                    }
                                }
                            }
                            else if (propertyType.IsEnum)
                            {
                                propertyValue = Enum.Parse(propertyType, propertyText);
                            }
                            else if (propertyType == typeof(double))
                            {
                                if (double.TryParse(propertyText, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                                    propertyValue = x;
                            }
                            else if (propertyType == typeof(int))
                            {
                                if (int.TryParse(propertyText, NumberStyles.Any, CultureInfo.InvariantCulture, out int x))
                                    propertyValue = x;
                            }
                            else if (propertyType == typeof(bool))
                            {
                                propertyValue = propertyText == "true";
                            }
                            else if (propertyType == typeof(DateTime))
                            {
                                DateTime d;
                                if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                                    propertyValue = d;
                                else if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                                    propertyValue = d;
                                else if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                                    propertyValue = d;
                            }
                            else if (propertyType == typeof(TimeSpan))
                            {
                                if (double.TryParse(propertyText, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                                    propertyValue = TimeSpan.FromMilliseconds(x);
                                else if (TimeSpan.TryParse(propertyText, out TimeSpan ts))
                                    propertyValue = ts;
                            }
                            else if (propertyType == typeof(string))
                            {
                                propertyValue = propertyText;
                            }
                            else if (propertyType == typeof(BallisticCoefficient))
                            {
                                if (BallisticCoefficient.TryParse(propertyText, CultureInfo.InvariantCulture, out BallisticCoefficient ballisticCoefficient))
                                    propertyValue = ballisticCoefficient;
                            }
                            else
                            {
                                throw new InvalidOperationException($"The type {propertyType.FullName} of the property {type.FullName}.{property.Name} is not supported");
                            }
                        }
                    }
                }

                if (propertyValue == null)
                {
                    if (!propertyAttribute.Optional)
                        throw new InvalidOperationException($"The XML element or attribute associated with the property {type.FullName}.{property.Name} is not found and the value is not optional");
                }
                else
                {
                    property.SetValue(r, propertyValue);
                }

            }
            return r;
        }

        /// <summary>
        /// De-serialize element.
        /// </summary>
        /// <typeparam name="T">The expected object type</typeparam>
        /// <param name="element">The element to be de-serialized</param>
        /// <returns></returns>
        public T Deserialize<T>(XmlElement element) => (T)Deserialize(typeof(T), element);


        private readonly static Lazy<Dictionary<string, Type>> ElementNames = new Lazy<Dictionary<string, Type>>(InitializeTypeDictionary);

        private static Dictionary<string, Type> InitializeTypeDictionary()
        {
            Dictionary<string, Type> r = new Dictionary<string, Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<BXmlElementAttribute>();
                    if (attr != null)
                        r.Add(attr.Name, type);
                }
            }
            return r; 
        }

        /// <summary>
        /// Deserialize element resolving its type by the element name
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public object Deserealize(XmlElement element)
        {
            if (!ElementNames.Value.TryGetValue(element.Name, out Type type))
                throw new ArgumentException($"The element name is not found among all currently loaded classes with {nameof(BXmlElementAttribute)} attribute", nameof(element));

            return Deserialize(type, element);
        }

        /// <summary>
        /// Constructor for a document
        /// </summary>
        /// <param name="document"></param>
        public BallisticXmlSerializer(XmlDocument document)
        {
            mDocument = document;
        }
       
        /// <summary>
        /// Creates the element with the name specified
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <returns></returns>
        protected XmlElement CreateElement(string name) => mDocument.CreateElement(name);

        /// <summary>
        /// Adds a text attribute to the node
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="name">The attribute name</param>
        /// <param name="value">The text value of the attribute</param>
        /// <returns></returns>
        protected XmlAttribute AddAttribute(XmlElement node, string name, string value)
        {
            var attr = mDocument.CreateAttribute(name);
            attr.Value = value;
            node.Attributes.Append(attr);
            return attr;
        }

        /// <summary>
        /// Gets a text attribute value
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="name">The attribute name</param>
        /// <param name="defaultValue">The default value to return in case there is no attribute</param>
        /// <returns></returns>
        protected string GetAttribute(XmlElement node, string name, string defaultValue = null)
        {
            if (node.Attributes == null)
                return defaultValue;
            if (node.Attributes[name] == null)
                return defaultValue;
            return node.Attributes[name].Value;
        }

        /// <summary>
        /// Add an attribute that contains a measurment
        /// </summary>
        /// <typeparam name="T">A measurement unit (e.g. AngularUnit)</typeparam>
        /// <param name="node">The node</param>
        /// <param name="name">The name of the attribute</param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        protected XmlAttribute AddMeasurementAttribute<T>(XmlElement node, string name, Measurement<T> value)
            where T : Enum
        {
            return AddAttribute(node, name, value.Text);
        }

        /// <summary>
        /// Add an attribute with real value
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="name">The name of the attribute</param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        protected XmlAttribute AddRealAttribute(XmlElement node, string name, double value)
        {
            return AddAttribute(node, name, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets a real value of the attribute
        /// </summary>
        /// <param name="node">The node</param>
        /// <param name="name">The name of the attribute</param>
        /// <param name="defaultValue">The value</param>
        /// <returns></returns>
        protected double? GetRealAttribute(XmlElement node, string name, double? defaultValue = null)
        {
            string s = GetAttribute(node, name);
            if (s == null)
                return defaultValue;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return defaultValue;
            return v;
        }

        /// <summary>
        /// Gets the value of the attribute as a measurment
        /// </summary>
        /// <typeparam name="T">A measurement unit (e.g. AngularUnit)</typeparam>
        /// <param name="node">The node</param>
        /// <param name="name">The attribute name</param>
        /// <param name="defaultMeasurement">The value to return if there is no attribute</param>
        /// <returns></returns>
        protected Measurement<T>? GetMeasurmentAttribute<T>(XmlElement node, string name, Measurement<T>? defaultMeasurement = null)
            where T : Enum
        {
            string s = GetAttribute(node, name);
            if (s == null)
                return defaultMeasurement;
            if (!Measurement<T>.TryParse(CultureInfo.InvariantCulture, s, out Measurement<T> v))
                return defaultMeasurement;
            return v;
        }

        /// <summary>
        /// Adds ballistic coefficient as an attribute
        /// </summary>
        /// <param name="node">The element</param>
        /// <param name="name">The attribute</param>
        /// <param name="bc">The ballistic coefficient</param>
        /// <returns></returns>
        protected XmlAttribute AddBallisticCoefficientAttribute(XmlElement node, string name, BallisticCoefficient bc)
        {
            return AddAttribute(node, name, bc.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gets ballistic coefficient as a value
        /// </summary>
        /// <param name="node">The element</param>
        /// <param name="name">The attribute name</param>
        /// <param name="defaultValue">The default value</param>
        /// <returns></returns>
        protected BallisticCoefficient GetBallisticCoefficientAttribute(XmlElement node, string name, BallisticCoefficient defaultValue)
        {
            string s = GetAttribute(node, name);
            if (s == null)
                return defaultValue;
            if (BallisticCoefficient.TryParse(s, CultureInfo.InvariantCulture, out BallisticCoefficient v))
                return defaultValue;
            return v;
        }


        /// <summary>
        /// Reads legacy ammunition info from the XML node
        /// </summary>
        /// <param name="legacyEntry"></param>
        /// <returns></returns>
        public AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(XmlElement legacyEntry)
        {
            if (legacyEntry == null)
                throw new ArgumentNullException(nameof(legacyEntry));

            AmmunitionLibraryEntry entry = new AmmunitionLibraryEntry()
            {
                Ammunition = new Ammunition()
            };

            if (legacyEntry.Attributes["table"] == null)
                throw new ArgumentException("The element must have table attribute", nameof(legacyEntry));

            if (!Enum.TryParse<DragTableId>(legacyEntry.Attributes["table"].Value, out DragTableId table))
                throw new ArgumentException("Unknown table identifier", nameof(legacyEntry));

            if (legacyEntry.Attributes["bc"] == null)
                throw new ArgumentException("The element must have bc attribute", nameof(legacyEntry));

            if (!double.TryParse(legacyEntry.Attributes["bc"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double bc))
                throw new ArgumentException("Ballistic coefficient is not a number", nameof(legacyEntry));

            entry.Ammunition.BallisticCoefficient = new BallisticCoefficient(bc, table);

            if (legacyEntry.Attributes["bullet-weight"] == null)
                throw new ArgumentException("The element must have bullet-weight attribute", nameof(legacyEntry));

            if (!Measurement<WeightUnit>.TryParse(CultureInfo.InvariantCulture, legacyEntry.Attributes["bullet-weight"].Value, out Measurement<WeightUnit> weight))
                throw new ArgumentException("Can't parse bullet weight", nameof(legacyEntry));

            entry.Ammunition.Weight = weight;

            if (legacyEntry.Attributes["muzzle-velocity"] == null)
                throw new ArgumentException("The element must have muzzle-velocity attribute", nameof(legacyEntry));

            if (!Measurement<VelocityUnit>.TryParse(CultureInfo.InvariantCulture, legacyEntry.Attributes["muzzle-velocity"].Value, out Measurement<VelocityUnit> muzzleVelocity))
                throw new ArgumentException("Can't parse muzzle velocity", nameof(legacyEntry));

            entry.Ammunition.MuzzleVelocity = muzzleVelocity;

            if (legacyEntry.Attributes["bullet-length"] != null)
            {
                if (Measurement<DistanceUnit>.TryParse(CultureInfo.InvariantCulture, legacyEntry.Attributes["bullet-length"].Value, out Measurement<DistanceUnit> bulletLength))
                    entry.Ammunition.BulletLength = bulletLength;
            }

            if (legacyEntry.Attributes["bullet-diameter"] != null)
            {
                if (Measurement<DistanceUnit>.TryParse(CultureInfo.InvariantCulture, legacyEntry.Attributes["bullet-diameter"].Value, out Measurement<DistanceUnit> bulletDiameter))
                    entry.Ammunition.BulletDiameter = bulletDiameter;
            }

            if (legacyEntry.Attributes["name"] == null)
                throw new ArgumentException("The element must have name attribute", nameof(legacyEntry));

            entry.Name = legacyEntry.Attributes["name"].Value;

            if (legacyEntry.Attributes["barrel-length"] != null)
            {
                if (Measurement<DistanceUnit>.TryParse(CultureInfo.InvariantCulture, legacyEntry.Attributes["barrel-length"].Value, out Measurement<DistanceUnit> bulletLength))
                    entry.BarrelLength = bulletLength;
            }

            if (legacyEntry.Attributes["source"] != null)
                entry.Source = legacyEntry.Attributes["source"].Value;

            if (legacyEntry.Attributes["caliber"] != null)
                entry.Caliber = legacyEntry.Attributes["caliber"].Value;

            if (legacyEntry.Attributes["bullet-type"] != null)
                entry.AmmunitionType = legacyEntry.Attributes["bullet-type"].Value;

            return entry;
        }


        /// <summary>
        /// Reads legacy ammunition info from the XML text
        /// </summary>
        /// <param name="legacyXml">Either the XML or the file name containing the library entry</param>
        /// <param name="fileName">The flag indicating whether the first parameter is a file name ([c]true[/c]) or an XML ([c]false[/c])</param>
        /// <returns></returns>
        public AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(string xmlText, bool fileName)
        {
            XmlDocument document = new XmlDocument();
            if (fileName)
                document.Load(xmlText);
            else
                document.LoadXml(xmlText);

            if (document.DocumentElement.Name != "ammo-info-ex")
                throw new ArgumentException("The root element of the XML must be ammo-info-ex", nameof(xmlText));

            return ReadLegacyAmmunitionLibraryEntry(document.DocumentElement);
        }
    }
}
