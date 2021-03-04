using BallisticCalculator.Reticle.Data;
using FluentAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test
{
    public class ReticleSerializerTest
    {
        [Fact]
        public void RoundTripBDC()
        {
            ReticleBulletDropCompensatorPoint bdc = new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition() { X = new Measurement<AngularUnit>(1, AngularUnit.MOA), Y = new Measurement<AngularUnit>(2, AngularUnit.MOA) },
                Drop = new Measurement<AngularUnit>(2.5, AngularUnit.MOA),
                TextOffset = new Measurement<AngularUnit>(5, AngularUnit.Mil)
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(bdc);
            var bdc2 = serializer.Deserialize<ReticleBulletDropCompensatorPoint>(xml);

            bdc2.Position.X.Should().Be(bdc.Position.X);
            bdc2.Position.Y.Should().Be(bdc.Position.Y);
            bdc2.Drop.Should().Be(bdc.Drop);
            bdc2.TextOffset.Should().Be(bdc.TextOffset);
        }
    }
}

