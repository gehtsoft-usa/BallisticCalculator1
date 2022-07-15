using Gehtsoft.Measurements;
using System;

namespace BallisticCalculator.Debug
{
    internal static class Debug5
    {
        public static void Do(String[] _)
        {
            var ammo = new BallisticCalculator.Ammunition(
              weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
              ballisticCoefficient: new BallisticCalculator.BallisticCoefficient(0.262, BallisticCalculator.DragTableId.G7),
              muzzleVelocity: new Measurement<VelocityUnit>(2580, VelocityUnit.FeetPerSecond),
              bulletDiameter: new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
              bulletLength: new Measurement<DistanceUnit>(1.272, DistanceUnit.Inch));

            //define ACOG scope
            var sight = new BallisticCalculator.Sight(
                sightHeight: new Measurement<DistanceUnit>(1.5, DistanceUnit.Inch),
                verticalClick: new Measurement<AngularUnit>(0.1, AngularUnit.Mil),
                horizontalClick: new Measurement<AngularUnit>(0.1, AngularUnit.Mil)
                );

            //M16 rifling
            var rifling = new BallisticCalculator.Rifling(
                riflingStep: new Measurement<DistanceUnit>(10, DistanceUnit.Inch),
                direction: BallisticCalculator.TwistDirection.Right);

            //standard 100 yard ACOG zeroing
            var zero = new BallisticCalculator.ZeroingParameters(
                distance: new Measurement<DistanceUnit>(100, DistanceUnit.Meter),
                ammunition: null,
                atmosphere: null
                );

            //define rifle by sight, zeroing and rifling parameters
            var rifle = new BallisticCalculator.Rifle(sight: sight, zero: zero, rifling: rifling);

            //define atmosphere
            var atmosphere = new BallisticCalculator.Atmosphere(
                altitude: new Measurement<DistanceUnit>(0, DistanceUnit.Meter),
                pressure: new Measurement<PressureUnit>(25.52, PressureUnit.InchesOfMercury),
                pressureAtSeaLevel: false,
                temperature: new Measurement<TemperatureUnit>(21, TemperatureUnit.Celsius),
                humidity: 0.2);

            var calc = new BallisticCalculator.TrajectoryCalculator();
            //shot parameters
            var shot = new BallisticCalculator.ShotParameters()
            {
                MaximumDistance = new Measurement<DistanceUnit>(1000, DistanceUnit.Meter),
                Step = DistanceUnit.Meter.New(50),
                //calculate sight angle for the specified zero distance
                SightAngle = calc.SightAngle(ammo, rifle, atmosphere),
                ShotAngle = new Measurement<AngularUnit>(9, AngularUnit.Degree)
            };

            //define winds
            BallisticCalculator.Wind[] wind = null;

            //calculate trajectory
            var trajectory = calc.Calculate(ammo, rifle, atmosphere, shot, wind);

            foreach (var point in trajectory)
            {
                Console.WriteLine($"{point.Time} {point.Distance.In(DistanceUnit.Meter):N0} {point.Velocity.In(VelocityUnit.FeetPerSecond):N0} {point.DropAdjustment.In(AngularUnit.Mil):N2} {point.WindageAdjustment.In(AngularUnit.Mil):N2}");
            }
        }
    }
        
}
