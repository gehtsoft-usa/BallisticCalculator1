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
        public string LineColor { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleLine() : base(ReticleElementType.Line)
        {
        }
    }
}
