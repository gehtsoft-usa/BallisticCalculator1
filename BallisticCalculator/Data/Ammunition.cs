using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class Ammunition
    {
        public Measurement<WeightUnit> Weight { get; set; }
        public BallisticCoefficient BallisticCoefficient { get; set; }
        public Measurement<VelocityUnit> MuzzleVelocity { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? BulletDiameter { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Measurement<DistanceUnit>? BulletLength { get; set; }

        public Ammunition()
        {

        }

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

