using System;
using System.Text;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// Rectangle
    /// </summary>
    [BXmlElement("reticle-rectangle")]
    public class ReticleRectangle : ReticleElement
    {
        /// <summary>
        /// Position of the top-left corner
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition TopLeft { get; set; }

        /// <summary>
        /// The size of the rectangle
        /// </summary>
        [BXmlProperty(Name = "size", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Size { get; set; }

        /// <summary>
        /// <para>The flag indicating whether the path needs to be filled</para>
        /// <para>Default value is no.</para>
        /// </summary>
        [BXmlProperty(Name = "fill", Optional = true)]
        public bool? Fill { get; set; }

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
        [BXmlProperty(Name = "color", Optional = true)]
        public string Color { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleRectangle() : base(ReticleElementType.Rectangle)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticleElement other)
        {
            if (other is ReticleRectangle rectangle)
                return Equals(TopLeft, rectangle.TopLeft) &&
                    Equals(Size, rectangle.Size) &&
                    Equals(LineWidth, rectangle.LineWidth) &&
                    Equals(Color, rectangle.Color) &&
                    Equals(Fill, rectangle.Fill);

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
            sb.Append("Rectangle(")
                .Append("p=")
                .Append(TopLeft.ToString(format, formatProvider))
                .Append(",s=")
                .Append(Size.ToString(format, formatProvider))
                .Append(",w=")
                .Append(LineWidth?.ToString(format, formatProvider) ?? "null")
                .Append(",c=")
                .Append(Color ?? "null")
                .Append(",f=")
                .Append(Fill?.ToString(formatProvider).ToLower() ?? "null")
                .Append(')');

            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashUtil.HashCombine(TopLeft, Size, LineWidth, Color, Fill);
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticleElement Clone()
        {
            return new ReticleRectangle()
            {
                TopLeft = this.TopLeft.Clone(),
                Size = this.Size.Clone(),
                Color = this.Color,
                Fill = this.Fill,
                LineWidth = this.LineWidth,
            };
        }
    }
}
