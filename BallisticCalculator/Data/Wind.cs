using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// Wind specification
    /// </summary>
    public class Wind
    {
        /// <summary>
        /// Maximum range at which these winds condition are observed.
        /// 
        /// The value is used to specify multiple winds along the trajectory. If multiple 
        /// winds are specified, they must be sorted by the range in ascending order.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? MaximumRange { get; set; }

        /// <summary>
        /// Wind velocity
        /// </summary>
        public Measurement<VelocityUnit> Velocity { get; set; }

        /// <summary>
        /// Wind direction
        /// </summary>
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

