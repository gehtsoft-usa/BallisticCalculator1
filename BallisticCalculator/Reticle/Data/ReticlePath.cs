using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System.Collections;
using System.Collections.Generic;

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
        [BXmlProperty(Name = "line-color", Optional = true)]
        public string LineColor { get; set; }

        /// <summary>
        /// <para>A fill color</para>
        /// <para>The value is an <see href="https://www.w3schools.com/colors/colors_names.asp">html color name</see></para>
        /// <para>If no value is set, a fill color will be used </para>
        /// </summary>
        [BXmlProperty(Name = "fill-color", Optional = true)]
        public string FillColor { get; set; }

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
    }
}
