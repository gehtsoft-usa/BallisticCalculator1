using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The definition of the rifle
    /// </summary>
    public class Rifle
    {
        /// <summary>
        /// Rifling specification
        /// 
        /// The value is needed only in case drift calculation is needed
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Rifling Rifling { get; set; }

        /// <summary>
        /// The sight specification
        /// </summary>
        public Sight Sight { get; set; }

        /// <summary>
        /// Zeroing parameters
        /// 
        /// These parameters are used only to calculate sight angle by
        /// <see cref="TrajectoryCaculator.SightAngle(Ammunition, Rifle, Atmosphere)">TrajectoryCaculator.SightAngle</see> method.
        /// </summary>
        public ZeroingParameters Zero { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Rifle()
        {

        }

        /// <summary>
        /// Parameterized/serialization constructor
        /// </summary>
        /// <param name="sight"></param>
        /// <param name="zero"></param>
        /// <param name="rifling"></param>
        [JsonConstructor]
        public Rifle(Sight sight, ZeroingParameters zero, Rifling rifling = null)
        {
            Sight = sight;
            Zero = zero;
            Rifling = rifling;
        }
    }

}

