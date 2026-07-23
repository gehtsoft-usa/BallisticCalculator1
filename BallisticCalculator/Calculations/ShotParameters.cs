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
        /// <para>Vertical barrel elevation established by zeroing (formerly SightAngle).</para>
        /// <para>Computed by [clink=BallisticCalculator.TrajectoryCalculator.CalculateZeroParameters.KD8]CalculateZeroParameters[/clink]
        /// (together with [clink=BallisticCalculator.ShotParameters.ZeroWindageAdjustment.NZ]ZeroWindageAdjustment[/clink])
        /// and copied onto the shot by [clink=BallisticCalculator.ShotParameters.Apply.JkC]Apply[/clink].
        /// Positive points the barrel up.</para>
        /// </summary>
        [BXmlProperty("zero-drop-adjustment")]
        public Measurement<AngularUnit> ZeroDropAdjustment { get; set; }

        /// <summary>
        /// <para>Horizontal barrel angle established by zeroing.</para>
        /// <para>Positive tilts the barrel left, shifting impact toward positive (left) windage, so a
        /// positive value cancels a negative (right) drift, mirroring how [clink=BallisticCalculator.ShotParameters.ZeroDropAdjustment.So6]ZeroDropAdjustment[/clink]
        /// compensates drop. When omitted, treated as zero.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("zero-windage-adjustment", Optional = true)]
        public Measurement<AngularUnit>? ZeroWindageAdjustment { get; set; }

        /// <summary>
        /// <para>Additional vertical adjustment dialed on the sight for this shot (elevation clicks).</para>
        /// <para>Accumulated on top of [clink=BallisticCalculator.ShotParameters.ZeroDropAdjustment.So6]ZeroDropAdjustment[/clink]; positive raises impact. When omitted, treated as zero.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("shot-drop-adjustment", Optional = true)]
        public Measurement<AngularUnit>? ShotDropAdjustment { get; set; }

        /// <summary>
        /// <para>Additional horizontal adjustment dialed on the sight for this shot (windage clicks).</para>
        /// <para>Accumulated on top of [clink=BallisticCalculator.ShotParameters.ZeroWindageAdjustment.NZ]ZeroWindageAdjustment[/clink]; positive shifts impact toward
        /// positive (left) windage, so a positive value cancels a negative (right) drift. When omitted, treated as zero.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("shot-windage-adjustment", Optional = true)]
        public Measurement<AngularUnit>? ShotWindageAdjustment { get; set; }

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
        /// <para>The azimuth of the shot.</para>
        /// <para>Compass bearing of the line of fire (0 degrees is North, clockwise to 90 degrees East). Used to
        /// orient the Coriolis and Eötvös deflection; it does not tilt the trajectory itself.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("azimuth-angle", Optional = true)]
        public Measurement<AngularUnit>? BarrelAzimuth { get; set; }

        /// <summary>
        /// <para>Geographic latitude of the shot, used for the Coriolis / Eötvös deflection.</para>
        /// <para>North positive, South negative. When `null`, Earth rotation is ignored.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("latitude", Optional = true)]
        public Measurement<AngularUnit>? Latitude { get; set; }

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

        /// <summary>
        /// <para>Copies the calculated zeroing adjustments onto this shot.</para>
        /// <para>Sets [clink=BallisticCalculator.ShotParameters.ZeroDropAdjustment.So6]ZeroDropAdjustment[/clink]
        /// and [clink=BallisticCalculator.ShotParameters.ZeroWindageAdjustment.NZ]ZeroWindageAdjustment[/clink]
        /// from the given [clink=BallisticCalculator.ZeroCalculatedParameters]ZeroCalculatedParameters[/clink].</para>
        /// </summary>
        /// <param name="zero">The calculated zeroing parameters to apply.</param>
        public void Apply(ZeroCalculatedParameters zero)
        {
            ArgumentNullException.ThrowIfNull(zero);
            ZeroDropAdjustment = zero.ZeroDropAdjustment;
            ZeroWindageAdjustment = zero.ZeroWindageAdjustment;
        }
    }
}
