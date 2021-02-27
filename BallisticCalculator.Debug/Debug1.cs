using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallisticCalculator.Debug
{
    static class Debug1
    {
        public static void Do()
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

            var calc = new TrajectoryCalculator();

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
