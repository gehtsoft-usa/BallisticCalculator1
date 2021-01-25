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

        private void ValidatePoint(TrajectoryPoint point, TrajectoryPoint template)
        {
        }

        [Theory]
        [InlineData("g1_nowind", 0.005, 0.2, 0.2)]
        [InlineData("g1_twist", 0.005, 0.2, 0.02)]
        [InlineData("g7_nowind", 0.005, 0.2, 0.2)]
        [InlineData("g1_wind", 0.005, 0.2, 0.2)]
        [InlineData("g1_wind_hot", 0.005, 0.2, 0.2)]
        [InlineData("g1_wind_cold", 0.005, 0.2, 0.2)]
        public void TrajectoryTest(string name, double velocityAccuracyInPercent, double dropAccuracyInMOA, double windageAccuracyInMOA)
        {
            TableLoader template = TableLoader.FromResource(name);

            var cal = new TrajectoryCaculator();

            ShotParameters shot = new ShotParameters()
            {
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                SightAngle = cal.SightAngle(template.Ammunition, template.Rifle, template.Atmosphere)
            };

            var winds = template.Wind == null ? null : new Wind[] { template.Wind };

            var trajectory = cal.Calculate(template.Ammunition, template.Rifle, template.Atmosphere, shot, winds);

            trajectory.Length.Should().Be(template.Trajectory.Count);

            for (int i = 0; i < trajectory.Length; i++)
            {
                var point = trajectory[i];
                var templatePoint = template.Trajectory[i];

                point.Distance.In(templatePoint.Distance.Unit).Should().BeApproximately(templatePoint.Distance.Value, templatePoint.Distance.Value * velocityAccuracyInPercent, $"@{point.Distance:N0}");
                point.Velocity.In(templatePoint.Velocity.Unit).Should().BeApproximately(templatePoint.Velocity.Value, templatePoint.Velocity.Value * velocityAccuracyInPercent, $"@{point.Distance:N0}");

                var dropAccuracyInInch = Measurement<AngularUnit>.Convert(dropAccuracyInMOA, AngularUnit.MOA, AngularUnit.InchesPer100Yards) * templatePoint.Distance.In(DistanceUnit.Yard) / 100;
                var windageAccuracyInInch = Measurement<AngularUnit>.Convert(windageAccuracyInMOA, AngularUnit.MOA, AngularUnit.InchesPer100Yards) * templatePoint.Distance.In(DistanceUnit.Yard) / 100;

                point.Drop.In(DistanceUnit.Inch).Should().BeApproximately(templatePoint.Drop.In(DistanceUnit.Inch), dropAccuracyInInch, $"@{point.Distance:N0}");
                point.Windage.In(DistanceUnit.Inch).Should().BeApproximately(templatePoint.Windage.In(DistanceUnit.Inch), windageAccuracyInInch, $"@{point.Distance:N0}");
            }

        }
       
        [Fact]
        public void Debug()
        {
            //define M855 projectile out of 20 inch barrel
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(62, WeightUnit.Grain),
                muzzleVelocity: new Measurement<VelocityUnit>(3095, VelocityUnit.FeetPerSecond),
                ballisticCoefficient: new BallisticCoefficient(0.304, DragTableId.G1),
                bulletDiameter: new Measurement<DistanceUnit>(0.224, DistanceUnit.Inch),
                bulletLength: new Measurement<DistanceUnit>(0.9, DistanceUnit.Inch));

            //define ACOG scope
            var sight = new Sight(
                sightHeight: new Measurement<DistanceUnit>(3.5, DistanceUnit.Inch),
                verticalClick: new Measurement<AngularUnit>(1.0 / 3.0, AngularUnit.InchesPer100Yards),
                horizontalClick: new Measurement<AngularUnit>(1.0 / 3.0, AngularUnit.InchesPer100Yards)
                );

            //M16 rifling
            var rifling = new Rifling(
                riflingStep: new Measurement<DistanceUnit>(12, DistanceUnit.Inch),
                direction: TwistDirection.Right);

            //standard 100 yard ACOG zeroing
            var zero = new ZeroingParameters(
                distance: new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                ammunition: null,
                atmosphere: null
                );

            //define rifle by sight, zeroing and rifling parameters
            var rifle = new Rifle(sight: sight, zero: zero, rifling: rifling);

            //define atmosphere
            var atmosphere = new Atmosphere(
                pressure: new Measurement<PressureUnit>(29.92, PressureUnit.InchesOfMercury),
                pressureAtSeaLevel: true,
                altitude: new Measurement<DistanceUnit>(100, DistanceUnit.Foot),
                temperature: new Measurement<TemperatureUnit>(59, TemperatureUnit.Fahrenheit),
                humidity: 0.78);

            var calc = new TrajectoryCaculator();

            //shot parameters
            var shot = new ShotParameters()
            {
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                Step = new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                //calculate sight angle for the specified zero distance
                SightAngle = calc.SightAngle(ammo, rifle, atmosphere)
            };

            //define winds

            Wind[] wind = new Wind[2]
            {
                new Wind()
                {
                    Direction = new Measurement<AngularUnit>(45, AngularUnit.Degree),
                    Velocity = new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour),
                    MaximumRange = new Measurement<DistanceUnit>(500, DistanceUnit.Yard),
                },
                new Wind()
                {
                    Direction = new Measurement<AngularUnit>(15, AngularUnit.Degree),
                    Velocity = new Measurement<VelocityUnit>(5, VelocityUnit.MilesPerHour),
                }
            };


            //calculate trajectory
            var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot, wind);

            //print trajectory
            foreach (var point in trajectory)
                Console.WriteLine($"{point.Time} {point.Distance.In(DistanceUnit.Yard):N0} {point.Velocity.In(VelocityUnit.FeetPerSecond):N0} {point.Drop.In(DistanceUnit.Inch):N2} {point.Windage.In(DistanceUnit.Inch):N2}");




        }
               
    }
}
