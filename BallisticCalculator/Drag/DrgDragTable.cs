using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Gehtsoft.Measurements;

namespace BallisticCalculator
{
    /// <summary>
    /// The custom drag table loaded from a drg file.
    /// </summary>
    public class DrgDragTable : DragTable
    {
        /// <summary>
        /// The ammunition definition from the drag table file
        /// </summary>
        public AmmunitionLibraryEntry Ammunition { get; private set; }

        /// <summary>
        /// The table identifier (always `GC`).
        /// </summary>
        public override DragTableId TableId => DragTableId.GC;

        /// <summary>
        /// Constructs a custom table from pre-computed points and ammunition metadata (used by the factory).
        /// </summary>
        internal DrgDragTable(DragTableDataPoint[] points, AmmunitionLibraryEntry ammunition) : base(points)
        {
            Ammunition = ammunition;
        }

        /// <summary>
        /// <para>Constructs a custom table from measured drag points and the bullet metadata.</para>
        /// <para>The parameters are exactly the values a drg file header carries, so a table built this
        /// way and saved keeps all of its metadata.</para>
        /// </summary>
        /// <param name="points">The drag points. They must be sorted in ascending order by the Mach number and at least two points are required.</param>
        /// <param name="name">The name of the projectile.</param>
        /// <param name="bulletWeight">The bullet weight.</param>
        /// <param name="bulletDiameter">The bullet diameter.</param>
        /// <param name="bulletLength">The optional bullet length. It is not used by the drag curve, but it is required for the spin drift and the aerodynamic jump.</param>
        /// <param name="source">The optional description of the data origin.</param>
        /// <param name="muzzleVelocity">The optional muzzle velocity of the reference load. When it is not specified, a placeholder of 500 meters per second is used.</param>
        /// <exception cref="ArgumentNullException">The points are null.</exception>
        /// <exception cref="ArgumentException">There are fewer than two points, or the points are not sorted by the Mach number.</exception>
        public DrgDragTable(DragTableDataPoint[] points, string name,
                            Measurement<WeightUnit> bulletWeight, Measurement<DistanceUnit> bulletDiameter,
                            Measurement<DistanceUnit>? bulletLength = null, string source = null,
                            Measurement<VelocityUnit>? muzzleVelocity = null)
            : base(Validate(points))
        {
            Ammunition = MakeEntry(name, bulletWeight, bulletDiameter, bulletLength, source, muzzleVelocity);
        }

        private static DragTableDataPoint[] Validate(DragTableDataPoint[] points)
        {
            ArgumentNullException.ThrowIfNull(points);
            if (points.Length < 2)
                throw new ArgumentException("At least two drag points are required", nameof(points));
            for (int i = 1; i < points.Length; i++)
                if (points[i].Mach <= points[i - 1].Mach)
                    throw new ArgumentException("The drag points must be sorted in ascending order by the Mach number", nameof(points));
            return points;
        }

        //the metadata carried by a drg file header, in the shape the engine consumes it
        private static AmmunitionLibraryEntry MakeEntry(string name, Measurement<WeightUnit> weight, Measurement<DistanceUnit> diameter,
                                                        Measurement<DistanceUnit>? length, string source, Measurement<VelocityUnit>? muzzleVelocity)
            => new AmmunitionLibraryEntry()
            {
                Name = name,
                Source = string.IsNullOrWhiteSpace(source) ? "drg file" : source.Trim(),
                Ammunition = new Ammunition()
                {
                    BallisticCoefficient = new BallisticCoefficient(1, DragTableId.GC, BallisticCoefficientValueType.FormFactor),
                    Weight = weight,
                    BulletDiameter = diameter,
                    BulletLength = length,
                    MuzzleVelocity = muzzleVelocity ?? new Measurement<VelocityUnit>(500, VelocityUnit.MetersPerSecond)
                }
            };

