using System;
using System.Text;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A line
    /// </summary>
    [BXmlElement("reticle-line")]
    public class ReticleLine : ReticleElement
    {
        /// <summary>
        /// Position of the line start
        /// </summary>
        [BXmlProperty(Name = "start", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Start { get; set; }

        /// <summary>
        /// Position of the line end
        /// </summary>
        [BXmlProperty(Name = "end", ChildElement = true, FlattenChild = true)]
        public ReticlePosition End { get; set; }

        /// <summary>
        /// <para>The flag indicating the line width</para>
        /// <para>If no value is set, the smallest possible line width will be used</para>
        /// </summary>
        [BXmlProperty(Name = "line-width", Optional = true)]
        public Measurement<AngularUnit>? LineWidth { get; set; }

        /// <summary>
        /// <para>The line color.</para>
        /// <para>The value is an <see href="https://www.w3schools.com/colors/colors_names.asp">html color name</see></para>
        /// <para>If no value is, a black color will be used</para>
        /// </summary>
        [BXmlProperty(Name = "line-color", Optional = true)]
        public string Color { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleLine() : base(ReticleElementType.Line)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticleElement other)
        {
            if (other is ReticleLine line)
                return object.Equals(Start, line.Start) &&
                    object.Equals(End, line.End) &&
                    object.Equals(LineWidth, line.LineWidth) &&
                    object.Equals(Color, line.Color);

            return false;
        }

        /// <summary>
        /// Converts object to the string of the format specified.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        protected override string ToStringInternal(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Line(")
                .Append("s=")
                .Append(Start.ToString(format, formatProvider))
                .Append(",e=")
                .Append(End.ToString(format, formatProvider))
                .Append(",w=")
                .Append(LineWidth?.ToString(format, formatProvider) ?? "null")
                .Append(",c=")
                .Append(Color ?? "null")
                .Append(')');
            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashUtil.HashCombine(Start, End, LineWidth, Color);
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticleElement Clone()
        {
            return new ReticleLine()
            {
                Start = this.Start.Clone(),
                End = this.End.Clone(),
                Color = this.Color,
                LineWidth = this.LineWidth,
            };
        }
    }
}
