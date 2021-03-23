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
        /// Constructor for the coordinates specified as numbers
        /// </summary>
        /// <param name="x">The x-coordinate</param>
        /// <param name="y">The y-coordinate</param>
        /// <param name="unit">The unit. </param>
        public ReticlePosition(double x, double y, AngularUnit unit)
        {
            X = unit.New(x);
            Y = unit.New(y);
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
