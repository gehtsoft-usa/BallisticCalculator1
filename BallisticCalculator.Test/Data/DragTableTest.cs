using AwesomeAssertions;
using Gehtsoft.Measurements;
using System;
using Xunit;

namespace BallisticCalculator.Test.Data
{
    public class DragTableTest
    {
        private static void TestDataPoint(DragTableNode dataPoint, DragTable table)
        {
            var node = table.Find(dataPoint.Mach + 0.01);
            node.Should().NotBeNull();
            node.Mach.Should().BeLessThan(dataPoint.Mach + 0.01);
            (node.Next == null || node.Next.Mach > dataPoint.Mach).Should().BeTrue();

            //check that drag coefficient ranges are calculated correctly
            node.CalculateDrag(dataPoint.Mach).Should().BeApproximately(dataPoint.DragCoefficient, 1e-7);
            if (node.Next != null)
                node.CalculateDrag(node.Next.Mach).Should().BeApproximately(node.Next.DragCoefficient, 1e-7);
        }

        [Theory]
        [InlineData(DragTableId.G1)]
        [InlineData(DragTableId.G2)]
        [InlineData(DragTableId.G7)]
        [InlineData(DragTableId.G8)]
        [InlineData(DragTableId.GS)]
        public void TestTable(DragTableId id)
        {
            DragTable table = DragTable.Get(id);
            for (int i = 0; i < table.Count; i++)
            {
                var dataPoint = table[i];
                TestDataPoint(dataPoint, table);
            }
        }

        [Fact]
        public void Drg()
        {
            using var stream = typeof(DragTableTest).Assembly.GetManifestResourceStream($"BallisticCalculator.Test.resources.drg.txt");
            var table = DrgDragTable.Open(stream);

            table.Ammunition.Name.Should().Be(".30 Lapua AP492 10.7g");
            table.Ammunition.Ammunition.Weight.In(WeightUnit.Gram).Should().BeApproximately(10.7, 1e-7);
            table.Ammunition.Ammunition.BulletDiameter.Should()
                .NotBeNull()
                .And.Subject.As<Measurement<DistanceUnit>?>()
                .Value.In(DistanceUnit.Millimeter).Should().BeApproximately(7.83, 1e-7);
            table.Ammunition.Ammunition.BulletLength.Should()
                .NotBeNull()
                .And.Subject.As<Measurement<DistanceUnit>?>()
                .Value.In(DistanceUnit.Millimeter).Should().BeApproximately(29.0, 1e-7);
            table.Ammunition.Source.Should().Be("Radar Data");

            table.Ammunition.Ammunition
                .BallisticCoefficient.Value.Should().Be(1);
            table.Ammunition.Ammunition
                .BallisticCoefficient.Table.Should().Be(DragTableId.GC);
            table.Ammunition.Ammunition
                .BallisticCoefficient.ValueType.Should().Be(BallisticCoefficientValueType.FormFactor);
            table.Ammunition.Ammunition
                .GetBallisticCoefficient().Should().BeApproximately(0.2482, 5e-5);

            table.Count.Should().Be(32);
            table[0].DragCoefficient.Should().Be(0.180);
            table[0].Mach.Should().Be(0);

            table[3].DragCoefficient.Should().Be(0.152);
            table[3].Mach.Should().Be(0.5);

            table[31].DragCoefficient.Should().Be(0.210);
            table[31].Mach.Should().Be(5);

        }

        //every header field of a drg file must be reachable after it is read from a file on disk
        [Fact]
        public void DrgFromFile_ExposesAllMetadata()
        {
            var fileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.drg");
            try
            {
                using (var src = typeof(DragTableTest).Assembly.GetManifestResourceStream("BallisticCalculator.Test.resources.drg.txt"))
                using (var dst = new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    src.CopyTo(dst);

                var table = DrgDragTable.Open(fileName);

                table.TableId.Should().Be(DragTableId.GC);
                table.Ammunition.Should().NotBeNull();
                table.Ammunition.Name.Should().Be(".30 Lapua AP492 10.7g");
                table.Ammunition.Source.Should().Be("Radar Data");
                table.Ammunition.Ammunition.Weight.In(WeightUnit.Gram).Should().BeApproximately(10.7, 1e-7);
                table.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Millimeter).Should().BeApproximately(7.83, 1e-7);
                table.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Millimeter).Should().BeApproximately(29.0, 1e-7);
                table.Count.Should().Be(32);
            }
            finally
            {
                System.IO.File.Delete(fileName);
            }
        }

        [Fact]
        public void DrgConstructor_CarriesMetadata()
        {
            var points = new[]
            {
                new DragTableDataPoint(0, 0.14),
                new DragTableDataPoint(1, 0.30),
                new DragTableDataPoint(5, 0.25),
            };

            var table = new DrgDragTable(points, "hand-made",
                new Measurement<WeightUnit>(168, WeightUnit.Grain),
                new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch),
                new Measurement<DistanceUnit>(1.226, DistanceUnit.Inch),
                "measured");

            table.TableId.Should().Be(DragTableId.GC);
            table.Count.Should().Be(3);
            table.Ammunition.Name.Should().Be("hand-made");
            table.Ammunition.Source.Should().Be("measured");
            table.Ammunition.Ammunition.Weight.In(WeightUnit.Grain).Should().BeApproximately(168, 1e-7);
            table.Ammunition.Ammunition.BulletDiameter.Value.In(DistanceUnit.Inch).Should().BeApproximately(0.308, 1e-7);
            table.Ammunition.Ammunition.BulletLength.Value.In(DistanceUnit.Inch).Should().BeApproximately(1.226, 1e-7);
            table.Ammunition.Ammunition.BallisticCoefficient.ValueType.Should().Be(BallisticCoefficientValueType.FormFactor);
            table.Ammunition.Ammunition.BallisticCoefficient.Table.Should().Be(DragTableId.GC);

            //the optional metadata defaults the same way a file without those fields loads
            var bare = new DrgDragTable(points, "bare",
                new Measurement<WeightUnit>(168, WeightUnit.Grain),
                new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch));
            bare.Ammunition.Ammunition.BulletLength.Should().BeNull();
            bare.Ammunition.Source.Should().Be("drg file");
            bare.Ammunition.Ammunition.MuzzleVelocity.In(VelocityUnit.MetersPerSecond).Should().BeApproximately(500, 1e-7);
        }

        [Fact]
        public void DrgConstructor_GuardsPoints()
        {
            var weight = new Measurement<WeightUnit>(168, WeightUnit.Grain);
            var diameter = new Measurement<DistanceUnit>(0.308, DistanceUnit.Inch);

            ((Action)(() => new DrgDragTable(null, "x", weight, diameter)))
                .Should().Throw<ArgumentNullException>();
            ((Action)(() => new DrgDragTable(new[] { new DragTableDataPoint(0, 0.14) }, "x", weight, diameter)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => new DrgDragTable(new[] { new DragTableDataPoint(1, 0.14), new DragTableDataPoint(0, 0.30) }, "x", weight, diameter)))
                .Should().Throw<ArgumentException>();
        }
    }
}