        /// <summary>
        /// Reads the drag file from a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static DrgDragTable Open(Stream stream, Encoding encoding = null)
        {
            List<DragTableDataPoint> points = new List<DragTableDataPoint>();
            string ammoName = null;
            Measurement<DistanceUnit> diameter = Measurement<DistanceUnit>.ZERO;
            Measurement<WeightUnit> weight = Measurement<WeightUnit>.ZERO;
            Measurement<DistanceUnit>? length = null;
            string source = null;

            using var ts = new StreamReader(stream, encoding ?? Encoding.ASCII, true, 4096, true);

            while (true)
            {
                var line = ts.ReadLine();
                if (line == null)
                    break;
                if (string.IsNullOrEmpty(ammoName))
                    ReadHeader(line, out ammoName, out weight, out diameter, out length, out source);
                else
                    ReadPoint(line, points);
            }

            if (points.Count < 1)
                throw new ArgumentException("No points is found in the drg file", nameof(stream));

            return new DrgDragTable(points.ToArray(), MakeEntry(ammoName, weight, diameter, length, source, null));
        }

        private static void ReadHeader(string line, out string ammoName, out Measurement<WeightUnit> weight, out Measurement<DistanceUnit> diameter,
                                      out Measurement<DistanceUnit>? length, out string source)
        {
            var parts = line.Split(',');
            if (parts.Length < 4)
                throw new ArgumentException("The first line of stream must have at least 4 values");
            if (parts[0].Trim() != "CFM" && parts[0].Trim() != "BRL")
                throw new ArgumentException("Only CFM or BRL drg files are supported");
            ammoName = parts[1].Trim();
            weight = new Measurement<WeightUnit>(Double.Parse(parts[2], CultureInfo.InvariantCulture), WeightUnit.Kilogram);
            diameter = new Measurement<DistanceUnit>(Double.Parse(parts[3], CultureInfo.InvariantCulture), DistanceUnit.Meter);

            //the optional 5th field is the bullet length in meters, the optional 6th field is the source of the data
            length = null;
            if (parts.Length > 4 && Double.TryParse(parts[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lengthMeters) && lengthMeters > 0)
                length = new Measurement<DistanceUnit>(lengthMeters, DistanceUnit.Meter);

            source = null;
            if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                source = parts[5].Trim();
        }

        private static readonly char[] gPointSeparator = new char[] { ' ', '\t' };

        private static void ReadPoint(string line, List<DragTableDataPoint> points)
        {
            var parts = line.Split(gPointSeparator, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length != 2)
                return;

            double bc = Double.Parse(parts[0], CultureInfo.InvariantCulture);
            double mach = Double.Parse(parts[1], CultureInfo.InvariantCulture);
            points.Add(new DragTableDataPoint(mach, bc));

        }

        /// <summary>
        /// Reads the drag file from a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static DrgDragTable Open(string fileName, Encoding encoding = null)
        {
            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Open(fs, encoding);
        }

        /// <summary>
        /// Writes the drag table to a stream in the CFM .drg format (symmetric with Open).
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public void Save(Stream stream, Encoding encoding = null)
        {
            using var w = new StreamWriter(stream, encoding ?? Encoding.ASCII, 4096, true);

            var ammo = Ammunition?.Ammunition;
            double weightKg = ammo != null ? ammo.Weight.In(WeightUnit.Kilogram) : 0.0;
            double diameterM = ammo?.BulletDiameter?.In(DistanceUnit.Meter) ?? 0.0;
            double lengthM = ammo?.BulletLength?.In(DistanceUnit.Meter) ?? 0.0;
            string name = (Ammunition?.Name ?? "custom").Replace(',', ' ');
            string source = (Ammunition?.Source ?? string.Empty).Replace(',', ' ').Trim();

            w.WriteLine(string.Format(CultureInfo.InvariantCulture, "CFM,{0},{1:R},{2:R},{3:R},{4}", name, weightKg, diameterM, lengthM, source));
            for (int i = 0; i < Count; i++)
                w.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:R} {1:R}", this[i].DragCoefficient, this[i].Mach));
        }

        /// <summary>
        /// Writes the drag table to a file in the CFM .drg format (symmetric with Open).
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        public void Save(string fileName, Encoding encoding = null)
        {
            using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            Save(fs, encoding);
        }
    }
}
