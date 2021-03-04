using System;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// The attribute to markup the ballistic calculator serializable property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BXmlPropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the attribute to be used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flag indicating that the value is optional
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// Flag indicating that the value is a child element.
        /// 
        /// The type must be attributed with <see cref="BXmlElementAttribute"/> or 
        /// <see cref="BXmlSelectAttribute"/>
        /// 
        /// If name is set, it will be forced for the element and 
        /// the original element name will be save as "type" attribute.
        /// 
        /// If name is not set, the element name will be taken from 
        /// the BXmlElement of the target type. 
        /// </summary>
        public bool ChildElement { get; set; } = false;


        /// <summary>
        /// Flag indicating that the properties of the child
        /// element must be saved as attributes of the containing element.
        /// 
        /// The name of the property will be used as a prefix for the 
        /// attributes of the child element.
        /// 
        /// DO NOT use this flag for child elements that contains collections
        /// or child elements themselves
        /// </summary>
        public bool FlattenChild { get; set; } = false;

        /// <summary>
        /// The flag indicating that the value is a collection
        /// 
        /// The type must be an IEnumeration of type 
        /// attributed with <see cref="BXmlElementAttribute"/> or
        /// <see cref="BXmlSelectAttribute"/> and must have Add method.
        /// 
        /// For the collection the name property is obligatory. 
        /// </summary>
        public bool Collection { get; set; } = false;

        /// <summary>
        /// The default constructor
        /// </summary>
        public BXmlPropertyAttribute()
        {

        }

        /// <summary>
        /// The parameterized constructor
        /// </summary>
        /// <param name="name"></param>
        public BXmlPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
