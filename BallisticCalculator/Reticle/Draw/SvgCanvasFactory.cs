using System;

namespace BallisticCalculator.Reticle.Draw
{
    /// <summary>
    /// The factory to create an SVG drawing canvas
    /// </summary>
    public static class SvgCanvasFactory
    {
        /// <summary>
        /// Creates an SVG canvas
        /// </summary>
        /// <param name="title"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="viewBoxWidth"></param>
        /// <param name="YtoXratio"></param>
        /// <returns></returns>
        public static IReticleCanvas Create(string title, string width, string height, int viewBoxWidth = 10000, double YtoXratio = 1) => new SvgCanvas(title, width, height, viewBoxWidth, YtoXratio);

        /// <summary>
        /// Gets SVG content of canvas
        /// </summary>
        /// <param name="canvas">The canvas previously created using `Create` method.</param>
        /// <returns></returns>
        public static string ToSvg(IReticleCanvas canvas) => ((canvas as SvgCanvas) ?? throw new ArgumentException($"The canvas must be created by {nameof(SvgCanvasFactory)} class", nameof(canvas))).ToSvg();
    }
}
