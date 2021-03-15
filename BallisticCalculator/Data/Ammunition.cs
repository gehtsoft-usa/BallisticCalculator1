using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// Definition of a ammunition and projectile
    /// </summary>
    [BXmlElement("ammunition")]
    public class Ammunition
    {
        /// <summary>
        /// Projectile weight
        /// </summary>
        [BXmlProperty("bullet-weight")]
        public Measurement<WeightUnit> Weight { get; set; }

        /// <summary>
        /// Ballistic coefficient
        /// </summary>
        [BXmlProperty("ballistic-coefficient")]
        public BallisticCoefficient BallisticCoefficient { get; set; }

        /// <summary>
        /// Muzzle velocity
        /// </summary>
        [BXmlProperty("muzzle-velocity")]
        public Measurement<VelocityUnit> MuzzleVelocity { get; set; }

        /// <summary>
        /// <para>Diameter of the projectile</para>
        /// <para>The value is required only if drift calculation is needed.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("bullet-diameter", Optional = true)]
        public Measurement<DistanceUnit>? BulletDiameter { get; set; }

        /// <summary>
        /// <para>Length of the projectile</para>
        /// <para>The value is required only if drift calculation is needed.</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("bullet-length", Optional = true)]
        public Measurement<DistanceUnit>? BulletLength { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Ammunition()
        {
        }

        /// <summary>
        /// Serialization/parameterized constructor
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="ballisticCoefficient"></param>
        /// <param name="muzzleVelocity"></param>
        /// <param name="bulletDiameter"></param>
        /// <param name="bulletLength"></param>
        [JsonConstructor]
        public Ammunition(Measurement<WeightUnit> weight, BallisticCoefficient ballisticCoefficient, Measurement<VelocityUnit> muzzleVelocity,
                          Measurement<DistanceUnit>? bulletDiameter = null, Measurement<DistanceUnit>? bulletLength = null)
        {
            Weight = weight;
            BallisticCoefficient = ballisticCoefficient;
            MuzzleVelocity = muzzleVelocity;
            BulletDiameter = bulletDiameter;
            BulletLength = bulletLength;
        }
    }
}

