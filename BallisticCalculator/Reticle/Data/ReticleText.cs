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
        /// Position of the top-left corner of the text
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
    }
}
