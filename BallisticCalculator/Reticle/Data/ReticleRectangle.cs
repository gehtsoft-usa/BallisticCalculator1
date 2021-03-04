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
        /// The flag indicating whether the path needs to be filled
        /// 
        /// Default value is no.
        /// </summary>
        [BXmlProperty(Name = "fill", Optional = true)]
        public bool? Fill { get; set; }

        /// <summary>
        /// The flag indicating the line width
        /// 
        /// If no value is set, the smallest possible line width will be used
        /// </summary>
        [BXmlProperty(Name = "line-width", Optional = true)]
        public Measurement<AngularUnit>? LineWidth { get; set; }

        /// <summary>
        /// The line color.
        /// 
        /// The value is an <see href="https://www.w3schools.com/colors/colors_names.asp">html color name</see>
        /// 
        /// If no value is, a black color will be used
        /// </summary>
        [BXmlProperty(Name = "line-color", Optional = true)]
        public string LineColor { get; set; }

        /// <summary>
        /// A fill color
        /// 
        /// The value is an <see href="https://www.w3schools.com/colors/colors_names.asp">html color name</see>
        /// 
        /// If no value is set, a fill color will be used 
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
