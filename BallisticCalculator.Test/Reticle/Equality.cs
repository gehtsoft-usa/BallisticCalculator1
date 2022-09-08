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
    public class Equality
    {
        [Theory]
        [InlineData(1, 2, AngularUnit.Mil, 1, 2, AngularUnit.Mil, true)]
        [InlineData(1.33333, 2.66666, AngularUnit.Mil, 1.33333, 2.66666, AngularUnit.Mil, true)]
        [InlineData(1, 2, AngularUnit.Mil, 2, 1, AngularUnit.Mil, false)]
        public void PathElement_MoveTo(double x1, double y1, AngularUnit u1, double x2, double y2, AngularUnit u2, bool equals)
        {
            var e1 = new ReticlePathElementMoveTo()
            {
                Position = new ReticlePosition(x1, y1, u1)
            };

            var e2 = new ReticlePathElementMoveTo()
            {
                Position = new ReticlePosition(x2, y2, u2)
            };

            e1.Equals(e2).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, AngularUnit.Mil, 1, 2, AngularUnit.Mil, true)]
        [InlineData(1.33333, 2.66666, AngularUnit.Mil, 1.33333, 2.66666, AngularUnit.Mil, true)]
        [InlineData(1, 2, AngularUnit.Mil, 2, 1, AngularUnit.Mil, false)]
        public void PathElement_LineTo(double x1, double y1, AngularUnit u1, double x2, double y2, AngularUnit u2, bool equals)
        {
            var e1 = new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(x1, y1, u1)
            };

            var e2 = new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(x2, y2, u2)
            };

            e1.Equals(e2).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, AngularUnit.Mil, true, true, 1, 2, AngularUnit.Mil, true, true, true)]
        [InlineData(2, 2, AngularUnit.Mil, true, true, 1, 2, AngularUnit.Mil, true, true, false)]
        [InlineData(1, 2, AngularUnit.MOA, true, true, 1, 2, AngularUnit.Mil, true, true, false)]
        [InlineData(1, 3, AngularUnit.Mil, true, true, 1, 2, AngularUnit.Mil, true, true, false)]
        [InlineData(1, 2, AngularUnit.Mil, false, true, 1, 2, AngularUnit.Mil, true, true, false)]
        [InlineData(1, 2, AngularUnit.Mil, true, false, 1, 2, AngularUnit.Mil, true, true, false)]
        public void PathElement_Arc(double x1, double y1, AngularUnit u1, bool majorArc1, bool clockWise1, double x2, double y2, AngularUnit u2, bool majorArc2, bool clockWise2, bool equals)
        {
            var e1 = new ReticlePathElementArc()
            {
                Position = new ReticlePosition(x1, y1, u1),
                MajorArc = majorArc1,
                ClockwiseDirection = clockWise1,
            };

            var e2 = new ReticlePathElementArc()
            {
                Position = new ReticlePosition(x2, y2, u2),
                MajorArc = majorArc2,
                ClockwiseDirection = clockWise2,
            };

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4.0, AngularUnit.Mil, "color", true, true)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    0, 2, 3, 4.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 0, 3, 4.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 0, 4.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 0.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4.0, AngularUnit.MOA, "color", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4.0, AngularUnit.Mil, "color1", true, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4.0, AngularUnit.Mil, "color", false, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4.0, AngularUnit.Mil, "color", null, false)]
        [InlineData(1, 2, 3, 4.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, null, AngularUnit.Mil, "color", true, false)]
        public void Circle(double x1, double y1, double r1, double? lw1, AngularUnit u1, string color1, bool? fill1,
                           double x2, double y2, double r2, double? lw2, AngularUnit u2, string color2, bool? fill2,
                           bool equals)
        {
            var e1 = new ReticleCircle()
            {
                Center = new ReticlePosition(x1, y1, u1),
                Radius = u1.New(r1),
                LineWidth = lw1 == null ? null : u1.New(lw1.Value),
                Color = color1,
                Fill = fill1
            };

            var e2 = new ReticleCircle()
            {
                Center = new ReticlePosition(x2, y2, u2),
                Radius = u2.New(r2),
                LineWidth = lw2 == null ? null : u2.New(lw2.Value),
                Color = color2,
                Fill = fill2
            };

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 2, 3, AngularUnit.Mil, "color", "text", true)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    4, 2, 3, AngularUnit.Mil, "color", "text", false)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 4, 3, AngularUnit.Mil, "color", "text", false)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 2, 4, AngularUnit.Mil, "color", "text", false)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 2, 3, AngularUnit.MOA, "color", "text", false)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 2, 3, AngularUnit.Mil, "color1", "text", false)]
        [InlineData(1, 2, 3, AngularUnit.Mil, "color", "text",
                    1, 2, 3, AngularUnit.Mil, "color", "text1", false)]
        public void Text(double x1, double y1, double h1, AngularUnit u1, string color1, string text1,
                         double x2, double y2, double h2, AngularUnit u2, string color2, string text2,
                         bool equals)
        {
            var e1 = new ReticleText()
            {
                Position = new ReticlePosition(x1, y1, u1),
                TextHeight = u1.New(h1),
                Color = color1,
                Text = text1,
            };

            var e2 = new ReticleText()
            {
                Position = new ReticlePosition(x2, y2, u2),
                TextHeight = u2.New(h2),
                Color = color2,
                Text = text2,
            };

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    6, 2, 3, 4, 5.0, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 6, 3, 4, 5.0, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 6, 4, 5.0, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 6, 5.0, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 4, 6.0, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 4, null, AngularUnit.Mil, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 4, 5.0, AngularUnit.MOA, "color", false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color",
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color1", false)]
        public void Line(double x11, double y11, double x12, double y12, double? lw1, AngularUnit u1, string color1,
                         double x21, double y21, double x22, double y22, double? lw2, AngularUnit u2, string color2,
                           bool equals)
        {
            var e1 = new ReticleLine()
            {
                Start = new ReticlePosition(x11, y11, u1),
                End = new ReticlePosition(x12, y12, u1),
                LineWidth = lw1 == null ? null : u1.New(lw1.Value),
                Color = color1,
            };
            var e2 = new ReticleLine()
            {
                Start = new ReticlePosition(x21, y21, u2),
                End = new ReticlePosition(x22, y22, u2),
                LineWidth = lw2 == null ? null : u1.New(lw2.Value),
                Color = color2,
            };

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, true)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    6, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 6, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 6, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 6, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 6.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, null, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.MOA, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color1", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, null, true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", false, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", null, false)]
        public void Rectangle(double x11, double y11, double x12, double y12, double? lw1, AngularUnit u1, string color1, bool? fill1,
                              double x21, double y21, double x22, double y22, double? lw2, AngularUnit u2, string color2, bool? fill2,
                              bool equals)
        {
            var e1 = new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(x11, y11, u1),
                Size = new ReticlePosition(x12, y12, u1),
                LineWidth = lw1 == null ? null : u1.New(lw1.Value),
                Color = color1,
                Fill = fill1
            };

            var e2 = new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(x21, y21, u2),
                Size = new ReticlePosition(x22, y22, u2),
                LineWidth = lw2 == null ? null : u1.New(lw2.Value),
                Color = color2,
                Fill = fill2
            };

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }

        [Theory]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, true)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    6, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 6, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 6, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 6, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 6.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, null, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.MOA, "color", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color1", true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, null, true, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", false, false)]
        [InlineData(1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", true,
                    1, 2, 3, 4, 5.0, AngularUnit.Mil, "color", null, false)]
        public void Path(double x11, double y11, double x12, double y12, double? lw1, AngularUnit u1, string color1, bool? fill1,
                              double x21, double y21, double x22, double y22, double? lw2, AngularUnit u2, string color2, bool? fill2,
                              bool equals)
        {
            var e1 = new ReticlePath()
            {
                LineWidth = lw1 == null ? null : u1.New(lw1.Value),
                Color = color1,
                Fill = fill1
            };
            e1.Elements.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition(x11, y11, u1) });
            e1.Elements.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition(x12, y12, u1) });

            var e2 = new ReticlePath()
            {
                LineWidth = lw2 == null ? null : u1.New(lw2.Value),
                Color = color2,
                Fill = fill2
            };
            e2.Elements.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition(x21, y21, u2) });
            e2.Elements.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition(x22, y22, u2) });

            e1.Equals(e2).Should().Be(equals);
            e2.Equals(e1).Should().Be(equals);
        }
    }
}
