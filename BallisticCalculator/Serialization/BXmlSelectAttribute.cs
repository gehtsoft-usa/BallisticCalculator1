using System;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// The type that may be serialized by multiple option(s)
    /// 
    /// The attribute to markup the class or interface that may 
    /// be serialized by one or more possible options
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
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
