using FluentAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Data
{
    public class AmmunitionTest
    {
        [Fact]
        public void GetBallisiticCoefficient_ForBallisicCoefficient()
        {
            var ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.234, DragTableId.G1),
                Weight = new Measurement<WeightUnit>(55, WeightUnit.Grain),
                BulletDiameter = new Measurement<DistanceUnit>(0.224, DistanceUnit.Inch)
            };

            ammo.GetBallisticCoefficient().Should().BeApproximately(0.234, 5e-4);
        }

        [Theory]
        [InlineData(40, 0.204, 1.184, 0.116)]
        [InlineData(155, 0.308, 0.981, 0.238)]
        public void GetBallisiticCoefficient_ForFormFactor(double weight, double diameter, double formFactor, double bc)
        {
            var ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(formFactor, DragTableId.G1, BallisticCoefficientValueType.FormFactor),
                Weight = new Measurement<WeightUnit>(weight, WeightUnit.Grain),
                BulletDiameter = new Measurement<DistanceUnit>(diameter, DistanceUnit.Inch)
            };

            ammo.GetBallisticCoefficient().Should().BeApproximately(bc, 5e-4);
        }
    }
        
        
}

