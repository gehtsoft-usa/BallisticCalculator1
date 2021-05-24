using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// <para>Reticle path.</para>
    /// <para>Path is a sequence of drawing primitives - lines and arcs.</para>
    /// </summary>
    [BXmlElement("reticle-path")]
    public class ReticlePath : ReticleElement
    {
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
        /// The collection of reticle's elements
        /// </summary>
        [BXmlProperty(Name = "elements", Collection = true)]
        public ReticlePathElementsCollection Elements { get; } = new ReticlePathElementsCollection();

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePath() : base(ReticleElementType.Path)
        {
        }

        /// <summary>
        /// Checks whether the element equals to another elements
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected override bool EqualsInternal(ReticleElement other)
        {
            if (other is ReticlePath path)
            {
                if (!object.Equals(LineWidth, path.LineWidth) ||
                    !object.Equals(Color, path.Color) ||
                    !object.Equals(Fill, path.Fill))
                    return false;

                if (Elements.Count != path.Elements.Count)
                    return false;

                for (int i = 0; i < Elements.Count; i++)
                    if (!object.Equals(Elements[i], path.Elements[i]))
                        return false;

                return true;
            }
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

            sb.Append("Path(")
               .Append("w=")
               .Append(LineWidth?.ToString(format, formatProvider) ?? "null")
               .Append(",c=")
               .Append(Color ?? "null")
               .Append(",f=")
               .Append(Fill?.ToString(formatProvider).ToLower() ?? "null");

            if (Elements.Count > 0)
            {
                sb.Append(",[");
                for (int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(Elements[i].ToString());
                }
                sb.Append("]");
            }
            sb.Append(')');
            return sb.ToString();
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int c = HashUtil.HashCombine(LineWidth, Color, Fill);
            foreach (var element in Elements)
                c = HashUtil.CodeCombine(c, element.GetHashCode());
            return c;
        }

        /// <summary>Creates a new object that is a copy of the current instance.</summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public override ReticleElement Clone()
        {
            var copy = new ReticlePath()
            {
                Color = this.Color,
                Fill = this.Fill,
                LineWidth = this.LineWidth,
            };

            for (int i = 0; i < Elements.Count; i++)
                copy.Elements.Add(this.Elements[i].Clone());

            return copy;
        }
    }
}
