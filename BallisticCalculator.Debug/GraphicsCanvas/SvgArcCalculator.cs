using System;
using System.Drawing;

namespace BallisticCalculator.Reticle.Graphics
{
    internal class SvgArcCalculator
    {
        private readonly bool  mLargeArc;
        private readonly bool mSweepFlag;
        private readonly float mX1;
        private readonly float mY1;
        private readonly float mX2;
        private readonly float mY2;
        private readonly float mR1;
        private readonly float mR2;
        private readonly float mAngle;

        private double mCorrRx;
        public float CorrRx => (float)mCorrRx;
        private double mCorrRy;
        public float CorrRy => (float)mCorrRy;
        private double mCX;
        public float CX => (float)mCX;
        private double mCY;
        public float CY => (float)mCY;
        private double mAngleStart;
        public float AngleStart => (float)mAngleStart;
        private double mAngleExtent;
        public float AngleExtent => (float)mAngleExtent;

        public SvgArcCalculator(float r, float x1, float y1, float x2, float y2, bool largeArc, bool clockwiseDirection)
        {
            mLargeArc = largeArc;
            mSweepFlag = clockwiseDirection;
            mX1 = x1;
            mX2 = x2;
            mY1 = y1;
            mY2 = y2;
            mR1 = mR2 = r;
            mAngle = 0;
            GetCalculatedArcValues();
        }

        private void GetCalculatedArcValues()
        {
            /*
             *	This algorithm is taken from the Batik source. All cudos to the Batik crew.
             */
            PointF startPoint = new PointF(mX1, mY1);
            PointF endPoint = new PointF(mX2, mY2);

            double x0 = startPoint.X;
            double y0 = startPoint.Y;

            double x = endPoint.X;
            double y = endPoint.Y;

            // Compute the half distance between the current and the final point
            double dx2 = (x0 - x) / 2.0;
            double dy2 = (y0 - y) / 2.0;

            // Convert angle from degrees to radians
            double radAngle = mAngle * Math.PI / 180;
            double cosAngle = Math.Cos(radAngle);
            double sinAngle = Math.Sin(radAngle);

            //
            // Step 1 : Compute (x1, y1)
            //
            double x1 = (cosAngle * dx2 + sinAngle * dy2);
            double y1 = (-sinAngle * dx2 + cosAngle * dy2);
            // Ensure radii are large enough

            double rx = Math.Abs(mR1);
            double ry = Math.Abs(mR2);

            double Prx = rx * rx;
            double Pry = ry * ry;
            double Px1 = x1 * x1;
            double Py1 = y1 * y1;

            // check that radii are large enough
            double radiiCheck = Px1 / Prx + Py1 / Pry;
            if (radiiCheck > 1)
            {
                rx = Math.Sqrt(radiiCheck) * rx;
                ry = Math.Sqrt(radiiCheck) * ry;
                Prx = rx * rx;
                Pry = ry * ry;
            }

            //
            // Step 2 : Compute (cx1, cy1)
            //
            double sign = (mLargeArc == mSweepFlag) ? -1 : 1;
            double sq = ((Prx * Pry) - (Prx * Py1) - (Pry * Px1)) / ((Prx * Py1) + (Pry * Px1));
            sq = (sq < 0) ? 0 : sq;
            double coef = (sign * Math.Sqrt(sq));
            double cx1 = coef * ((rx * y1) / ry);
            double cy1 = coef * -((ry * x1) / rx);

            //
            // Step 3 : Compute (cx, cy) from (cx1, cy1)
            //
            double sx2 = (x0 + x) / 2.0;
            double sy2 = (y0 + y) / 2.0;
            double cx = sx2 + (cosAngle * cx1 - sinAngle * cy1);
            double cy = sy2 + (sinAngle * cx1 + cosAngle * cy1);

            //
            // Step 4 : Compute the angleStart (angle1) and the angleExtent (dangle)
            //
            double ux = (x1 - cx1);
            double uy = (y1 - cy1);
            double vx = (-x1 - cx1);
            double vy = (-y1 - cy1);
            double p, n;

            // Compute the angle start
            n = Math.Sqrt((ux * ux) + (uy * uy));
            p = ux; // (1 * ux) + (0 * uy)
            sign = (uy < 0) ? -1d : 1d;
            double angleStart = sign * Math.Acos(p / n);
            angleStart = angleStart * 180 / Math.PI;

            // Compute the angle extent
            n = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            p = ux * vx + uy * vy;
            sign = (ux * vy - uy * vx < 0) ? -1d : 1d;
            double angleExtent = sign * Math.Acos(p / n);
            angleExtent = angleExtent * 180 / Math.PI;

            if (!mSweepFlag && angleExtent > 0)
            {
                angleExtent -= 360f;
            }
            else if (mSweepFlag && angleExtent < 0)
            {
                angleExtent += 360f;
            }
            angleExtent %= 360f;
            angleStart %= 360f;

            mCorrRx = rx;
            mCorrRy = ry;
            mCX = cx;
            mCY = cy;
            mAngleStart = angleStart;
            mAngleExtent = angleExtent;
        }
    }
}