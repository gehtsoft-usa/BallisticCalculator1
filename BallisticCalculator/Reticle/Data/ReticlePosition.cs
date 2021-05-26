using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A position on the reticle
    /// </summary>
    [BXmlElement("position")]
    public sealed class ReticlePosition : IEquatable<ReticlePosition>, IFormattable, ICloneable
    {
        /// <summary>
        /// X-coordinate
        /// </summary>
        [BXmlProperty("x")]
        public Measurement<AngularUnit> X { get; set; }

        /// <summary>
        /// X-coordinate
        /// </summary>
        [BXmlProperty("y")]
        public Measurement<AngularUnit> Y { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReticlePosition()
        {
        }

        /// <summary>
        /// Constructor for the coordinates specified as numbers
        /// </summary>
        /// <param name="x">The x-coordinate</param>
        /// <param name="y">The y-coordinate</param>
        /// <param name="unit">The unit. </param>
        public ReticlePosition(double x, double y, AngularUnit unit)
        {
            X = unit.New(x);
            Y = unit.New(y);
        }

        /// <summary>
        /// Constructor for the specified position coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public ReticlePosition(Measurement<AngularUnit> x, Measurement<AngularUnit> y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Checks whether the class equals to another class
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ReticlePosition other)
        {
            if (other == null)
                return false;
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Checks whether the class equals to another class
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ReticlePosition t)
                return Equals(t);
            return object.ReferenceEquals(obj, this);
        }

        /// <summary>
        /// Returns hash code of the object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => HashUtil.HashCombine(X, Y);

        /// <summary>
        /// Converts the object to string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString("NF", CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the object to string representation with the format and culture specified
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('(')
                .Append(X.ToString(format, formatProvider))
                .Append(':')
                .Append(Y.ToString(format, formatProvider))
                .Append(')');
            return sb.ToString();
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public ReticlePosition Clone()
        {
            return new ReticlePosition()
            {
                X = this.X,
                Y = this.Y
            };
        }

        object ICloneable.Clone() => Clone();
    }
}