using AwesomeAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Data
{
    public class AmmunitionLibraryEntryTest
    {
        [Fact]
        public void ParameterizedConstructor_SetsAllProperties()
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.45, DragTableId.G7),
                muzzleVelocity: new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond));

            var entry = new AmmunitionLibraryEntry(
                ammunition: ammo,
                name: "168 gr BTHP",
                source: "unit-test",
                caliber: ".308 Winchester",
                ammunitionType: "BTHP",
                barrelLength: new Measurement<DistanceUnit>(24, DistanceUnit.Inch));

            entry.Ammunition.Should().BeSameAs(ammo);
            entry.Name.Should().Be("168 gr BTHP");
            entry.Source.Should().Be("unit-test");
            entry.Caliber.Should().Be(".308 Winchester");
            entry.AmmunitionType.Should().Be("BTHP");
            entry.BarrelLength.Should().Be(new Measurement<DistanceUnit>(24, DistanceUnit.Inch));
        }

        [Fact]
        public void ParameterizedConstructor_OptionalBarrelLengthDefaultsToNull()
        {
            var ammo = new Ammunition(
                weight: new Measurement<WeightUnit>(55, WeightUnit.Grain),
                ballisticCoefficient: new BallisticCoefficient(0.243, DragTableId.G1),
                muzzleVelocity: new Measurement<VelocityUnit>(3200, VelocityUnit.FeetPerSecond));

            var entry = new AmmunitionLibraryEntry(ammo, "55 gr FMJ", "src", "5.56x45", "FMJ");

            entry.BarrelLength.Should().BeNull();
            entry.Name.Should().Be("55 gr FMJ");
        }
    }
}
