using System;

namespace BallisticCalculator.Reticle.Draw
{
    /// <summary>
    /// The interface to a path on a reticle canvas.
    /// </summary>
    public interface IReticleCanvasPath : IDisposable
    {
        /// <summary>
        /// Move to the position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void MoveTo(float x, float y);
        
        /// <summary>
        /// Draw line to the position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void LineTo(float x, float y);
        
        /// <summary>
        /// Draw arc
        /// </summary>
        /// <param name="r"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="largeArc"></param>
        /// <param name="clockwiseDirection"></param>
        void Arc(float r, float x, float y, bool largeArc, bool clockwiseDirection);
        
        /// <summary>
        /// Close path back to the first point
        /// </summary>
        void Close();
    }
}