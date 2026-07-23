using AwesomeAssertions;
using Gehtsoft.Measurements;
using System;
using System.Linq;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Tests for <see cref="TrajectoryCalculator.CalculateZeroParameters"/>, which solves the
    /// vertical + horizontal barrel adjustments by driving the full trajectory (so drift, Coriolis
    /// and aero jump are folded into the zero) rather than a private loop.
    /// </summary>
    public class ZeroParametersTest
    {
        private static Ammunition Ammo() => new Ammunition(
            weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
            ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
            muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
            bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
            bulletLength: new Measurement<DistanceUnit>(1.210, DistanceUnit.Inch));

        private static Sight Sight() => new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch),
            Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO);

        // With rifling — spin drift active.
        private static Rifle RifledRifle(double zeroYd) => new Rifle(
            sight: Sight(),
            zero: new ZeroingParameters(new Measurement<DistanceUnit>(zeroYd, DistanceUnit.Yard), null, null),
            rifling: new Rifling(new Measurement<DistanceUnit>(11, DistanceUnit.Inch), TwistDirection.Right));

        // No rifling — no drift, so windage comes only from Coriolis (if latitude is set).
        private static Rifle PlainRifle(double zeroYd) => new Rifle(
            sight: Sight(),
            zero: new ZeroingParameters(new Measurement<DistanceUnit>(zeroYd, DistanceUnit.Yard), null, null),
            rifling: null);

        // Reproduce the solver's trajectory (single row at the zero distance) with the returned
        // adjustments Apply'd, and return the impact point.
        private static TrajectoryPoint ImpactAtZero(TrajectoryCalculator cal, Ammunition ammo, Rifle rifle,
            Atmosphere atmo, ZeroingParameters zero, ZeroCalculatedParameters result, ShotParameters context = null,
            Wind[] wind = null)
        {
            var shot = new ShotParameters
            {
                Step = zero.Distance,
                MaximumDistance = zero.Distance,
                ShotAngle = context?.ShotAngle,
                BarrelAzimuth = context?.BarrelAzimuth,
                Latitude = context?.Latitude,
            };
            shot.Apply(result);
            var traj = cal.Calculate(ammo, rifle, atmo, shot, wind);
            return traj.Last(p => p != null);
        }

        /// <summary>
        /// With no drift and no Coriolis the horizontal solve is a no-op (windage stays null) and the
        /// vertical adjustment alone puts the impact on aim at the zero distance.
        /// </summary>
        [Fact]
        public void PlainZero_NoWindage_ZeroesVertically()
        {
            var ammo = Ammo();
            var rifle = PlainRifle(100);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();

            var result = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero);

            result.ZeroWindageAdjustment.Should().BeNull("no drift and no Coriolis ⇒ nothing to correct horizontally");
            result.ZeroDropAdjustment.In(AngularUnit.MOA).Should().BeGreaterThan(0, "the barrel is elevated to zero");

            var impact = ImpactAtZero(cal, ammo, rifle, atmo, rifle.Zero, result);
            impact.Drop.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
            impact.Windage.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
        }

        /// <summary>
        /// The returned adjustments actually zero the rifle: driven back through the trajectory the
        /// impact lands on the aim point at the zero distance.
        /// </summary>
        [Fact]
        public void ComputedZero_PutsImpactOnAim()
        {
            var ammo = Ammo();
            var rifle = RifledRifle(200);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();

            var result = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero);
            var impact = ImpactAtZero(cal, ammo, rifle, atmo, rifle.Zero, result);

            impact.Drop.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
            impact.Windage.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
        }

        /// <summary>
        /// A right-hand twist drifts right, so the horizontal zero dials a positive (left) correction.
        /// </summary>
        [Fact]
        public void RightTwist_ProducesLeftWindageCorrection()
        {
            var ammo = Ammo();
            var rifle = RifledRifle(1000);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();

            var result = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero);

            result.ZeroWindageAdjustment.Should().NotBeNull();
            result.ZeroWindageAdjustment.Value.In(AngularUnit.MOA).Should().BeGreaterThan(0, "left correction counters the right drift");
        }

        /// <summary>
        /// Non-zero impact offsets are honored: the impact lands at the requested offset, not the aim point.
        /// </summary>
        [Fact]
        public void Offsets_LandAtRequestedOffset()
        {
            var ammo = Ammo();
            var rifle = RifledRifle(100);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();

            var zero = new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null)
            {
                VerticalOffset = new Measurement<DistanceUnit>(2, DistanceUnit.Inch),    // up
                HorizontalOffset = new Measurement<DistanceUnit>(1, DistanceUnit.Inch),  // left
            };

            var result = cal.CalculateZeroParameters(ammo, atmo, rifle, zero);
            var impact = ImpactAtZero(cal, ammo, rifle, atmo, zero, result);

            impact.Drop.In(DistanceUnit.Inch).Should().BeApproximately(2, 0.02);
            impact.Windage.In(DistanceUnit.Inch).Should().BeApproximately(1, 0.02);
        }

        /// <summary>
        /// Coriolis is folded into the zero: on a smooth-bore (no drift) a set latitude still produces
        /// a windage correction (N hemisphere deflects right ⇒ positive/left correction).
        /// </summary>
        [Fact]
        public void Coriolis_AffectsWindageZero()
        {
            var ammo = Ammo();
            var rifle = PlainRifle(1000);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();

            var noLat = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero);
            noLat.ZeroWindageAdjustment.Should().BeNull("no drift, no latitude ⇒ no horizontal correction");

            var context = new ShotParameters { Latitude = new Measurement<AngularUnit>(60, AngularUnit.Degree) };
            var withLat = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero, context);

            withLat.ZeroWindageAdjustment.Should().NotBeNull();
            withLat.ZeroWindageAdjustment.Value.In(AngularUnit.MOA).Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Zeroing under a crosswind folds the wind deflection (and aero jump) into the zero: the
        /// computed adjustments put the impact on aim when re-run under that same wind.
        /// </summary>
        [Fact]
        public void ZeroWithWind_PutsImpactOnAim()
        {
            var ammo = Ammo();
            var rifle = RifledRifle(300);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var wind = new[] { new Wind(new Measurement<VelocityUnit>(12, VelocityUnit.MilesPerHour),
                                        new Measurement<AngularUnit>(90, AngularUnit.Degree)) };

            var result = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero, shot: null, wind: wind);
            var impact = ImpactAtZero(cal, ammo, rifle, atmo, rifle.Zero, result, context: null, wind: wind);

            impact.Drop.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
            impact.Windage.In(DistanceUnit.Inch).Should().BeApproximately(0, 0.02);
        }

        /// <summary>
        /// A crosswind from the right pushes the bullet left, so it adds a rightward (negative)
        /// component to the windage zero relative to the calm-air (drift-only, positive) zero.
        /// </summary>
        [Fact]
        public void Wind_ShiftsWindageZero()
        {
            var ammo = Ammo();
            var rifle = RifledRifle(300);
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var wind = new[] { new Wind(new Measurement<VelocityUnit>(12, VelocityUnit.MilesPerHour),
                                        new Measurement<AngularUnit>(90, AngularUnit.Degree)) };

            var calm = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero);
            var windy = cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero, shot: null, wind: wind);

            double calmMoa = calm.ZeroWindageAdjustment?.In(AngularUnit.MOA) ?? 0;
            double windyMoa = windy.ZeroWindageAdjustment?.In(AngularUnit.MOA) ?? 0;
            windyMoa.Should().BeLessThan(calmMoa, "a wind from the right pushes impact left ⇒ the correction shifts right");
        }

        [Fact]
        public void NullArguments_Throw()
        {
            var cal = new TrajectoryCalculator();
            var ammo = Ammo();
            var rifle = RifledRifle(100);
            var atmo = new Atmosphere();

            ((Action)(() => cal.CalculateZeroParameters(null, atmo, rifle, rifle.Zero))).Should().Throw<ArgumentNullException>();
            ((Action)(() => cal.CalculateZeroParameters(ammo, atmo, null, rifle.Zero))).Should().Throw<ArgumentNullException>();
            ((Action)(() => cal.CalculateZeroParameters(ammo, atmo, rifle, null))).Should().Throw<ArgumentNullException>();
        }
    }
}
