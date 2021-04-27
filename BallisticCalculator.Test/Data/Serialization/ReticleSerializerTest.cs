using BallisticCalculator.Reticle.Data;
using FluentAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Data.Serialization
{
    public class ReticleSerializerTest
    {
        [Fact]
        public void RoundTripBDC()
        {
            ReticleBulletDropCompensatorPoint bdc = new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition() { X = new Measurement<AngularUnit>(1, AngularUnit.MOA), Y = new Measurement<AngularUnit>(2, AngularUnit.MOA) },
                TextOffset = new Measurement<AngularUnit>(5, AngularUnit.Mil)
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(bdc);
            var bdc2 = serializer.Deserialize<ReticleBulletDropCompensatorPoint>(xml);

            bdc2.Position.X.Should().Be(bdc.Position.X);
            bdc2.Position.Y.Should().Be(bdc.Position.Y);
            bdc2.TextOffset.Should().Be(bdc.TextOffset);
        }

        [Fact]
        public void RoundTripPath()
        {
            var path = new ReticlePath()
            {
                Fill = true,
                Color = "red",
                LineWidth = AngularUnit.MOA.New(5),
            };

            path.Elements.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition() { X = AngularUnit.MOA.New(10), Y = AngularUnit.MOA.New(20) } });
            path.Elements.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition() { X = AngularUnit.MOA.New(15), Y = AngularUnit.MOA.New(25) } });
            path.Elements.Add(new ReticlePathElementArc()
            {
                Position = new ReticlePosition() { X = AngularUnit.MOA.New(25), Y = AngularUnit.MOA.New(35) },
                Radius = AngularUnit.MOA.New(5),
                ClockwiseDirection = false,
                MajorArc = false
            });

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(path);
            var path2 = serializer.Deserialize<ReticlePath>(xml);

            path2.Should().NotBeNull();
            path2.Fill.Should().BeTrue();
            path2.Color.Should().Be("red");
            path2.LineWidth.Should().Be(AngularUnit.MOA.New(5));

            path2.Elements.Should().HaveCount(3);
            path2.Elements[0].ElementType.Should().Be(ReticlePathElementType.MoveTo);
            path2.Elements[0].As<ReticlePathElementMoveTo>().Position.X.Should().Be(AngularUnit.MOA.New(10));
            path2.Elements[0].As<ReticlePathElementMoveTo>().Position.Y.Should().Be(AngularUnit.MOA.New(20));

            path2.Elements[1].ElementType.Should().Be(ReticlePathElementType.LineTo);
            path2.Elements[1].As<ReticlePathElementLineTo>().Position.X.Should().Be(AngularUnit.MOA.New(15));
            path2.Elements[1].As<ReticlePathElementLineTo>().Position.Y.Should().Be(AngularUnit.MOA.New(25));

            path2.Elements[2].ElementType.Should().Be(ReticlePathElementType.Arc);
            path2.Elements[2].As<ReticlePathElementArc>().Position.X.Should().Be(AngularUnit.MOA.New(25));
            path2.Elements[2].As<ReticlePathElementArc>().Position.Y.Should().Be(AngularUnit.MOA.New(35));
            path2.Elements[2].As<ReticlePathElementArc>().Radius.Should().Be(AngularUnit.MOA.New(5));
            path2.Elements[2].As<ReticlePathElementArc>().ClockwiseDirection.Should().BeFalse();
            path2.Elements[2].As<ReticlePathElementArc>().MajorArc.Should().BeFalse();
        }

        [Fact]
        public void RoundTripReticleElements()
        {
            var reticle = new ReticleDefinition()
            {
                Size = new ReticlePosition(150, 175, AngularUnit.MOA),
                Name = "Test Reticle",
                Zero = new ReticlePosition(75, 100, AngularUnit.MOA)
            };

            reticle.BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(0, -5, AngularUnit.MOA),
                TextOffset = AngularUnit.MOA.New(2),
            });

            reticle.Elements.Add(new ReticleLine()
            {
                Start = new ReticlePosition(1, 2, AngularUnit.Mil),
                End = new ReticlePosition(3, 4, AngularUnit.Mil),
                Color = "gray",
                LineWidth = AngularUnit.Mil.New(1.5),
            });

            reticle.Elements.Add(new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(7, 8, AngularUnit.Mil),
                Size = new ReticlePosition(0.5, 0.75, AngularUnit.Mil),
                Color = "gray",
                Fill = true,
                LineWidth = AngularUnit.Mil.New(1.5),
            });

            reticle.Elements.Add(new ReticleText()
            {
                Position = new ReticlePosition(0, -7, AngularUnit.MOA),
                Text = "Text",
                Color = "black",
                TextHeight = AngularUnit.MOA.New(1),
            });

            reticle.Elements.Add(new ReticleCircle()
            {
                Center = new ReticlePosition(5, 6, AngularUnit.MOA),
                Radius = AngularUnit.MOA.New(1.5),
                Fill = false,
                LineWidth = AngularUnit.MOA.New(0.25),
                Color = "black",
            });

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(reticle);
            var reticle2 = serializer.Deserialize<ReticleDefinition>(xml);

            reticle2.Should().NotBeNull();
            reticle2.Name.Should().Be("Test Reticle");
            reticle2.Size.X.Should().Be(AngularUnit.MOA.New(150));
            reticle2.Size.Y.Should().Be(AngularUnit.MOA.New(175));
            reticle2.Zero.X.Should().Be(AngularUnit.MOA.New(75));
            reticle2.Zero.Y.Should().Be(AngularUnit.MOA.New(100));

            reticle2.BulletDropCompensator.Should().HaveCount(1);
            reticle2.BulletDropCompensator[0].Position.X.Should().Be(AngularUnit.MOA.New(0));
            reticle2.BulletDropCompensator[0].Position.Y.Should().Be(AngularUnit.MOA.New(-5));
            reticle2.BulletDropCompensator[0].TextOffset.Should().Be(AngularUnit.MOA.New(2));

            reticle2.Elements.Should().HaveCount(4);

            reticle2.Elements[0].ElementType.Should().Be(ReticleElementType.Line);

            reticle2.Elements[1].ElementType.Should().Be(ReticleElementType.Rectangle);

            reticle2.Elements[2].ElementType.Should().Be(ReticleElementType.Text);

            reticle2.Elements[3].ElementType.Should().Be(ReticleElementType.Circle);
        }
    }
}

