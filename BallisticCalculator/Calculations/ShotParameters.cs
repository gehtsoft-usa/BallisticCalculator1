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
        /// <para>Sight angle</para>
        /// <para>
        /// Use <see cref="TrajectoryCalculator.SightAngle(Ammunition, Rifle, Atmosphere, DragTable)">TrajectoryCalculator.SightAngle</see> to calculate
        /// sight angle by the zero distance
        /// </para>
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
        /// The azymuth of the shot
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("azimuth-angle", Optional = true)]
        public Measurement<AngularUnit>? BarrelAzymuth { get; set; }

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
