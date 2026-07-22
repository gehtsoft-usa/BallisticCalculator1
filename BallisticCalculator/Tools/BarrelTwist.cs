using System;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// <para>A recommended range of barrel twists for a projectile, from the slowest twist that still
    /// reaches a minimum gyroscopic stability to the fastest twist at an over-stable ceiling.</para>
    /// <para>Twist is a barrel step (distance per turn), so a slower twist is a longer step and a
    /// faster twist is a shorter step. Higher stability needs a faster twist, therefore
    /// [c]MinimumTwist &gt;= OptimalTwist &gt;= MaximumTwist[/c] as step lengths.</para>
    /// </summary>
    public sealed class TwistRecommendation
    {
        /// <summary>The slowest twist (longest step) that still reaches <see cref="MinimumStability"/>. A longer step under-stabilizes.</summary>
        public Measurement<DistanceUnit> MinimumTwist { get; }

        /// <summary>The twist (step) that reaches <see cref="OptimalStability"/> — the recommended choice.</summary>
        public Measurement<DistanceUnit> OptimalTwist { get; }

        /// <summary>The fastest twist (shortest step) at <see cref="MaximumStability"/>, i.e. the over-stable ceiling.</summary>
        public Measurement<DistanceUnit> MaximumTwist { get; }

        /// <summary>The minimum gyroscopic stability factor targeted by <see cref="MinimumTwist"/>.</summary>
        public double MinimumStability { get; }

        /// <summary>The optimal gyroscopic stability factor targeted by <see cref="OptimalTwist"/>.</summary>
        public double OptimalStability { get; }

        /// <summary>The maximum gyroscopic stability factor targeted by <see cref="MaximumTwist"/>.</summary>
        public double MaximumStability { get; }

        internal TwistRecommendation(Measurement<DistanceUnit> minimumTwist, double minimumStability,
                                     Measurement<DistanceUnit> optimalTwist, double optimalStability,
                                     Measurement<DistanceUnit> maximumTwist, double maximumStability)
        {
            MinimumTwist = minimumTwist; MinimumStability = minimumStability;
            OptimalTwist = optimalTwist; OptimalStability = optimalStability;
            MaximumTwist = maximumTwist; MaximumStability = maximumStability;
        }
    }

    /// <summary>
    /// <para>Recommends a barrel twist for a projectile from its gyroscopic stability requirement, and the
    /// inverse — the stability a given twist yields.</para>
    /// <para>Uses the same Miller stability model as the trajectory engine (weight, diameter, length,
    /// muzzle velocity, plus the velocity and temperature/pressure corrections), so a twist recommended
    /// here reproduces the target stability when fed back through the engine.</para>
    /// </summary>
    public static class BarrelTwist
    {
        /// <summary>
        /// <para>Returns the gyroscopic (Miller) stability factor for the projectile fired from a barrel with the given twist.</para>
        /// <para>Requires the bullet diameter and length. When the atmosphere is null the temperature/pressure correction is 1 (ICAO standard, 29.92 inHg / 59 F).</para>
        /// </summary>
        /// <param name="ammunition">The projectile. Bullet diameter and length are required.</param>
        /// <param name="riflingStep">The barrel twist as a step (distance per turn), for example 1 turn in 8 inches.</param>
        /// <param name="atmosphere">The atmosphere for the temperature/pressure correction. Null means ICAO standard.</param>
        public static double Stability(Ammunition ammunition, Measurement<DistanceUnit> riflingStep, Atmosphere atmosphere = null)
        {
            ValidateBullet(ammunition);
            double diameter = ammunition.BulletDiameter.Value.In(DistanceUnit.Inch);
            double t = riflingStep.In(DistanceUnit.Inch) / diameter;
            double l = ammunition.BulletLength.Value.In(DistanceUnit.Inch) / diameter;
            double sd = 30 * ammunition.Weight.In(WeightUnit.Grain)
                      / (t * t * diameter * diameter * diameter * l * (1 + l * l));
            return sd * VelocityFactor(ammunition) * PressureTemperatureFactor(atmosphere);
        }

        /// <summary>
        /// <para>Returns the barrel twist (step, distance per turn) needed to reach the target gyroscopic stability.</para>
        /// <para>This is the inverse of <see cref="Stability"/>: feeding the result back into it reproduces the target.</para>
        /// </summary>
        /// <param name="ammunition">The projectile. Bullet diameter and length are required.</param>
        /// <param name="targetStability">The desired gyroscopic stability factor. Must be greater than 0.</param>
        /// <param name="atmosphere">The atmosphere for the temperature/pressure correction. Null means ICAO standard.</param>
        public static Measurement<DistanceUnit> RecommendedTwist(Ammunition ammunition, double targetStability, Atmosphere atmosphere = null)
        {
            ValidateBullet(ammunition);
            if (targetStability <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetStability), "The target stability must be greater than zero");

            double diameter = ammunition.BulletDiameter.Value.In(DistanceUnit.Inch);
            double l = ammunition.BulletLength.Value.In(DistanceUnit.Inch) / diameter;
            double sdNeeded = targetStability / (VelocityFactor(ammunition) * PressureTemperatureFactor(atmosphere));
            // t = twist in calibers per turn; RiflingStep = t * diameter.
            double t = Math.Sqrt(30 * ammunition.Weight.In(WeightUnit.Grain)
                               / (sdNeeded * diameter * diameter * diameter * l * (1 + l * l)));
            return new Measurement<DistanceUnit>(t * diameter, DistanceUnit.Inch);
        }

        /// <summary>
        /// <para>Recommends the slowest, optimal, and fastest twists for the projectile, targeting the given
        /// minimum, optimal, and maximum gyroscopic stability factors.</para>
        /// <para>Defaults are grounded in Litz/Berger practice and cross-checked against published minimum
        /// twists: minimum 1.5 (the recommended floor for full stability and undegraded BC — below it the
        /// bullet flies but BC drops; this reproduces manufacturers' published minimum twists), optimal 2.0
        /// (a comfortable margin robust to cold/dense air, low muzzle velocity, and lot variation), maximum
        /// 3.0 (an over-stabilization ceiling — beyond it precision may suffer slightly). The theoretical
        /// stability threshold is 1.0, but it is not a usable recommendation.</para>
        /// </summary>
        /// <param name="ammunition">The projectile. Bullet diameter and length are required.</param>
        /// <param name="atmosphere">The atmosphere for the temperature/pressure correction. Null means ICAO standard.</param>
        /// <param name="minimumStability">Stability target for the slowest twist (longest step).</param>
        /// <param name="optimalStability">Stability target for the recommended twist.</param>
        /// <param name="maximumStability">Stability target for the fastest twist (shortest step).</param>
        public static TwistRecommendation Recommend(Ammunition ammunition, Atmosphere atmosphere = null,
            double minimumStability = 1.5, double optimalStability = 2.0, double maximumStability = 3.0)
        {
            if (!(minimumStability <= optimalStability && optimalStability <= maximumStability))
                throw new ArgumentException("Stability targets must satisfy minimum <= optimal <= maximum");

            return new TwistRecommendation(
                RecommendedTwist(ammunition, minimumStability, atmosphere), minimumStability,
                RecommendedTwist(ammunition, optimalStability, atmosphere), optimalStability,
                RecommendedTwist(ammunition, maximumStability, atmosphere), maximumStability);
        }

        private static double VelocityFactor(Ammunition ammunition) =>
            Math.Pow(ammunition.MuzzleVelocity.In(VelocityUnit.FeetPerSecond) / 2800.0, 1.0 / 3.0);

        private static double PressureTemperatureFactor(Atmosphere atmosphere)
        {
            if (atmosphere == null)
                return 1;
            double ft = atmosphere.Temperature.In(TemperatureUnit.Fahrenheit);
            double pt = atmosphere.Pressure.In(PressureUnit.InchesOfMercury);
            return ((ft + 460) / (59 + 460)) * (29.92 / pt);
        }

        private static void ValidateBullet(Ammunition ammunition)
        {
            ArgumentNullException.ThrowIfNull(ammunition);
            if (ammunition.BulletDiameter == null || ammunition.BulletLength == null)
                throw new ArgumentException("Bullet diameter and length are required to compute gyroscopic stability", nameof(ammunition));
        }
    }
}
