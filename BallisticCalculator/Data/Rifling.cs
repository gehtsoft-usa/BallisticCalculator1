using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The specification of the rifling
    /// </summary>
    public class Rifling
    {
        /// <summary>
        /// Rifling step 
        /// 
        /// Rifling step is the distance at which the rifling makes on full revolution, 
        /// e.g. AR-15 1:12 barrel means 1 full revolution in 12 inches. 
        /// </summary>
        public Measurement<DistanceUnit> RiflingStep { get; set; }

        /// <summary>
        /// Twist direction
        /// </summary>
        public TwistDirection Direction { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Rifling()
        {

        }

        /// <summary>
        /// Parameterized/serialization constructor
        /// </summary>
        /// <param name="riflingStep"></param>
        /// <param name="direction"></param>
        [JsonConstructor]
        public Rifling(Measurement<DistanceUnit> riflingStep, TwistDirection direction)
        {
            RiflingStep = riflingStep;
            Direction = direction;
        }
    }

}

