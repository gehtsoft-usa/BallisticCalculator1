using System;
using System.Collections.Generic;
using System.Linq;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// A single downrange velocity measurement (typical Doppler radar output): a velocity at a distance.
    /// </summary>
    public readonly struct RadarReading
    {
        /// <summary>
        /// The distance from the muzzle at which the velocity was measured.
        /// </summary>
        public Measurement<DistanceUnit> Distance { get; }

        /// <summary>
        /// The measured velocity at that distance.
        /// </summary>
        public Measurement<VelocityUnit> Velocity { get; }

        /// <summary>
        /// Constructs a radar reading from a distance and the velocity measured there.
        /// </summary>
        /// <param name="distance">The distance from the muzzle.</param>
        /// <param name="velocity">The velocity measured at that distance.</param>
        public RadarReading(Measurement<DistanceUnit> distance, Measurement<VelocityUnit> velocity)
        {
            Distance = distance;
            Velocity = velocity;
        }
    }

    /// <summary>
    /// <para>Builds a custom drag table from downrange velocity measurements (Doppler radar data).</para>
    /// <para>At each interval the retardation is recovered from the velocity decay and converted to a drag
    /// coefficient against Mach, inverting the engine's own drag law so the resulting table reproduces the
    /// measured velocities. The result is a [clink=BallisticCalculator.DrgDragTable]DrgDragTable[/clink] in the
    /// same form as a loaded drg file (a GC table with a form-factor of 1 plus the bullet weight and diameter),
    /// valid over the Mach range the data spans.</para>
    /// <para>The data is assumed to be flat fire in still air, so the whole speed change is drag. Provide the
    /// atmosphere the data was taken in; finer, cleaner spacing yields a better curve.</para>
    /// </summary>
    public static class RadarDragTableFactory
    {
        /// <summary>
        /// <para>Creates a custom drag table from radar readings and the bullet's weight and diameter.</para>
        /// <para>Readings are sorted by distance; velocity must strictly decrease with distance. At least
        /// three readings are required.</para>
        /// </summary>
        /// <param name="readings">The downrange velocity measurements.</param>
        /// <param name="bulletWeight">The bullet weight.</param>
        /// <param name="bulletDiameter">The bullet diameter.</param>
        /// <param name="atmosphere">The atmosphere the data was measured in; when null, a sea-level standard atmosphere is used.</param>
        /// <param name="name">An optional name for the resulting ammunition entry.</param>
        /// <returns>A custom drag table reproducing the measured velocity decay.</returns>
        /// <exception cref="ArgumentNullException">The readings are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The weight or diameter is not positive.</exception>
        /// <exception cref="ArgumentException">There are fewer than three readings, or velocity does not strictly decrease with distance.</exception>
        public static DrgDragTable Create(IEnumerable<RadarReading> readings, Measurement<WeightUnit> bulletWeight,
            Measurement<DistanceUnit> bulletDiameter, Atmosphere atmosphere = null, string name = null)
        {
            ArgumentNullException.ThrowIfNull(readings);
            double weightGrains = bulletWeight.In(WeightUnit.Grain);
            double diameterInch = bulletDiameter.In(DistanceUnit.Inch);
            if (weightGrains <= 0)
                throw new ArgumentOutOfRangeException(nameof(bulletWeight), "The bullet weight must be positive.");
            if (diameterInch <= 0)
                throw new ArgumentOutOfRangeException(nameof(bulletDiameter), "The bullet diameter must be positive.");

            atmosphere ??= new Atmosphere();

            var sorted = readings.OrderBy(x => x.Distance.In(DistanceUnit.Meter)).ToArray();
            if (sorted.Length < 3)
                throw new ArgumentException("At least three radar readings are required.", nameof(readings));

            // Density ratio and speed of sound are taken at the shot altitude and held constant over the
            // (flat, short) measured span, matching how the engine evaluates them along a level path.
            atmosphere.AtAltitude(atmosphere.Altitude, out double densityFactor, out Measurement<VelocityUnit> sound);
            double soundFps = sound.In(VelocityUnit.FeetPerSecond);

            // Sectional density is the ballistic coefficient the engine derives from a form-factor of 1; the
            // drag law is retardation = PIR * (1/SD) * densityFactor * Cd * v^2, so Cd = R * SD / (PIR * df * v^2).
            double sectionalDensity = weightGrains / 7000.0 / (diameterInch * diameterInch);

            var points = new List<DragTableDataPoint>(sorted.Length - 1);
            for (int i = 0; i < sorted.Length - 1; i++)
            {
                double x1 = sorted[i].Distance.In(DistanceUnit.Foot);
                double x2 = sorted[i + 1].Distance.In(DistanceUnit.Foot);
                double v1 = sorted[i].Velocity.In(VelocityUnit.FeetPerSecond);
                double v2 = sorted[i + 1].Velocity.In(VelocityUnit.FeetPerSecond);

                if (x2 <= x1)
                    throw new ArgumentException("Radar readings must be at distinct, increasing distances.", nameof(readings));
                if (v2 >= v1)
                    throw new ArgumentException("Velocity must strictly decrease with distance (flat fire, still air).", nameof(readings));

                double vMid = 0.5 * (v1 + v2);
                double retardation = -vMid * (v2 - v1) / (x2 - x1);      // ft/s^2, positive
                double mach = vMid / soundFps;
                double cd = retardation * sectionalDensity / (TrajectoryCalculator.PIR * densityFactor * vMid * vMid);
                points.Add(new DragTableDataPoint(mach, cd));
            }

            // Ascending Mach for the interpolating table (readings are sorted by increasing distance,
            // hence decreasing Mach).
            points.Reverse();

            var entry = new AmmunitionLibraryEntry
            {
                Name = name ?? "radar",
                Source = "radar data",
                Ammunition = new Ammunition
                {
                    BallisticCoefficient = new BallisticCoefficient(1, DragTableId.GC, BallisticCoefficientValueType.FormFactor),
                    Weight = bulletWeight,
                    BulletDiameter = bulletDiameter,
                    MuzzleVelocity = sorted[0].Velocity,
                }
            };

            return new DrgDragTable(points.ToArray(), entry);
        }
    }
}
