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
        /// <para>The file name of the custom drag table</para>
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BXmlProperty("custom-table", Optional = true)]
        public string CustomTableFileName { get; set; }

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

        /// <summary>
        /// Return the ballistic coefficient
        /// 
        /// It the BC specified is a coefficient, the method simply returns the corresponding value.
        /// If the BC specified is a form factor, the method calculates BC using form factor and sectional density.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException">The exception is thrown only when form-factor is used. The exception will be thrown if form factor, bullet weight or bullet diamter is not specified or is 0</exception>
        public double GetBallisticCoefficient()
        {
            if (this.BallisticCoefficient.ValueType == BallisticCoefficientValueType.Coefficient)
                return this.BallisticCoefficient.Value;
            else
            {
                if (this.BulletDiameter == null || this.BulletDiameter.Value.Value <= 0)
                    throw new ArgumentException("If form-factor is used, the bullet diameter must be set");

                if (this.BallisticCoefficient.Value <= 0)
                    throw new ArgumentException("Form factor should be greater than 0");

                if (this.Weight.Value <= 0)
                    throw new ArgumentException("If form-factor is used, the bullet weight must be set");

                return this.Weight.In(WeightUnit.Grain) / 7000.0 / Math.Pow(this.BulletDiameter.Value.In(DistanceUnit.Inch), 2) / this.BallisticCoefficient.Value;
            }
        }
    }
}

