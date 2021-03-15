using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A circle
    /// </summary>
    [BXmlElement("reticle-circle")]
    public class ReticleCircle : ReticleElement
    {
        /// <summary>
        /// The center of the circle
        /// </summary>
        [BXmlProperty(Name = "center", ChildElement = true, FlattenChild = true)]
        public ReticlePosition Center { get; set; }

        /// <summary>
        /// Position of the line end
        /// </summary>
        [BXmlProperty(Name = "radius")]
        public Measurement<AngularUnit> Radius { get; set; }

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
        /// <para>If no value is set, a fill color will be used</para>
        /// </summary>
        [BXmlProperty(Name = "fill-color", Optional = true)]
        public string FillColor { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticleCircle() : base(ReticleElementType.Circle)
        {
        }
    }
}
