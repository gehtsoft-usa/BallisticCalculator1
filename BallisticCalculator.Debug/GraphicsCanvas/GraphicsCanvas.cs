using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using BallisticCalculator.Reticle.Draw;

namespace BallisticCalculator.Reticle.Graphics
{
    public sealed class GraphicsCanvas : IReticleCanvas
    {
        private readonly System.Drawing.Graphics mGraphics;
        private Rectangle mDrawingArea;
        private readonly Brush mBackgroundBrush;

        public float Top => mDrawingArea.Top;
        public float Left => mDrawingArea.Left;
        public float Bottom => mDrawingArea.Bottom;
        public float Right => mDrawingArea.Right;
        public float Width => mDrawingArea.Width;
        public float Height => mDrawingArea.Height;

        public GraphicsCanvas(System.Drawing.Graphics g, Rectangle area, Color? background = null)
        {
            mGraphics = g;
            mDrawingArea = area;
            mBackgroundBrush = new SolidBrush(background ?? Color.White);
        }

        public static GraphicsCanvas FromImage(Image image, Color? background = null)
        {
            Rectangle area = new Rectangle(0, 0, image.Width, image.Height);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image);
            return new GraphicsCanvas(g, area, background);
        }

        public void Clear()
        {
            mGraphics.FillRectangle(mBackgroundBrush, mDrawingArea);
        }

        public void Circle(float x, float y, float radius, float width, bool fill, string color) => Ellipse(x, y, radius, radius, width, fill, color);

        public void Ellipse(float x, float y, float radiusX, float radiusY, float width, bool fill, string color)
        {
            float x1 = x - radiusX;
            float x2 = x + radiusX;
            float y1 = y - radiusY;
            float y2 = y + radiusY;
            if (fill)
                mGraphics.FillEllipse(TranslateBrush(color), x1, y1, x2 - x1, y2 - y1);

            if (!fill || width > 1)
                mGraphics.DrawEllipse(CreatePen(color, width), x1, y1, x2 - x1, y2 - y1);
        }

        public void Line(float x1, float y1, float x2, float y2, float width, string color)
        {
            mGraphics.DrawLine(CreatePen(color, width), x1, y1, x2, y2);
        }

        public void Polyline((float x, float y)[] points, float width, string color)
        {
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new PointF(points[i].x, points[i].y);

            mGraphics.DrawLines(CreatePen(color, width), pts);
        }

        public void Polygon((float x, float y)[] points, float width, bool fill, string color)
        {
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = new PointF(points[i].x, points[i].y);

            if (fill)
                mGraphics.FillPolygon(TranslateBrush(color), pts);

            if (!fill || width > 1)
                mGraphics.DrawPolygon(CreatePen(color, width), pts);
        }

        public void Rectangle(float x1, float y1, float x2, float y2, float width, bool fill, string color)
        {
            if (fill)
                mGraphics.FillRectangle(TranslateBrush(color), x1, y1, x2 - x1, y2 - y1);

            if (!fill || width > 1)
                mGraphics.DrawRectangle(CreatePen(color, width), x1, y1, x2 - x1, y2 - y1);
        }

        public void Text(float x, float y, float height, string text, string color)
        {
            mGraphics.DrawString(text, CreateFont("Verdana", height), TranslateBrush(color), x, y - height);
        }

        public IReticleCanvasPath CreatePath() => new ReticleGraphicsPath();

        public void Path(IReticleCanvasPath path, float width, bool fill, string color)
        {
            if (path is ReticleGraphicsPath graphicsPath)
            {
                if (fill)
                    mGraphics.FillPath(TranslateBrush(color), graphicsPath.Path);
                else
                    mGraphics.DrawPath(CreatePen(color, width), graphicsPath.Path);
            }
            else
                throw new ArgumentException($"Path object is not created by {nameof(GraphicsCanvas)} class", nameof(path));
        }

        private readonly static Dictionary<string, Color> gColorNames;
        private readonly static Dictionary<string, Brush> gSolidBrush;
        private readonly static Dictionary<string, Pen> gPenCache = new Dictionary<string, Pen>();
        private readonly static Dictionary<string, Font> gFontCache = new Dictionary<string, Font>();

#pragma warning disable S3963 // "static" fields should be initialized inline
        static GraphicsCanvas()
        {
            gColorNames = new Dictionary<string, Color>()
            {
                {"white", Color.White},
                {"black", Color.Black},
                {"lightgray", Color.LightGray},
                {"darkgray", Color.DarkGray},
                {"navy", Color.Navy},
                {"blue", Color.Blue},
                {"darkblue", Color.DarkBlue},
                {"red", Color.Red},
                {"darkred", Color.DarkRed},
                {"green", Color.Green},
                {"khaki", Color.Khaki},
                {"darkgreen", Color.DarkGreen},
                {"cyan", Color.Cyan},
                {"darkcyan", Color.DarkCyan},
                {"yellow", Color.Yellow},
                {"brown", Color.Brown},
                {"violet", Color.Violet},
                {"darkviolet", Color.DarkViolet},
                {"magenta", Color.Magenta},
                {"darkmagenta", Color.DarkMagenta},
            };

            gSolidBrush = new Dictionary<string, Brush>();

            foreach (string key in gColorNames.Keys)
                gSolidBrush[key] = new SolidBrush(gColorNames[key]);
        }
#pragma warning restore S3963 // "static" fields should be initialized inline

        private static Color TranslateColor(string name)
        {
            if (!gColorNames.TryGetValue(name ?? "black", out Color color))
                color = Color.Black;
            return color;
        }

        private static Brush TranslateBrush(string name)
        {
            if (!gSolidBrush.TryGetValue(name ?? "black", out Brush brush))
                brush = Brushes.Black;
            return brush;
        }

        private static Pen CreatePen(string color, float width)
        {
            int _width = width < 0.5 ? 1 : (int)Math.Round((double)width);

            string key = $"{color ?? "black"}{_width}";
            if (!gPenCache.TryGetValue(key, out Pen pen))
            {
                pen = new Pen(TranslateColor(color), _width);
                if (gPenCache.Count > 5000)
                    gPenCache.Clear();
                gPenCache[key] = pen;
            }

            return pen;
        }

        private static Font CreateFont(string face, float width)
        {
            int _width = width < 0.5 ? 1 : (int)Math.Round((double)width);

            string key = $"{face ?? "Verdana"}{_width}";
            if (!gFontCache.TryGetValue(key, out Font font))
            {
                font = new Font("Verdana", _width, GraphicsUnit.Pixel);
                if (gFontCache.Count > 5000)
                    gFontCache.Clear();
                gFontCache[key] = font;
            }

            return font;
        }
    }
}
