using System;
using System.Collections.Generic;
using System.Linq;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// The error inputs for a [clink=BallisticCalculator.Tools.HitProbability]HitProbability[/clink] estimate.
    /// </summary>
    public sealed class HitProbabilityParameters
    {
        /// <summary>
        /// The target size (diameter of a circular vital zone); a shot hits when it lands within half of it from the center.
        /// </summary>
        public Measurement<DistanceUnit> TargetSize { get; set; }

        /// <summary>
        /// The muzzle velocity standard deviation, in percent of the muzzle velocity.
        /// </summary>
        public double MuzzleVelocityDeviationPercent { get; set; }

        /// <summary>
        /// <para>The shooter's group size (angular), taken directly as the per-axis standard deviation of the aim.</para>
        /// <para>Use the best group of up to about ten shots from a fully supported position; that is close to a
        /// one-standard-deviation group (about 4 MOA for an ordinary shooter and rifle, 1 MOA for a precision
        /// setup). The extreme spread of a large group is roughly four times this. Shooting positions widen it
        /// through [c]HorizontalPositionMultiplier[/c] and [c]VerticalPositionMultiplier[/c].</para>
        /// </summary>
        public Measurement<AngularUnit> GroupSize { get; set; }

        /// <summary>
        /// <para>Widens the horizontal aim scatter relative to the fully supported group, for the shooting position.</para>
        /// <para>The default is 1 (fully supported). Rough values relative to supported: prone 2, kneeling 4, standing 5.</para>
        /// </summary>
        public double HorizontalPositionMultiplier { get; set; } = 1;

        /// <summary>
        /// <para>Widens the vertical aim scatter relative to the fully supported group, for the shooting position.</para>
        /// <para>The default is 1 (fully supported). Rough values relative to supported: prone 2, kneeling 3, standing 4.</para>
        /// </summary>
        public double VerticalPositionMultiplier { get; set; } = 1;

        /// <summary>
        /// The distance estimation error, as a one-standard-deviation percent of the range.
        /// </summary>
        public double DistanceErrorPercent { get; set; }

        /// <summary>
        /// The wind estimation error, as a one-standard-deviation percent of the wind speed.
        /// </summary>
        public double WindErrorPercent { get; set; }

        /// <summary>
        /// The number of shots to simulate; the default is 1000.
        /// </summary>
        public int Shots { get; set; } = 1000;

        /// <summary>
        /// An optional random seed for reproducible runs; when null the run is non-deterministic.
        /// </summary>
        public int? Seed { get; set; }
    }

    /// <summary>
    /// A single simulated shot's impact relative to the center of the target (positive is left and up, matching windage and drop).
    /// </summary>
    public readonly struct ShotImpact
    {
        /// <summary>The horizontal miss from center (positive left).</summary>
        public Measurement<DistanceUnit> Horizontal { get; }

        /// <summary>The vertical miss from center (positive up).</summary>
        public Measurement<DistanceUnit> Vertical { get; }

        /// <summary>Constructs a shot impact from its horizontal and vertical miss.</summary>
        /// <param name="horizontal">Horizontal miss (positive left).</param>
        /// <param name="vertical">Vertical miss (positive up).</param>
        public ShotImpact(Measurement<DistanceUnit> horizontal, Measurement<DistanceUnit> vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }

    /// <summary>
    /// The result of a [clink=BallisticCalculator.Tools.HitProbability]HitProbability[/clink] estimate.
    /// </summary>
    public sealed class HitProbabilityResult
    {
        /// <summary>
        /// The simulated shot impacts relative to the center of the target.
        /// </summary>
        public IReadOnlyList<ShotImpact> Shots { get; }

        /// <summary>
        /// The single-shot hit probability (fraction from 0 to 1).
        /// </summary>
        public double HitProbability { get; }

        /// <summary>The minimum number of shots to hit at least once with 50% probability, or null when a hit is impossible.</summary>
        public int? ShotsFor50Percent { get; }

        /// <summary>The minimum number of shots to hit at least once with 75% probability, or null when a hit is impossible.</summary>
        public int? ShotsFor75Percent { get; }

        /// <summary>The minimum number of shots to hit at least once with 90% probability, or null when a hit is impossible.</summary>
        public int? ShotsFor90Percent { get; }

        /// <summary>The minimum number of shots to hit at least once with 95% probability, or null when a hit is impossible.</summary>
        public int? ShotsFor95Percent { get; }

        /// <summary>The minimum number of shots to hit at least once with 98% probability, or null when a hit is impossible.</summary>
        public int? ShotsFor98Percent { get; }

        internal HitProbabilityResult(IReadOnlyList<ShotImpact> shots, double hitProbability)
        {
            Shots = shots;
            HitProbability = hitProbability;
            ShotsFor50Percent = ShotsToHit(hitProbability, 0.50);
            ShotsFor75Percent = ShotsToHit(hitProbability, 0.75);
            ShotsFor90Percent = ShotsToHit(hitProbability, 0.90);
            ShotsFor95Percent = ShotsToHit(hitProbability, 0.95);
            ShotsFor98Percent = ShotsToHit(hitProbability, 0.98);
        }

        // n such that 1 - (1-p)^n >= target  =>  n = ceil( ln(1-target) / ln(1-p) ).
        private static int? ShotsToHit(double p, double target)
        {
            if (p <= 0)
                return null;
            if (p >= 1)
                return 1;
            return (int)Math.Ceiling(Math.Log(1 - target) / Math.Log(1 - p));
        }
    }

    /// <summary>
    /// <para>Estimates the hit probability against a target by Monte Carlo over the shooter's error budget.</para>
    /// <para>Each simulated shot perturbs muzzle velocity, range estimate, wind estimate and aim (the shooter's
    /// group), then lands where the (mis-)dialed hold puts it relative to the target center. Nominal (zero-error)
    /// conditions land dead center, so the tool assumes the come-up and wind hold are correct for the estimated
    /// range and wind. Muzzle velocity and wind enter through the trajectory's sensitivities and range through
    /// the actual drop curve, all precomputed from a few trajectory runs.</para>
    /// </summary>
    public static class HitProbability
    {
        private const double MvPerturb = 0.05;   // reference muzzle-velocity perturbation for the sensitivity runs

        /// <summary>
        /// <para>Estimates hit probability for the given shot and error budget. The target is at the shot's maximum distance.</para>
        /// </summary>
        /// <param name="calculator">The trajectory calculator.</param>
        /// <param name="ammunition">The ammunition.</param>
        /// <param name="atmosphere">The atmosphere; when null a sea-level standard atmosphere is used.</param>
        /// <param name="rifle">The rifle.</param>
        /// <param name="shot">The shot parameters; its maximum distance is the target range.</param>
        /// <param name="wind">The true wind (the shooter estimates it with error); may be null.</param>
        /// <param name="parameters">The target size and error budget.</param>
        /// <param name="dragTable">Custom drag table (required when the ballistic coefficient table is GC).</param>
        /// <returns>The simulated impacts, hit probability, and minimum shot counts.</returns>
        public static HitProbabilityResult Estimate(TrajectoryCalculator calculator, Ammunition ammunition,
            Atmosphere atmosphere, Rifle rifle, ShotParameters shot, Wind[] wind,
            HitProbabilityParameters parameters, DragTable dragTable = null)
        {
            ArgumentNullException.ThrowIfNull(calculator);
            ArgumentNullException.ThrowIfNull(ammunition);
            ArgumentNullException.ThrowIfNull(rifle);
            ArgumentNullException.ThrowIfNull(shot);
            ArgumentNullException.ThrowIfNull(parameters);
            atmosphere ??= new Atmosphere();

            double radius = parameters.TargetSize.In(DistanceUnit.Meter) / 2.0;
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(parameters), "The target size must be positive.");
            if (parameters.Shots < 1)
                throw new ArgumentOutOfRangeException(nameof(parameters), "The number of shots must be at least one.");
            if (parameters.MuzzleVelocityDeviationPercent < 0 || parameters.DistanceErrorPercent < 0 || parameters.WindErrorPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(parameters), "Error percentages must not be negative.");
            if (parameters.HorizontalPositionMultiplier <= 0 || parameters.VerticalPositionMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(parameters), "Position multipliers must be positive.");

            double sigMv = parameters.MuzzleVelocityDeviationPercent / 100.0;
            double sigRange = parameters.DistanceErrorPercent / 100.0;
            double sigWind = parameters.WindErrorPercent / 100.0;

            double range = shot.MaximumDistance.In(DistanceUnit.Meter);
            // Curves must extend past the target so an over-estimated range still interpolates.
            double curveMaxYd = shot.MaximumDistance.In(DistanceUnit.Yard) * (1 + 5 * sigRange) + 1;
            var curveShot = CloneShot(shot,
                step: new Measurement<DistanceUnit>(curveMaxYd / 400.0, DistanceUnit.Yard),
                maximumDistance: new Measurement<DistanceUnit>(curveMaxYd, DistanceUnit.Yard));

            var withWind = calculator.Calculate(ammunition, rifle, atmosphere, curveShot, wind, dragTable);
            var noWind = calculator.Calculate(ammunition, rifle, atmosphere, curveShot, null, dragTable);
            var mvShifted = calculator.Calculate(PerturbMuzzleVelocity(ammunition, 1 + MvPerturb), rifle, atmosphere, curveShot, wind, dragTable);

            var rW = Ranges(withWind); var dropW = Values(withWind, p => p.Drop); var windW = Values(withWind, p => p.Windage);
            var rNW = Ranges(noWind); var windNW = Values(noWind, p => p.Windage);
            var rMv = Ranges(mvShifted); var dropMv = Values(mvShifted, p => p.Drop); var windMv = Values(mvShifted, p => p.Windage);

            // Values at the (fixed) target range.
            double dropAtR = Interp(rW, dropW, range);
            double windAtR = Interp(rW, windW, range);
            double mvDrop = Interp(rMv, dropMv, range) - dropAtR;        // drop change for +MvPerturb of MV
            double mvWind = Interp(rMv, windMv, range) - windAtR;

            // Per-axis aim standard deviation at the target: the supported group scaled by the position.
            double groupLinearSd = Math.Tan(parameters.GroupSize.In(AngularUnit.Radian)) * range;
            double groupSdHorizontal = groupLinearSd * parameters.HorizontalPositionMultiplier;
            double groupSdVertical = groupLinearSd * parameters.VerticalPositionMultiplier;

            var rng = parameters.Seed.HasValue ? new Random(parameters.Seed.Value) : new Random();
            var shots = new ShotImpact[parameters.Shots];
            int hits = 0;

            for (int i = 0; i < parameters.Shots; i++)
            {
                double eMv = Gaussian(rng), eRange = Gaussian(rng), eWind = Gaussian(rng);
                double gx = Gaussian(rng), gy = Gaussian(rng);

                double rEst = range * (1 + sigRange * eRange);
                if (rEst < range * 0.05)
                    rEst = range * 0.05;
                double mvScale = sigMv * eMv / MvPerturb;   // fraction of the reference perturbation

                // Vertical: actual drop at the target minus the come-up dialed for the estimated range.
                double dropActual = dropAtR + mvDrop * mvScale;
                double dropDial = Interp(rW, dropW, rEst) * (range / rEst);
                double missV = dropActual - dropDial + groupSdVertical * gy;

                // Horizontal: actual windage (true wind) minus the hold dialed for the estimated wind and range.
                double windActual = windAtR + mvWind * mvScale;
                double noWindEst = Interp(rNW, windNW, rEst);
                double driftEst = Interp(rW, windW, rEst) - noWindEst;                 // wind-induced part at rEst
                double windDial = (noWindEst + driftEst * (1 + sigWind * eWind)) * (range / rEst);
                double missH = windActual - windDial + groupSdHorizontal * gx;

                shots[i] = new ShotImpact(
                    new Measurement<DistanceUnit>(missH, DistanceUnit.Meter),
                    new Measurement<DistanceUnit>(missV, DistanceUnit.Meter));

                if (missH * missH + missV * missV <= radius * radius)
                    hits++;
            }

            return new HitProbabilityResult(shots, (double)hits / parameters.Shots);
        }

        private static ShotParameters CloneShot(ShotParameters shot, Measurement<DistanceUnit> step, Measurement<DistanceUnit> maximumDistance) =>
            new ShotParameters
            {
                Step = step,
                MaximumDistance = maximumDistance,
                ZeroDropAdjustment = shot.ZeroDropAdjustment,
                ZeroWindageAdjustment = shot.ZeroWindageAdjustment,
                ShotDropAdjustment = shot.ShotDropAdjustment,
                ShotWindageAdjustment = shot.ShotWindageAdjustment,
                ShotAngle = shot.ShotAngle,
                CantAngle = shot.CantAngle,
                BarrelAzimuth = shot.BarrelAzimuth,
                Latitude = shot.Latitude,
            };

        private static Ammunition PerturbMuzzleVelocity(Ammunition ammo, double factor) =>
            new Ammunition(
                weight: ammo.Weight,
                ballisticCoefficient: ammo.BallisticCoefficient,
                muzzleVelocity: ammo.MuzzleVelocity * factor,
                bulletDiameter: ammo.BulletDiameter,
                bulletLength: ammo.BulletLength);

        private static double[] Ranges(TrajectoryPoint[] traj) =>
            traj.Where(p => p != null).Select(p => p.Distance.In(DistanceUnit.Meter)).ToArray();

        private static double[] Values(TrajectoryPoint[] traj, Func<TrajectoryPoint, Measurement<DistanceUnit>> sel) =>
            traj.Where(p => p != null).Select(p => sel(p).In(DistanceUnit.Meter)).ToArray();

        // Linear interpolation over an ascending range grid, clamped at the ends.
        private static double Interp(double[] r, double[] v, double x)
        {
            if (x <= r[0]) return v[0];
            int last = r.Length - 1;
            if (x >= r[last]) return v[last];
            for (int i = 1; i <= last; i++)
            {
                if (x <= r[i])
                {
                    double t = (x - r[i - 1]) / (r[i] - r[i - 1]);
                    return v[i - 1] + t * (v[i] - v[i - 1]);
                }
            }
            return v[last];
        }

        private static double Gaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();   // in (0, 1]
            double u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
