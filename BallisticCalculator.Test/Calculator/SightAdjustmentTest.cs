using AwesomeAssertions;
using Gehtsoft.Measurements;
using System;
using System.Linq;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Tests for the dialed sight settings on <see cref="ShotParameters"/>:
    /// <see cref="ShotParameters.ZeroDropAdjustment"/> / <see cref="ShotParameters.ZeroWindageAdjustment"/>
    /// (zeroing) accumulated with <see cref="ShotParameters.ShotDropAdjustment"/> /
    /// <see cref="ShotParameters.ShotWindageAdjustment"/> (per-shot clicks) into the initial
    /// barrel orientation.
    /// </summary>
    public class SightAdjustmentTest
    {
        // Config with rifling + bullet dimensions so spin drift is active (needed for the
        // "positive windage correction cancels the right drift" test).
        private static (Ammunition, Rifle, Atmosphere) Config()
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

            return (ammo, rifle, new Atmosphere());
        }

        private static ShotParameters BaseShot(Measurement<AngularUnit> zeroDrop) =>
            new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(800, DistanceUnit.Yard),
                ZeroDropAdjustment = zeroDrop,
            };

        private static TrajectoryPoint PointAt(TrajectoryPoint[] trajectory, double yards) =>
            trajectory.Where(p => p != null).First(p => Math.Abs(p.Distance.In(DistanceUnit.Yard) - yards) < 0.5);

        /// <summary>
        /// A pure windage tilt preserves vz/vx exactly per step, so the resulting
        /// WindageAdjustment equals the dialed angle at every range (positive = left).
        /// </summary>
        [Fact]
        public void ShotWindageAdjustment_ProducesEqualAngularWindage()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var shot = BaseShot(zero);
            shot.ShotWindageAdjustment = new Measurement<AngularUnit>(5, AngularUnit.MOA);

            var baseline = cal.Calculate(ammo, rifle, atmo, BaseShot(zero));
            var adjusted = cal.Calculate(ammo, rifle, atmo, shot);

            foreach (var yards in new[] { 100.0, 400.0, 800.0 })
            {
                double delta = PointAt(adjusted, yards).WindageAdjustment.In(AngularUnit.MOA)
                             - PointAt(baseline, yards).WindageAdjustment.In(AngularUnit.MOA);
                // Positive (left) and equal to the dialed 5 MOA at all ranges.
                delta.Should().BeApproximately(5.0, 0.01, $"@{yards}yd");
            }
        }

        /// <summary>
        /// The user's correction requirement: a right-twist bullet drifts right (negative
        /// WindageAdjustment); dialing the equal-and-opposite positive correction returns the
        /// windage to ~0 at the range it was read at.
        /// </summary>
        [Fact]
        public void PositiveWindageCorrection_CancelsRightDrift()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var baseline = cal.Calculate(ammo, rifle, atmo, BaseShot(zero));
            double driftMoa = PointAt(baseline, 600).WindageAdjustment.In(AngularUnit.MOA);
            driftMoa.Should().BeLessThan(0, "a right twist drifts right → negative windage");

            var corrected = BaseShot(zero);
            corrected.ShotWindageAdjustment = new Measurement<AngularUnit>(-driftMoa, AngularUnit.MOA);
            var result = cal.Calculate(ammo, rifle, atmo, corrected);

            PointAt(result, 600).WindageAdjustment.In(AngularUnit.MOA).Should().BeApproximately(0, 0.05);
        }

        /// <summary>
        /// A positive per-shot elevation click raises the point of impact by ~the dialed angle.
        /// </summary>
        [Fact]
        public void ShotDropAdjustment_RaisesImpact()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var shot = BaseShot(zero);
            shot.ShotDropAdjustment = new Measurement<AngularUnit>(5, AngularUnit.MOA);

            var baseline = cal.Calculate(ammo, rifle, atmo, BaseShot(zero));
            var adjusted = cal.Calculate(ammo, rifle, atmo, shot);

            double delta = PointAt(adjusted, 500).DropAdjustment.In(AngularUnit.MOA)
                         - PointAt(baseline, 500).DropAdjustment.In(AngularUnit.MOA);
            delta.Should().BeApproximately(5.0, 0.15);
        }

        /// <summary>
        /// Windage accumulation: splitting the total across the zeroing and per-shot fields, or
        /// putting it all in either one, yields identical trajectories.
        /// </summary>
        [Fact]
        public void WindageAdjustments_AccumulateIdentically()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            ShotParameters Shot(double? zeroW, double? shotW)
            {
                var s = BaseShot(zero);
                if (zeroW != null) s.ZeroWindageAdjustment = new Measurement<AngularUnit>(zeroW.Value, AngularUnit.MOA);
                if (shotW != null) s.ShotWindageAdjustment = new Measurement<AngularUnit>(shotW.Value, AngularUnit.MOA);
                return s;
            }

            var split = cal.Calculate(ammo, rifle, atmo, Shot(2, 3));
            var allZero = cal.Calculate(ammo, rifle, atmo, Shot(5, null));
            var allShot = cal.Calculate(ammo, rifle, atmo, Shot(null, 5));

            split.Length.Should().Be(allZero.Length).And.Be(allShot.Length);
            for (int i = 0; i < split.Length; i++)
            {
                split[i].Windage.Should().Be(allZero[i].Windage, $"@{i}");
                split[i].Windage.Should().Be(allShot[i].Windage, $"@{i}");
            }
        }

        /// <summary>
        /// Drop accumulation: folding the per-shot elevation into ZeroDropAdjustment gives the
        /// same trajectory as keeping it in ShotDropAdjustment.
        /// </summary>
        [Fact]
        public void DropAdjustments_AccumulateIdentically()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;
            var extra = new Measurement<AngularUnit>(4, AngularUnit.MOA);

            var split = BaseShot(zero);
            split.ShotDropAdjustment = extra;

            var combined = BaseShot(zero + extra);

            var a = cal.Calculate(ammo, rifle, atmo, split);
            var b = cal.Calculate(ammo, rifle, atmo, combined);

            a.Length.Should().Be(b.Length);
            for (int i = 0; i < a.Length; i++)
                a[i].Drop.Should().Be(b[i].Drop, $"@{i}");
        }

        /// <summary>
        /// Apply copies both calculated zeroing adjustments onto the shot.
        /// </summary>
        [Fact]
        public void Apply_CopiesBothAdjustments()
        {
            var shot = new ShotParameters();
            var zero = new ZeroCalculatedParameters(
                new Measurement<AngularUnit>(6.5, AngularUnit.MOA),
                new Measurement<AngularUnit>(-1.25, AngularUnit.MOA));

            shot.Apply(zero);

            shot.ZeroDropAdjustment.Should().Be(zero.ZeroDropAdjustment);
            shot.ZeroWindageAdjustment.Should().Be(zero.ZeroWindageAdjustment);
        }

        /// <summary>
        /// A null windage on the calculated parameters clears the shot's windage adjustment.
        /// </summary>
        [Fact]
        public void Apply_NullWindage_ClearsWindage()
        {
            var shot = new ShotParameters { ZeroWindageAdjustment = new Measurement<AngularUnit>(3, AngularUnit.MOA) };
            var zero = new ZeroCalculatedParameters(new Measurement<AngularUnit>(5, AngularUnit.MOA));

            shot.Apply(zero);

            shot.ZeroDropAdjustment.Should().Be(zero.ZeroDropAdjustment);
            shot.ZeroWindageAdjustment.Should().BeNull();
        }

        [Fact]
        public void Apply_Null_Throws()
        {
            var shot = new ShotParameters();
            ((Action)(() => shot.Apply(null))).Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// A shot with Apply'd adjustments produces the same trajectory as setting the fields directly.
        /// </summary>
        [Fact]
        public void Apply_MatchesDirectFieldAssignment()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var drop = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;
            var wind = new Measurement<AngularUnit>(2, AngularUnit.MOA);

            var applied = BaseShot(Measurement<AngularUnit>.ZERO);
            applied.Apply(new ZeroCalculatedParameters(drop, wind));

            var direct = BaseShot(drop);
            direct.ZeroWindageAdjustment = wind;

            var a = cal.Calculate(ammo, rifle, atmo, applied);
            var b = cal.Calculate(ammo, rifle, atmo, direct);

            a.Length.Should().Be(b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                a[i].Drop.Should().Be(b[i].Drop, $"@{i}");
                a[i].Windage.Should().Be(b[i].Windage, $"@{i}");
            }
        }

        /// <summary>
        /// Absence of the new fields is a bit-identical no-op vs supplying explicit zeros.
        /// </summary>
        [Fact]
        public void NullAdjustments_MatchExplicitZeros()
        {
            var (ammo, rifle, atmo) = Config();
            var cal = new TrajectoryCalculator();
            var zero = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero).ZeroDropAdjustment;

            var withNulls = BaseShot(zero);
            var withZeros = BaseShot(zero);
            withZeros.ZeroWindageAdjustment = Measurement<AngularUnit>.ZERO;
            withZeros.ShotWindageAdjustment = Measurement<AngularUnit>.ZERO;
            withZeros.ShotDropAdjustment = Measurement<AngularUnit>.ZERO;

            var a = cal.Calculate(ammo, rifle, atmo, withNulls);
            var b = cal.Calculate(ammo, rifle, atmo, withZeros);

            a.Length.Should().Be(b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                a[i].Drop.Should().Be(b[i].Drop, $"@{i}");
                a[i].Windage.Should().Be(b[i].Windage, $"@{i}");
            }
        }
    }
}
