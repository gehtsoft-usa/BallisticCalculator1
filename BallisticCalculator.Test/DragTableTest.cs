using FluentAssertions;
using System;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace BallisticCalculator.Test
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
                ((Action)(() => TestDataPoint(dataPoint, table))).Should().NotThrow();
            }
        }
    }

    public class BallisticCoefficientTest
    {
        [Theory]
        [InlineData(1, DragTableId.G1, null, "1G1", 1e-7)]
        [InlineData(0.765, DragTableId.G7, null, "0.765G7", 1e-7)]
        [InlineData(0.7655678, DragTableId.G2, null, "0.7655678G2", 1e-7)]
        [InlineData(0.7655678, DragTableId.GS, "N3", "0.766GS", 1e-3)]
        public void ToStringAndParse(double value, DragTableId tableId, string format, string expected, double expectedAccuracy)
        {
            var bc1 = new BallisticCoefficient(value, tableId);
            if (format != null)
                bc1.ToString(format, CultureInfo.InvariantCulture).Should().Be(expected);
            else
                bc1.ToString(CultureInfo.InvariantCulture).Should().Be(expected);

            BallisticCoefficient.TryParse(expected, out BallisticCoefficient bc2).Should().BeTrue();
            bc2.Value.Should().BeApproximately(value, expectedAccuracy);
            bc2.Table.Should().Be(tableId);
        }

        [Fact]
        public void Serialize()
        {
            var bc1 = new BallisticCoefficient(1.2345678, DragTableId.G7);
            var s = JsonSerializer.Serialize(bc1);
            var bc2 = JsonSerializer.Deserialize<BallisticCoefficient>(s);
            bc2.Value.Should().Be(1.2345678);
            bc2.Table.Should().Be(DragTableId.G7);
        }
    }
}

