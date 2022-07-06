using FluentAssertions;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace BallisticCalculator.Test.Data
{
    public class BallisticCoefficientTest
    {
        [Theory]
        [InlineData(1, DragTableId.G1, BallisticCoefficientValueType.Coefficient, null, "1G1", 1e-7)]
        [InlineData(1, DragTableId.G1, BallisticCoefficientValueType.FormFactor, null, "F1G1", 1e-7)]
        [InlineData(1, DragTableId.GC, BallisticCoefficientValueType.FormFactor, null, "F1GC", 1e-7)]
        [InlineData(0.765, DragTableId.G7, BallisticCoefficientValueType.Coefficient, null, "0.765G7", 1e-7)]
        [InlineData(0.765, DragTableId.G7, BallisticCoefficientValueType.FormFactor, null, "F0.765G7", 1e-7)]
        [InlineData(0.7655678, DragTableId.G2, BallisticCoefficientValueType.Coefficient, null, "0.7655678G2", 1e-7)]
        [InlineData(0.7655678, DragTableId.GS, BallisticCoefficientValueType.Coefficient, "N3", "0.766GS", 1e-3)]
        public void ToStringAndParse(double value, DragTableId tableId, BallisticCoefficientValueType valueType, string format, string expected, double expectedAccuracy)
        {
            var bc1 = new BallisticCoefficient(value, tableId, valueType);
            if (format != null)
                bc1.ToString(format, CultureInfo.InvariantCulture).Should().Be(expected);
            else
                bc1.ToString(CultureInfo.InvariantCulture).Should().Be(expected);

            BallisticCoefficient.TryParse(expected, out BallisticCoefficient bc2).Should().BeTrue();
            bc2.ValueType.Should().Be(valueType);
            bc2.Value.Should().BeApproximately(value, expectedAccuracy);
            bc2.Table.Should().Be(tableId);
        }

        [Theory]
        [InlineData(BallisticCoefficientValueType.Coefficient)]
        [InlineData(BallisticCoefficientValueType.FormFactor)]
        public void Serialize(BallisticCoefficientValueType valueType)
        {
            var bc1 = new BallisticCoefficient(1.2345678, DragTableId.G7, valueType);
            var s = JsonSerializer.Serialize(bc1);
            var bc2 = JsonSerializer.Deserialize<BallisticCoefficient>(s);
            bc2.ValueType.Should().Be(valueType);
            bc2.Value.Should().Be(1.2345678);
            bc2.Table.Should().Be(DragTableId.G7);
        }
    }
}

