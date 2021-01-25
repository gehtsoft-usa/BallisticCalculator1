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
    public class ShotParameters
    {
        /// <summary>
        /// Sight angle 
        /// 
        /// Use <see cref="TrajectoryCaculator.SightAngle(Ammunition, Rifle, Atmosphere)">TrajectoryCaculator.SightAngle</see> to calculate
        /// sight angle by the zero distance
        /// </summary>
        public Measurement<AngularUnit> SightAngle { get; set; }

        /// <summary>
        /// The angle at which the rifle is canted
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? CantAngle { get; set; }
        
        /// <summary>
        /// The angle of the shot
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<AngularUnit>? ShotAngle { get; set; }

        /// <summary>
        /// Output table step
        /// </summary>
        public Measurement<DistanceUnit> Step { get; set; }

        /// <summary>
        /// Output table maximum distance
        /// </summary>
        public Measurement<DistanceUnit> MaximumDistance { get; set; }
    }
}
