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
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.215, DistanceUnit.Inch)),
        };

        // the sectional density of the sample bullet - the scale the synthesized curve is on
        private const double SampleSd = 168 / 7000.0 / (0.308 * 0.308);

        // A flat BC(M) = k must produce Cd(M) = Cd_base(M) / k * SD at every base node, and the
        // entry must come back carrying the form factor of one that curve is run with.
        [Fact]
        public void FlatBc_ScalesBaseCurve()
        {
            const double k = 0.5;
            var baseCurve = DragTable.Get(DragTableId.G7);
            var entry = SampleEntry();
            var table = DrgDragTableFactory.Build(entry, DragTableId.G7,
                new[] { new BcAtMach(0.0, k), new BcAtMach(5.0, k) });

            table.Count.Should().Be(baseCurve.Count);
            for (int i = 0; i < baseCurve.Count; i++)
            {
                table[i].Mach.Should().Be(baseCurve[i].Mach);
                table[i].DragCoefficient.Should().BeApproximately(baseCurve[i].DragCoefficient / k * SampleSd, 1e-12);
            }

            entry.Ammunition.BallisticCoefficient.ValueType.Should().Be(BallisticCoefficientValueType.FormFactor);
            entry.Ammunition.BallisticCoefficient.Value.Should().Be(1.0);
            entry.Ammunition.BallisticCoefficient.Table.Should().Be(DragTableId.GC);
            // and that form factor resolves to the sectional density the curve was scaled by
            entry.Ammunition.GetBallisticCoefficient().Should().BeApproximately(SampleSd, 1e-12);
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

            var entry = SampleEntry();
            var table = DrgDragTableFactory.Build(entry, DragTableId.G7,
                new[] { new BcAtMach(0.0, k), new BcAtMach(5.0, k) });
            var ammoGc = entry.Ammunition;   // GC with the form factor of one the factory stamped

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
            reopened.Ammunition.Ammunition.BulletLength.Should().NotBeNull();
            reopened.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Meter)
                .Should().BeApproximately(entry.Ammunition.BulletLength.Value.In(DistanceUnit.Meter), 1e-9);
            reopened.Ammunition.Source.Should().Be("unit-test");
        }

        // The bullet length and the source are optional in the header: when they are missing
        // (or zeroed, as older versions of Save wrote them) the load must not fail.
        [Fact]
        public void Save_OmittedLengthAndSource()
        {
            using var ms = new MemoryStream();
            using (var w = new StreamWriter(ms, System.Text.Encoding.ASCII, 4096, true))
            {
                w.WriteLine("CFM, no-metadata, 0.01089, 0.00782,0,");
                w.WriteLine("0.14 0");
                w.WriteLine("0.30 1");
                w.WriteLine("0.25 5");
            }
            ms.Position = 0;

            var table = DrgDragTable.Open(ms);

            table.Ammunition.Name.Should().Be("no-metadata");
            table.Ammunition.Ammunition.BulletLength.Should().BeNull();
            table.Ammunition.Source.Should().Be("drg file");
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
                table[i].DragCoefficient.Should().BeApproximately(baseCurve[i].DragCoefficient / expectedBc * SampleSd, 1e-12);
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

            //the 5th and the 6th header fields of the reference file: length in meters and the data source
            original.Ammunition.Ammunition.BulletLength.Should().NotBeNull();
            original.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Meter).Should().BeApproximately(0.03114, 1e-9);
            original.Ammunition.Source.Should().Be("Radar Data");

            reopened.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Meter)
                .Should().BeApproximately(original.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Meter), 1e-9);
            reopened.Ammunition.Source.Should().Be(original.Ammunition.Source);
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

            // the weight and the diameter set the scale of the curve, so they are required
            ((Action)(() => DrgDragTableFactory.Build(new AmmunitionLibraryEntry { Name = "no-ammo" }, DragTableId.G7, ok)))
                .Should().Throw<ArgumentException>();

            var noDiameter = SampleEntry();
            noDiameter.Ammunition.BulletDiameter = null;
            ((Action)(() => DrgDragTableFactory.Build(noDiameter, DragTableId.G7, ok)))
                .Should().Throw<ArgumentException>();

            var noWeight = SampleEntry();
            noWeight.Ammunition.Weight = Measurement<WeightUnit>.ZERO;
            ((Action)(() => DrgDragTableFactory.Build(noWeight, DragTableId.G7, ok)))
                .Should().Throw<ArgumentException>();
        }

        // The point of putting the synthesized curve on the physical (drg) scale: a table built from
        // a BC profile is a drg file like any other, so writing it out and reading it back gives the
        // same drag curve, the same effective ballistic coefficient and the same trajectory.
        [Fact]
        public void Build_SaveOpen_ProducesTheSameTrajectory()
        {
            var entry = SampleEntry();
            var table = DrgDragTableFactory.Build(entry, DragTableId.G7,
                new[] { new BcAtMach(0.8, 0.21), new BcAtMach(1.6, 0.23), new BcAtMach(3.0, 0.25) });

            using var ms = new MemoryStream();
            table.Save(ms);
            ms.Position = 0;
            var reopened = DrgDragTable.Open(ms);

            // the curve itself survives exactly - Save writes round-trippable doubles
            reopened.Count.Should().Be(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                reopened[i].Mach.Should().Be(table[i].Mach);
                reopened[i].DragCoefficient.Should().Be(table[i].DragCoefficient);
            }

            // ... and so does the ballistic coefficient the engine derives from the pair
            reopened.Ammunition.Ammunition.GetBallisticCoefficient()
                .Should().BeApproximately(entry.Ammunition.GetBallisticCoefficient(), 1e-9);

            // the drg header has no slot for the muzzle velocity, so restore it before comparing
            reopened.Ammunition.Ammunition.MuzzleVelocity = entry.Ammunition.MuzzleVelocity;

            var built = Trajectory(entry.Ammunition, table);
            var loaded = Trajectory(reopened.Ammunition.Ammunition, reopened);

            loaded.Length.Should().Be(built.Length);
            for (int i = 0; i < built.Length; i++)
            {
                loaded[i].Velocity.In(VelocityUnit.FeetPerSecond)
                    .Should().BeApproximately(built[i].Velocity.In(VelocityUnit.FeetPerSecond), 1e-6, $"velocity@{built[i].Distance}");
                loaded[i].Drop.In(DistanceUnit.Inch)
                    .Should().BeApproximately(built[i].Drop.In(DistanceUnit.Inch), 1e-6, $"drop@{built[i].Distance}");
            }
        }

        // The format has two header tags in the wild, CFM and BRL, and they mean the same table.
        // Open must accept either and read them identically; Save always writes CFM, so a BRL file
        // round-trips to a CFM one with the same content and only the tag changed.
        [Fact]
        public void CfmAndBrlHeaders_ReadIdentically()
        {
            const string body = "0.14 0\n0.43 1\n0.35 2\n0.32 2.5\n";
            const string header = " a bullet, 0.01089, 0.00782, 0.03114, Radar Data";

            DrgDragTable Parse(string tag)
            {
                using var ms = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(tag + "," + header + "\n" + body));
                return DrgDragTable.Open(ms);
            }

            var cfm = Parse("CFM");
            var brl = Parse("BRL");

            brl.Count.Should().Be(cfm.Count);
            for (int i = 0; i < cfm.Count; i++)
            {
                brl[i].Mach.Should().Be(cfm[i].Mach);
                brl[i].DragCoefficient.Should().Be(cfm[i].DragCoefficient);
            }
            brl.Ammunition.Name.Should().Be(cfm.Ammunition.Name);
            brl.Ammunition.Source.Should().Be(cfm.Ammunition.Source);
            brl.Ammunition.Ammunition.GetBallisticCoefficient()
                .Should().Be(cfm.Ammunition.Ammunition.GetBallisticCoefficient());

            // an unknown tag is still rejected
            ((Action)(() => Parse("XYZ"))).Should().Throw<ArgumentException>();
        }

        // Third-party drg compatibility, both directions. A real BRL radar file holds the
        // projectile's physical drag coefficient and is run with a form factor of one; inverting it
        // into a BC-vs-Mach profile and feeding that back through the factory must return the very
        // same curve. If Build were on any other scale this could not hold.
        [Fact]
        public void RealDrgFile_InvertsAndRebuildsToTheSameCurve()
        {
            using var src = typeof(DrgDragTableFactoryTest).Assembly
                .GetManifestResourceStream("BallisticCalculator.Test.resources.sierra_168_brl.drg");
            var file = DrgDragTable.Open(src);

            var ammo = file.Ammunition.Ammunition;
            double sd = ammo.Weight.In(WeightUnit.Grain) / 7000.0
                        / Math.Pow(ammo.BulletDiameter.Value.In(DistanceUnit.Inch), 2);
            // the file pairs with a form factor of one, so its effective BC is that sectional density
            ammo.GetBallisticCoefficient().Should().BeApproximately(sd, 1e-12);

            // BC(M) = SD * Cd_base(M) / Cd_file(M), sampled on the base curve's own grid inside the
            // range the file covers, so the two interpolations are compared at shared nodes
            var g7 = DragTable.Get(DragTableId.G7);
            double machMax = file[file.Count - 1].Mach;
            var knots = new List<BcAtMach>();
            var nodes = new List<double>();
            for (int i = 0; i < g7.Count; i++)
            {
                double mach = g7[i].Mach;
                if (mach <= 0 || mach > machMax)
                    continue;
                double cdFile = file.Find(mach).CalculateDrag(mach);
                knots.Add(new BcAtMach(mach, sd * g7[i].DragCoefficient / cdFile));
                nodes.Add(mach);
            }
            knots.Count.Should().BeGreaterThan(20, "the sample file spans a usable Mach range");

            var rebuilt = DrgDragTableFactory.Build(
                new AmmunitionLibraryEntry
                {
                    Name = file.Ammunition.Name,
                    Ammunition = new Ammunition(ammo.Weight, ammo.BallisticCoefficient, ammo.MuzzleVelocity, ammo.BulletDiameter, ammo.BulletLength),
                },
                DragTableId.G7, knots);

            foreach (double mach in nodes)
            {
                double expected = file.Find(mach).CalculateDrag(mach);
                rebuilt.Find(mach).CalculateDrag(mach).Should()
                    .BeApproximately(expected, Math.Abs(expected) * 1e-9, $"cd@M{mach}");
            }
        }

        private static TrajectoryPoint[] Trajectory(Ammunition ammunition, DragTable table)
        {
            var cal = new TrajectoryCalculator();
            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));
            var atmo = new Atmosphere();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammunition, atmo, rifle, rifle.Zero, dragTable: table));
            return cal.Calculate(ammunition, rifle, atmo, shot, dragTable: table);
        }
    }
}
