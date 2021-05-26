using System;
using System.Text;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// Arc path element
    /// </summary>
    [BXmlElement("reticle-path-arc")]
    public class ReticlePathElementArc : ReticlePathElement
    {
        /// <summary>
        /// The radius of the arc
        /// </summary>
        [BXmlProperty(Name = "radius")]
        public Measurement<AngularUnit> Radius { get; set; }

        /// <summary>
        /// The clockwise or counterclockwise direction of the arc
        /// </summary>
        [BXmlProperty(Name = "clockwise")]
        public bool ClockwiseDirection { get; set; }

        /// <summary>
        /// The flag indicating whether major or minor arc should be used
        /// </summary>
        [BXmlProperty(Name = "major-arc")]
        public bool MajorArc { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePathElementArc() : base(ReticlePathElementType.Arc)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticlePathElement other)
        {
            if (other is ReticlePathElementArc arc)
                return Equals(Position, arc.Position) &&
                    Equals(Radius, arc.Radius) &&
                    Equals(MajorArc, arc.MajorArc) &&
                    Equals(ClockwiseDirection, arc.ClockwiseDirection);
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
            sb.Append("A(")
                .Append(Position.ToString(format, formatProvider))
                .Append(',')
                .Append(Radius.ToString(format, formatProvider))
                .Append(',')
                .Append(MajorArc ? "maj" : "min")
                .Append(',')
                .Append(ClockwiseDirection ? "cw" : "ccw")
                .Append(')');
            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashUtil.HashCombine(Position, Radius, MajorArc, ClockwiseDirection);
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticlePathElement Clone()
        {
            return new ReticlePathElementArc()
            {
                Position = this.Position.Clone(),
                Radius = this.Radius,
                ClockwiseDirection = this.ClockwiseDirection,
                MajorArc = this.MajorArc
            };
        }
    }
}
