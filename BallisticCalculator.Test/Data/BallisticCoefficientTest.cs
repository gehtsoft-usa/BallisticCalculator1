using AwesomeAssertions;
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
        // RA4 is the one table id that is not two characters long - it must still round-trip.
        [InlineData(0.132, DragTableId.RA4, BallisticCoefficientValueType.Coefficient, null, "0.132RA4", 1e-7)]
        [InlineData(0.132, DragTableId.RA4, BallisticCoefficientValueType.FormFactor, null, "F0.132RA4", 1e-7)]
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

        // Every drag table id (regardless of length: G1, GI, RA4, ...) must round-trip
        // through ToString() -> TryParse() for both value types.
        [Fact]
        public void EveryTableId_RoundTrips()
        {
            foreach (DragTableId tableId in System.Enum.GetValues(typeof(DragTableId)))
            {
                foreach (var valueType in new[] { BallisticCoefficientValueType.Coefficient, BallisticCoefficientValueType.FormFactor })
                {
                    var bc1 = new BallisticCoefficient(0.3456, tableId, valueType);
                    var text = bc1.ToString(CultureInfo.InvariantCulture);

                    BallisticCoefficient.TryParse(text, CultureInfo.InvariantCulture, out var bc2)
                        .Should().BeTrue($"'{text}' ({tableId}/{valueType}) must parse");
                    bc2.Table.Should().Be(tableId, $"table id for '{text}'");
                    bc2.ValueType.Should().Be(valueType, $"value type for '{text}'");
                    bc2.Value.Should().BeApproximately(0.3456, 1e-7, $"value for '{text}'");
                }
            }
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

