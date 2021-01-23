using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class Rifling
    {
        public Measurement<DistanceUnit> RiflingStep { get; set; }

        public TwistDirection Direction { get; set; }

        public Rifling()
        {

        }

        [JsonConstructor]
        public Rifling(Measurement<DistanceUnit> riflingStep, TwistDirection direction)
        {
            RiflingStep = riflingStep;
            Direction = direction;
        }
    }

}

