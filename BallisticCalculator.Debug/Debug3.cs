using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Reticle.Draw;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using Svg;

namespace BallisticCalculator.Debug
{
    public static class Debug3
    {
        private static void Draw(ReticleDefinition reticle, string baseName, TrajectoryPoint[] trajectory)
        {
            const double svgWidth = 10;
            double svgHeight = Math.Round(reticle.Size.Y / reticle.Size.X * svgWidth, 2);

            var canvas = SvgCanvasFactory.Create("reticle", $"{svgWidth}in", $"{svgHeight}in");
            var controller = new ReticleDrawController(reticle, canvas);
            controller.DrawReticle();
            controller.DrawBulletDropCompensator(trajectory, DistanceUnit.Yard.New(100), false, DistanceUnit.Yard, "black");
            var svg = SvgCanvasFactory.ToSvg(canvas);

            const int pngWidth = 1024;
            int pngHeight = (int)((reticle.Size.Y / reticle.Size.X) * pngWidth);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svg);
            var svgDocument = SvgDocument.Open(xmlDocument);
            var bm = svgDocument.Draw(pngWidth, pngHeight);
            var bm1 = new Bitmap(pngWidth, pngHeight);
            Graphics g = Graphics.FromImage(bm1);
            g.FillRectangle(Brushes.White, 0, 0, pngWidth, pngHeight);
            g.DrawImage(bm, 0, 0);
            bm1.Save($"{baseName}.png", ImageFormat.Png);
            xmlDocument.Save($"{baseName}.svg");

            xmlDocument = new XmlDocument();
            BallisticXmlSerializer serializer = new BallisticXmlSerializer(xmlDocument);
            xmlDocument.AppendChild(serializer.Serialize(reticle));
            xmlDocument.Save($"{baseName}.reticle");
        }

        public static void Do(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage: reticlename (for M855)");
                Console.WriteLine("       reticlename 55 (for M193)");
                return;
            }

            XmlDocument document = new XmlDocument();
            document.Load($@"..\..\..\..\data\reticle\{args[0]}.reticle");
            BallisticXmlDeserializer ds = new BallisticXmlDeserializer();
            var reticle = ds.Deserialize<ReticleDefinition>(document.DocumentElement);

            TrajectoryPoint[] trajectoryPoints;

            if (args.Length > 1 && args[1] == "55")
                trajectoryPoints = Debug1.M193(false, DistanceUnit.Yard.New(10));
            else
                trajectoryPoints = Debug1.M855(false, DistanceUnit.Yard.New(10));

            Draw(reticle, args[0], trajectoryPoints);
        }
    }
}
