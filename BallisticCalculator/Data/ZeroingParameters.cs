using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class ZeroingParameters
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Ammunition Ammunition { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Atmosphere Atmosphere { get; set; }
        public Measurement<DistanceUnit> Distance { get; set; }

        public ZeroingParameters()
        {

        }

        [JsonConstructor]
        public ZeroingParameters(Measurement<DistanceUnit> distance, Ammunition ammunition, Atmosphere atmosphere)
        {
            Ammunition = ammunition;
            Atmosphere = atmosphere;
            Distance = distance;
        }

    }

}

