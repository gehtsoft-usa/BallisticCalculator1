using FluentAssertions;
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
                ((Action)(() => TestDataPoint(dataPoint, table))).Should().NotThrow();
            }
        }
    }
}

