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
        /// The type must be attributed with <see cref="BXmlElementAttribute"/>
        /// </summary>
        public bool ChildElement { get; set; } = false;

        /// <summary>
        /// The flag indicating that the value is a collection
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
