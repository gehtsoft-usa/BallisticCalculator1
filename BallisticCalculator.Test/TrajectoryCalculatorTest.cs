using FluentAssertions;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BallisticCalculator.Test
{
    public class TrajectoryCalculatorTest
    {
        [Theory]
        [InlineData(0.365, DragTableId.G1, 2600, VelocityUnit.FeetPerSecond, 100, DistanceUnit.Yard, 5.674, AngularUnit.MOA, 0.5e-2)]
        [InlineData(0.47, DragTableId.G7, 725, VelocityUnit.MetersPerSecond, 200, DistanceUnit.Meter, 8.171, AngularUnit.MOA, 0.5e-2)]
        public void Zero1(double ballisticCoefficient, DragTableId ballisticTable, double muzzleVelocity, VelocityUnit velocityUnit, double zeroDistance, DistanceUnit distanceUnit, double sightAngle, AngularUnit sightAngleUnit, double sightAngleAccuracy)
        {
            Ammunition ammunition = new Ammunition(
                weight: new Measurement<WeightUnit>(69, WeightUnit.Grain),
                muzzleVelocity: new Measurement<VelocityUnit>(muzzleVelocity, velocityUnit),
                ballisticCoefficient: new BallisticCoefficient(ballisticCoefficient, ballisticTable)
                );

            Rifle rifle = new Rifle(
                sight: new Sight(sightHeight: new Measurement<DistanceUnit>(3.2, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(distance: new Measurement<DistanceUnit>(zeroDistance, distanceUnit), ammunition: null, atmosphere: null));

            Atmosphere atmosphere = new Atmosphere();       //default atmosphere

            var sightAngle1 = (new TrajectoryCaculator()).SightAngle(ammunition, rifle, atmosphere);
            sightAngle1.In(sightAngleUnit).Should().BeApproximately(sightAngle, sightAngleAccuracy);
        }

        private void ValidatePoint(TrajectoryPoint point, Measurement<DistanceUnit> distance, Measurement<VelocityUnit> velocity, 
            Measurement<DistanceUnit> drop, Measurement<DistanceUnit> windage,
            Measurement<EnergyUnit> energy)
        {
            point.Distance.In(distance.Unit).Should().BeApproximately(distance.Value, distance.Value * 0.005);
            point.Velocity.In(velocity.Unit).Should().BeApproximately(velocity.Value, velocity.Value * 0.005);

            var adjustmentAccuracy = distance.In(DistanceUnit.Yard) / 1000; //0.1"@100 yard, 0.2@200 yard, 0.3@300 yards...

            point.Drop.In(DistanceUnit.Inch).Should().BeApproximately(drop.In(DistanceUnit.Inch), adjustmentAccuracy);
            point.Windage.In(DistanceUnit.Inch).Should().BeApproximately(windage.In(DistanceUnit.Inch), adjustmentAccuracy);
        }

        [Fact]
        public void G1_DefaultAtmosphere_NoWind()
        {
            Ammunition ammunition = new Ammunition(
               weight: new Measurement<WeightUnit>(69, WeightUnit.Grain),
               muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
               ballisticCoefficient: new BallisticCoefficient(0.365, DragTableId.G1)
               );

            Rifle rifle = new Rifle(
                sight: new Sight(sightHeight: new Measurement<DistanceUnit>(2.5, DistanceUnit.Inch), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                zero: new ZeroingParameters(distance: new Measurement<DistanceUnit>(100, DistanceUnit.Yard), ammunition: null, atmosphere: null));

            var atmosphere = new Atmosphere();

            var cal = new TrajectoryCaculator();

            ShotParameters shot = new ShotParameters()
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                SightAngle = cal.SightAngle(ammunition, rifle, atmosphere)
            };



            var trajectory = cal.Calculate(ammunition, rifle, atmosphere, shot, null);

            trajectory.Length.Should().Be(21);

            ValidatePoint(trajectory[0], new Measurement<DistanceUnit>(0, DistanceUnit.Yard), new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-2.5, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(975.5, EnergyUnit.FootPound));

            ValidatePoint(trajectory[1], new Measurement<DistanceUnit>(50, DistanceUnit.Yard), new Measurement<VelocityUnit>(2478.7, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-0.5, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(886.6, EnergyUnit.FootPound));

            ValidatePoint(trajectory[2], new Measurement<DistanceUnit>(100, DistanceUnit.Yard), new Measurement<VelocityUnit>(2360.6, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(0, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(804.1, EnergyUnit.FootPound));

            ValidatePoint(trajectory[4], new Measurement<DistanceUnit>(200, DistanceUnit.Yard), new Measurement<VelocityUnit>(2133.9, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-3.8, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(657.1, EnergyUnit.FootPound));

            ValidatePoint(trajectory[6], new Measurement<DistanceUnit>(300, DistanceUnit.Yard), new Measurement<VelocityUnit>(1920.1, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-15.2, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(532.0, EnergyUnit.FootPound));

            ValidatePoint(trajectory[8], new Measurement<DistanceUnit>(400, DistanceUnit.Yard), new Measurement<VelocityUnit>(1720.5, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-36.1, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(427.2, EnergyUnit.FootPound));

            ValidatePoint(trajectory[10], new Measurement<DistanceUnit>(500, DistanceUnit.Yard), new Measurement<VelocityUnit>(1537.5, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-68.8, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(341.1, EnergyUnit.FootPound));

            ValidatePoint(trajectory[12], new Measurement<DistanceUnit>(600, DistanceUnit.Yard), new Measurement<VelocityUnit>(1374.5, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-116.2, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(272.6, EnergyUnit.FootPound));

            ValidatePoint(trajectory[14], new Measurement<DistanceUnit>(700, DistanceUnit.Yard), new Measurement<VelocityUnit>(1235.8, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-182.1, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(272.6, EnergyUnit.FootPound));

            ValidatePoint(trajectory[16], new Measurement<DistanceUnit>(800, DistanceUnit.Yard), new Measurement<VelocityUnit>(1126.2, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-270.8, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(183.0, EnergyUnit.FootPound));

            ValidatePoint(trajectory[18], new Measurement<DistanceUnit>(900, DistanceUnit.Yard), new Measurement<VelocityUnit>(1045.3, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-386.5, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(157.7, EnergyUnit.FootPound));

            ValidatePoint(trajectory[20], new Measurement<DistanceUnit>(1000, DistanceUnit.Yard), new Measurement<VelocityUnit>(984.7, VelocityUnit.FeetPerSecond),
                                         new Measurement<DistanceUnit>(-533.8, DistanceUnit.Inch), new Measurement<DistanceUnit>(0, DistanceUnit.Inch),
                                         new Measurement<EnergyUnit>(139.9, EnergyUnit.FootPound));
        }
       
               
    }
}
