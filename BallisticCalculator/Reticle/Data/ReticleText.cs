using System;
using System.Text;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// Text
    /// </summary>
    [BXmlElement("reticle-text")]
    public class ReticleText : ReticleElement
    {
        /// <summary>
        /// Position of the bottom-left corner of the text
        /// </summary>
        [BXmlProperty(Name = "position", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Position { get; set; }

        /// <summary>
        /// The height of the text
        /// </summary>
        [BXmlProperty(Name = "text-height")]
        public Measurement<AngularUnit> TextHeight { get; set; }

        /// <summary>
        /// The text
        /// </summary>
        [BXmlProperty(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// The text
        /// </summary>
        [BXmlProperty(Name = "anchor", Optional = true)]
        public TextAnchor? Anchor { get; set; }

        /// <summary>
        /// <para>The text color.</para>
        /// <para>The value is an <see href="https://www.w3schools.com/colors/colors_names.asp">html color name</see></para>
        /// <para>If no value is, a black color will be used</para>
        /// </summary>
        [BXmlProperty(Name = "text-color", Optional = true)]
        public string Color { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleText() : base(ReticleElementType.Text)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticleElement other)
        {
            if (other is ReticleText text)
                return Equals(Position, text.Position) &&
                    Equals(TextHeight, text.TextHeight) &&
                    Equals(Text, text.Text) &&
                    Equals(Color, text.Color);

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
            sb.Append("Text(")
                .Append("p=")
                .Append(Position.ToString(format, formatProvider))
                .Append(",h=")
                .Append(TextHeight.ToString(format, formatProvider))
                .Append(",t=")
                .Append(Text)
                .Append(",c=")
                .Append(Color ?? "null")
                .Append(",a=")
                .Append((Anchor?.ToString()) ?? "default")
                .Append(')');

            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashUtil.HashCombine(Position, TextHeight, Text, Color, Anchor);
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticleElement Clone()
        {
            return new ReticleText()
            {
                Position = this.Position.Clone(),
                TextHeight = this.TextHeight,
                Text = this.Text,
                Color = this.Color,
                Anchor = this.Anchor,
            };
        }
    }
}
