using AwesomeAssertions;
using Gehtsoft.Measurements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Thread-safety contract for <see cref="TrajectoryCalculator"/>: one configured instance is
    /// safe for concurrent use.
    ///
    /// How this is tested: a race that corrupts shared state cannot be caught by asserting "no
    /// exception" (a torn read usually produces a wrong *number*, not a crash), and the race may
    /// simply not fire on a given run. So the approach is a *determinism-under-load* check —
    /// establish a single-threaded baseline for a set of diverse shots, then hammer the SAME shared
    /// calculator from every core with those shots interleaved, and assert every concurrent result
    /// is bit-identical to its baseline. Calculate is deterministic (no parallel reductions), so any
    /// non-identical result means shared state leaked between threads. High iteration count over
    /// mixed configs maximizes the interleavings exercised.
    /// </summary>
    public class ThreadSafetyTest
    {
        private sealed class Case
        {
            public string Name;
            public Ammunition Ammo;
            public Rifle Rifle;
            public Atmosphere Atmo;
            public Wind[] Wind;
            public ShotParameters Shot;
        }

        private static Case[] BuildCases()
        {
            var cal = new TrajectoryCalculator();
            var atmo = new Atmosphere();

            Case Make(string name, Ammunition ammo, Rifle rifle, double maxYd, Wind[] wind,
                Measurement<AngularUnit>? shotAngle = null, Measurement<AngularUnit>? lat = null, Measurement<AngularUnit>? az = null)
            {
                var shot = new ShotParameters
                {
                    Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                    MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
                    ShotAngle = shotAngle,
                    Latitude = lat,
                    BarrelAzimuth = az,
                };
                shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero));
                return new Case { Name = name, Ammo = ammo, Rifle = rifle, Atmo = atmo, Wind = wind, Shot = shot };
            }

            Rifle Rif(double zeroYd, double twist = 0) => new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(zeroYd, DistanceUnit.Yard), null, null),
                rifling: twist > 0 ? new Rifling(new Measurement<DistanceUnit>(twist, DistanceUnit.Inch), TwistDirection.Right) : null);

            Ammunition Full(double bc, DragTableId t, double mv) => new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(bc, t),
                muzzleVelocity: new Measurement<VelocityUnit>(mv, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.210, DistanceUnit.Inch));

            var wind = new[] { new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour), new Measurement<AngularUnit>(90, AngularUnit.Degree)) };

            return new[]
            {
                Make("g7 drift", Full(0.223, DragTableId.G7, 2700), Rif(100, 11), 1000, null),
                Make("g1 wind+drift", Full(0.450, DragTableId.G1, 2700), Rif(100, 11), 1000, wind),
                Make("ra4 subsonic", Full(0.132, DragTableId.RA4, 1080), Rif(50, 16), 300, null),
                Make("g7 coriolis", Full(0.320, DragTableId.G7, 2960), Rif(100, 9), 1500, null,
                    lat: new Measurement<AngularUnit>(45, AngularUnit.Degree), az: new Measurement<AngularUnit>(90, AngularUnit.Degree)),
                Make("g5 incline", Full(0.250, DragTableId.G5, 2500), Rif(200), 800, null,
                    shotAngle: new Measurement<AngularUnit>(15, AngularUnit.Degree)),
            };
        }

        private static string Diff(TrajectoryPoint[] a, TrajectoryPoint[] b)
        {
            if (a.Length != b.Length)
                return $"length {a.Length} vs {b.Length}";
            for (int i = 0; i < a.Length; i++)
            {
                if ((a[i] == null) != (b[i] == null))
                    return $"null mismatch @{i}";
                if (a[i] == null)
                    continue;
                if (a[i].Drop.In(DistanceUnit.Meter) != b[i].Drop.In(DistanceUnit.Meter)) return $"drop @{i}";
                if (a[i].Windage.In(DistanceUnit.Meter) != b[i].Windage.In(DistanceUnit.Meter)) return $"windage @{i}";
                if (a[i].Velocity.In(VelocityUnit.FeetPerSecond) != b[i].Velocity.In(VelocityUnit.FeetPerSecond)) return $"velocity @{i}";
                if (a[i].Time.Ticks != b[i].Time.Ticks) return $"time @{i}";
            }
            return null;
        }

        [Fact]
        public void SharedCalculator_ParallelCalculate_IsDeterministic()
        {
            var cases = BuildCases();
            var cal = new TrajectoryCalculator();   // one shared instance
            var baseline = cases.Select(c => cal.Calculate(c.Ammo, c.Rifle, c.Atmo, c.Shot, c.Wind)).ToArray();

            var errors = new ConcurrentQueue<string>();
            const int iterations = 800;
            Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                try
                {
                    int k = i % cases.Length;
                    var c = cases[k];
                    var traj = cal.Calculate(c.Ammo, c.Rifle, c.Atmo, c.Shot, c.Wind);
                    var d = Diff(traj, baseline[k]);
                    if (d != null)
                        errors.Enqueue($"iter {i} [{c.Name}]: {d}");
                }
                catch (Exception ex)
                {
                    errors.Enqueue($"iter {i}: {ex.GetType().Name} {ex.Message}");
                }
            });

            errors.Should().BeEmpty();
        }

        [Fact]
        public void SharedCalculator_ParallelCalculateZeroParameters_IsDeterministic()
        {
            var cases = BuildCases();
            var cal = new TrajectoryCalculator();
            var baseline = cases.Select(c => cal.CalculateZeroParameters(c.Ammo, c.Atmo, c.Rifle, c.Rifle.Zero, c.Shot, c.Wind)).ToArray();

            var errors = new ConcurrentQueue<string>();
            const int iterations = 800;
            Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                try
                {
                    int k = i % cases.Length;
                    var c = cases[k];
                    var z = cal.CalculateZeroParameters(c.Ammo, c.Atmo, c.Rifle, c.Rifle.Zero, c.Shot, c.Wind);
                    if (z.ZeroDropAdjustment.In(AngularUnit.MOA) != baseline[k].ZeroDropAdjustment.In(AngularUnit.MOA))
                        errors.Enqueue($"iter {i} [{c.Name}]: drop zero");
                    double zw = z.ZeroWindageAdjustment?.In(AngularUnit.MOA) ?? double.NaN;
                    double bw = baseline[k].ZeroWindageAdjustment?.In(AngularUnit.MOA) ?? double.NaN;
                    if (!zw.Equals(bw))
                        errors.Enqueue($"iter {i} [{c.Name}]: windage zero");
                }
                catch (Exception ex)
                {
                    errors.Enqueue($"iter {i}: {ex.GetType().Name} {ex.Message}");
                }
            });

            errors.Should().BeEmpty();
        }

        /// <summary>
        /// The standard drag tables are process-wide singletons: concurrent Get for the same id must
        /// always return the identical (reference-equal) instance, and never throw. This guards the
        /// Lazy-based initialization against a regression to per-call construction.
        /// </summary>
        [Fact]
        public void DragTableGet_Concurrent_ReturnsSameSingletonPerId()
        {
            var ids = new[] { DragTableId.G1, DragTableId.G2, DragTableId.G5, DragTableId.G6,
                              DragTableId.G7, DragTableId.G8, DragTableId.GI, DragTableId.GS, DragTableId.RA4 };
            var expected = ids.ToDictionary(id => id, id => DragTable.Get(id));

            var errors = new ConcurrentQueue<string>();
            Parallel.For(0, 8000, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                try
                {
                    var id = ids[i % ids.Length];
                    if (!ReferenceEquals(DragTable.Get(id), expected[id]))
                        errors.Enqueue($"{id}: different instance");
                }
                catch (Exception ex)
                {
                    errors.Enqueue($"{ex.GetType().Name} {ex.Message}");
                }
            });

            errors.Should().BeEmpty();
        }
    }
}
