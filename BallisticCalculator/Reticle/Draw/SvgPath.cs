using System;
using System.Text;

namespace BallisticCalculator.Reticle.Draw
{
    internal sealed class SvgPath : IReticleCanvasPath
    {
        private readonly StringBuilder mPath = new StringBuilder();

        public string GetSvgPath() => mPath.ToString();

        public bool Closed { get; set; } = false;

        public void Dispose()
        {
            //no managed resource associated with path in SVG
        }

        public void MoveTo(float x, float y)
        {
            if (mPath.Length > 0)
                mPath.Append(' ');
            mPath.AppendFormat("M{0},{1}", (int)Math.Floor((double)x), (int)Math.Floor((double)y));
        }

        public void LineTo(float x, float y)
        {
            if (mPath.Length > 0)
                mPath.Append(' ');
            mPath.AppendFormat("L{0},{1}", (int)Math.Floor((double)x), (int)Math.Floor((double)y));
        }

        public void Arc(float r, float x, float y, bool largeArc, bool clockwiseDirection)
        {
            mPath.AppendFormat("A{0},{1} 0 {2},{3} {4},{5}", (int)Math.Floor((double)r), (int)Math.Floor((double)r), largeArc ? 1 : 0, clockwiseDirection ? 1 : 0, (int)Math.Floor((double)x), (int)Math.Floor((double)y));
        }

        public void Close()
        {
            Closed = true;
            if (mPath.Length > 0)
                mPath.Append(' ');
            mPath.Append('z');
        }
    }
}
