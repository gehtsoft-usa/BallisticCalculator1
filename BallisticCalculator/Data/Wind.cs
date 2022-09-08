using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// Wind specification
    /// </summary>
    [BXmlElement("wind")]
    public class Wind
    {
        /// <summary>
        /// <para>Maximum range at which these winds condition are observed.</para>
        /// <para>
        /// The value is used to specify multiple winds along the trajectory. If multiple
        /// winds are specified, they must be sorted by the range in ascending order.
        /// </para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("maximum-range", Optional = true)]
        public Measurement<DistanceUnit>? MaximumRange { get; set; }

        /// <summary>
        /// Wind velocity
        /// </summary>
        [BXmlProperty("velocity")]
        public Measurement<VelocityUnit> Velocity { get; set; }

        /// <summary>
        /// Wind direction
        ///
        /// Directions:
        /// * 0 degrees   - wind toward the shooter
        /// * 90 degrees  - wind from the left of the shooter
        /// * 270/-90 degrees  - wind from the right of the shooter
        /// * 180 degrees  - wind toward the target
        /// </summary>
        [BXmlProperty("direction")]
        public Measurement<AngularUnit> Direction { get; set; }

        [JsonIgnore]
        internal Wind Next { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Wind()
        {
        }

        /// <summary>
        /// Parameterized/serialization constructor
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="direction"></param>
        /// <param name="maximumRange"></param>
        [JsonConstructor]
        public Wind(Measurement<VelocityUnit> velocity, Measurement<AngularUnit> direction, Measurement<DistanceUnit>? maximumRange = null)
        {
            Velocity = velocity;
            Direction = direction;
            MaximumRange = maximumRange;
        }
    }
}

