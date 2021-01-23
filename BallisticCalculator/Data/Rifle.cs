using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class Rifle
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Rifling Rifling { get; set; }

        public Sight Sight { get; set; }

        public ZeroingParameters Zero { get; set; }

        public Rifle()
        {

        }

        [JsonConstructor]
        public Rifle(Sight sight, ZeroingParameters zero, Rifling rifling = null)
        {
            Sight = sight;
            Zero = zero;
            Rifling = rifling;
        }
    }

}

