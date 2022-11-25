using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BallisticCalculator.Reticle;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using BallisticCalculator.Reticle.Graphics;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using Svg;

namespace BallisticCalculator.Debug
{
    public static class Debug2
    {
        private static void Draw(ReticleDefinition reticle, string baseName)
        {
            var canvas = SvgCanvasFactory.Create("reticle", "2in", "2in");
            var controller = new ReticleDrawController(reticle, canvas);
            controller.DrawReticle();
            var svg = SvgCanvasFactory.ToSvg(canvas);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svg);
            var svgDocument = SvgDocument.Open(xmlDocument);
            var bm = svgDocument.Draw(1024, 1024);
            var bm1 = new Bitmap(1024, 1024);
            Graphics g = Graphics.FromImage(bm1);
            g.FillRectangle(Brushes.White, 0, 0, 1024, 1024);
            g.DrawImage(bm, 0, 0);
            bm1.Save($"{baseName}.png", ImageFormat.Png);
            xmlDocument.Save($"{baseName}.svg");

            xmlDocument = new XmlDocument();
            BallisticXmlSerializer serializer = new BallisticXmlSerializer(xmlDocument);
            xmlDocument.AppendChild(serializer.Serialize(reticle));
            xmlDocument.Save($"{baseName}.reticle");

            xmlDocument = new XmlDocument();
            xmlDocument.Load($"{baseName}.reticle");
            BallisticXmlDeserializer deserializer = new BallisticXmlDeserializer();
            _ = deserializer.Deserialize<ReticleDefinition>(xmlDocument.DocumentElement);
        }

        private static void DrawGraphics(ReticleDefinition reticle, string baseName)
        {
            var bm1 = new Bitmap(1024, 1024);
            var canvas = GraphicsCanvas.FromImage(bm1, Color.White);
            canvas.Clear();
            var controller = new ReticleDrawController(reticle, canvas);
            controller.DrawReticle();
            bm1.Save($"{baseName}-a.png", ImageFormat.Png);
        }

        private static void Mildot()
        {
            var reticle = new MilDotReticle();
            Draw(reticle, "mildot");
            DrawGraphics(reticle, "mildot");
        }

        private static void TestShapes()
        {
            var reticle = new ReticleDefinition()
            {
                Name = "Test",
                Size = new ReticlePosition(10, 10, AngularUnit.Mil),
                Zero = new ReticlePosition(5, 5, AngularUnit.Mil)
            };

            //test path 1
            var path = new ReticlePath()
            {
                Color = "red",
                Fill = true,
            };

            path.Elements.Add(new ReticlePathElementMoveTo()
            {
                Position = new ReticlePosition(-2.5, 0, AngularUnit.Mil),
            });

            path.Elements.Add(new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(0, 1, AngularUnit.Mil),
            });

            path.Elements.Add(new ReticlePathElementLineTo()
            {
                Position = new ReticlePosition(2.5, 0, AngularUnit.Mil),
            });

            path.Elements.Add(new ReticlePathElementArc()
            {
                Position = new ReticlePosition(-2.5, 0, AngularUnit.Mil),
                ClockwiseDirection = false,
                MajorArc = true,
                Radius = AngularUnit.Mil.New(2.5)
            });

            reticle.Elements.Add(path);

            reticle.Elements.Add(new ReticleLine()
            {
                Start = new ReticlePosition(-1, 0, AngularUnit.Mil),
                End = new ReticlePosition(1, 0, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.1),
                Color = "blue",
            });

            reticle.Elements.Add(new ReticleLine()
            {
                Start = new ReticlePosition(0, -1, AngularUnit.Mil),
                End = new ReticlePosition(0, 1, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.1),
                Color = "blue",
            });

            reticle.Elements.Add(new ReticleText()
            {
                Position = new ReticlePosition(0, 0, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(0.25),
                Color = "black",
                Text = "Left Text",
            });

            reticle.Elements.Add(new ReticleText()
            {
                Position = new ReticlePosition(0, 0, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(0.25),
                Color = "black",
                Text = "Right Text",
                Anchor = TextAnchor.Right,
            });

            reticle.Elements.Add(new ReticleText()
            {
                Position = new ReticlePosition(0, -0.5, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(0.25),
                Color = "black",
                Text = "Center Text",
                Anchor = TextAnchor.Center,
            });

            Draw(reticle, "test");
            DrawGraphics(reticle, "test");
        }

        public static void Do(string[] _)
        {
            Mildot();
            TestShapes();
        }
    }
}
