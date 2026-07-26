using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BallisticCalculator.Tools;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// One row of the report's smoothed "Mach / CD / BC-G1 / BC-G7" summary table.
    /// </summary>
    internal sealed class WarnerDragRow
    {
        public double Mach { get; init; }
        public double Cd { get; init; }
        public double BcG1 { get; init; }
        public double BcG7 { get; init; }

        public double Bc(DragTableId table) => table == DragTableId.G1 ? BcG1 : BcG7;
    }

    /// <summary>
    /// One row of the report's "Distance / Velocity / Time" table.
    /// </summary>
    internal sealed class WarnerVelocityRow
    {
        public Measurement<DistanceUnit> Distance { get; init; }
        public Measurement<VelocityUnit> Velocity { get; init; }
        public double Time { get; init; }
    }

    /// <summary>
    /// Reads a transcribed Warner Tool Company drag report from an embedded resource
    /// (warner_*.txt): bullet header, the multi-BC/CD summary table and the velocity table.
    /// </summary>
    internal sealed class WarnerReport
    {
        public string Name { get; private set; }
        public Measurement<WeightUnit> Weight { get; private set; }
        public Measurement<DistanceUnit> Diameter { get; private set; }
        public DragTableId BaseTable { get; private set; }
        public IReadOnlyList<WarnerDragRow> Drag => mDrag;
        public IReadOnlyList<WarnerVelocityRow> Velocity => mVelocity;

        private readonly List<WarnerDragRow> mDrag = new List<WarnerDragRow>();
        private readonly List<WarnerVelocityRow> mVelocity = new List<WarnerVelocityRow>();

        /// <summary>
        /// The bullet's sectional density - the ballistic coefficient the engine derives from a
        /// form factor of one, and therefore the scale factor between the report's physical CD and
        /// the table synthesized from its BC column.
        /// </summary>
        public double SectionalDensity =>
            Weight.In(WeightUnit.Grain) / 7000.0 / Math.Pow(Diameter.In(DistanceUnit.Inch), 2);

        public Measurement<VelocityUnit> MuzzleVelocity => mVelocity[0].Velocity;

        public static WarnerReport FromResource(string name)
        {
            using Stream stream = typeof(WarnerReport).Assembly
                .GetManifestResourceStream($"BallisticCalculator.Test.resources.{name}.txt");
            return new WarnerReport(stream);
        }

        private WarnerReport(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var f = line.Split(';');
                switch (f[0])
                {
                    case "bullet":
                        Name = f[1];
                        Weight = new Measurement<WeightUnit>(f[2]);
                        Diameter = new Measurement<DistanceUnit>(f[3]);
                        break;
                    case "base":
                        BaseTable = Enum.Parse<DragTableId>(f[1]);
                        break;
                    case "cd":
                        mDrag.Add(new WarnerDragRow
                        {
                            Mach = Num(f[1]),
                            Cd = Num(f[2]),
                            BcG1 = Num(f[3]),
                            BcG7 = Num(f[4]),
                        });
                        break;
                    case "v":
                        mVelocity.Add(new WarnerVelocityRow
                        {
                            Distance = new Measurement<DistanceUnit>(f[1]),
                            Velocity = new Measurement<VelocityUnit>(f[2]),
                            Time = Num(f[3]),
                        });
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected row '{f[0]}' in a Warner report");
                }
            }
        }

        private static double Num(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        /// <summary>
        /// The report's multi-BC column as the factory's knots.
        /// </summary>
        public BcAtMach[] BcCurve()
        {
            var knots = new BcAtMach[mDrag.Count];
            for (int i = 0; i < mDrag.Count; i++)
                knots[i] = new BcAtMach(mDrag[i].Mach, mDrag[i].Bc(BaseTable));
            return knots;
        }

        /// <summary>
        /// The report's velocity table as radar readings.
        /// </summary>
        public RadarReading[] RadarReadings()
        {
            var readings = new RadarReading[mVelocity.Count];
            for (int i = 0; i < mVelocity.Count; i++)
                readings[i] = new RadarReading(mVelocity[i].Distance, mVelocity[i].Velocity);
            return readings;
        }
    }
}
