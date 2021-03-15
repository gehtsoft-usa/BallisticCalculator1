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
        public ReticleRectangle() : base(ReticleElementType.Rectangle)
        {
        }
    }
}
