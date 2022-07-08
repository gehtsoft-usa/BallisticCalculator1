using FluentAssertions;
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
    }
}

