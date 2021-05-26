using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Globalization;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>The information about bullet drop compensator point</para>
    /// <para>
    /// Typically, an applications shows a distance that corresponds to the
    /// specific point.
    /// </para>
    /// </summary>
    [BXmlElement("bdc")]
    public sealed class ReticleBulletDropCompensatorPoint : IEquatable<ReticleBulletDropCompensatorPoint>, IFormattable, ICloneable
    {
        /// <summary>
        /// Position of the BDC point at reticle
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// <para>Offset of the text from the BDC point</para>
        /// <para>Positive value means text at right of BDC, negative - text at left of BDC</para>
        /// </summary>
        [BXmlProperty("text-offset")]
        public Measurement<AngularUnit> TextOffset { get; set; }

        /// <summary>
        /// The height of the BDC text
        /// </summary>
        [BXmlProperty("text-height")]
        public Measurement<AngularUnit> TextHeight { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleBulletDropCompensatorPoint()
        {
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(ReticleBulletDropCompensatorPoint other)
        {
            if (other == null)
                return false;

            return Equals(Position, other.Position) &&
                Equals(TextOffset, other.TextOffset) &&
                Equals(TextHeight, other.TextHeight);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ReticleBulletDropCompensatorPoint other)
                return Equals(other);
            return object.ReferenceEquals(this, obj);
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashUtil.HashCombine(Position, TextOffset, TextHeight);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => ToString("NF", CultureInfo.InvariantCulture);

        /// <summary>Formats the value of the current instance using the specified format.</summary>
        /// <param name="format">The format to use.   -or-   A null reference (Nothing in Visual Basic) to use the default format defined for the type of the <see cref="IFormattable"></see> implementation.</param>
        /// <param name="formatProvider">The provider to use to format the value.   -or-   A null reference (Nothing in Visual Basic) to obtain the numeric format information from the current locale setting of the operating system.</param>
        /// <returns>The value of the current instance in the specified format.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Bdc(")
                .Append("p=")
                .Append(Position.ToString(format, formatProvider))
                .Append(",o=")
                .Append(TextOffset.ToString(format, formatProvider))
                .Append(",h=")
                .Append(TextHeight.ToString(format, formatProvider))
                .Append(')');
            return sb.ToString();
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticleBulletDropCompensatorPoint Clone()
        {
            return new ReticleBulletDropCompensatorPoint()
            {
                Position = this.Position.Clone(),
                TextHeight = this.TextHeight,
                TextOffset = this.TextOffset
            };
        }
    }
}
