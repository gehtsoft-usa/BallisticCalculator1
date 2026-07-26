using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// <para>Accuracy of the two custom-drag-curve paths against real published Doppler-radar
    /// reports (Warner Tool Company .338 Flatline 285 gr and 6.5 mm Flatline 123 gr, embedded as
    /// <c>warner_*.txt</c>). Each report carries both kinds of input side by side: a smoothed
    /// Mach/CD/multi-BC table and a downrange velocity table.</para>
    /// <para>What these tests pin down:</para>
    /// <list type="bullet">
    /// <item><description><see cref="RadarDragTableFactory"/> reproduces the report's velocity table
    /// essentially exactly (0.05 % / 5 ms).</description></item>
    /// <item><description><see cref="DrgDragTableFactory"/> reproduces the report's own CD column to
    /// 0.5 %, on either base curve - but its table is on the reciprocal-BC scale (1/sectional
    /// density above the physical Cd), so it must be run at BC = coefficient 1.0.</description></item>
    /// <item><description>The two paths then differ by a single constant factor of ~0.78, which is a
    /// pure air-density ratio: the reports' CD/BC columns are the density-independent aerodynamic
    /// values while their velocity tables are raw measurements from a high desert range on a warm
    /// day. Given that atmosphere the multi-BC curve reproduces the measured velocities to
    /// 0.5 %.</description></item>
    /// </list>
    /// </summary>
    public class WarnerReportTest
    {
        private const double ReportRangeYd = 1500, StepYd = 100;

        private static Rifle Rifle() => new Rifle(
            sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
            zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));

        private static Atmosphere SeaLevel() => new Atmosphere();

        /// <summary>
        /// <para>The conditions the reports' velocity tables were shot in: a warm day on a high
        /// desert range. Warner's radar work was done at the New Mexico Tech / EMRTC facility near
        /// Socorro, NM - campus at ~4600 ft, field ranges climbing into the foothills past 6000 ft -
        /// and 6000 ft plus 78 F puts the air density at 0.770 of standard, a density altitude of
        /// ~8700 ft.</para>
        /// <para>Only two numbers of an atmosphere reach the trajectory: the density factor and the
        /// speed of sound (which sets the Mach the drag curve is sampled at). Those two are
        /// identifiable from the data - 0.770 and a 78 F sound speed reproduce both reports to
        /// under 0.1 % - but the altitude / station pressure / temperature / humidity split behind
        /// them is not, so these are representative conditions rather than the logged ones.
        /// Humidity in particular is a sub-1 % lever here, and it works backwards from intuition:
        /// water vapour is lighter than air, so *dry* desert air is the denser one.</para>
        /// </summary>
        private static Atmosphere MeasuredAtmosphere() => new Atmosphere(
            altitude: new Measurement<DistanceUnit>(6000, DistanceUnit.Foot),
            pressure: new Measurement<PressureUnit>(23.98, PressureUnit.InchesOfMercury),
            temperature: new Measurement<TemperatureUnit>(78, TemperatureUnit.Fahrenheit),
            humidity: 0.25);

        /// <summary>The air density of an atmosphere relative to the engine's standard density.</summary>
        private static double DensityFactor(Atmosphere atmosphere) =>
            atmosphere.Density.In(DensityUnit.KilogramPerCubicMeter) /
            Atmosphere.StandardDensity.In(DensityUnit.KilogramPerCubicMeter);

        private static TrajectoryPoint[] Run(Ammunition ammo, Atmosphere atmosphere, DragTable table)
        {
            var rifle = Rifle();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(StepYd, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(ReportRangeYd, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmosphere, rifle, rifle.Zero, dragTable: table));
            return cal.Calculate(ammo, rifle, atmosphere, shot, dragTable: table);
        }

        private static TrajectoryPoint At(TrajectoryPoint[] trajectory, Measurement<DistanceUnit> distance) =>
            trajectory.First(p => p != null &&
                Math.Abs(p.Distance.In(DistanceUnit.Yard) - distance.In(DistanceUnit.Yard)) < 0.5);

        /// <summary>
        /// The report's multi-BC column, synthesized into a custom (GC) drag table. The factory also
        /// stamps the entry's ammunition with the form factor of one the curve is run with, so
        /// [c]table.Ammunition.Ammunition[/c] is the ammunition to shoot it with.
        /// </summary>
        private static DrgDragTable MultiBcTable(WarnerReport report, DragTableId? baseTable = null)
        {
            var entry = new AmmunitionLibraryEntry
            {
                Name = report.Name,
                Ammunition = new Ammunition(
                    weight: report.Weight,
                    ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
                    muzzleVelocity: report.MuzzleVelocity,
                    bulletDiameter: report.Diameter),
            };
            var id = baseTable ?? report.BaseTable;
            var knots = report.Drag.Select(r => new BcAtMach(r.Mach, r.Bc(id))).ToArray();
            return DrgDragTableFactory.Build(entry, id, knots);
        }

        /// <summary>
        /// The velocity table, inverted back into a drag curve, reproduces the velocity table -
        /// including the report's own time of flight, which the inversion never sees.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void RadarTable_ReproducesReportVelocityTable(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var table = RadarDragTableFactory.Create(report.RadarReadings(), report.Weight, report.Diameter,
                SeaLevel(), report.Name);

            // the factory hands back the ammunition to use: GC with a form factor of one
            table.Ammunition.Ammunition.BallisticCoefficient.ValueType
                .Should().Be(BallisticCoefficientValueType.FormFactor);

            var trajectory = Run(table.Ammunition.Ammunition, SeaLevel(), table);

            foreach (var row in report.Velocity)
            {
                var point = At(trajectory, row.Distance);
                double expected = row.Velocity.In(VelocityUnit.FeetPerSecond);
                point.Velocity.In(VelocityUnit.FeetPerSecond).Should()
                    .BeApproximately(expected, expected * 0.0005, $"velocity@{row.Distance}");
                point.Time.TotalSeconds.Should()
                    .BeApproximately(row.Time, 0.005, $"time@{row.Distance}");
            }
        }

        /// <summary>
        /// The multi-BC column, synthesized into a drag table, reproduces the report's own CD
        /// column directly: the factory scales the curve by the bullet's sectional density, so the
        /// table holds the projectile's physical drag coefficient just like a drg file does.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void MultiBcTable_ReproducesReportDragCoefficients(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var table = MultiBcTable(report);

            foreach (var row in report.Drag)
            {
                double physicalCd = table.Find(row.Mach).CalculateDrag(row.Mach);
                physicalCd.Should().BeApproximately(row.Cd, row.Cd * 0.005, $"cd@M{row.Mach}");
            }
        }

        /// <summary>
        /// The synthesis is an absolute drag curve, so it does not matter which standard curve the
        /// report's BC column is referenced to. The reports publish both a G1 and a G7 column for
        /// the same measured CD, which is exactly the material for that check.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void MultiBcTable_IsIndependentOfTheBaseCurve(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var fromG1 = MultiBcTable(report, DragTableId.G1);
            var fromG7 = MultiBcTable(report, DragTableId.G7);

            foreach (var row in report.Drag)
            {
                double g1 = fromG1.Find(row.Mach).CalculateDrag(row.Mach);
                double g7 = fromG7.Find(row.Mach).CalculateDrag(row.Mach);
                g1.Should().BeApproximately(g7, g7 * 0.005, $"base-independent@M{row.Mach}");
            }
        }

        /// <summary>
        /// <para>Why the two tables of one report disagree: the drag curve recovered from the
        /// velocity table is a constant ~0.78 of the curve synthesized from the CD/BC column, at
        /// every Mach number. A constant, Mach-independent factor is an air density ratio and
        /// nothing else - the CD/BC columns are sea-level referenced, the velocity table was shot
        /// at altitude.</para>
        /// <para>The shapes agreeing to a few percent is the actual cross-validation of the two
        /// factories against each other on real data.</para>
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void MultiBcAndRadarTables_DifferOnlyByAirDensity(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var radar = RadarDragTableFactory.Create(report.RadarReadings(), report.Weight, report.Diameter,
                SeaLevel(), report.Name);
            var multiBc = MultiBcTable(report);

            var ratios = new double[radar.Count];
            for (int i = 0; i < radar.Count; i++)
            {
                double mach = radar[i].Mach;
                double synthesized = multiBc.Find(mach).CalculateDrag(mach);
                ratios[i] = radar[i].DragCoefficient / synthesized;
            }

            double mean = ratios.Average();
            mean.Should().BeInRange(0.76, 0.80, "the velocity table was measured at ~0.78 of the standard air density");
            ((ratios.Max() - ratios.Min()) / mean).Should().BeLessThan(0.04,
                "the factor must be Mach-independent - the two curves have the same shape");

            // ... and it is the density of the atmosphere the other tests run in. Not exactly: the
            // ratio above puts both curves on a sea-level Mach axis, while the real measurement had
            // a warmer, faster speed of sound, which shifts the sampled Cd by another ~1.5 %.
            DensityFactor(MeasuredAtmosphere())
                .Should().BeApproximately(mean, 0.02, "the measured atmosphere carries that density");
        }

        /// <summary>
        /// The whole point: given the air density its velocity table was actually measured at, the
        /// report's multi-BC column reproduces those measured velocities to 0.5 %. Run at sea level
        /// - the density its CD/BC columns are referenced to - it is 10 %+ slow, which is the
        /// mismatch that makes the two tables of one report look irreconcilable.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void MultiBcTrajectory_ReproducesReportVelocities_AtTheMeasuredDensityAltitude(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var table = MultiBcTable(report);
            var ammunition = table.Ammunition.Ammunition;

            var measured = Run(ammunition, MeasuredAtmosphere(), table);
            foreach (var row in report.Velocity)
            {
                double expected = row.Velocity.In(VelocityUnit.FeetPerSecond);
                At(measured, row.Distance).Velocity.In(VelocityUnit.FeetPerSecond).Should()
                    .BeApproximately(expected, expected * 0.005, $"velocity@{row.Distance}");
            }

            // control: the same curve at sea level is far outside that band, so the test above
            // cannot pass by accident.
            var lastRow = report.Velocity[report.Velocity.Count - 1];
            var atSeaLevel = At(Run(ammunition, SeaLevel(), table), lastRow.Distance);
            double error = 1 - atSeaLevel.Velocity.In(VelocityUnit.FeetPerSecond) / lastRow.Velocity.In(VelocityUnit.FeetPerSecond);
            error.Should().BeGreaterThan(0.10,
                "at sea level the sea-level-referenced drag curve loses 10 %+ of the measured velocity");
        }

        /// <summary>
        /// A curve synthesized from a report's multi-BC column is a drg file like any other: saving
        /// it and loading it back gives the same drag coefficients and the same trajectory against
        /// the report's own velocity table. This is what the shared physical Cd scale buys - the two
        /// factories and the file format all mean the same thing by a drag coefficient.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void MultiBcTable_SurvivesTheDrgRoundTrip(string resource)
        {
            var report = WarnerReport.FromResource(resource);
            var built = MultiBcTable(report);

            using var stream = new MemoryStream();
            built.Save(stream);
            stream.Position = 0;
            var loaded = DrgDragTable.Open(stream);

            loaded.Count.Should().Be(built.Count);
            for (int i = 0; i < built.Count; i++)
            {
                loaded[i].Mach.Should().Be(built[i].Mach);
                loaded[i].DragCoefficient.Should().Be(built[i].DragCoefficient);
            }

            // the drg header carries no muzzle velocity, so put the report's back before shooting it
            loaded.Ammunition.Ammunition.MuzzleVelocity = report.MuzzleVelocity;

            var trajectory = Run(loaded.Ammunition.Ammunition, MeasuredAtmosphere(), loaded);
            foreach (var row in report.Velocity)
            {
                double expected = row.Velocity.In(VelocityUnit.FeetPerSecond);
                At(trajectory, row.Distance).Velocity.In(VelocityUnit.FeetPerSecond).Should()
                    .BeApproximately(expected, expected * 0.005, $"velocity@{row.Distance}");
            }
        }

        /// <summary>
        /// Both paths of one report, driven end to end, land on the same trajectory once each is
        /// given the air density it belongs to: the curve inverted from the velocity table at sea
        /// level, and the curve synthesized from the CD column at the measured density altitude.
        /// This is the round trip the report's two tables are supposed to describe.
        /// </summary>
        [Theory]
        [InlineData("warner_338_flatline")]
        [InlineData("warner_65_flatline")]
        public void BothPaths_AgreeOnTheWholeTrajectory(string resource)
        {
            var report = WarnerReport.FromResource(resource);

            var radar = RadarDragTableFactory.Create(report.RadarReadings(), report.Weight, report.Diameter,
                SeaLevel(), report.Name);
            var fromVelocities = Run(radar.Ammunition.Ammunition, SeaLevel(), radar);
            var multiBc = MultiBcTable(report);
            var fromMultiBc = Run(multiBc.Ammunition.Ammunition, MeasuredAtmosphere(), multiBc);

            foreach (var row in report.Velocity)
            {
                double expected = At(fromVelocities, row.Distance).Velocity.In(VelocityUnit.FeetPerSecond);
                At(fromMultiBc, row.Distance).Velocity.In(VelocityUnit.FeetPerSecond).Should()
                    .BeApproximately(expected, expected * 0.005, $"velocity@{row.Distance}");
            }
        }
    }
}
