using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using Gehtsoft.Measurements;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

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
        /// <para>Deserialize the element as the object of the specified type. </para>
        /// <para>The method returns `null` if the object has a type other than `T`. </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public T Deserialize<T>(XmlElement element) where T : class => Deserialize(element) as T;

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

        interface IReader
        {
            bool CanSet(PropertyInfo propertyInfo);
            bool CanGet(PropertyInfo propertyInfo);
            void Set(PropertyInfo propertyInfo, object value);
            object Get(PropertyInfo propertyInfo);
            object GetInstance();
        }

        class ConstructorReader : IReader
        {
            private readonly ConstructorInfo mConstructor;
            private readonly ParameterInfo[] mConstructorParams;
            private readonly object[] mConstructorParamValues;

            public ConstructorReader(ConstructorInfo constructorInfo)
            {
                mConstructor = constructorInfo;
                mConstructorParams = constructorInfo.GetParameters();
                mConstructorParamValues = new object[mConstructorParams.Length];
            }

            public bool CanSet(PropertyInfo propertyInfo) => true;
            public bool CanGet(PropertyInfo propertyInfo) => false;

            public void Set(PropertyInfo propertyInfo, object value)
            {
                for (int i = 0; i < mConstructorParams.Length; i++)
                {
                    if (string.Equals(propertyInfo.Name, mConstructorParams[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        mConstructorParamValues[i] = value;
                        break;
                    }
                }
            }
            public object Get(PropertyInfo propertyInfo) => null;

            public object GetInstance() => mConstructor.Invoke(mConstructorParamValues);
        }

        class AccessorReader : IReader
        {
            private readonly object mInstance;

            public AccessorReader(Type type)
            {
                mInstance = Activator.CreateInstance(type);
            }

            public bool CanSet(PropertyInfo propertyInfo) => propertyInfo.SetMethod != null;
            public bool CanGet(PropertyInfo propertyInfo) => propertyInfo.GetMethod != null;

            public void Set(PropertyInfo propertyInfo, object value)
            {
                if (propertyInfo.SetMethod != null)
                    propertyInfo.SetValue(mInstance, value);
            }

            public object Get(PropertyInfo propertyInfo)
            {
                if (propertyInfo.GetMethod != null)
                    return propertyInfo.GetValue(mInstance);
                return null;
            }

            public object GetInstance() => mInstance;
        }

        static class ReaderFactory
        {
            public static IReader CreateReader(Type type)
            {
                var specificConstructor = type.GetConstructors()
                    .FirstOrDefault(c => c.GetCustomAttribute<BXmlConstructorAttribute>() != null);

                if (specificConstructor != null)
                    return new ConstructorReader(specificConstructor);
                return new AccessorReader(type);
            }
        }



        private object Deserialize(XmlElement element, Type type, string attributePrefix)
        {
            var reader = ReaderFactory.CreateReader(type);

            foreach (var property in type.GetProperties())
            {
                var propertyAttribute = property.GetCustomAttribute<BXmlPropertyAttribute>();
                if (propertyAttribute == null)
                    continue;

                ReadProperty(element, type, propertyAttribute, attributePrefix, property, reader);
            }

            return reader.GetInstance();
        }

        private void ReadProperty(XmlElement element, Type type, BXmlPropertyAttribute propertyAttribute, string attributePrefix, PropertyInfo property, IReader reader)
        {
            bool found = false;
            Type targetType = GetReadTargetType(propertyAttribute, property);

            BXmlElementAttribute elementAttribute1 = targetType.GetCustomAttribute<BXmlElementAttribute>();
            BXmlSelectAttribute selectAttribute = targetType.GetCustomAttribute<BXmlSelectAttribute>();

            if ((!propertyAttribute.ChildElement || elementAttribute1 == null) && string.IsNullOrEmpty(propertyAttribute.Name))
                throw new InvalidOperationException($"The value of the property {type.FullName}.{property.Name} must have the name specified in the {nameof(BXmlPropertyAttribute)}");

            if ((propertyAttribute.ChildElement || propertyAttribute.Collection) &&
                (elementAttribute1 == null && selectAttribute == null))
            {
                throw new InvalidOperationException($"The type of the property {type.FullName}.{property.Name} must have either {nameof(BXmlElementAttribute)} or {nameof(BXmlSelectAttribute)}");
            }

            if (propertyAttribute.ChildElement)
                found = ReadChildElementProperty(element, propertyAttribute, property, reader, targetType, elementAttribute1, selectAttribute);
            else if (propertyAttribute.Collection)
                found = ReadCollectionProperty(element, propertyAttribute, property, reader, targetType, elementAttribute1, selectAttribute);
            else
                found = ReadAttributeProperty(element, type, propertyAttribute, attributePrefix, property, reader);

            if (!found && !propertyAttribute.Optional)
                throw new InvalidOperationException($"The value of the property {type.FullName}.{property.Name} is not found but the property is not optional");
        }

        private bool ReadAttributeProperty(XmlElement element, Type type, BXmlPropertyAttribute propertyAttribute, string attributePrefix, PropertyInfo property, IReader reader)
        {
            object propertyValue = ReadAttribute(type, element, property.Name, property.PropertyType, propertyAttribute, attributePrefix);
            if (propertyValue != null)
            {
                reader.Set(property, propertyValue);
                return true;
            }
            return false;
        }

        private bool ReadChildElementProperty(XmlElement element, BXmlPropertyAttribute propertyAttribute, PropertyInfo property, IReader reader, Type targetType, BXmlElementAttribute elementAttribute1, BXmlSelectAttribute selectAttribute)
        {
            string elementToSearch = null;

            if (elementAttribute1 == null && string.IsNullOrEmpty(propertyAttribute.Name))
                throw new InvalidOperationException($"The value of the property {property.DeclaringType.FullName}.{property.Name} must have the name specified in the {nameof(BXmlPropertyAttribute)}");

            elementToSearch = propertyAttribute.Name ?? elementAttribute1?.Name;

            if (!propertyAttribute.FlattenChild)
                return ReadChildElementProperty_NotFlatten(element, property, reader, targetType, elementAttribute1, selectAttribute, elementToSearch);
            else
                return ReadChildElementProperty_Flatten(element, property, reader, targetType, elementToSearch);
        }

        private bool ReadChildElementProperty_NotFlatten(XmlElement element, PropertyInfo property, IReader reader, Type targetType, BXmlElementAttribute elementAttribute1, BXmlSelectAttribute selectAttribute, string elementToSearch)
        {
            bool found = false;
            
            var childElement = FindChildElement(element, elementToSearch);

            if (childElement != null)
            {
                object propertyValue;
                if (elementAttribute1 != null)
                    propertyValue = Deserialize(childElement, targetType);
                else
                    propertyValue = Deserialize(childElement, selectAttribute.Options);

                reader.Set(property, propertyValue);
                found = true;
            }

            return found;
        }

        private bool ReadChildElementProperty_Flatten(XmlElement element, PropertyInfo property, IReader reader, Type targetType, string elementToSearch)
        {
            bool found = false;
            string prefix = $"{elementToSearch}-";
            bool any = false;
            for (int i = 0; !any && i < element.Attributes.Count; i++)
            {
                if (element.Attributes[i].Name.StartsWith(prefix))
                    any = true;
            }

            if (any)
            {
                object propertyValue = Deserialize(element, targetType, elementToSearch);
                reader.Set(property, propertyValue);
                found = true;
            }

            return found;
        }

        private bool ReadCollectionProperty(XmlElement element, BXmlPropertyAttribute propertyAttribute, PropertyInfo property, IReader reader, Type targetType, BXmlElementAttribute elementAttribute1, BXmlSelectAttribute selectAttribute)
        {
            object collection = ReadCollectionProperty_InitializeCollection(property, targetType, reader);

            MethodInfo method = collection.GetType().GetMethod("Add", new Type[] { targetType });

            if (method == null)
                throw new InvalidOperationException($"The type of the property {property.DeclaringType.FullName}.{property.Name} has no Add({targetType.FullName}) method");

            var collectionElement = FindChildElement(element, propertyAttribute.Name);

            if (collectionElement == null)
                return false;

            object[] parameters = new object[] { null };

            Func<XmlElement, object> deserializer;
            
            if (elementAttribute1 != null)
                deserializer = e => Deserialize(e, targetType);
            else
                deserializer = e => Deserialize(e, selectAttribute.Options);

            foreach (XmlNode node in collectionElement.ChildNodes)
            {
                if (node is not XmlElement childElement)
                    continue;
                
                parameters[0] = deserializer(childElement);
                method.Invoke(collection, parameters);

            }

            if (reader.CanSet(property))
            {
                if (property.PropertyType.IsArray)
                    collection = collection.GetType().GetMethod("ToArray", new Type[] { }).Invoke(collection, Array.Empty<object>());
                reader.Set(property, collection);
            }

            return true;
        }

        private object ReadCollectionProperty_InitializeCollection(PropertyInfo property, Type targetType, IReader reader)
        {
            object collection = null;
            if (reader.CanSet(property))
            {
                if (property.PropertyType.IsArray)
                    collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(targetType));
                else
                    collection = Activator.CreateInstance(property.PropertyType);
            }
            else
            {
                if (reader.CanGet(property))
                    collection = reader.Get(property);
                if (collection == null)
                    throw new InvalidOperationException($"The value of the property {property.DeclaringType.FullName}.{property.Name} has no set accessor and is not initialized by default");
            }
            return collection;
        }

        private Type GetReadTargetType(BXmlPropertyAttribute propertyAttribute, PropertyInfo property)
        {
            Type targetType = null;
            if (propertyAttribute.Collection)
            {
                foreach (Type iface in property.PropertyType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        targetType = iface.GetGenericArguments()[0];
                        break;
                    }
                }
                if (targetType == null)
                    throw new InvalidOperationException($"The type of the value {property.DeclaringType.FullName}.{property.Name} must implement IEnumerable interface");
            }
            else
            {
                targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            }
            return targetType;
        }

        private string ElementPath(XmlElement element)
        {
            var path = new List<XmlElement>();
            while (element != null)
            {
                path.Add(element);
                element = element.ParentNode as XmlElement;
            }

            var builder = new StringBuilder();
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

        class ReadAttributeAction
        {
            internal Func<Type, bool> Probe { get; }

            internal Func<Type, string, object> Value { get; }

            internal ReadAttributeAction(Func<Type, bool> probe, Func<Type, string, object> value)
            {
                Probe = probe;
                Value = value;
            }
        }

        private static readonly ReadAttributeAction[] gReadAttributeActions = new ReadAttributeAction[]
        {
            new ReadAttributeAction(
                (type) => SerializerTools.IsTypeMeasurement(type),
                (propertyType, propertyText) => propertyType.GetConstructor(new Type[] { typeof(string) })?.Invoke(new object[] { propertyText })
            ),

            new ReadAttributeAction(
                (type) => type.IsEnum,
                (propertyType, propertyText) => Enum.Parse(propertyType, propertyText)
            ),

            new ReadAttributeAction(
                (type) => type == typeof(double),
                (propertyType, propertyText) =>
                {
                    if (double.TryParse(propertyText, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                        return x;
                    return null;
                }
            ),

            new ReadAttributeAction(
                (type) => type == typeof(float),
                (propertyType, propertyText) =>
                {
                    if (float.TryParse(propertyText, NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                        return x;
                    return null;
                }
            ),

            new ReadAttributeAction(
                (type) => type == typeof(int),
                (propertyType, propertyText) =>
                {
                    if (int.TryParse(propertyText, NumberStyles.Any, CultureInfo.InvariantCulture, out int x))
                        return x;
                    return null;
                }
            ),

            new ReadAttributeAction(
                (type) => type == typeof(bool),
                (propertyType, propertyText) => propertyText == "true"
            ),

            new ReadAttributeAction(
                (type) => type == typeof(DateTime),
                (propertyType, propertyText) =>
                {
                 if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out DateTime d))
                        return d;
                    else if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                        return d;
                    else if (DateTime.TryParseExact(propertyText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out d))
                        return d;
                 return null;
                }
            ),

            new ReadAttributeAction(
                (type) => type == typeof(TimeSpan),
                (propertyType, propertyText) =>
                {
                    if (double.TryParse(propertyText, NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
                        return TimeSpan.FromMilliseconds(x);
                    else if (TimeSpan.TryParse(propertyText, out TimeSpan ts))
                        return ts;
                    return null;
                }
            ),

            new ReadAttributeAction(
                (type) => type == typeof(string),
                (propertyType, propertyText) => propertyText
            ),

            new ReadAttributeAction(
                (type) => type == typeof(BallisticCoefficient),
                (propertyType, propertyText) =>
                {
                    if (BallisticCoefficient.TryParse(propertyText, CultureInfo.InvariantCulture, out BallisticCoefficient ballisticCoefficient))
                        return ballisticCoefficient;
                    return null;
                }
            ),
        };



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
            string name = GetAttributeName(propertyAttribute, attributePrefix);
            string propertyText = element.Attributes[name]?.Value;

            if (propertyText == null)
                return null;

            propertyType = SerializerTools.RemoveNullabilityFromType(propertyType);

            for (int i = 0; i < gReadAttributeActions.Length; i++)
            {
                if (gReadAttributeActions[i].Probe(propertyType))
                    return gReadAttributeActions[i].Value(propertyType, propertyText);
            }
            throw new InvalidOperationException($"The type {propertyType.FullName} of the property {type.FullName}.{propertyName} is not supported");
        }

        private string GetAttributeName(BXmlPropertyAttribute propertyAttribute, string attributePrefix)
        {
            if (string.IsNullOrEmpty(attributePrefix))
                return propertyAttribute.Name;
            else
                return $"{attributePrefix}-{propertyAttribute.Name}";
        }

        class LegacyReaderAction
        {
            public string Attribute { get; }
            public string AdditionalAttribute { get; }
            
            public bool Optional { get; }
            public Func<string, string, object> TryParse { get; }
            private readonly Func<AmmunitionLibraryEntry, object> mTargetLeftSide;
            private readonly PropertyInfo mTargetProperty;

            public LegacyReaderAction(string attribute, bool optional, Func<string, string, object> tryParse, Expression<Func<AmmunitionLibraryEntry, object>> target, string additionalAttribute = null)
            {
                Attribute = attribute;
                AdditionalAttribute = additionalAttribute;
                Optional = optional;
                TryParse = tryParse;
                if (target != null)
                {
                    if (!DiscoverExpression(target.Parameters, target.Body, out var targetExpression, out var property))
                        throw new ArgumentException($"The expression {target} is not supported", nameof(target));
                    mTargetLeftSide = targetExpression;
                    mTargetProperty = property;
                }
            }

            private static bool DiscoverExpression(ReadOnlyCollection<ParameterExpression> parameters, Expression body, out Func<AmmunitionLibraryEntry, object> target, out PropertyInfo property)
            {
                if (body is MemberExpression memberExpression)
                {
                    var leftSideExpression = Expression.Lambda(memberExpression.Expression, parameters);
                    target = (Func<AmmunitionLibraryEntry, object>)leftSideExpression.Compile();
                    property = memberExpression.Member as PropertyInfo;
                    return true;
                }
                if (body is UnaryExpression unaryExpression)
                    return DiscoverExpression(parameters, unaryExpression.Operand, out target, out property);
                else
                {
                    target = null;
                    property = null;
                    return false;
                }

            }

            public void Proccess(XmlElement element, AmmunitionLibraryEntry target)
            {
                var attribute = element.Attributes[Attribute];
                if (attribute == null)
                {
                     if (!Optional)
                        throw new InvalidOperationException($"The element {element.Name} does not have the attribute {Attribute}");
                    return;
                }

                if (TryParse != null && mTargetProperty != null)
                {
                    string text = attribute.Value, text1 = null;
                    if (AdditionalAttribute != null)
                    {
                        var additionalAttribute = element.Attributes[AdditionalAttribute];
                        if (additionalAttribute != null)
                            text1 = additionalAttribute.Value;
                    }

                    object value = TryParse(text, text1);
                    if (value == null)
                        throw new InvalidOperationException($"The value {text} of the attribute {Attribute} could not be parsed");

                    object targetObject = mTargetLeftSide.Invoke(target);
                    mTargetProperty.SetValue(targetObject, value);
                }
            }
        }

        private static Measurement<T>? TryParseMeasurement<T>(string text) 
            where T : Enum
        {
            if (Measurement<T>.TryParse(CultureInfo.InvariantCulture, text, out Measurement<T> weight))
                return weight;
            return null;

        }

        private static readonly LegacyReaderAction[] gLegacyReaderAction = new LegacyReaderAction[]
        {
            new LegacyReaderAction("table", false, null, null),
            new LegacyReaderAction("bc", false, (bc, table) =>
            {
                if (!Enum.TryParse<DragTableId>(table, out var tv))
                    return null;
                if (!double.TryParse(bc, NumberStyles.Float, CultureInfo.InvariantCulture, out var bv))
                    return null;
                return new BallisticCoefficient(bv, tv);
            }, entry => entry.Ammunition.BallisticCoefficient, "table"),
            new LegacyReaderAction("bullet-weight", false, (text, text1) => TryParseMeasurement<WeightUnit>(text), (entry) => entry.Ammunition.Weight),
            new LegacyReaderAction("muzzle-velocity", false, (text, text1) => TryParseMeasurement<VelocityUnit>(text), (entry) => entry.Ammunition.MuzzleVelocity),
            new LegacyReaderAction("bullet-length", true, (text, text1) => TryParseMeasurement<DistanceUnit>(text), (entry) => entry.Ammunition.BulletLength),
            new LegacyReaderAction("bullet-diameter", true, (text, text1) => TryParseMeasurement<DistanceUnit>(text), (entry) => entry.Ammunition.BulletDiameter),
            new LegacyReaderAction("name", false, (text, text1) => text, (entry) => entry.Name),
            new LegacyReaderAction("barrel-length", true, (text, text1) => TryParseMeasurement<DistanceUnit>(text), (entry) => entry.BarrelLength),
            new LegacyReaderAction("source", true, (text, text1) => text, (entry) => entry.Source),
            new LegacyReaderAction("caliber", true, (text, text1) => text, (entry) => entry.Caliber),
            new LegacyReaderAction("bullet-type", true, (text, text1) => text, (entry) => entry.AmmunitionType),
        };

        /// <summary>
        /// Reads legacy ammunition info from the XML node
        /// </summary>
        /// <param name="legacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(XmlElement legacyEntry)
        {
            if (legacyEntry == null)
                throw new ArgumentNullException(nameof(legacyEntry));

            var entry = new AmmunitionLibraryEntry()
            {
                Ammunition = new Ammunition()
            };

            for (int i = 0; i < gLegacyReaderAction.Length; i++)
                gLegacyReaderAction[i].Proccess(legacyEntry, entry);
            
            return entry;
        }

        /// <summary>
        /// Reads raw legacy entry from the text
        /// </summary>
        /// <param name="rawLegacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntry(string rawLegacyEntry)
        {
            var document = new XmlDocument();
            document.LoadXml(rawLegacyEntry);
            return ReadLegacyAmmunitionLibraryEntry(document.DocumentElement);
        }

        /// <summary>
        /// Reads raw legacy entry from the stream
        /// </summary>
        /// <param name="rawLegacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntryFromStream(Stream rawLegacyEntry)
        {
            var document = new XmlDocument();
            document.Load(rawLegacyEntry);
            return ReadLegacyAmmunitionLibraryEntry(document.DocumentElement);
        }

        /// <summary>
        /// Reads raw legacy entry from the file
        /// </summary>
        /// <param name="rawLegacyEntry"></param>
        /// <returns></returns>
        public static AmmunitionLibraryEntry ReadLegacyAmmunitionLibraryEntryFromFile(string rawLegacyEntry)
        {
            using var file = new FileStream(rawLegacyEntry, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ReadLegacyAmmunitionLibraryEntryFromStream(file);
        }

        /// <summary>
        /// Reads the object of type t from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T ReadFromStream<T>(Stream stream)
            where T : class
        {
            XmlDocument document = new XmlDocument();
            document.Load(stream);
            var xmlDeseralizer = new BallisticXmlDeserializer();
            return xmlDeseralizer.Deserialize<T>(document.DocumentElement);
        }

        /// <summary>
        /// Reads the object of type t from the file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static T ReadFromFile<T>(string fileName)
            where T : class
        {
            using var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ReadFromStream<T>(file);
        }
    }
}
