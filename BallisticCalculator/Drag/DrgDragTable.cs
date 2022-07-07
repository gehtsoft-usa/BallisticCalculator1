﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        private DrgDragTable(DragTableDataPoint[] points) : base(points)
        {
        }

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

            var ts = new StreamReader(stream, encoding ?? Encoding.ASCII, true);
            var ss = new char[] { ' ', '\t' };

            while (true)
            {
                var line = ts.ReadLine();
                if (line == null)
                    break;
                if (string.IsNullOrEmpty(ammoName))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 4)
                        throw new ArgumentException("The first line of stream must have at least 4 values", nameof(stream));
                    if (parts[0].Trim() != "CFM")
                        throw new ArgumentException("Only CFM drg files are supported", nameof(stream));
                    ammoName = parts[1].Trim();
                    weight = new Measurement<WeightUnit>(Double.Parse(parts[2], CultureInfo.InvariantCulture), WeightUnit.Kilogram);
                    diameter = new Measurement<DistanceUnit>(Double.Parse(parts[3], CultureInfo.InvariantCulture), DistanceUnit.Meter);
                }
                else
                {
                    var parts = line.Split(ss, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;
                    double bc = Double.Parse(parts[0], CultureInfo.InvariantCulture);
                    double mach = Double.Parse(parts[1], CultureInfo.InvariantCulture);
                    points.Add(new DragTableDataPoint(mach, bc));
                }
            }

            if (points.Count < 1)
                throw new ArgumentException("No points is found in the drg file", nameof(stream));

            var r = new DrgDragTable(points.ToArray())
            {
                Ammunition = new AmmunitionLibraryEntry()
                {
                    Name = ammoName,
                    Source = "drg file",
                    Ammunition = new Ammunition()
                    {
                        BallisticCoefficient = new BallisticCoefficient(1, DragTableId.GC, BallisticCoefficientValueType.FormFactor),
                        Weight = weight,
                        BulletDiameter = diameter,
                        MuzzleVelocity = new Measurement<VelocityUnit>(500, VelocityUnit.MetersPerSecond)
                    }
                }
            };
            return r;
        }

        /// <summary>
        /// Reads the drag file from a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static DragTable Open(string fileName, Encoding encoding = null)
        {
            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Open(fs, encoding);
        }
    }
}
