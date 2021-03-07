using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using Gehtsoft.Measurements;
using System.Globalization;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// Deserializer of ballistic calculator values
    /// </summary>
    public class BallisticXmlDeserializer
    {
        private readonly ElementDictionary mElementDictionary = new ElementDictionary();

        /// <summary>
        /// Default constructor
        /// </summary>
        public BallisticXmlDeserializer()
        {

        }

        /// <summary>
        /// Deserialize object from the element detecting the type by its name
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public object Deserialize(XmlElement element)
        {
            string typeName;
            if (element.Attributes["data-type"] != null)
                typeName = element.Attributes["data-type"].Value;
            else
                typeName = element.Name;

            Type type = mElementDictionary.Search(typeName);

            if (type == null)
                throw new InvalidOperationException($"Can't find a type with attribute {nameof(BXmlElementAttribute)} that has the name associated with the element name {element.Name}");

            return Deserialize(element, type);
        }

        /// <summary>
        /// Deserialize the element when it's possible type is limited to the list of the types specified
        /// </summary>
        /// <param name="element"></param>
        /// <param name="possibleTypes"></param>
        /// <returns></returns>
        public object Deserialize(XmlElement element, Type[] possibleTypes)
        {
            string typeName;
            if (element.Attributes["data-type"] != null)
                typeName = element.Attributes["data-type"].Value;
            else
                typeName = element.Name;

            Type type = ScanTypesForElements(possibleTypes, typeName);
            if (type == null)
                throw new ArgumentException($"Element type {typeName} of {ElementPath(element)} is not found along the types specified");

            return Deserialize(element, type);
        }

        /// <summary>
        /// Deserialize object from the element using the specified type
        /// <param name="element"/>
        /// <param name="type"/>
        /// </summary>
        public object Deserialize(XmlElement element, Type type) => Deserialize(element, type, null);

        private object Deserialize(XmlElement element, Type type, string attributePrefix)
        {
            //check whether the object has specific constructor
            ConstructorInfo specificConstructor = null;
            foreach (var constructor in type.GetConstructors())
                if (constructor.GetCustomAttribute<BXmlConstructor>() != null)
                {
                    specificConstructor = constructor;
                    break;
                }

            object value = null;
            ParameterInfo[] constructorParams = null;
            object[] constructorParamValues = null;

            Func<PropertyInfo, Action<object>> setAction = null;
            Func<PropertyInfo, Func<object>> getAction = null;

            if (specificConstructor != null)
            {
                constructorParams = specificConstructor.GetParameters();
                constructorParamValues = new object[constructorParams.Length];

                setAction = (propertyInfo) => (propertyValue) =>
                {
                    for (int i = 0; i < constructorParams.Length; i++)
                    {
                        if (string.Equals(propertyInfo.Name, constructorParams[i].Name, StringComparison.OrdinalIgnoreCase))
                        {
                            constructorParamValues[i] = propertyValue;
                            break;
                        }
                    }
                };
                getAction = (propertyInfo) => null;
            }
            else
            {
                value = Activator.CreateInstance(type);
                setAction = (propertyInfo) => propertyInfo.SetMethod == null ? null : propertyValue => propertyInfo.SetValue(value, propertyValue);
                getAction = (propertyInfo) => propertyInfo.GetMethod == null ? null : () => propertyInfo.GetValue(value);
            }


            foreach (var property in type.GetProperties())
            {
                var propertyAttribute = property.GetCustomAttribute<BXmlPropertyAttribute>();
                if (propertyAttribute == null)
                    continue;

                ReadProperty(element, type, property.Name, property.PropertyType, propertyAttribute, attributePrefix,
                             setAction(property), getAction(property)); 
            }

            if (specificConstructor != null)
                value = specificConstructor.Invoke(constructorParamValues);

            return value;
        }

        private void ReadProperty(XmlElement element, Type type, string propertyName, Type propertyType, BXmlPropertyAttribute propertyAttribute, string attributePrefix, Action<object> setProperty, Func<object> getProperty = null)
        {
            bool found = false;

            Type targetType = null;

            if (propertyAttribute.Collection)
            {
                foreach (Type iface in propertyType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        targetType = iface.GetGenericArguments()[0];
                        break;
                    }
                }
                if (targetType == null)
                    throw new InvalidOperationException($"The type of the value {type.FullName}.{propertyName} must implement IEnumerable interface");
            }
            else
            {
                targetType = Nullable.GetUnderlyingType(propertyType);
                if (targetType == null)
                    targetType = propertyType;

            }

            BXmlElementAttribute elementAttribute1 = targetType.GetCustomAttribute<BXmlElementAttribute>();
            BXmlSelectAttribute selectAttribute = targetType.GetCustomAttribute<BXmlSelectAttribute>();

            if ((!propertyAttribute.ChildElement || elementAttribute1 == null) && string.IsNullOrEmpty(propertyAttribute.Name))
                throw new InvalidOperationException($"The value of the property {type.FullName}.{propertyName} must have the name specified in the {nameof(BXmlPropertyAttribute)}");

            if ((propertyAttribute.ChildElement || propertyAttribute.Collection) &&
                (elementAttribute1 == null && selectAttribute == null))
                throw new InvalidOperationException($"The type of the property {type.FullName}.{propertyName} must have either {nameof(BXmlElementAttribute)} or {nameof(BXmlSelectAttribute)}");


            if (propertyAttribute.ChildElement)
            {
                string elementToSearch = null;

                if (elementAttribute1 == null && string.IsNullOrEmpty(propertyAttribute.Name))
                    throw new InvalidOperationException($"The value of the property {type.FullName}.{propertyName} must have the name specified in the {nameof(BXmlPropertyAttribute)}");

                elementToSearch = propertyAttribute.Name ?? elementAttribute1?.Name;

                if (!propertyAttribute.FlattenChild)
                {
                    var childElement = FindChildElement(element, elementToSearch);

                    if (childElement != null)
                    {
                        object propertyValue;
                        if (elementAttribute1 != null)
                            propertyValue = Deserialize(childElement, targetType);
                        else
                            propertyValue = Deserialize(childElement, selectAttribute.Options);

                        setProperty(propertyValue);
                        found = true;
                    }
                }
                else
                {
                    string prefix = $"{elementToSearch}-";
                    bool any = false;
                    for (int i = 0; !any && i < element.Attributes.Count; i++)
                        if (element.Attributes[i].Name.StartsWith(prefix))
                            any = true;

                    if (any)
                    {
                        object propertyValue = Deserialize(element, targetType, elementToSearch);
                        setProperty(propertyValue);
                        found = true;
                    }
                }
            }
            else if (propertyAttribute.Collection)
            {
                object collection = null;
                if (setProperty != null)
                {
                    if (propertyType.IsArray)
                        collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(targetType));
                    else
                        collection = Activator.CreateInstance(propertyType);
                }
                else
                {

                    if (getProperty != null)
                        collection = getProperty();
                    if (collection == null)
                        throw new InvalidOperationException($"The value of the property {type.FullName}.{propertyName} has no set accessor and is not initialized by default");
                }

                MethodInfo method = collection.GetType().GetMethod("Add", new Type[] { targetType });
                if (method == null)
                    throw new InvalidOperationException($"The type of the property {type.FullName}.{propertyName} has no Add({targetType.FullName}) method");

                var collectionElement = FindChildElement(element, propertyAttribute.Name);
                if (collectionElement != null)
                {
                    object[] parameters = new object[] { null };
                    foreach (XmlNode node in collectionElement.ChildNodes)
                    {
                        if (node is XmlElement childElement)
                        {
                            if (elementAttribute1 != null)
                                parameters[0] = Deserialize(childElement, targetType);
                            else
                                parameters[0] = Deserialize(childElement, selectAttribute.Options);

                            method.Invoke(collection, parameters);
                        }
                    }

                    if (setProperty != null)
                    {
                        if (propertyType.IsArray)
                            collection = collection.GetType().GetMethod("ToArray", new Type[] { }).Invoke(collection, Array.Empty<object>());
                        setProperty(collection);
                    }

                    found = true;
                }
            }
            else
            {
                object propertyValue = ReadAttribute(type, element, propertyName, propertyType, propertyAttribute, attributePrefix);
                if (propertyValue != null)
                {
                    setProperty(propertyValue);
                    found = true;
                }
            }

            if (!found && !propertyAttribute.Optional)
                throw new InvalidOperationException($"The value of the property {type.FullName}.{propertyName} is not found but the property is not optional");
        }

        private string ElementPath(XmlElement element)
        {
            List<XmlElement> path = new List<XmlElement>();
            while (element != null)
            {
                path.Add(element);
                element = element.ParentNode as XmlElement;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = path.Count - 1; i >= 0; i--)
            {
                builder.Append("\\");
                builder.Append(path[i].Name);
            }

            return builder.ToString();
        }

        private XmlElement FindChildElement(XmlElement parent, string name)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name == name)
                    return node as XmlElement;
            }
            return null;
        }

        private Type ScanTypesForElements(Type[] possibleTypes, string typeName)
        {
            foreach (Type type in possibleTypes)
            {
                var selectAttribute = type.GetCustomAttribute<BXmlSelectAttribute>();
                if (selectAttribute != null)
                {
                    var r = ScanTypesForElements(selectAttribute.Options, typeName);
                    if (r != null)
                        return r;

                    continue;
                }
                var elementAttribute = type.GetCustomAttribute<BXmlElementAttribute>();
                if (elementAttribute != null)
                {
                    if (elementAttribute.Name == typeName)
                        return type;

                    continue;
                }
                throw new ArgumentException($"Type {type.FullName} must be attributed either using {nameof(BXmlElementAttribute)} or {nameof(BXmlSelectAttribute)}", nameof(possibleTypes));
            }
            return null;
        }

        /// <summary>
        /// Reads a simple value from the attribute
        /// </summary>
        /// <param name="type">The type being read</param>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <param name="propertyAttribute"></param>
        /// <param name="attributePrefix"></param>
        /// <returns></returns>
        private object ReadAttribute(Type type, XmlElement element, string propertyName, Type propertyType, BXmlPropertyAttribute propertyAttribute, string attributePrefix)
        {
            object propertyValue = null;

            string name;
            if (string.IsNullOrEmpty(attributePrefix))
                name = propertyAttribute.Name;
            else
                name = $"{attributePrefix}-{propertyAttribute.Name}";

            string propertyText = element.Attributes[name]?.Value;
            
            if (propertyText != null)
            {
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
                        catch (Exception)
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
                    throw new InvalidOperationException($"The type {propertyType.FullName} of the property {type.FullName}.{propertyName} is not supported");
                }
            }
            return propertyValue;
        }

        /// <summary>
        /// Reads legacy ammunition info from the XML node
        /// </summary>
        /// <param name="legacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(XmlElement legacyEntry)
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
        /// Reads raw legacy entry from the text
        /// </summary>
        /// <param name="rawLegacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(string rawLegacyEntry)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(rawLegacyEntry);
            return ReadLegacyAmmunitionLibraryEntry(document.DocumentElement);
        }
    }
}
