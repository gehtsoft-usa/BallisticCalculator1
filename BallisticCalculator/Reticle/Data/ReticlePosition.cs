using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Data
{
    /// <summary>
    /// A position on the reticle
    /// </summary>
    [BXmlElement("position")]
    public class ReticlePosition
    {
        /// <summary>
        /// X-coordinate
        /// </summary>
        [BXmlProperty("x")]
        public Measurement<AngularUnit> X { get; set; }

        /// <summary>
        /// X-coordinate
        /// </summary>
        [BXmlProperty("y")]
        public Measurement<AngularUnit> Y { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReticlePosition()
        {
        }

        /// <summary>
        /// Constructor for the specified position coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public ReticlePosition(Measurement<AngularUnit> x, Measurement<AngularUnit> y)
        {
            X = x;
            Y = y;
        }
    }
}
