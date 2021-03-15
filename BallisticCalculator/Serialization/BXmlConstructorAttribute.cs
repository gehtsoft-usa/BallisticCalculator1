using System;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// <para>The attribute to mark up to constructor to be used to deserialization</para>
    /// <para>The constructor must have the parameter with names matching to the serialized attributes</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public class BXmlConstructorAttribute: Attribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BXmlConstructorAttribute()
        {
        }
    }
}
