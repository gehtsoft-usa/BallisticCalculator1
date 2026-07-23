using System;
using System.Collections.Generic;
using System.IO;
using AwesomeAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    public class DrgDragTableFactoryTest
    {
        private static AmmunitionLibraryEntry SampleEntry() => new AmmunitionLibraryEntry
        {
            Name = "test-bullet",
            Source = "unit-test",
            Ammunition = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch)),
        };

        // A flat BC(M) = k must produce Cd_custom(M) = Cd_base(M) / k at every base node.
        [Fact]
        public void FlatBc_ScalesBaseCurve()
        {
            const double k = 0.5;
            var baseCurve = DragTable.Get(DragTableId.G7);
            var table = DrgDragTableFactory.Build(SampleEntry(), DragTableId.G7,
                new[] { new BcAtMach(0.0, k), new BcAtMach(5.0, k) });

            table.Count.Should().Be(baseCurve.Count);
            for (int i = 0; i < baseCurve.Count; i++)
            {
                table[i].Mach.Should().Be(baseCurve[i].Mach);
                table[i].DragCoefficient.Should().BeApproximately(baseCurve[i].DragCoefficient / k, 1e-12);
            }
        }

        // Running the synthesized (flat-BC) table with BC=1.0 must reproduce the standard
        // table run with BC=k, to well within measurement noise.
        [Fact]
        public void FlatBc_ReproducesStandardTrajectory()
        {
            const double k = 0.475;
            var cal = new TrajectoryCalculator();
            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));
            var atmo = new Atmosphere();

            var ammoStd = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(k, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));

            var ammoGc = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));

            var table = DrgDragTableFactory.Build(SampleEntry(), DragTableId.G7,
                new[] { new BcAtMach(0.0, k), new BcAtMach(5.0, k) });

            ShotParameters ShotFor(Ammunition a, DragTable t) => new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                ZeroDropAdjustment = cal.CalculateZeroParameters(a, atmo, rifle, rifle.Zero, dragTable: t).ZeroDropAdjustment,
            };

            var trajStd = cal.Calculate(ammoStd, rifle, atmo, ShotFor(ammoStd, null), null);
            var trajGc = cal.Calculate(ammoGc, rifle, atmo, ShotFor(ammoGc, table), null, table);

            trajGc.Length.Should().Be(trajStd.Length);
            for (int i = 0; i < trajStd.Length; i++)
            {
                trajGc[i].Velocity.In(VelocityUnit.FeetPerSecond)
                    .Should().BeApproximately(trajStd[i].Velocity.In(VelocityUnit.FeetPerSecond), 0.05, $"@{trajStd[i].Distance:N0}");
                trajGc[i].Drop.In(DistanceUnit.Inch)
                    .Should().BeApproximately(trajStd[i].Drop.In(DistanceUnit.Inch), 0.02, $"@{trajStd[i].Distance:N0}");
            }
        }

        // Synthesize -> Save -> Open must round-trip the points and ammo metadata.
        [Fact]
        public void Save_RoundTrips()
        {
            var entry = SampleEntry();
            var knots = new[] { new BcAtMach(0.0, 0.30), new BcAtMach(1.0, 0.28), new BcAtMach(3.0, 0.33) };
            var table = DrgDragTableFactory.Build(entry, DragTableId.G1, knots);

            using var ms = new MemoryStream();
            table.Save(ms);
            ms.Position = 0;
            var reopened = DrgDragTable.Open(ms);

            reopened.Count.Should().Be(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                reopened[i].Mach.Should().BeApproximately(table[i].Mach, 1e-9);
                reopened[i].DragCoefficient.Should().BeApproximately(table[i].DragCoefficient, 1e-9);
            }
            reopened.Ammunition.Name.Should().Be("test-bullet");
            reopened.Ammunition.Ammunition.Weight.In(WeightUnit.Kilogram)
                .Should().BeApproximately(entry.Ammunition.Weight.In(WeightUnit.Kilogram), 1e-9);
            reopened.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Meter)
                .Should().BeApproximately(entry.Ammunition.BulletDiameter.Value.In(DistanceUnit.Meter), 1e-9);
        }

        // BC is interpolated linearly between knots and held flat beyond them.
        [Fact]
        public void Bc_InterpolatesBetweenKnots()
        {
            // two knots at Mach 1 (BC 0.2) and Mach 2 (BC 0.4); at Mach 1.5 BC = 0.3
            var table = DrgDragTableFactory.Build(SampleEntry(), DragTableId.G1,
                new[] { new BcAtMach(1.0, 0.2), new BcAtMach(2.0, 0.4) });
            var baseCurve = DragTable.Get(DragTableId.G1);

            for (int i = 0; i < baseCurve.Count; i++)
            {
                double m = baseCurve[i].Mach;
                double expectedBc = m <= 1.0 ? 0.2 : m >= 2.0 ? 0.4 : 0.2 + (m - 1.0) * 0.2;
                table[i].DragCoefficient.Should().BeApproximately(baseCurve[i].DragCoefficient / expectedBc, 1e-12);
            }
        }

        // A real 3rd-party .drg (BRL header, radar data) must survive Open -> Save -> Open.
        [Fact]
        public void RealDrgFile_SaveRoundTrips()
        {
            using var src = typeof(DrgDragTableFactoryTest).Assembly
                .GetManifestResourceStream("BallisticCalculator.Test.resources.sierra_168_brl.drg");
            var original = DrgDragTable.Open(src);

            using var ms = new MemoryStream();
            original.Save(ms);
            ms.Position = 0;
            var reopened = DrgDragTable.Open(ms);

            reopened.Count.Should().Be(original.Count);
            for (int i = 0; i < original.Count; i++)
            {
                reopened[i].Mach.Should().BeApproximately(original[i].Mach, 1e-9);
                reopened[i].DragCoefficient.Should().BeApproximately(original[i].DragCoefficient, 1e-9);
            }
            reopened.Ammunition.Name.Should().Be(original.Ammunition.Name);
            reopened.Ammunition.Ammunition.Weight.In(WeightUnit.Kilogram)
                .Should().BeApproximately(original.Ammunition.Ammunition.Weight.In(WeightUnit.Kilogram), 1e-9);
            reopened.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Meter)
                .Should().BeApproximately(original.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Meter), 1e-9);
        }

        [Fact]
        public void Build_GuardsBadInput()
        {
            var entry = SampleEntry();
            var ok = new[] { new BcAtMach(0.0, 0.3) };

            ((Action)(() => DrgDragTableFactory.Build(null, DragTableId.G7, ok)))
                .Should().Throw<ArgumentNullException>();
            ((Action)(() => DrgDragTableFactory.Build(entry, DragTableId.G7, null)))
                .Should().Throw<ArgumentNullException>();
            ((Action)(() => DrgDragTableFactory.Build(entry, DragTableId.GC, ok)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => DrgDragTableFactory.Build(entry, DragTableId.G7, new List<BcAtMach>())))
                .Should().Throw<ArgumentException>();
            ((Action)(() => DrgDragTableFactory.Build(entry, DragTableId.G7, new[] { new BcAtMach(1.0, 0.0) })))
                .Should().Throw<ArgumentException>();
        }
    }
}
