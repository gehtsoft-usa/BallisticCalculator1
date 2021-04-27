using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using FluentAssertions;
using Gehtsoft.Measurements;
using Moq;
using Xunit;

namespace BallisticCalculator.Test
{
    public class ReticleControllerTest
    {
        private Mock<IReticleCanvas> CreateMockCanvas()
        {
            var canvas = new Mock<IReticleCanvas>();
            canvas.Setup(canvas => canvas.Left).Returns(0);
            canvas.Setup(canvas => canvas.Right).Returns(10000);
            canvas.Setup(canvas => canvas.Top).Returns(0);
            canvas.Setup(canvas => canvas.Bottom).Returns(10000);
            
            canvas.Setup(canvas => canvas.Width).Returns(10000);
            canvas.Setup(canvas => canvas.Height).Returns(10000);

            return canvas;
        }

        private ReticleDefinition CreateReticle(double size = 10, double? zeroy = null)
        {
            var definition = new ReticleDefinition()
            {
                Name = "Test",
                Size = new ReticlePosition(size, size, AngularUnit.Mil),
                Zero = new ReticlePosition(size / 2, zeroy ?? (size / 2), AngularUnit.Mil)
            };
            return definition;

        }

        private bool Approximately(float x1, float x2, float epsilon) => Math.Abs(x1 - x2) < epsilon;
        private bool Approximately(float x1, float x2) => Approximately(x1, x2, 1e-2f);

        [Theory]
        [InlineData(-5, -5, 5, 5, 0.001, "red", 0, 10000, 10000, 0, 1)]
        [InlineData(4, 4, 0, 0, 1, "blue", 9000, 1000, 5000, 5000, 1000)]
        public void Line(
            double sx0, double sy0, double sx1, double sy1, double sw, string color,
            float dx0, float dy0, float dx1, float dy1, float dw)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            reticle.Elements.Add(new ReticleLine()
            {
                Start = new ReticlePosition(sx0, sy0, AngularUnit.Mil),
                End = new ReticlePosition(sx1, sy1, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(sw),
                Color = color,
            });

            canvas.Setup(canvas => canvas.Line(
                It.Is<float>(f => Approximately(f, dx0)),
                It.Is<float>(f => Approximately(f, dy0)),
                It.Is<float>(f => Approximately(f, dx1)),
                It.Is<float>(f => Approximately(f, dy1)),
                It.Is<float>(f => Approximately(f, dw)),
                It.Is<string>(s => s == color))).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Theory]
        [InlineData(-5, 0, 5, 5, 0.001, "red", true, 0, 5000, 5000, 10000, 1)]
        [InlineData(4, 4, 1, 1, 1, "blue", false, 9000, 1000, 10000, 2000, 1000)]
        public void Rectangle(
           double sx0, double sy0, double sx1, double sy1, double sw, string color, bool fill,
           float dx0, float dy0, float dx1, float dy1, float dw)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            reticle.Elements.Add(new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(sx0, sy0, AngularUnit.Mil),
                Size = new ReticlePosition(sx1, sy1, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(sw),
                Color = color,
                Fill = fill,
            });

            canvas.Setup(canvas => canvas.Rectangle(
                It.Is<float>(f => Approximately(f, dx0)),
                It.Is<float>(f => Approximately(f, dy0)),
                It.Is<float>(f => Approximately(f, dx1)),
                It.Is<float>(f => Approximately(f, dy1)),
                It.Is<float>(f => Approximately(f, dw)),
                It.Is<bool>(b => b == fill),
                It.Is<string>(s => s == color))).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }
    }
}
