using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using BallisticCalculator.Test.Calculator;
using AwesomeAssertions;
using Gehtsoft.Measurements;
using Moq;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    public class ReticleControllerTest
    {
        private static Mock<IReticleCanvas> CreateMockCanvas()
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

        private static ReticleDefinition CreateReticle(double size = 10, double? zeroy = null)
        {
            var definition = new ReticleDefinition()
            {
                Name = "Test",
                Size = new ReticlePosition(size, size, AngularUnit.Mil),
                Zero = new ReticlePosition(size / 2, zeroy ?? (size / 2), AngularUnit.Mil)
            };
            return definition;
        }

        private static bool Approximately(float x1, float x2, float epsilon) => Math.Abs(x1 - x2) < epsilon;
        private static bool Approximately(float x1, float x2) => Approximately(x1, x2, 1e-2f);

        [Theory]
        [InlineData(-5, -5, 5, 5, 0.001, "red", 0, 10000, 10000, 0, 1)]
        [InlineData(4, 4, 0, 0, 1, "blue", 9000, 1000, 5000, 5000, 1000)]
        public void ReticleElement_Line(
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
                It.Is<string>(s => s == color),
                It.IsAny<ReticleLineStyle>())).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Theory]
        [InlineData(-5, 0, 5, 5, 0.001, "red", true, 0, 5000, 5000, 10000, 1)]
        [InlineData(4, 4, 1, 1, 1, "blue", false, 9000, 1000, 10000, 2000, 1000)]
        public void ReticleElement_Rectangle(
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
                It.Is<string>(s => s == color),
                It.IsAny<ReticleLineStyle>())).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Theory]
        [InlineData(0, 0, 2, 0.001, "red", true, 5000, 5000, 2000, 1)]
        [InlineData(1, 1, 2.5, 0.001, "red", true, 6000, 4000, 2500, 1)]
        [InlineData(-1.5, -1.5, 0.5, 0.1, "blue", false, 3500, 6500, 500, 100)]
        public void ReticleElement_Circle(
           double sx0, double sy0, double sr, double sw, string color, bool fill,
           float dx0, float dy0, float dr, float dw)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            reticle.Elements.Add(new ReticleCircle()
            {
                Center = new ReticlePosition(sx0, sy0, AngularUnit.Mil),
                Radius = AngularUnit.Mil.New(sr),
                LineWidth = AngularUnit.Mil.New(sw),
                Color = color,
                Fill = fill,
            });

            canvas.Setup(canvas => canvas.Circle(
                It.Is<float>(f => Approximately(f, dx0)),
                It.Is<float>(f => Approximately(f, dy0)),
                It.Is<float>(f => Approximately(f, dr)),
                It.Is<float>(f => Approximately(f, dw)),
                It.Is<bool>(b => b == fill),
                It.Is<string>(s => s == color),
                It.IsAny<ReticleLineStyle>())).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Theory]
        [InlineData(0, 0, 0.5, "the text", "cyan", 5000, 5000, 500)]
        public void ReticleElement_Text(
           double sx0, double sy0, double sh, string text, string color,
           float dx0, float dy0, float dh)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            reticle.Elements.Add(new ReticleText()
            {
                Position = new ReticlePosition(sx0, sy0, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(sh),
                Text = text,
                Color = color,
            });

            canvas.Setup(canvas => canvas.Text(
                It.Is<float>(f => Approximately(f, dx0)),
                It.Is<float>(f => Approximately(f, dy0)),
                It.Is<float>(f => Approximately(f, dh)),
                It.Is<string>(s => s == text),
                It.Is<string>(s => s == color),
                It.Is<TextAnchor>(a => a == TextAnchor.Left))).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Fact]
        public void ReticleElement_Path()
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();

            var reticlePath = new ReticlePath()
            {
                Color = "red",
                Fill = true,
            };

            reticlePath.Elements.Add(new ReticlePathElementMoveTo()
            {
                Position = new ReticlePosition(-2.5, 0, AngularUnit.Mil),
            });

            reticlePath.Elements.Add(new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(0, 1, AngularUnit.Mil),
            });

            reticlePath.Elements.Add(new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(2.5, 0, AngularUnit.Mil),
            });

            reticlePath.Elements.Add(new ReticlePathElementArc()
            {
                Position = new ReticlePosition(-2.5, 0, AngularUnit.Mil),
                ClockwiseDirection = false,
                MajorArc = true,
                Radius = AngularUnit.Mil.New(2.5)
            });

            reticle.Elements.Add(reticlePath);

            int order = 0;

            var canvasPath = new Mock<IReticleCanvasPath>();
            canvasPath.Setup(
                path => path.MoveTo(
                    It.Is<float>(f => Approximately(f, 2500)),
                    It.Is<float>(f => Approximately(f, 5000))
                    ))
                .Callback(() => (order++).Should().Be(1, "Order of adding path items into path failed"))
                .Verifiable();

            canvasPath.Setup(
                path => path.LineTo(
                    It.Is<float>(f => Approximately(f, 5000)),
                    It.Is<float>(f => Approximately(f, 4000))
                    ))
                .Callback(() => (order++).Should().Be(2, "Order of adding path items into path failed"))
                .Verifiable();

            canvasPath.Setup(
                path => path.LineTo(
                    It.Is<float>(f => Approximately(f, 7500)),
                    It.Is<float>(f => Approximately(f, 5000))
                    ))
                .Callback(() => (order++).Should().Be(3, "Order of adding path items into path failed"))
                .Verifiable();

            canvasPath.Setup(
                path => path.Arc(
                    It.Is<float>(f => Approximately(f, 2500)),
                    It.Is<float>(f => Approximately(f, 2500)),
                    It.Is<float>(f => Approximately(f, 5000)),
                    It.Is<bool>(f => f),
                    It.Is<bool>(f => !f)
                    ))
                .Callback(() => (order++).Should().Be(4, "Order of adding path items into path failed"))
                .Verifiable();

            canvas.Setup(
                canvas => canvas.CreatePath())
                .Returns(canvasPath.Object)
                .Callback(() => (order++).Should().Be(0, "Creating a patch must be first action"))
                .Verifiable();

            canvas.Setup(
                canvas => canvas.Path(
                    It.Is<IReticleCanvasPath>(path => path == canvasPath.Object),
                    It.Is<float>(f => Approximately(f, 1)),
                    It.Is<bool>(f => f),
                    It.Is<string>(s => s == "red"),
                    It.IsAny<ReticleLineStyle>()))
                .Callback(() => (order++).Should().Be(5, "Drawing path must be the last action"))
                .Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
            canvasPath.Verify();
        }

        [Theory]
        [InlineData(-5, -5, 5, 5, 0.001, "red", 0, 9000, 10000, -1000, 1)]
        [InlineData(-5, -4, 5, 4, 0.001, "red", 0, 8000, 10000, 0, 1)]
        [InlineData(4, 3, 0, 0, 1, "blue", 9000, 1000, 5000, 4000, 1000)]
        [InlineData(-4, -3, 0, 0, 1, "blue", 1000, 7000, 5000, 4000, 1000)]
        public void ReticleElement_Line_ZeroOffCenter(
            double sx0, double sy0, double sx1, double sy1, double sw, string color,
            float dx0, float dy0, float dx1, float dy1, float dw)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle(10, 4);
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
                It.Is<string>(s => s == color),
                It.IsAny<ReticleLineStyle>())).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawReticle();
            canvas.Verify();
        }

        [Fact]
        public void Reticle_DrawShot()
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            var trajectory = TableLoader.FromResource("g1_wind");

            // How constants calculated:
            //     Reticle: 10x10mil
            //     Canvas: 10000x1000 pixels
            //     Center of Target @ 250ft (see trajectory -> -3.2moa drop (0.95068 mil), 2.6moa windage (0.75828 mil))
            //     Map center to target: mil to pixel y: 950 below zero, x: 758 LEFT of zero against
            //       zero @ 5000x5000. The windage is positive, which in the trajectory means the
            //       bullet went LEFT, so the target sits left of centre - see
            //       Reticle_DrawShot_WindageGoesToTheDriftSide.
            //     Linear size of the target = atan(8 inches / 250 yards) = 0.905414549 mil => 905 pixels
            //     c1 = center - size / 2, c2 = center + size / 2

            canvas.Setup(canva => canva.Rectangle(
                It.Is<float>(f => Approximately(f, 3789, 5)),
                It.Is<float>(f => Approximately(f, 5497, 5)),
                It.Is<float>(f => Approximately(f, 4694, 5)),
                It.Is<float>(f => Approximately(f, 6403, 5)),
                It.Is<float>(f => Approximately(f, 1, 0.1f)),
                It.Is<bool>(b => !b),
                It.Is<string>(s => s == "zecolor"),
                It.IsAny<ReticleLineStyle>())).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawTarget(trajectory.Trajectory, DistanceUnit.Inch.New(8), DistanceUnit.Yard.New(250), "zecolor");

            canvas.Verify();
        }

        /// <summary>
        /// The reticle's X grows to the right, while TrajectoryPoint.Windage is positive to the
        /// LEFT. DrawTarget has to negate one to get the other, and for a long time it did not, so
        /// every target was mirrored across the vertical axis. The drop must NOT be negated, because
        /// the reticle's Y and DropAdjustment agree that positive is up.
        /// </summary>
        [Theory]
        [InlineData(-1.0, true, "right-drifting bullet (negative windage) draws right of centre")]
        [InlineData(1.0, false, "left-drifting bullet (positive windage) draws left of centre")]
        public void Reticle_DrawShot_WindageGoesToTheDriftSide(double windageMil, bool expectRight, string because)
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();          // 10x10 mil, zero at 5,5 -> 1000 px per mil

            var distance = DistanceUnit.Yard.New(1000);
            var point = new TrajectoryPoint(
                time: TimeSpan.FromSeconds(1), distance: distance, distanceFlat: distance,
                velocity: VelocityUnit.FeetPerSecond.New(1500), mach: 1.3,
                drop: DistanceUnit.Inch.New(-100), dropFlat: DistanceUnit.Inch.New(-100),
                dropAdjustment: AngularUnit.Mil.New(-2),
                lineOfSightElevation: Measurement<DistanceUnit>.ZERO,
                lineOfDepartureElevation: Measurement<DistanceUnit>.ZERO,
                windage: DistanceUnit.Inch.New(windageMil * 36),
                windageAdjustment: AngularUnit.Mil.New(windageMil),
                energy: EnergyUnit.FootPound.New(1000),
                optimalGameWeight: WeightUnit.Pound.New(100));
            // FindByDistance needs a point beyond the target distance before it returns anything
            var beyond = new TrajectoryPoint(TimeSpan.FromSeconds(2), DistanceUnit.Yard.New(1100),
                VelocityUnit.FeetPerSecond.New(1400), 1.2, DistanceUnit.Inch.New(-150),
                DistanceUnit.Inch.New(0), EnergyUnit.FootPound.New(900), WeightUnit.Pound.New(90));

            float x1 = 0, x2 = 0, y1 = 0;
            canvas.Setup(c => c.Rectangle(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(),
                                          It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>(),
                                          It.IsAny<string>(), It.IsAny<ReticleLineStyle>()))
                  .Callback<float, float, float, float, float, bool, string, ReticleLineStyle>(
                      (a, b, c, d, e, f, g, h) => { x1 = a; y1 = b; x2 = c; });

            new ReticleDrawController(reticle, canvas.Object)
                .DrawTarget(new[] { point, beyond }, DistanceUnit.Inch.New(36), distance, "red");

            double centreX = (x1 + x2) / 2.0;
            if (expectRight)
                centreX.Should().BeGreaterThan(5000, because);
            else
                centreX.Should().BeLessThan(5000, because);

            // the magnitude is the windage, mirrored only in sign
            Math.Abs(centreX - 5000).Should().BeApproximately(1000 * Math.Abs(windageMil), 5,
                "the offset from the axis is the windage itself");
            // and the drop is NOT negated: 2 mil of drop is 2000 px below the 5000 axis
            double centreY = y1 + (x2 - x1) / 2.0;
            centreY.Should().BeApproximately(7000, 5,
                "DropAdjustment shares its sign convention with the reticle's Y");
        }

        [Fact]
        public void BDC_LongRange()
        {
            var canvas = CreateMockCanvas();
            var reticle = CreateReticle();
            var trajectory = TableLoader.FromResource("g1_nowind");
            
            //we need detailed trajectory to calculate BDC
            var calc = new TrajectoryCalculator();
            trajectory.ShotParameters.ZeroDropAdjustment = calc.CalculateZeroParameters(trajectory.Ammunition, trajectory.Atmosphere, trajectory.Rifle, trajectory.Rifle.Zero).ZeroDropAdjustment;
            trajectory.ShotParameters.Step = DistanceUnit.Yard.New(10);
            trajectory.ShotParameters.MaximumDistance = DistanceUnit.Yard.New(500);
            var trajectory1 = calc.Calculate(trajectory.Ammunition, trajectory.Rifle, trajectory.Atmosphere, trajectory.ShotParameters, new[] { trajectory.Wind });

            reticle.BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(AngularUnit.Mil.New(0), AngularUnit.Mil.New(-0.5)),
                TextHeight = AngularUnit.Mil.New(0.5),
                TextOffset = AngularUnit.Mil.New(0),
            });

            reticle.BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(AngularUnit.Mil.New(0), AngularUnit.Mil.New(-1)),
                TextHeight = AngularUnit.Mil.New(0.5),
                TextOffset = AngularUnit.Mil.New(1),
            });

            reticle.BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(AngularUnit.Mil.New(0), AngularUnit.Mil.New(-2)),
                TextHeight = AngularUnit.Mil.New(0.5),
                TextOffset = AngularUnit.Mil.New(-1),
            });

            canvas.Setup(canvas => canvas.Text(
                It.Is<float>(f => Approximately(f, 5000)),                          //x == center
                It.Is<float>(f => Approximately(f, 5750)),                          //x - 0.5mil(dot) + 0.5mil(text height) / 2
                It.Is<float>(f => Approximately(f, 500)),
                It.Is<string>(s => Approximately(float.Parse(s), 195, 10f)),        //~195 yards on the trajectory of test
                It.Is<string>(s => s == "black"),
                It.Is<TextAnchor>(a => a == TextAnchor.Left))).Verifiable();

            canvas.Setup(canvas => canvas.Text(
                It.Is<float>(f => Approximately(f, 6000)),                          //x == center + 1 mil
                It.Is<float>(f => Approximately(f, 6250)),                          //x - 1mil(dot) + 0.5mil(text height) / 2
                It.Is<float>(f => Approximately(f, 500)),
                It.Is<string>(s => Approximately(float.Parse(s), 257, 10f)),        //~257 yards on the trajectory of test
                It.Is<string>(s => s == "black"),
                It.Is<TextAnchor>(a => a == TextAnchor.Left))).Verifiable();

            canvas.Setup(canvas => canvas.Text(
                It.Is<float>(f => Approximately(f, 4000)),                          //x == center - 1 mil
                It.Is<float>(f => Approximately(f, 7250)),                          //x - 2mil(dot) + 0.5mil(text height) / 2
                It.Is<float>(f => Approximately(f, 500)),
                It.Is<string>(s => Approximately(float.Parse(s), 356, 10f)),        //~356 yards on the trajectory of test
                It.Is<string>(s => s == "black"),
                It.Is<TextAnchor>(a => a == TextAnchor.Left))).Verifiable();

            ReticleDrawController controller = new ReticleDrawController(reticle, canvas.Object);
            controller.DrawBulletDropCompensator(trajectory1, trajectory.Rifle.Zero.Distance, false, DistanceUnit.Yard, "black");

            canvas.Verify();
        }
    }
}
