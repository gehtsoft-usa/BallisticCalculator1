using System;
using System.IO;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace BallisticCalculator.Test.Calculator
{
    /// <summary>
    /// Covers the file-based Open/Save overloads and the header-parsing guards of
    /// <see cref="DrgDragTable"/> (the stream overloads are exercised in DrgDragTableFactoryTest).
    /// </summary>
    public class DrgDragTableTest
    {
        private static AmmunitionLibraryEntry SampleEntry() => new AmmunitionLibraryEntry
        {
            Name = "file-bullet",
            Source = "unit-test",
            Ammunition = new Ammunition(
                weight: new Gehtsoft.Measurements.Measurement<Gehtsoft.Measurements.WeightUnit>(168, Gehtsoft.Measurements.WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(1.0, DragTableId.GC),
                muzzleVelocity: new Gehtsoft.Measurements.Measurement<Gehtsoft.Measurements.VelocityUnit>(2700, Gehtsoft.Measurements.VelocityUnit.FeetPerSecond),
                bulletDiameter: new Gehtsoft.Measurements.Measurement<Gehtsoft.Measurements.DistanceUnit>(0.308, Gehtsoft.Measurements.DistanceUnit.Inch)),
        };

        [Fact]
        public void SaveToFile_ThenOpenFromFile_RoundTrips()
        {
            var knots = new[] { new BcAtMach(0.0, 0.30), new BcAtMach(1.0, 0.28), new BcAtMach(3.0, 0.33) };
            var table = DrgDragTableFactory.Build(SampleEntry(), DragTableId.G1, knots);

            var path = Path.GetTempFileName();
            try
            {
                table.Save(path);
                var reopened = DrgDragTable.Open(path);

                reopened.Count.Should().Be(table.Count);
                for (int i = 0; i < table.Count; i++)
                {
                    reopened[i].Mach.Should().BeApproximately(table[i].Mach, 1e-9);
                    reopened[i].DragCoefficient.Should().BeApproximately(table[i].DragCoefficient, 1e-9);
                }
                reopened.Ammunition.Name.Should().Be("file-bullet");
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static DrgDragTable OpenText(string content)
        {
            using var ms = new MemoryStream(Encoding.ASCII.GetBytes(content));
            return DrgDragTable.Open(ms);
        }

        [Fact]
        public void Open_HeaderWithTooFewFields_Throws()
        {
            ((Action)(() => OpenText("CFM,name,0.01\n0.3 1.0\n")))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Open_HeaderWithUnknownTag_Throws()
        {
            ((Action)(() => OpenText("XYZ,name,0.01,0.007,0,0\n0.3 1.0\n")))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Open_HeaderOnly_NoPoints_Throws()
        {
            ((Action)(() => OpenText("CFM,name,0.01,0.007,0,0\n")))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Open_BrlHeader_Succeeds()
        {
            var table = OpenText("BRL,name,0.0109,0.00782,0,0\n0.20 0.5\n0.30 1.5\n0.25 3.0\n");
            table.Count.Should().Be(3);
            table.Ammunition.Name.Should().Be("name");
        }
    }
}
