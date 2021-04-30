using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;

namespace BallisticCalculator.Reticle.Graphics
{
    internal sealed class ReticleGraphicsPath : IReticleCanvasPath
    {
        private GraphicsPath mPath = new GraphicsPath();
        private PointF mCurrentPosition = new PointF(0, 0);

        public GraphicsPath Path => mPath;

        public ReticleGraphicsPath()
        {
            mPath.StartFigure();
        }

        public void Dispose()
        {
            mPath?.Dispose();
            mPath = null;
        }

        public void MoveTo(float x, float y)
        {
            mCurrentPosition = new PointF(x, y);
        }

        public void LineTo(float x, float y)
        {
            PointF destination = new PointF(x, y);
            mPath.AddLine(mCurrentPosition, destination);
            mCurrentPosition = destination;
        }

        public void Arc(float r, float x, float y, bool largeArc, bool clockwiseDirection)
        {
            PointF destination = new PointF(x, y);
            if (Math.Abs(destination.X - mCurrentPosition.X) < 0.5 &&
                Math.Abs(destination.Y - mCurrentPosition.Y) < 0.5)
                return;

            if (r < 0.5)
            {
                mPath.AddLine(mCurrentPosition, destination);
            }
            else
            {
                SvgArcCalculator calculator = new SvgArcCalculator(r, mCurrentPosition.X, mCurrentPosition.Y, x, y, largeArc, clockwiseDirection);

                mPath.AddArc(calculator.CX - calculator.CorrRx,
                    calculator.CY - calculator.CorrRy,
                    calculator.CorrRx * 2, calculator.CorrRy * 2,
                   calculator.AngleStart, calculator.AngleExtent);
            }
            mCurrentPosition = destination;
        }

        public void Close()
        {
            mPath.CloseFigure();
        }
    }
}