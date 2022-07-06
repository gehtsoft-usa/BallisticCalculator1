using BallisticCalculator.Serialization;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The definition of the rifle
    /// </summary>
    [BXmlElement("rifle")]
    public class Rifle
    {
        /// <summary>
        /// <para>Rifling specification</para>
        /// <para>The value is needed only in case drift calculation is needed</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty(ChildElement = true, Optional = true)]
        public Rifling Rifling { get; set; }

        /// <summary>
        /// The sight specification
        /// </summary>
        [BXmlProperty(ChildElement = true)]
        public Sight Sight { get; set; }

        /// <summary>
        /// <para>Zeroing parameters</para>
        /// <para>
        /// These parameters are used only to calculate sight angle by
        /// <see cref="TrajectoryCalculator.SightAngle(Ammunition, Rifle, Atmosphere, DragTable)">TrajectoryCaculator.SightAngle</see> method.
        /// </para>
        /// </summary>
        [BXmlProperty(ChildElement = true)]
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

