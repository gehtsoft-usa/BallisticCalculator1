using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// Parameters of the shot
    /// </summary>
    [BXmlElement("shot-parameters")]
    public class ShotParameters
    {
        /// <summary>
        /// Sight angle 
        /// 
        /// Use <see cref="TrajectoryCaculator.SightAngle(Ammunition, Rifle, Atmosphere)">TrajectoryCaculator.SightAngle</see> to calculate
        /// sight angle by the zero distance
        /// </summary>
        [BXmlProperty("sight-angle")]
        public Measurement<AngularUnit> SightAngle { get; set; }

        /// <summary>
        /// The angle at which the rifle is canted
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("cant-angle", Optional = true)]
        public Measurement<AngularUnit>? CantAngle { get; set; }
        
        /// <summary>
        /// The angle of the shot
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("shot-angle", Optional = true)]
        public Measurement<AngularUnit>? ShotAngle { get; set; }

        /// <summary>
        /// Output table step
        /// </summary>
        [BXmlProperty("step")]
        public Measurement<DistanceUnit> Step { get; set; }

        /// <summary>
        /// Output table maximum distance
        /// </summary>
        [BXmlProperty("maximum-distance")]
        public Measurement<DistanceUnit> MaximumDistance { get; set; }
    }
}
