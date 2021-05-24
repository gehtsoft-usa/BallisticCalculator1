using System;
using System.Collections.Generic;
using System.Globalization;
using BallisticCalculator.Serialization;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The base class for path elements</para>
    /// <para>
    /// The implementations are <see cref="ReticlePathElementArc" />, <see cref="ReticlePathElementMoveTo" />, and
    /// <see cref="ReticlePathElementLineTo" />
    /// </para>
    /// </summary>
    [BXmlSelect(typeof(ReticlePathElementMoveTo), typeof(ReticlePathElementLineTo), typeof(ReticlePathElementArc))]
    public abstract class ReticlePathElement : IEquatable<ReticlePathElement>, IEqualityComparer<ReticlePathElement>, IFormattable, ICloneable
    {
        /// <summary>
        /// The position of action
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// The type of the action
        /// </summary>
        public ReticlePathElementType ElementType { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="elementType"></param>
        protected ReticlePathElement(ReticlePathElementType elementType)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// Cast this object to the specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T As<T>() where T : ReticlePathElement => this as T;

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ReticlePathElement other)
        {
            if (other == null)
                return false;
            if (this.GetType() != other.GetType())
                return false;

            return EqualsInternal(other);
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected abstract bool EqualsInternal(ReticlePathElement other);

        /// <summary>
        /// Checks whether the object equals to another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ReticlePathElement e)
                return Equals(e);
            return object.ReferenceEquals(this, obj);
        }

        /// <summary>
        /// Checks equality of two objects
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ReticlePathElement x, ReticlePathElement y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return true;

            return x.Equals(y);
        }

        /// <summary>
        /// Returns the hash code of the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ReticlePathElement obj)
        {
            if (obj == null)
                return 0;
            else
                return obj.GetHashCode();
        }

        /// <summary>
        /// Converts the object to the string of the default format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString("NF", CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the object to the string of the format specified
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider) => ToStringInternal(format, formatProvider);

        /// <summary>
        /// Converts the object to the string of the format specified
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        protected abstract string ToStringInternal(string format, IFormatProvider formatProvider);

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public abstract ReticlePathElement Clone();
    }
}
