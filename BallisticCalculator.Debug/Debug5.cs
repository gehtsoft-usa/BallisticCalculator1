using Gehtsoft.Measurements;
using System;

namespace BallisticCalculator.Debug
{
    internal static class Debug5
    {
        public static void Do(String[] _)
        {
            var ammo = new BallisticCalculator.Ammunition(
              weight: new Measurement<WeightUnit>(65, WeightUnit.Grain),
              ballisticCoefficient: new BallisticCalculator.BallisticCoefficient(0.365, BallisticCalculator.DragTableId.G1),
              muzzleVelocity: new Measurement<VelocityUnit>(2600, VelocityUnit.FeetPerSecond),
              bulletDiameter: new Measurement<DistanceUnit>(0.22, DistanceUnit.Inch),
              bulletLength: new Measurement<DistanceUnit>(0.8, DistanceUnit.Inch));

            var sight = new BallisticCalculator.Sight(
                sightHeight: new Measurement<DistanceUnit>(2.5, DistanceUnit.Inch),
                verticalClick: new Measurement<AngularUnit>(0.1, AngularUnit.Mil),
                horizontalClick: new Measurement<AngularUnit>(0.1, AngularUnit.Mil)
                );

            //M16 rifling
            var rifling = new BallisticCalculator.Rifling(
                riflingStep: new Measurement<DistanceUnit>(10, DistanceUnit.Inch),
                direction: BallisticCalculator.TwistDirection.Right);

            //standard 100 yard ACOG zeroing
            var zero = new BallisticCalculator.ZeroingParameters(
                distance: new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                ammunition: null,
                atmosphere: null
                );

            //define rifle by sight, zeroing and rifling parameters
            var rifle = new BallisticCalculator.Rifle(sight: sight, zero: zero, rifling: null);

            //define atmosphere
            var atmosphere = new BallisticCalculator.Atmosphere();

            var calc = new BallisticCalculator.TrajectoryCalculator();
            //shot parameters
            var shot = new BallisticCalculator.ShotParameters()
            {
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Yard),
                Step = DistanceUnit.Yard.New(50),
                //calculate sight angle for the specified zero distance
                SightAngle = calc.SightAngle(ammo, rifle, atmosphere),
                ShotAngle = new Measurement<AngularUnit>(30, AngularUnit.Degree),
            };

            //define winds
            //BallisticCalculator.Wind[] wind = null;
            BallisticCalculator.Wind[] wind = { new BallisticCalculator.Wind(new Measurement<VelocityUnit>(10, VelocityUnit.MilesPerHour), new Measurement<AngularUnit>(90, AngularUnit.Degree)) };

            //calculate trajectory
            var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot, wind);

            foreach (var point in trajectory)
            {
                Console.WriteLine($"{point.Time};{point.Distance.In(DistanceUnit.Yard):N0};{point.Velocity.In(VelocityUnit.FeetPerSecond):N1};{point.Drop.In(DistanceUnit.Inch):N1};{point.LineOfSightElevation.In(DistanceUnit.Inch):N1};{point.Windage.In(DistanceUnit.Inch):N1};{point.DropVsLineOfDeparture.In(DistanceUnit.Inch):N1}");
            }
        }
    }
        
}
