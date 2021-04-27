using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BallisticCalculator.Reticle.Data;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Draw
{
    public class ReticleDrawController
    {
        private readonly CoordinateTranslator translator;
        private readonly ReticleDefinition reticle;
        private readonly IReticleCanvas canvas;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reticle">The reticle</param>
        /// <param name="canvas">The canvas</param>
        public ReticleDrawController(ReticleDefinition reticle, IReticleCanvas canvas)
        {
            this.reticle = reticle;
            this.canvas = canvas;
            translator = new CoordinateTranslator(reticle.Size.X, reticle.Size.Y, reticle.Zero.X, reticle.Zero.Y, canvas.Width, canvas.Height);
        }

        private void DrawElement(ReticleElement element)
        {
            switch (element)
            {
                case ReticleLine line:
                    {
                        translator.Transform(line.Start.X, line.Start.Y, out float x0, out float y0);
                        translator.Transform(line.End.X, line.End.Y, out float x1, out float y1);
                        canvas.Line(x0, y0, x1, y1, translator.TransformL(line.LineWidth), line.Color ?? "black");

                    }
                    break;
                case ReticleCircle circle:
                    {
                        translator.Transform(circle.Center.X, circle.Center.Y, out float x0, out float y0);
                        canvas.Circle(x0, y0, translator.TransformL(circle.Radius), translator.TransformL(circle.LineWidth), circle.Fill ?? false, circle.Color ?? "black");

                    }
                    break;
                case ReticleRectangle rectangle:
                    {
                        translator.Transform(rectangle.TopLeft.X, rectangle.TopLeft.Y, out float x0, out float y0);
                        float x1 = translator.TransformL(rectangle.Size.X);
                        float y1 = translator.TransformL(rectangle.Size.Y);
                        canvas.Rectangle(x0, y0, x0 + x1, y0 + y1, translator.TransformL(rectangle.LineWidth), rectangle.Fill ?? false, rectangle.Color ?? "black");
                    }
                    break;
                case ReticleText text:
                    {
                        translator.Transform(text.Position.X, text.Position.Y, out float x0, out float y0);
                        var h = translator.TransformL(text.TextHeight);
                        canvas.Text(x0, y0, h, text.Text, text.Color);
                    }
                    break;
                case ReticlePath path:
                    {
                        using var path1 = canvas.CreatePath();
                        foreach (var pathElement in path.Elements)
                        {
                            switch (pathElement)
                            {
                                case ReticlePathElementMoveTo moveTo:
                                    {
                                        translator.Transform(moveTo.Position.X, moveTo.Position.Y, out float x, out float y);
                                        path1.MoveTo(x, y);
                                    }
                                    break;
                                case ReticlePathElementLineTo lineTo:
                                    {
                                        translator.Transform(lineTo.Position.X, lineTo.Position.Y, out float x, out float y);
                                        path1.LineTo(x, y);
                                    }
                                    break;
                                case ReticlePathElementArc arc:
                                    {
                                        translator.Transform(arc.Position.X, arc.Position.Y, out float x, out float y);
                                        path1.Arc(translator.TransformL(arc.Radius), x, y, arc.MajorArc, arc.ClockwiseDirection);
                                    }
                                    break;
                            }
                        }
                        if (path.Fill ?? false)
                            path1.Close();
                        canvas.Path(path1, translator.TransformL(path.LineWidth), path.Fill ?? false, path.Color ?? "black");
                    }
                    break;
            }
        }

        /// <summary>
        /// Draws the reticle on the specified canvas
        /// </summary>
        public void DrawReticle()
        {
            foreach (var element in reticle.Elements) 
                DrawElement(element);

        }

        private IEnumerable<ReticleElement> CalculateBdc(ReticleDefinition reticle, IEnumerable<TrajectoryPoint> trajectory, Measurement<DistanceUnit> zero, bool closeBdc, DistanceUnit distanceUnits, string color)
        {
            TrajectoryPoint previousPoint = null;
            foreach (var point in trajectory)
            {
                if (!closeBdc && point.Distance < zero)
                {
                    previousPoint = point;
                    continue;
                }
                if (closeBdc && point.Distance >= zero)
                    break;
                if (previousPoint != null)
                {
                    foreach (var bdcPoint in reticle.BulletDropCompensator)
                    {
                        if ((previousPoint.DropAdjustment >= bdcPoint.Position.Y &&
                             point.DropAdjustment <= bdcPoint.Position.Y) ||
                            (previousPoint.DropAdjustment <= bdcPoint.Position.Y &&
                             point.DropAdjustment >= bdcPoint.Position.Y))
                        {
                            var x = bdcPoint.Position.X + bdcPoint.TextOffset;
                            var y = bdcPoint.Position.Y - bdcPoint.TextHeight / 2;

                            yield return new ReticleText()
                            {
                                Position = new ReticlePosition() { X = x, Y = y},
                                TextHeight = bdcPoint.TextHeight,
                                Text = Math.Round(point.Distance.In(distanceUnits)).ToString(),
                                Color = color,
                            };


                        }
                    }
                }
                previousPoint = point;
            }
        }

        /// <summary>
        /// <para>BDC on the specified canvas</para>
        /// <para>Call this method after drawing the reticle</para>
        /// </summary>
        /// <param name="trajectory">The trajectory to get the BDC parameters</param>
        /// <param name="zero">The Zero distance</param>
        /// <param name="closeBdc">The flag indicating whether BDC point should take distance closer than zero (`true`) or more far than zero (`false`)</param>
        /// <param name="units">The units of the distance for BDC labels</param>
        /// <param name="color">The text color for BDC labels</param>
        public void DrawBulletDropCompensator(IEnumerable<TrajectoryPoint> trajectory, Measurement<DistanceUnit> zero, bool closeBdc, DistanceUnit units, string color)
        {
            foreach (var bdc in CalculateBdc(reticle, trajectory, zero, closeBdc, units, color))
                DrawElement(bdc);
        }

        /// <summary>
        /// <para>Draws a square target on the specified canvas</para>
        /// <para>Use this method before drawing the reticle</para>
        /// </summary>
        /// <param name="trajectory">The trajectory to get the BDC parameters</param>
        /// <param name="targetSize">The size of a side of a rectangular target</param>
        /// <param name="targetDistance">The distance to the target</param>
        /// <param name="color">The color of the target</param>
        public void DrawTarget(IEnumerable<TrajectoryPoint> trajectory, 
                                Measurement<DistanceUnit> targetSize, Measurement<DistanceUnit> targetDistance,
                                string color)
        {
            var angularTargetSize = MeasurementMath.Atan(targetSize / targetDistance);
            var trajectoryPoint = FindByDistance(trajectory, targetDistance);
            if (trajectoryPoint != null)
            {
                var centerY = trajectoryPoint.DropAdjustment;
                var centerX = trajectoryPoint.WindageAdjustment;
                var x0 = centerX - angularTargetSize / 2;
                var y0 = centerY - angularTargetSize / 2;

                DrawElement(new ReticleRectangle()
                    {
                        TopLeft = new ReticlePosition(x0, y0),
                        Size = new ReticlePosition(angularTargetSize, angularTargetSize),
                        Fill = false,
                        Color = color,
                    });
            }
        }

        private static TrajectoryPoint FindByDistance(IEnumerable<TrajectoryPoint> trajectory, Measurement<DistanceUnit> distance)
        {
            TrajectoryPoint previous = null;

            foreach (var point in trajectory)
            {
                if (point.Distance > distance)
                    return previous;
                previous = point;
            }
            return null;
        }
    }
}
