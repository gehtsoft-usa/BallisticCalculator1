using System;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// <para>The type that may be serialized by multiple option(s)</para>
    /// <para>
    /// The attribute to markup the class or interface that may
    /// be serialized by one or more possible options
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class BXmlSelectAttribute : Attribute
    {
        /// <summary>
        /// The list of the types that are be derived from this class
        /// </summary>
        public Type[] Options { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BXmlSelectAttribute()
        {
        }

        /// <summary>
        /// The constructor with the list of the types
        /// </summary>
        /// <param name="options"></param>
        public BXmlSelectAttribute(params Type[] options)
        {
            Options = options;
        }
    }
}
