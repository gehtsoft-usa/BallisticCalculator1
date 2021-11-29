using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallisticCalculator.Reticle;
using BallisticCalculator.Reticle.Data;
using FluentAssertions;
using Gehtsoft.Measurements;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    public class Clone
    {
        private static void ShouldHavePositionsDifferent(object x1, object x2)
        {
            x1.Should().NotBeNull();
            x2.Should().NotBeNull();
            x1.Should().NotBeSameAs(x2);

            (x2.GetType().IsInstanceOfType(x1) ||
             x1.GetType().IsInstanceOfType(x2)).Should().BeTrue();

            var properties = x1.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(ReticlePosition))
                {
                    var v1 = property.GetValue(x1);
                    var v2 = property.GetValue(x2);
                    v1.Should().NotBeSameAs(v2);
                }
            }
        }

        [Fact]
        public void CloneMildot()
        {
            var r1 = new MilDotReticle();
            var r2 = r1.Clone();

            r2.Should().NotBeNull();
            r2.Should().NotBeSameAs(r1);

            r2.Name.Should().Be(r1.Name);
            r2.Size.Should().Be(r1.Size);
            r2.Size.Should().NotBeSameAs(r1.Size);
            r2.Zero.Should().Be(r1.Zero);

            (r2.GetType().IsInstanceOfType(r1) ||
             r1.GetType().IsInstanceOfType(r2)).Should().BeTrue();

            ShouldHavePositionsDifferent(r1, r2);

            r2.Elements.Should().HaveCount(r1.Elements.Count);

            for (int i = 0; i < r2.Elements.Count; i++)
            {
                r2.Elements[i].Should().Be(r1.Elements[i]);
                ShouldHavePositionsDifferent(r2.Elements[i], r1.Elements[i]);
            }
            r2.BulletDropCompensator.Should().HaveCount(r2.BulletDropCompensator.Count);
            for (int i = 0; i < r2.BulletDropCompensator.Count; i++)
            {
                r2.BulletDropCompensator[i].Should().Be(r1.BulletDropCompensator[i]);
                ShouldHavePositionsDifferent(r2.Elements[i], r1.Elements[i]);
            }
        }

        [Fact]
        public void ClonePath()
        {
            var p1 = new ReticlePath()
            {
                Color = "color",
                LineWidth = AngularUnit.Mil.New(0.5),
                Fill = true,
            };

            p1.Elements.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition(1, 2, AngularUnit.Mil) });
            p1.Elements.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition(3, 4, AngularUnit.Mil) });
            p1.Elements.Add(new ReticlePathElementArc() { Position = new ReticlePosition(5, 6, AngularUnit.Mil), ClockwiseDirection = true, MajorArc = true, Radius = AngularUnit.Mil.New(0.25) });

            var p2 = p1.Clone() as ReticlePath;

            p2.Should().Be(p1);

            for (int i = 0; i < p1.Elements.Count; i++)
                ShouldHavePositionsDifferent(p1.Elements[i], p2.Elements[i]);
        }
    }
}
