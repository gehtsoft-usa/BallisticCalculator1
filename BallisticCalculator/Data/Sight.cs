using Gehtsoft.Measurements;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class Sight
    {
        public Measurement<DistanceUnit> SightHeight { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? VerticalClick { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? HorizontalClick { get; set; }

        public Sight()
        {

        }

        [JsonConstructor]
        public Sight(Measurement<DistanceUnit> sightHeight, Measurement<AngularUnit> verticalClick, Measurement<AngularUnit> horizontalClick)
        {
            SightHeight = sightHeight;
            VerticalClick = verticalClick;
            HorizontalClick = horizontalClick;
        }
    }

}

