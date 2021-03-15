using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// The attribute to markup the ballistic calculator serializable class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BXmlElementAttribute : Attribute
    {
        /// <summary>
        /// The name of the element to be used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The default constructor
        /// </summary>
        public BXmlElementAttribute()
        {
        }

        /// <summary>
        /// The parameterized constructor
        /// </summary>
        /// <param name="name"></param>
        public BXmlElementAttribute(string name)
        {
            Name = name;
        }
    }
}
