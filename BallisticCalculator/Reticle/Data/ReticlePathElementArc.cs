using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// Arc path element
    /// </summary>
    [BXmlElement("reticle-path-arc")]
    public class ReticlePathElementArc : ReticlePathElement
    {
        /// <summary>
        /// The radius of the arc
        /// </summary>
        [BXmlProperty(Name = "radius")]
        public Measurement<AngularUnit> Radius { get; set; }

        /// <summary>
        /// The clockwise or counterclockwise direction of the arc
        /// </summary>
        [BXmlProperty(Name = "clockwise")]
        public bool ClockwiseDirection { get; set; }

        /// <summary>
        /// The flag indicating whether major or minor arc should be used
        /// </summary>
        [BXmlProperty(Name = "major-arc")]
        public bool MajorArc { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ReticlePathElementArc() : base(ReticlePathElementType.Arc)
        {
        }
    }
}
