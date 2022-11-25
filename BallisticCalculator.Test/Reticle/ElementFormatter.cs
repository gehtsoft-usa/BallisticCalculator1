using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallisticCalculator.Reticle.Data;
using FluentAssertions;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    public class ElementFormatter
    {
        [Fact]
        public void Circle1()
        {
            ReticleCircle circle = new ReticleCircle()
            {
                Center = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                Radius = AngularUnit.Mil.New(1.234),
                LineWidth = AngularUnit.Mil.New(0.01),
                Fill = true,
                Color = "black",
            };

            circle.ToString().Should().Be("Circle(p=(1.2345mil:6.789mil),r=1.234mil,w=0.01mil,c=black,f=true)");
        }

        [Fact]
        public void Circle2()
        {
            ReticleCircle circle = new ReticleCircle()
            {
                Center = new ReticlePosition(1.2345, 6.789, AngularUnit.MOA),
                Radius = AngularUnit.MOA.New(1.234),
                LineWidth = null,
                Fill = null,
                Color = null,
            };

            circle.ToString().Should().Be("Circle(p=(1.2345moa:6.789moa),r=1.234moa,w=null,c=null,f=null)");
        }

        [Fact]
        public void Line1()
        {
            ReticleLine line = new ReticleLine()
            {
                Start = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                End = new ReticlePosition(5.4321, 9.876, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.01),
                Color = "black",
            };

            line.ToString().Should().Be("Line(s=(1.2345mil:6.789mil),e=(5.4321mil:9.876mil),w=0.01mil,c=black)");
        }

        [Fact]
        public void Rectangle1()
        {
            ReticleRectangle rectangle = new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                Size = new ReticlePosition(5.4321, 9.876, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.01),
                Color = "black",
                Fill = false,
            };

            rectangle.ToString().Should().Be("Rectangle(p=(1.2345mil:6.789mil),s=(5.4321mil:9.876mil),w=0.01mil,c=black,f=false)");
        }

        [Fact]
        public void Text1()
        {
            ReticleText text = new ReticleText()
            {
                Position = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(1),
                Color = "black",
                Text = "123",
            };

            text.ToString().Should().Be("Text(p=(1.2345mil:6.789mil),h=1mil,t=123,c=black,a=default)");
        }

        [Fact]
        public void Move()
        {
            ReticlePathElementMoveTo m = new ReticlePathElementMoveTo()
            {
                Position = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
            };
            m.ToString().Should().Be("M(1.2345mil:6.789mil)");
        }

        [Fact]
        public void Line()
        {
            ReticlePathElementLineTo m = new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
            };
            m.ToString().Should().Be("L(1.2345mil:6.789mil)");
        }

        [Fact]
        public void Arc1()
        {
            ReticlePathElementArc m = new ReticlePathElementArc()
            {
                Position = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                Radius = AngularUnit.Mil.New(3.21),
                MajorArc = true,
                ClockwiseDirection = true,
            };
            m.ToString().Should().Be("A((1.2345mil:6.789mil),3.21mil,maj,cw)");
        }

        [Fact]
        public void Arc2()
        {
            ReticlePathElementArc m = new ReticlePathElementArc()
            {
                Position = new ReticlePosition(1.2345, 6.789, AngularUnit.Mil),
                Radius = AngularUnit.Mil.New(3.21),
                MajorArc = false,
                ClockwiseDirection = false,
            };
            m.ToString().Should().Be("A((1.2345mil:6.789mil),3.21mil,min,ccw)");
        }

        [Fact]
        public void Path1()
        {
            ReticlePath p = new ReticlePath()
            {
                LineWidth = AngularUnit.Mil.New(10),
                Color = "red",
                Fill = true,
            };

            p.Elements.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition(1.23, 5.54, AngularUnit.MOA) });
            p.Elements.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition(7.8, 8.9, AngularUnit.MOA) });

            p.ToString().Should().Be("Path(w=10mil,c=red,f=true,[M(1.23moa:5.54moa),L(7.8moa:8.9moa)])");
        }

        [Fact]
        public void Path2()
        {
            ReticlePath p = new ReticlePath();
            p.ToString().Should().Be("Path(w=null,c=null,f=null)");
        }

        [Fact]
        public void Bdc1()
        {
            ReticleBulletDropCompensatorPoint pt = new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(0, -5, AngularUnit.Mil),
                TextOffset = AngularUnit.Mil.New(1),
                TextHeight = AngularUnit.Mil.New(0.5)
            };

            pt.ToString().Should().Be("Bdc(p=(0mil:-5mil),o=1mil,h=0.5mil)");
        }

        [Fact]
        public void Bdc2()
        {
            ReticleBulletDropCompensatorPoint pt = new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(0, -5, AngularUnit.Mil),
                TextOffset = AngularUnit.Mil.New(-0.25),
                TextHeight = AngularUnit.Mil.New(0.5)
            };

            pt.ToString().Should().Be("Bdc(p=(0mil:-5mil),o=-0.25mil,h=0.5mil)");
        }
    }
}

