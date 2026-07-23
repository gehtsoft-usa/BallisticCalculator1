using System;
using AwesomeAssertions;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Tools
{
    public class MovingTargetLeadTest
    {
        private static Measurement<VelocityUnit> Mph(double v) => new Measurement<VelocityUnit>(v, VelocityUnit.MilesPerHour);
        private static Measurement<AngularUnit> Deg(double a) => new Measurement<AngularUnit>(a, AngularUnit.Degree);

        /// <summary>
        /// A full crossing target (90 degrees) leads by exactly speed times time of flight.
        /// </summary>
        [Fact]
        public void FullCrossing_LeadEqualsSpeedTimesTof()
        {
            var lead = MovingTargetLead.Lead(Mph(10), Deg(90), TimeSpan.FromSeconds(1.5));

            double expectedMeters = Mph(10).In(VelocityUnit.MetersPerSecond) * 1.5;
            lead.In(DistanceUnit.Meter).Should().BeApproximately(expectedMeters, 1e-9);
        }

        /// <summary>
        /// Head-on and straight-away targets (0 and 180 degrees) have no crossing component, so no lead.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(180)]
        public void NoCrossingComponent_NoLead(double directionDeg)
        {
            var lead = MovingTargetLead.Lead(Mph(15), Deg(directionDeg), TimeSpan.FromSeconds(1));
            lead.In(DistanceUnit.Meter).Should().BeApproximately(0, 1e-9);
        }

        /// <summary>
        /// The two crossing directions produce equal-magnitude, opposite-sign leads (left + / right −).
        /// </summary>
        [Fact]
        public void OppositeCrossings_FlipSign()
        {
            var fromRight = MovingTargetLead.Lead(Mph(12), Deg(90), TimeSpan.FromSeconds(1));
            var fromLeft = MovingTargetLead.Lead(Mph(12), Deg(270), TimeSpan.FromSeconds(1));

            fromRight.In(DistanceUnit.Meter).Should().BeGreaterThan(0);
            fromLeft.In(DistanceUnit.Meter).Should().BeApproximately(-fromRight.In(DistanceUnit.Meter), 1e-9);
        }

        /// <summary>
        /// A partial crossing scales by the sine of the direction.
        /// </summary>
        [Fact]
        public void PartialCrossing_ScalesBySine()
        {
            var full = MovingTargetLead.Lead(Mph(10), Deg(90), TimeSpan.FromSeconds(2));
            var partial = MovingTargetLead.Lead(Mph(10), Deg(30), TimeSpan.FromSeconds(2));

            partial.In(DistanceUnit.Meter).Should().BeApproximately(full.In(DistanceUnit.Meter) * Math.Sin(Math.PI / 6), 1e-9);
        }

        /// <summary>
        /// The angular lead is the arctangent of the linear lead over the range.
        /// </summary>
        [Fact]
        public void LeadAngle_IsAtanOfLeadOverRange()
        {
            var speed = Mph(10);
            var dir = Deg(90);
            var tof = TimeSpan.FromSeconds(0.6);
            var range = new Measurement<DistanceUnit>(300, DistanceUnit.Yard);

            var lead = MovingTargetLead.Lead(speed, dir, tof);
            var angle = MovingTargetLead.LeadAngle(speed, dir, tof, range);

            double expected = Math.Atan(lead.In(DistanceUnit.Meter) / range.In(DistanceUnit.Meter));
            angle.In(AngularUnit.Radian).Should().BeApproximately(expected, 1e-9);
        }

        /// <summary>
        /// The unit of the result is chosen by the caller via In; the same lead reads consistently across units.
        /// </summary>
        [Fact]
        public void Lead_ConvertsToRequestedUnits()
        {
            var lead = MovingTargetLead.Lead(Mph(20), Deg(90), TimeSpan.FromSeconds(1));

            double meters = lead.In(DistanceUnit.Meter);
            lead.In(DistanceUnit.Inch).Should().BeApproximately(meters / 0.0254, 1e-6);
            lead.In(DistanceUnit.Centimeter).Should().BeApproximately(meters * 100, 1e-6);
        }

        /// <summary>
        /// The TrajectoryPoint overloads use the point's time of flight and distance.
        /// </summary>
        [Fact]
        public void TrajectoryPointOverloads_UsePointTimeAndDistance()
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));
            var rifle = new Rifle(
                sight: new Sight(new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(new Measurement<DistanceUnit>(100, DistanceUnit.Yard), null, null));
            var atmo = new Atmosphere();
            var cal = new TrajectoryCalculator();
            var shot = new ShotParameters
            {
                Step = new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(500, DistanceUnit.Yard),
            };
            shot.Apply(cal.CalculateZeroParameters(ammo, atmo, rifle, rifle.Zero));

            var traj = cal.Calculate(ammo, rifle, atmo, shot);
            var point = System.Array.Find(traj, p => p != null && Math.Abs(p.Distance.In(DistanceUnit.Yard) - 300) < 0.5);
            point.Should().NotBeNull();

            var speed = Mph(8);
            var dir = Deg(90);

            MovingTargetLead.Lead(speed, dir, point).In(DistanceUnit.Meter)
                .Should().Be(MovingTargetLead.Lead(speed, dir, point.Time).In(DistanceUnit.Meter));
            MovingTargetLead.LeadAngle(speed, dir, point).In(AngularUnit.Mil)
                .Should().Be(MovingTargetLead.LeadAngle(speed, dir, point.Time, point.Distance).In(AngularUnit.Mil));
        }

        [Fact]
        public void NullPoint_Throws()
        {
            ((Action)(() => MovingTargetLead.Lead(Mph(10), Deg(90), (TrajectoryPoint)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => MovingTargetLead.LeadAngle(Mph(10), Deg(90), (TrajectoryPoint)null))).Should().Throw<ArgumentNullException>();
        }
    }
}
