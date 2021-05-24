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
        [Fact]
        public void CloneMildot()
        {
            var r1 = new MilDotReticle();
            var r2 = r1.Clone();

            r2.Should().NotBeNull();
            r2.Should().NotBeSameAs(r1);

            r2.Name.Should().Be(r1.Name);
            r2.Size.Should().Be(r1.Size);
            r2.Zero.Should().Be(r1.Zero);

            r2.Elements.Should().HaveCount(r1.Elements.Count);

            for (int i = 0; i < r2.Elements.Count; i++)
                r2.Elements[i].Should().Be(r1.Elements[i]);

            r2.BulletDropCompensator.Should().HaveCount(r2.BulletDropCompensator.Count);
            
            for (int i = 0; i < r2.BulletDropCompensator.Count; i++)
                r2.BulletDropCompensator[i].Should().Be(r1.BulletDropCompensator[i]);
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

            var p2 = p1.Clone();
            
            p2.Should().Be(p1);
        }
    }
}
