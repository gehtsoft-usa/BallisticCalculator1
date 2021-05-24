using BallisticCalculator.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>An element of a reticle</para>
    /// <para>
    /// The implementation types are <see cref="ReticleCircle" />,
    /// <see cref="ReticleLine" />, <see cref="ReticlePath" />, <see cref="ReticleRectangle" />, and
    /// <see cref="ReticleText" />.
    /// </para>
    /// </summary>
    [BXmlSelect(typeof(ReticleCircle), typeof(ReticlePath),
                typeof(ReticleLine), typeof(ReticleRectangle), typeof(ReticleText))]
    public abstract class ReticleElement : IEquatable<ReticleElement>, IEqualityComparer<ReticleElement>, IFormattable, ICloneable
    {
        /// <summary>
        /// The type of the element
        /// </summary>
        public ReticleElementType ElementType { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        protected ReticleElement(ReticleElementType type)
        {
            ElementType = type;
        }

        /// <summary>
        /// Cast to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T As<T>() where T : ReticleElement => this as T;

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ReticleElement other)
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
        protected abstract bool EqualsInternal(ReticleElement other);

        /// <summary>
        /// Checks equality of two objects
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ReticleElement x, ReticleElement y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return true;

            return x.Equals(y);
        }

        /// <summary>
        /// Checks whether the object equals to another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ReticleElement e)
                return Equals(e);
            return object.ReferenceEquals(this, obj);
        }

        /// <summary>
        /// Returns the hash code of the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(ReticleElement obj)
        {
            if (obj == null)
                return 0;
            else
                return obj.GetHashCode();
        }

        /// <summary>
        /// Coverts object to default string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString("NF", CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts object to the string of the format specified
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider) => ToStringInternal(format, formatProvider);

        /// <summary>
        /// Converts object to the string of the format specified.
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
        public abstract ReticleElement Clone();
    }
}
