using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class ShotParameters
    {
        public Measurement<AngularUnit> SightAngle { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? CantAngle { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? ShotAngle { get; set; }
        public Measurement<DistanceUnit> Step { get; set; }
        public Measurement<DistanceUnit> MaximumDistance { get; set; }
    }
}
