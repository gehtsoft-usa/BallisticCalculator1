using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class Wind
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? MaximumRange { get; set; }
        public Measurement<VelocityUnit> Velocity { get; set; }
        public Measurement<AngularUnit> Direction { get; set; }

        [JsonIgnore]
        internal Wind Next { get; set; }

        public Wind()
        {

        }

        [JsonConstructor]
        public Wind(Measurement<VelocityUnit> velocity, Measurement<AngularUnit> direction, Measurement<DistanceUnit>? maximumRange = null)
        {
            Velocity = velocity;
            Direction = direction;
            MaximumRange = maximumRange;
        }
    }

}

