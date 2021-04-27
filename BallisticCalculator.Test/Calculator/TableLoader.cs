using Gehtsoft.Measurements;
using NReco.Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BallisticCalculator.Test.Calculator
{
    internal class TableLoader
    {
        public Ammunition Ammunition { get; }
        public Rifle Rifle { get; }
        public Atmosphere Atmosphere { get; }
        public Wind Wind { get; }
        public ShotParameters ShotParameters { get; set; }

        private readonly List<TrajectoryPoint> mTrajectory = new List<TrajectoryPoint>();
        public IReadOnlyList<TrajectoryPoint> Trajectory => mTrajectory;

        public static TableLoader FromResource(string name)
        {
            using Stream stream = typeof(TableLoader).Assembly.GetManifestResourceStream($"BallisticCalculator.Test.resources.{name}.txt");
            return new TableLoader(stream);
        }

        public TableLoader(Stream table)
        {
            using var text = new StreamReader(table, Encoding.UTF8, true, 4096, true);
            CsvReader csv = new CsvReader(text, ";");
            List<string> units = new List<string>();
            while (csv.Read())
            {
                if (csv.FieldsCount == 0)
                    continue;
                if (csv[0] == "ammo")
                {
                    if (csv.FieldsCount < 6)
                    {
                        Ammunition = new Ammunition(
                            ballisticCoefficient: new BallisticCoefficient(csv[1]),
                            weight: new Measurement<WeightUnit>(csv[2]),
                            muzzleVelocity: new Measurement<VelocityUnit>(csv[3]));
                    }
                    else
                    {
                        Ammunition = new Ammunition(
                                    weight: new Measurement<WeightUnit>(csv[2]),
                                    ballisticCoefficient: new BallisticCoefficient(csv[1]),
                                    muzzleVelocity: new Measurement<VelocityUnit>(csv[3]),
                                    bulletDiameter: new Measurement<DistanceUnit>(csv[4]),
                                    bulletLength: new Measurement<DistanceUnit>(csv[5]));
                    }
                }
                else if (csv[0] == "rifle")
                {
                    if (csv.FieldsCount < 5)
                    {
                        Rifle = new Rifle(
                            sight: new Sight(sightHeight: new Measurement<DistanceUnit>(csv[1]), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                            zero: new ZeroingParameters(distance: new Measurement<DistanceUnit>(csv[2]), null, null),
                            rifling: null);
                    }
                    else
                    {
                        Rifle = new Rifle(
                            sight: new Sight(sightHeight: new Measurement<DistanceUnit>(csv[1]), Measurement<AngularUnit>.ZERO, Measurement<AngularUnit>.ZERO),
                            zero: new ZeroingParameters(distance: new Measurement<DistanceUnit>(csv[2]), null, null),
                            rifling: new Rifling(new Measurement<DistanceUnit>(csv[3]), csv[4] == "left" ? TwistDirection.Left : TwistDirection.Right));
                    }
                }
                else if (csv[0] == "wind")
                {
                    Wind = new Wind(new Measurement<VelocityUnit>(csv[1]), new Measurement<AngularUnit>(csv[2]));
                }
                else if (csv[0] == "atmosphere")
                {
                    Atmosphere = new Atmosphere(
                        altitude: new Measurement<DistanceUnit>(csv[4]),
                        pressure: new Measurement<PressureUnit>(csv[3]),
                        temperature: new Measurement<TemperatureUnit>(csv[1]),
                        humidity: double.Parse(csv[2], CultureInfo.InvariantCulture) / 100.0);
                }
                else if (csv[0] == "shot")
                {
                    ShotParameters = new ShotParameters()
                    {
                        ShotAngle = new Measurement<AngularUnit>(csv[1]),
                        CantAngle = new Measurement<AngularUnit>(csv[2]),
                    };
                }
                else
                {
                    if (char.IsDigit(csv[0][0]))
                    {
                        if (units.Count == 0)
                            throw new InvalidOperationException("The header with the unit names for the table is not found");

                        var distance = new Measurement<DistanceUnit>(csv[0] + units[0]);
                        var drop = new Measurement<DistanceUnit>(csv[1] + units[1]);
                        var windage = new Measurement<DistanceUnit>(csv[3] + units[3]);
                        var velocity = new Measurement<VelocityUnit>(csv[5] + units[5]);
                        var mach = double.Parse(csv[6], CultureInfo.InvariantCulture);
                        var energy = new Measurement<EnergyUnit>(csv[7] + units[7]);
                        var time = double.Parse(csv[8], CultureInfo.InvariantCulture);

                        mTrajectory.Add(new TrajectoryPoint(
                            time: TimeSpan.FromSeconds(time),
                            distance: distance,
                            velocity: velocity,
                            mach: mach,
                            drop: drop,
                            windage: windage,
                            energy: energy,
                            optimalGameWeight: Measurement<WeightUnit>.ZERO
                            ));
                    }
                    else
                    {
                        for (int i = 0; i < csv.FieldsCount; i++)
                            units.Add(csv[i]);
                    }
                }
            }
        }
    }
}
