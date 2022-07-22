using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using Svg;

namespace BallisticCalculator.Debug
{
    public static class Debug4
    {
        class MyDrag : DragTable
        {
            public override DragTableId TableId => DragTableId.GC;

            private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
            {
                new DragTableDataPoint(0.000, 0.180),
                new DragTableDataPoint(0.400, 0.178),
                new DragTableDataPoint(0.500, 0.154),
                new DragTableDataPoint(0.600, 0.129),
                new DragTableDataPoint(0.700, 0.131),
                new DragTableDataPoint(0.800, 0.136),
                new DragTableDataPoint(0.825, 0.140),
                new DragTableDataPoint(0.850, 0.144),
                new DragTableDataPoint(0.875, 0.153),
                new DragTableDataPoint(0.900, 0.177),
                new DragTableDataPoint(0.925, 0.226),
                new DragTableDataPoint(0.950, 0.260),
                new DragTableDataPoint(0.975, 0.349),
                new DragTableDataPoint(1.000, 0.427),
                new DragTableDataPoint(1.025, 0.450),
                new DragTableDataPoint(1.050, 0.452),
                new DragTableDataPoint(1.075, 0.450),
                new DragTableDataPoint(1.100, 0.447),
                new DragTableDataPoint(1.150, 0.437),
                new DragTableDataPoint(1.200, 0.429),
                new DragTableDataPoint(1.300, 0.418),
                new DragTableDataPoint(1.400, 0.406),
                new DragTableDataPoint(1.500, 0.394),
                new DragTableDataPoint(1.600, 0.382),
                new DragTableDataPoint(1.800, 0.359),
                new DragTableDataPoint(2.000, 0.339),
                new DragTableDataPoint(2.200, 0.321),
                new DragTableDataPoint(2.400, 0.301),
                new DragTableDataPoint(2.600, 0.280),
                new DragTableDataPoint(3.000, 0.250),
                new DragTableDataPoint(4.000, 0.200),
                new DragTableDataPoint(5.000, 0.180),
            };

            public MyDrag() : base(gDataPoints)
            {
            }
        }

        private static TrajectoryPoint[] Calculate(double bc, DragTableId id)
        {
            var dragTable = new MyDrag();

            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(bc, id),
                muzzleVelocity: new Measurement<VelocityUnit>(555, VelocityUnit.MetersPerSecond),
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
                distance: new Measurement<DistanceUnit>(50, DistanceUnit.Yard),
                ammunition: null,
                atmosphere: null
                );

            //define rifle by sight, zeroing and rifling parameters
            var rifle = new Rifle(sight: sight, zero: zero, rifling: rifling);

            //define atmosphere
            var atmosphere = new Atmosphere(
                altitude: new Measurement<DistanceUnit>(0, DistanceUnit.Foot),
                pressure: new Measurement<PressureUnit>(29.92, PressureUnit.InchesOfMercury),
                pressureAtSeaLevel: false,
                temperature: new Measurement<TemperatureUnit>(59, TemperatureUnit.Fahrenheit),
                humidity: 0.78);

            var calc = new TrajectoryCalculator();

            //shot parameters
            var shot = new ShotParameters()
            {
                MaximumDistance = new Measurement<DistanceUnit>(2000, DistanceUnit.Meter),
                Step = new Measurement<DistanceUnit>(100, DistanceUnit.Meter),
                //calculate sight angle for the specified zero distance
                SightAngle = calc.SightAngle(ammo, rifle, atmosphere, id == DragTableId.GC ? dragTable : null)
            };

            //calculate trajectory
            return calc.Calculate(ammo, rifle, atmosphere, shot, null, id == DragTableId.GC ? dragTable : null);
        }

        public static void Do(String[] _)
        {
            var trajectory1 = Calculate(0.223, DragTableId.G7);
            var trajectory2 = Calculate(0.2517, DragTableId.GC);
            var trajectory3 = Calculate(1, DragTableId.GC);

            //print trajectory
            for (int i = 0; i < trajectory1.Length; i++)
                Console.WriteLine($"{trajectory1[i].Distance.In(DistanceUnit.Meter):N0}, " +
                                  $"{trajectory1[i].Velocity.In(VelocityUnit.MetersPerSecond):N0}|{trajectory2[i].Velocity.In(VelocityUnit.MetersPerSecond):N0}|{trajectory3[i].Velocity.In(VelocityUnit.MetersPerSecond):N0}, " +
                                  $"{trajectory1[i].Drop.In(DistanceUnit.Inch):N2}|{trajectory2[i].Drop.In(DistanceUnit.Inch):N2}|{trajectory3[i].Drop.In(DistanceUnit.Inch):N2}");
        }
    }
}
