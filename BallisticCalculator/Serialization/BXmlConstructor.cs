using System;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// The attribute to mark up to constructor to be used to deserialization
    /// 
    /// The constructor must have the parameter with names matching to the serialized attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public class BXmlConstructor : Attribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BXmlConstructor()
        {

        }
    }
}
