using System;
using System.Linq;
using AwesomeAssertions;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class PointBlankRangeTest
    {
        private const double StepYd = 5;

        private static TrajectoryPoint[] Trajectory(double zeroYd = 250, double maxYd = 700)
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));
            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(zeroYd, DistanceUnit.Yard), null, null));
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(StepYd, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(maxYd, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero));
            return cal.Calculate(ammo, rifle, atmo, shot);
        }

        private static double DropInchAt(TrajectoryPoint[] traj, Measurement<DistanceUnit> range)
        {
            var p = traj.First(x => x != null && Math.Abs(x.Distance.In(DistanceUnit.Yard) - range.In(DistanceUnit.Yard)) < 0.5);
            return p.Drop.In(DistanceUnit.Inch);
        }

        private static double DropInchAt(TrajectoryPoint[] traj, double yards)
        {
            var p = traj.FirstOrDefault(x => x != null && Math.Abs(x.Distance.In(DistanceUnit.Yard) - yards) < 0.5);
            return p == null ? double.NaN : p.Drop.In(DistanceUnit.Inch);
        }

        [Fact]
        public void CenterAim_CorridorStaysWithinHalfAndExitsAtBottom()
        {
            var traj = Trajectory();
            var vital = new Measurement<DistanceUnit>(10, DistanceUnit.Inch);   // half = 5 in

            var r = PointBlankRange.Analyze(traj, vital, PointBlankAim.Center);

            DropInchAt(traj, r.MinimumRange).Should().BeInRange(-5, 5);
            DropInchAt(traj, r.MaximumRange).Should().BeInRange(-5, 5);
            // The point one step past the far edge has dropped out of the bottom of the zone.
            DropInchAt(traj, r.MaximumRange.In(DistanceUnit.Yard) + StepYd).Should().BeLessThan(-5);

            r.MaximumRange.In(DistanceUnit.Yard).Should().BeGreaterThan(r.MinimumRange.In(DistanceUnit.Yard));
            r.MaximumOrdinateRange.In(DistanceUnit.Yard).Should().BeInRange(r.MinimumRange.In(DistanceUnit.Yard), r.MaximumRange.In(DistanceUnit.Yard));
        }

        [Fact]
        public void BottomAim_CorridorSitsAboveLineOfSight()
        {
            var traj = Trajectory();
            var vital = new Measurement<DistanceUnit>(10, DistanceUnit.Inch);   // corridor 0 .. 10 in

            var r = PointBlankRange.Analyze(traj, vital, PointBlankAim.Bottom);

            DropInchAt(traj, r.MinimumRange).Should().BeInRange(0, 10);
            DropInchAt(traj, r.MaximumRange).Should().BeInRange(0, 10);
            // One step past the far edge the path has dropped below the line of sight (below the zone).
            DropInchAt(traj, r.MaximumRange.In(DistanceUnit.Yard) + StepYd).Should().BeLessThan(0);
        }

        [Fact]
        public void CenterVsBottom_DifferentCorridors()
        {
            var traj = Trajectory();
            var vital = new Measurement<DistanceUnit>(10, DistanceUnit.Inch);

            var center = PointBlankRange.Analyze(traj, vital, PointBlankAim.Center);
            var bottom = PointBlankRange.Analyze(traj, vital, PointBlankAim.Bottom);

            // Center allows the path to drop half a zone below the sight line, so it reaches farther;
            // bottom requires the path to stay at or above the sight line, so it starts later.
            center.MaximumRange.In(DistanceUnit.Yard).Should().BeGreaterThan(bottom.MaximumRange.In(DistanceUnit.Yard));
            center.MinimumRange.In(DistanceUnit.Yard).Should().BeLessThan(bottom.MinimumRange.In(DistanceUnit.Yard));
        }

        [Fact]
        public void DangerSpace_IsCorridorLength()
        {
            var traj = Trajectory();
            var r = PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(8, DistanceUnit.Inch));

            r.DangerSpace.In(DistanceUnit.Yard).Should().BeApproximately(
                r.MaximumRange.In(DistanceUnit.Yard) - r.MinimumRange.In(DistanceUnit.Yard), 1e-6);
        }

        [Fact]
        public void MaximumOrdinate_IsPeakDrop()
        {
            var traj = Trajectory();
            var r = PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(10, DistanceUnit.Inch));

            double peak = traj.Where(p => p != null).Max(p => p.Drop.In(DistanceUnit.Inch));
            r.MaximumOrdinate.In(DistanceUnit.Inch).Should().BeApproximately(peak, 1e-6);
            DropInchAt(traj, r.MaximumOrdinateRange).Should().BeApproximately(peak, 1e-6);
        }

        [Fact]
        public void LargerVitalZone_ReachesNoLessFar()
        {
            var traj = Trajectory();
            var small = PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(6, DistanceUnit.Inch));
            var large = PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(12, DistanceUnit.Inch));

            large.MaximumRange.In(DistanceUnit.Yard).Should().BeGreaterThanOrEqualTo(small.MaximumRange.In(DistanceUnit.Yard));
        }

        [Fact]
        public void NullTrajectory_Throws()
        {
            ((Action)(() => PointBlankRange.Analyze(null, new Measurement<DistanceUnit>(10, DistanceUnit.Inch))))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NonPositiveVitalZone_Throws()
        {
            var traj = Trajectory();
            ((Action)(() => PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(0, DistanceUnit.Inch))))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CorridorNeverLeftWithinTrajectory_Throws()
        {
            var traj = Trajectory(maxYd: 200);
            // A vital zone far larger than any drop over 200 yd keeps the path inside for the whole run.
            ((Action)(() => PointBlankRange.Analyze(traj, new Measurement<DistanceUnit>(400, DistanceUnit.Inch))))
                .Should().Throw<InvalidOperationException>();
        }
    }
}
