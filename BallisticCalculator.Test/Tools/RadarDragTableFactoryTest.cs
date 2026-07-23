using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class RadarDragTableFactoryTest
    {
        private const double WeightGr = 168, DiameterIn = 0.308, MuzzleFps = 2700, RefBc = 0.223;

        private static Rifle Rifle() => new Rifle(
            sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
            zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));

        // A reference G7 trajectory, and radar-like (distance, velocity) readings sampled from it.
        private static (TrajectoryPoint[] reference, List<RadarReading> readings) Reference(double stepYd = 10, double maxYd = 600)
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(WeightGr, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(RefBc, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(MuzzleFps, VelocityUnit.FeetPerSecond));
            var rifle = Rifle();
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(stepYd, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero));
            var traj = cal.Calculate(ammo, rifle, atmo, shot);

            var readings = traj.Where(p => p != null)
                .Select(p => new RadarReading(p.Distance, p.Velocity)).ToList();
            return (traj, readings);
        }

        private static DrgDragTable BuildTable(List<RadarReading> readings) =>
            RadarDragTableFactory.Create(readings,
                new Measurement<WeightUnit>(WeightGr, WeightUnit.Grain),
                new Measurement<DistanceUnit>(DiameterIn, DistanceUnit.Inch),
                new Atmosphere());

        [Fact]
        public void RoundTrip_ReproducesReferenceVelocity()
        {
            var (reference, readings) = Reference(stepYd: 10, maxYd: 600);
            var table = BuildTable(readings);

            var ammo = table.Ammunition.Ammunition;   // GC, form-factor 1, weight + diameter, MV from data
            var rifle = Rifle();
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(600, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero, dragTable: table));
            var rebuilt = cal.Calculate(ammo, rifle, atmo, shot, dragTable: table);

            foreach (var p in rebuilt.Where(x => x != null))
            {
                var refPoint = reference.First(x => x != null && Math.Abs(x.Distance.In(DistanceUnit.Yard) - p.Distance.In(DistanceUnit.Yard)) < 0.5);
                p.Velocity.In(VelocityUnit.FeetPerSecond).Should()
                    .BeApproximately(refPoint.Velocity.In(VelocityUnit.FeetPerSecond),
                                     refPoint.Velocity.In(VelocityUnit.FeetPerSecond) * 0.01, $"@{p.Distance.In(DistanceUnit.Yard):N0}yd");
            }
        }

        [Fact]
        public void RecoversPhysicalDragCoefficient()
        {
            var (_, readings) = Reference(stepYd: 5, maxYd: 600);
            var table = BuildTable(readings);

            // The physical Cd of this bullet is the G7 curve scaled by the form factor (SD / BC).
            double sd = WeightGr / 7000.0 / (DiameterIn * DiameterIn);
            double formFactor = sd / RefBc;
            var g7 = DragTable.Get(DragTableId.G7);

            foreach (double mach in new[] { 1.8, 2.0, 2.2 })
            {
                double got = table.Find(mach).CalculateDrag(mach);
                double expected = g7.Find(mach).CalculateDrag(mach) * formFactor;
                got.Should().BeApproximately(expected, expected * 0.05, $"@M{mach}");
            }
        }

        [Fact]
        public void ProducesGcTableWithBulletMetadata()
        {
            var (_, readings) = Reference();
            var table = BuildTable(readings);

            table.TableId.Should().Be(DragTableId.GC);
            table.Ammunition.Ammunition.Weight.In(WeightUnit.Grain).Should().BeApproximately(WeightGr, 1e-6);
            table.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Inch).Should().BeApproximately(DiameterIn, 1e-6);
        }

        [Fact]
        public void NullReadings_Throws()
        {
            ((Action)(() => RadarDragTableFactory.Create(null,
                new Measurement<WeightUnit>(WeightGr, WeightUnit.Grain),
                new Measurement<DistanceUnit>(DiameterIn, DistanceUnit.Inch))))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TooFewReadings_Throws()
        {
            var readings = new List<RadarReading>
            {
                new RadarReading(new Measurement<DistanceUnit>(0, DistanceUnit.Yard), new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond)),
                new RadarReading(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), new Measurement<VelocityUnit>(2500, VelocityUnit.FeetPerSecond)),
            };
            ((Action)(() => BuildTable(readings))).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NonDecreasingVelocity_Throws()
        {
            var readings = new List<RadarReading>
            {
                new RadarReading(new Measurement<DistanceUnit>(0, DistanceUnit.Yard), new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond)),
                new RadarReading(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), new Measurement<VelocityUnit>(2500, VelocityUnit.FeetPerSecond)),
                new RadarReading(new Measurement<DistanceUnit>(200, DistanceUnit.Yard), new Measurement<VelocityUnit>(2550, VelocityUnit.FeetPerSecond)),  // rises
            };
            ((Action)(() => BuildTable(readings))).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NonPositiveWeight_Throws()
        {
            var (_, readings) = Reference();
            ((Action)(() => RadarDragTableFactory.Create(readings,
                new Measurement<WeightUnit>(0, WeightUnit.Grain),
                new Measurement<DistanceUnit>(DiameterIn, DistanceUnit.Inch))))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
