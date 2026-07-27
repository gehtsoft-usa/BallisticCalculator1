using System;
using AwesomeAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Cross-check against a third-party multi-BC calculator: the Exbal printout
    /// <c>CLAUDE/data/exbal-sierra-168gr.pdf</c> - 308 Win, Sierra MatchKing .308" 168gr HPBT,
    /// 2561 fps, 100 yd zero, 10 mph 3 o'clock wind, four banded G1 ballistic coefficients.
    /// <para>The point of the test is the banded BC: Exbal switches BC at velocity thresholds, which
    /// we reproduce by synthesizing a custom drag curve from a near-step BC-vs-Mach profile. The
    /// single-BC run is kept alongside only to show how much that banding is worth downrange.</para>
    /// </summary>
    public class ExbalReportTest
    {
        private readonly ITestOutputHelper mOutput;
        public ExbalReportTest(ITestOutputHelper output) => mOutput = output;

        // dist(yd), drop(in) vs line of sight, wind(in), velocity(fps), energy(ft-lb), time(s).
        // The wind value of the last row is covered by the watermark in the printout.
        private static readonly double[][] Reference = new[]
        {
            new[] {    0.0,   -1.5,   0.0, 2561.0, 2446.0, 0.0000 },
            new[] {  100.0,   -0.0,  -0.8, 2368.0, 2091.0, 0.1218 },
            new[] {  200.0,   -4.7,  -3.4, 2184.0, 1778.0, 0.2537 },
            new[] {  300.0,  -16.7,  -8.0, 2003.0, 1497.0, 0.3970 },
            new[] {  400.0,  -37.4, -15.0, 1828.0, 1247.0, 0.5536 },
            new[] {  500.0,  -68.5, -24.6, 1663.0, 1031.0, 0.7256 },
            new[] {  600.0, -112.2, -37.3, 1508.0,  848.0, 0.9150 },
            new[] {  700.0, -171.3, -53.5, 1365.0,  695.0, 1.1242 },
            new[] {  800.0, -249.0, -73.5, 1244.0,  577.0, 1.3546 },
            new[] {  900.0, -349.2, -97.2, 1144.0,  488.0, 1.6065 },
            new[] { 1000.0, -475.9, double.NaN, 1069.0, 426.0, 1.8783 },
        };

        private static Rifle TheRifle() => new Rifle(
            sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch),
                             Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
            zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));

        // altitude 0, 29.53 inHg at sea level, 59 F, 78 % relative humidity
        private static Atmosphere TheAtmosphere() => new Atmosphere(
            altitude: Measurement<DistanceUnit>.ZERO,
            pressure: new Measurement<PressureUnit>(29.53, PressureUnit.InchesOfMercury),
            pressureAtSeaLevel: true,
            temperature: new Measurement<TemperatureUnit>(59, TemperatureUnit.Fahrenheit),
            humidity: 0.78);

        private static Wind[] TheWind() => new[]
        {
            new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour),
                     new Measurement<AngularUnit>(90, AngularUnit.Degree)),
        };

        // No rifling: the printout has no twist rate and no spin-drift or vertical-jump column,
        // so those terms have to stay out of the comparison.
        private static Ammunition TheAmmunition(BallisticCoefficient bc) => new Ammunition(
            weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
            ballisticCoefficient: bc,
            muzzleVelocity: new Measurement<VelocityUnit>(2561, VelocityUnit.FeetPerSecond),
            bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch));

        private static TrajectoryPoint[] Run(Ammunition ammunition, DragTable table)
        {
            var cal = new TrajectoryCalculator();
            var rifle = TheRifle();
            var atmo = TheAtmosphere();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
            };
            //Exbal zeros in calm air - its WIND column is the drift left to correct, not part of the
            //zero - so the crosswind must not be folded into the zero here
            shot.Apply(cal.CalculateZeroParameters(ammunition, atmo, rifle, rifle.Zero, dragTable: table));
            return cal.Calculate(ammunition, rifle, atmo, shot, TheWind(), table);
        }

        // Exbal's banded G1 BC: 0.462 above 2600 fps, 0.447 down to 2100, 0.424 down to 1600,
        // 0.405 below that. The factory interpolates BC linearly between knots, so a pair of knots
        // straddling each threshold renders the step.
        private static DrgDragTable BandedTable(Atmosphere atmosphere, out Ammunition ammunition)
        {
            double soundFps = atmosphere.SoundVelocity.In(VelocityUnit.FeetPerSecond);
            const double e = 0.001;
            double m2600 = 2600 / soundFps, m2100 = 2100 / soundFps, m1600 = 1600 / soundFps;
            var knots = new[]
            {
                new BcAtMach(0.0, 0.405),
                new BcAtMach(m1600 - e, 0.405), new BcAtMach(m1600 + e, 0.424),
                new BcAtMach(m2100 - e, 0.424), new BcAtMach(m2100 + e, 0.447),
                new BcAtMach(m2600 - e, 0.447), new BcAtMach(m2600 + e, 0.462),
                new BcAtMach(5.0, 0.462),
            };

            var entry = new AmmunitionLibraryEntry
            {
                Name = "Sierra MatchKing .308 168gr HPBT",
                Source = "exbal banded bc",
                Ammunition = TheAmmunition(new BallisticCoefficient(1.0, DragTableId.GC)),
            };
            var table = DrgDragTableFactory.Build(entry, DragTableId.G1, knots);
            ammunition = entry.Ammunition;
            return table;
        }

        /// <summary>
        /// The banded-BC run must reproduce the whole printout: every column of every row agrees to
        /// within the precision the report is printed at.
        /// </summary>
        [Fact]
        public void BandedBc_MatchesTheReport()
        {
            var atmo = TheAtmosphere();
            var table = BandedTable(atmo, out var ammunition);
            var traj = Run(ammunition, table);
            Report("banded bc", traj);

            foreach (var r in Reference)
            {
                var point = Find(traj, r[0]);
                point.Should().NotBeNull($"the trajectory must reach {r[0]:N0} yd");
                string at = $"@{r[0]:N0} yd";

                point.Velocity.In(VelocityUnit.FeetPerSecond).Should().BeApproximately(r[3], 5, $"velocity {at}");
                point.Energy.In(EnergyUnit.FootPound).Should().BeApproximately(r[4], 4, $"energy {at}");
                point.Drop.In(DistanceUnit.Inch).Should().BeApproximately(r[1], 0.5, $"drop {at}");
                point.Time.TotalSeconds.Should().BeApproximately(r[5], 0.003, $"time {at}");
                if (!double.IsNaN(r[2]))
                    (-point.Windage.In(DistanceUnit.Inch)).Should().BeApproximately(r[2], 0.3, $"windage {at}");
            }
        }

        /// <summary>
        /// Control case: running the same shot on the one BC that covers the muzzle velocity band
        /// walks away from the report as the bullet slows into the lower bands - which is what the
        /// banded curve exists to fix.
        /// </summary>
        [Fact]
        public void SingleBc_DriftsFromTheReport()
        {
            var traj = Run(TheAmmunition(new BallisticCoefficient(0.447, DragTableId.G1)), null);
            Report("single bc 0.447 G1", traj);

            //inside the 2100..2600 fps band the single BC is the right one, so it still matches
            Find(traj, 200).Drop.In(DistanceUnit.Inch).Should().BeApproximately(-4.7, 0.5, "drop@200 yd");

            //past that it is optimistic: too little drag, too flat, too little drift
            Find(traj, 1000).Velocity.In(VelocityUnit.FeetPerSecond).Should().BeGreaterThan(1069 + 25);
            Find(traj, 1000).Drop.In(DistanceUnit.Inch).Should().BeGreaterThan(-475.9 + 8);
        }

        private void Report(string title, TrajectoryPoint[] traj)
        {
            mOutput.WriteLine($"=== {title} ===");
            mOutput.WriteLine("  yd | V ref  ours   dV | E ref  ours |  drop ref    ours   d(in) d(MOA) | wind ref  ours   d(in) |  t ref  t ours      dt");
            foreach (var r in Reference)
            {
                var point = Find(traj, r[0]);
                if (point == null)
                {
                    mOutput.WriteLine($"{r[0],4:N0} | (no row)");
                    continue;
                }
                double velocity = point.Velocity.In(VelocityUnit.FeetPerSecond);
                double energy = point.Energy.In(EnergyUnit.FootPound);
                double drop = point.Drop.In(DistanceUnit.Inch);
                double wind = -point.Windage.In(DistanceUnit.Inch);   // the report prints right drift as negative
                double time = point.Time.TotalSeconds;
                double dropDelta = drop - r[1];
                double moa = r[0] > 0 ? Math.Atan(dropDelta / (r[0] * 36)) * 180 / Math.PI * 60 : 0;
                string windColumn = double.IsNaN(r[2])
                    ? $"     n/a {wind,6:N1}      -"
                    : $"{r[2],8:N1} {wind,6:N1} {wind - r[2],6:N1}";
                mOutput.WriteLine(
                    $"{r[0],4:N0} | {r[3],5:N0} {velocity,5:N0} {velocity - r[3],4:N0} | {r[4],5:N0} {energy,5:N0} | " +
                    $"{r[1],9:N1} {drop,7:N1} {dropDelta,7:N1} {moa,6:N2} | {windColumn} | " +
                    $"{r[5],6:N4} {time,7:N4} {time - r[5],7:N4}");
            }
        }

        private static TrajectoryPoint Find(TrajectoryPoint[] traj, double yards)
        {
            foreach (var point in traj)
            {
                if (point != null && Math.Abs(point.Distance.In(DistanceUnit.Yard) - yards) < 1.0)
                    return point;
            }
            return null;
        }
    }
}
