using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using BallisticCalculator.Data;
using BallisticCalculator.Reticle.Data;

namespace BallisticCalculator.Reticle.Draw
{
    /// <summary>
    /// SVG drawing canvas
    /// </summary>
    internal class SvgCanvas : IReticleCanvas
    {
        private void AppendAttribute(XmlNode node, string attributeName, string value)
        {
            XmlAttribute attribute = document.CreateAttribute(attributeName);
            attribute.Value = value;
            node.Attributes.Append(attribute);
        }

        private void AppendAttribute(XmlNode node, string attribute, float value) => AppendAttribute(node, attribute, $"{(int)Math.Floor((double)value)}");

        /// <summary>
        /// Canvas title
        /// </summary>
        public string Title { get; }
        /// <summary>
        /// Width of SVG canvas
        /// </summary>
        public string SvgWidth { get; }
        /// <summary>
        /// Height of SVG canvas
        /// </summary>
        public string SvgHeight { get; }
        /// <summary>
        /// Width of View Box
        /// </summary>
        public int ViewBoxWidth { get; }
        /// <summary>
        /// Height of View Box
        /// </summary>
        public int ViewBoxHeight { get; }

        private XmlDocument document;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title"></param>
        /// <param name="width">Width of canvas in HTML/CSS units (e.g. 5in)</param>
        /// <param name="height">Width of canvas in HTML/CSS units (e.g. 5in)</param>
        /// <param name="viewBoxWidth">The size of view box in drawing coordinates</param>
        /// <param name="YtoXratio">The ratio of conversion of X to Y view box size</param>
        public SvgCanvas(string title, string width, string height, int viewBoxWidth = 10000, double YtoXratio = 1)
        {
            Title = title;
            SvgWidth = width;
            SvgHeight = height;
            ViewBoxWidth = viewBoxWidth;
            ViewBoxHeight = (int)Math.Floor(YtoXratio * (double)ViewBoxWidth);
            Clear();
        }

        /// <summary>
        /// Maximum Y coordinate
        /// </summary>
        public float Top => 0;
        /// <summary>
        /// Minimum X coordinate
        /// </summary>
        public float Left => 0;
        /// <summary>
        /// Minimum X coordinate
        /// </summary>
        public float Bottom => ViewBoxHeight - 1;
        /// <summary>
        /// Maximum X coordinate
        /// </summary>
        public float Right => ViewBoxWidth - 1;
        /// <summary>
        /// Width
        /// </summary>
        public float Width => ViewBoxWidth;
        /// <summary>
        /// Height
        /// </summary>
        public float Height => ViewBoxHeight;

        /// <summary>
        /// Returns SVG document
        /// </summary>
        /// <returns></returns>
        public string ToSvg() => document?.DocumentElement?.OuterXml;

        /// <summary>
        /// Clears everything on canvas
        /// </summary>
        public void Clear()
        {
            document = new XmlDocument();
            XmlNode root = document.CreateElement("svg");

            AppendAttribute(root, "xmlns", "http://www.w3.org/2000/svg");
            AppendAttribute(root, "version", "1.1");
            AppendAttribute(root, "width", SvgWidth);
            AppendAttribute(root, "height", SvgHeight);
            AppendAttribute(root, "viewBox", $"0 0 {ViewBoxWidth} {ViewBoxHeight}");
            document.AppendChild(root);

            if (!string.IsNullOrEmpty(Title))
            {
                XmlNode title = document.CreateElement("title");
                title.AppendChild(document.CreateTextNode(Title));
                root.AppendChild(title);
            }
        }

        /// <summary>
        /// Draw circle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="radius"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>
        public void Circle(float x, float y, float radius, float width, bool fill, string color)
        {
            XmlNode element = document.CreateElement("circle");
            AppendAttribute(element, "cx", x);
            AppendAttribute(element, "cy", y);
            AppendAttribute(element, "r", radius < 0.5 ? 1.0f : radius);
            AppendAttribute(element, "stroke", color);
            AppendAttribute(element, "stroke-width", width < 1 && !fill ? 1 : width);
            AppendAttribute(element, "fill", fill ? color : "none");
            document.DocumentElement.AppendChild(element);
        }

        /// <summary>
        /// Draw line
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="width"></param>
        /// <param name="color"></param>
        public void Line(float x1, float y1, float x2, float y2, float width, string color)
        {
            XmlNode element = document.CreateElement("line");
            AppendAttribute(element, "x1", x1);
            AppendAttribute(element, "y1", y1);
            AppendAttribute(element, "x2", x2);
            AppendAttribute(element, "y2", y2);
            AppendAttribute(element, "stroke", color);
            AppendAttribute(element, "stroke-width", width < 1 ? 1 : width);

            document.DocumentElement.AppendChild(element);
        }

        /// <summary>
        /// Draws a rectangle
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>
        public void Rectangle(float x1, float y1, float x2, float y2, float width, bool fill, string color)
        {
            XmlNode element = document.CreateElement("rect");
            AppendAttribute(element, "x", x1);
            AppendAttribute(element, "y", y1);
            AppendAttribute(element, "width", x2 - x1);
            AppendAttribute(element, "height", y2 - y1);
            AppendAttribute(element, "stroke", color);
            AppendAttribute(element, "stroke-width", width < 1 && !fill ? 1 : width);
            AppendAttribute(element, "fill", fill ? color : "none");

            document.DocumentElement.AppendChild(element);
        }

        /// <summary>
        /// Draws a text
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public void Text(float x, float y, float height, string text, string color)
            => Text(x, y, height, text, color, TextAnchor.Left);

        /// <summary>
        /// Draws a text
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="anchor"></param>
        public void Text(float x, float y, float height, string text, string color, TextAnchor anchor)
        {
            XmlNode element = document.CreateElement("text");
            AppendAttribute(element, "x", x);
            AppendAttribute(element, "y", y);
            AppendAttribute(element, "font-family", "Verdana");
            AppendAttribute(element, "font-size", height);
            AppendAttribute(element, "fill", color);
            AppendAttribute(element, "text-anchor", anchor switch
                {
                    TextAnchor.Left => "start",
                    TextAnchor.Center => "middle",
                    TextAnchor.Right => "end",
                    _ => "start"
                });
            element.AppendChild(document.CreateTextNode(text));
            document.DocumentElement.AppendChild(element);
        }

        /// <summary>
        /// Creates a path
        /// </summary>
        /// <returns></returns>
        public IReticleCanvasPath CreatePath()
        {
            return new SvgPath();
        }

        /// <summary>
        /// Draws a path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <param name="color"></param>
        public void Path(IReticleCanvasPath path, float width, bool fill, string color)
        {
            if (path is SvgPath svgPath)
            {
                if (fill && !svgPath.Closed)
                    throw new ArgumentException("Path cannot be filled if it is not closed", nameof(fill));

                XmlNode element = document.CreateElement("path");
                AppendAttribute(element, "d", svgPath.GetSvgPath());
                AppendAttribute(element, "fill", fill ? color : "none");
                AppendAttribute(element, "stroke", !fill ? color : "none");
                if (!fill)
                    AppendAttribute(element, "stroke-width", width);

                document.DocumentElement.AppendChild(element);
            }
            else
            {
                throw new ArgumentException($"Path is not created by {nameof(SvgCanvas)}.{nameof(SvgCanvas.CreatePath)}", nameof(path));
            }
        }
    }
}
