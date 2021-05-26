using System;
using System.Text;
using BallisticCalculator.Serialization;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// MoveTo path element
    /// </summary>
    [BXmlElement("reticle-path-move-to")]
    public class ReticlePathElementMoveTo : ReticlePathElement
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePathElementMoveTo() : base(ReticlePathElementType.MoveTo)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticlePathElement other)
        {
            if (other is ReticlePathElementMoveTo m)
                return Position.Equals(m.Position);
            return false;
        }

        /// <summary>
        /// Converts the object to the string of the format specified
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        protected override string ToStringInternal(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("M")
                .Append(Position.ToString(format, formatProvider));
            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticlePathElement Clone()
        {
            return new ReticlePathElementMoveTo()
            {
                Position = this.Position.Clone(),
            };
        }
    }
}
