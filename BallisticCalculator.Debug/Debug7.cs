using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;

namespace BallisticCalculator.Debug
{
    internal static class Debug7
    {
        public static void Do(String[] args)
        {
            var ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.404, DragTableId.G1),
                BulletDiameter = DistanceUnit.Centimeter.New(0.782),
                BulletLength = DistanceUnit.Centimeter.New(2.872),
                Weight = WeightUnit.Grain.New(150),
                MuzzleVelocity = VelocityUnit.MetersPerSecond.New(830)
            };

            var atmo = new Atmosphere(DistanceUnit.Meter.New(0), PressureUnit.Pascal.New(101.321), TemperatureUnit.Celsius.New(15), 0);

            var rifle = new Rifle()
            {
                Rifling = new Rifling()
                {
                    Direction = TwistDirection.Right,
                    RiflingStep = DistanceUnit.Centimeter.New(25.4),
                },
                Sight = new Sight()
                {
                    SightHeight = DistanceUnit.Centimeter.New(5.35),
                },
                Zero = new ZeroingParameters()
                {
                    Distance = DistanceUnit.Meter.New(100)
                }
            };

            var calc = new TrajectoryCalculator();

            var angle = calc.SightAngle(ammo, rifle, atmo);

            Console.WriteLine("Sight Angle {0}", angle.To(AngularUnit.MOA).ToString());
        }
    }
}
