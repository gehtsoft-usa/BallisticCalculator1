using BallisticCalculator.Reticle.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallisticCalculator.Reticle.Draw
{
    /// <summary>
    /// The interface to a reticle canvas
    /// </summary>
    public interface IReticleCanvas
    {
        /// <summary>
        /// Maximum Y coordinate
        /// </summary>
        float Top { get; } 
        /// <summary>
        /// Minimum X coordinate
        /// </summary>
        float Left { get; } 
        /// <summary>
        /// Minimum X coordinate
        /// </summary>
        float Bottom { get; } 
        /// <summary>
        /// Maximum X coordinate
        /// </summary>
        float Right { get; }
        /// <summary>
        /// Width 
        /// </summary>
        float Width { get; }
        /// <summary>
        /// Height
        /// </summary>
        float Height { get; }

        /// <summary>
        /// Clears everything on canvas
        /// </summary>
        
        void Clear();
        /// <summary>
        /// Draw circle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>       
        void Circle(float x, float y, float radius, float width, bool fill, string color);
        /// <summary>
        /// Draw line
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="width"></param>
        /// <param name="color"></param>
        
        void Line(float x1, float y1, float x2, float y2, float width, string color);
        
        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>
       
        void Rectangle(float x1, float y1, float x2, float y2, float width, bool fill, string color);

        /// <summary>
        /// Draw a text
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>       
        void Text(float x, float y, float height, string text, string color);

        /// <summary>
        /// Draw a text
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="anchor"></param>
        void Text(float x, float y, float height, string text, string color, TextAnchor anchor);

        /// <summary>
        /// Create a path object
        /// </summary>
        /// <returns></returns>
        IReticleCanvasPath CreatePath();
        
        /// <summary>
        /// Draw a path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>
        void Path(IReticleCanvasPath path, float width, bool fill, string color);
    }
}
