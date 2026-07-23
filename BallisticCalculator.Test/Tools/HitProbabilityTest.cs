using System;
using System.Linq;
using AwesomeAssertions;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class HitProbabilityTest
    {
        private static (TrajectoryCalculator cal, Ammunition ammo, Atmosphere atmo, Rifle rifle, ShotParameters shot, Wind[] wind) Setup(double targetYd = 500)
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
                bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(1.210, DistanceUnit.Inch));
            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null),
                rifling: new Rifling(new Measurement<DistanceUnit>(11, DistanceUnit.Inch), TwistDirection.Right));
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(25, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(targetYd, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero));
            var wind = new[] { new Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour), new Measurement<AngularUnit>(90, AngularUnit.Degree)) };
            return (cal, ammo, atmo, rifle, shot, wind);
        }

        private static HitProbabilityParameters Params(double targetIn, double mvPct, double groupMoa, double rangePct, double windPct, int shots = 4000, int seed = 12345)
            => new HitProbabilityParameters
            {
                TargetSize = new Measurement<DistanceUnit>(targetIn, DistanceUnit.Inch),
                MuzzleVelocityDeviationPercent = mvPct,
                GroupSize = new Measurement<AngularUnit>(groupMoa, AngularUnit.MOA),
                DistanceErrorPercent = rangePct,
                WindErrorPercent = windPct,
                Shots = shots,
                Seed = seed,
            };

        [Fact]
        public void PerfectShooter_AllHitsDeadCenter()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var r = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, 0, 0, 0, 0, shots: 500));

            r.HitProbability.Should().Be(1.0);
            r.Shots.Should().OnlyContain(s => Math.Abs(s.Horizontal.In(DistanceUnit.Inch)) < 1e-6 && Math.Abs(s.Vertical.In(DistanceUnit.Inch)) < 1e-6);
            r.ShotsFor50Percent.Should().Be(1);
            r.ShotsFor98Percent.Should().Be(1);
        }

        [Fact]
        public void Shots_CountAndProbabilityConsistent()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var r = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, 1.0, 0.5, 5, 20));

            r.Shots.Count.Should().Be(4000);
            double radius = 5; // inches
            double fraction = (double)r.Shots.Count(s =>
                Math.Pow(s.Horizontal.In(DistanceUnit.Inch), 2) + Math.Pow(s.Vertical.In(DistanceUnit.Inch), 2) <= radius * radius) / r.Shots.Count;
            r.HitProbability.Should().BeApproximately(fraction, 1e-9);
            r.HitProbability.Should().BeInRange(0.0, 1.0);
        }

        [Fact]
        public void LargerGroup_LowersProbability()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var tight = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, 1.0, 0.5, 5, 20));
            var loose = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, 1.0, 3.0, 5, 20));

            loose.HitProbability.Should().BeLessThan(tight.HitProbability);
        }

        [Fact]
        public void ShootingPosition_WidensAndLowersProbability()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var supported = Params(20, 1.0, 1.0, 5, 20);        // multipliers default to 1
            var standing = Params(20, 1.0, 1.0, 5, 20);
            standing.HorizontalPositionMultiplier = 5;
            standing.VerticalPositionMultiplier = 4;

            var s = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, supported);
            var st = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, standing);

            st.HitProbability.Should().BeLessThan(s.HitProbability);
        }

        [Fact]
        public void SmallerTarget_LowersProbability()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var big = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(12, 1.0, 1.0, 5, 20));
            var small = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(4, 1.0, 1.0, 5, 20));

            small.HitProbability.Should().BeLessThan(big.HitProbability);
        }

        [Fact]
        public void ShotsToHit_FollowGeometricFormula()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var r = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(6, 1.0, 1.0, 5, 20));

            r.HitProbability.Should().BeInRange(0.01, 0.99, "the scenario should be a partial hit rate for this test to be meaningful");
            int expected90 = (int)Math.Ceiling(Math.Log(1 - 0.90) / Math.Log(1 - r.HitProbability));
            r.ShotsFor90Percent.Should().Be(expected90);
            r.ShotsFor95Percent.Value.Should().BeGreaterThanOrEqualTo(r.ShotsFor90Percent.Value);
        }

        [Fact]
        public void SameSeed_Reproducible()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var a = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(8, 1.0, 1.0, 5, 20, seed: 777));
            var b = HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(8, 1.0, 1.0, 5, 20, seed: 777));

            a.HitProbability.Should().Be(b.HitProbability);
        }

        [Fact]
        public void NullArguments_Throw()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            var p = Params(10, 1, 1, 5, 20);
            ((Action)(() => HitProbability.Estimate(null, ammo, atmo, rifle, shot, wind, p))).Should().Throw<ArgumentNullException>();
            ((Action)(() => HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void BadParameters_Throw()
        {
            var (cal, ammo, atmo, rifle, shot, wind) = Setup();
            ((Action)(() => HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(0, 1, 1, 5, 20))))
                .Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, -1, 1, 5, 20))))
                .Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, Params(10, 1, 1, 5, 20, shots: 0))))
                .Should().Throw<ArgumentOutOfRangeException>();
            var badPosition = Params(10, 1, 1, 5, 20);
            badPosition.VerticalPositionMultiplier = 0;
            ((Action)(() => HitProbability.Estimate(cal, ammo, atmo, rifle, shot, wind, badPosition)))
                .Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
