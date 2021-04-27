using System.Linq;
using System.Xml.Linq;
using BallisticCalculator.Reticle.Draw;
using FluentAssertions;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    public class SvgControllerTest
    {
        private const string SVG_NS = "http://www.w3.org/2000/svg";

        private static XName Name(string name) => XName.Get(name, SVG_NS);

        [Fact]
        public void Prefix()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Name.Should().Be(Name("svg"));
            document.Root.GetDefaultNamespace().NamespaceName.Should().Be("http://www.w3.org/2000/svg");
            document.Root.Should().HaveElement(Name("title"))
                .Which
                .Should().HaveValue("test");
        }

        [Fact]
        public void Line()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            canvas.Line(1, 2, 3, 4, 5, "black");
            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Should().HaveElement(Name("line"))
                .Which
                .Should()
                .BeOfType<XElement>()
                .And.HaveAttribute("x1", "1")
                .And.HaveAttribute("y1", "2")
                .And.HaveAttribute("x2", "3")
                .And.HaveAttribute("y2", "4")
                .And.HaveAttribute("stroke", "black")
                .And.HaveAttribute("stroke-width", "5")
                .And.Match(xe => xe.Parent.Name == Name("svg"));
        }

        [Fact]
        public void Text()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            canvas.Text(1, 2, 3, "text", "blue");
            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Should().HaveElement(Name("text"))
                .Which
                .Should()
                .BeOfType<XElement>()
                .And.HaveAttribute("x", "1")
                .And.HaveAttribute("y", "2")
                .And.HaveAttribute("font-size", "3")
                .And.HaveAttribute("fill", "blue")
                .And.HaveAttribute("font-family", "Verdana")
                .And.HaveValue("text")
                .And.Match(xe => xe.Parent.Name == Name("svg"));
        }

        [Fact]
        public void Circle_Filled()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            canvas.Circle(1, 2, 3, 4, true, "red");
            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Should().HaveElement(Name("circle"))
                .Which
                .Should()
                .BeOfType<XElement>()
                .And.HaveAttribute("cx", "1")
                .And.HaveAttribute("cy", "2")
                .And.HaveAttribute("r", "3")
                .And.HaveAttribute("stroke", "red")
                .And.HaveAttribute("fill", "red")
                .And.HaveAttribute("stroke-width", "4")
                .And.Match(xe => xe.Parent.Name == Name("svg"));
        }

        [Fact]
        public void Circle_NotFilled()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            canvas.Circle(1, 2, 3, 4, false, "red");
            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Should().HaveElement(Name("circle"))
                .Which
                .Should()
                .BeOfType<XElement>()
                .And.HaveAttribute("cx", "1")
                .And.HaveAttribute("cy", "2")
                .And.HaveAttribute("r", "3")
                .And.HaveAttribute("stroke", "red")
                .And.HaveAttribute("stroke-width", "4")
                .And.HaveAttribute("fill", "none")
                .And.Match(xe => xe.Parent.Name == Name("svg"));
        }

        [Fact]
        public void Path()
        {
            SvgCanvas canvas = new SvgCanvas("test", "2in", "2in");
            using (var path = canvas.CreatePath())
            {
                path.MoveTo(2500, 5000);
                path.LineTo(5000, 4000);
                path.LineTo(7500, 5000);
                path.Arc(2500, 2500, 5000, true, false);
                path.Close();
                canvas.Path(path, 0, true, "red");
            }

            var s = canvas.ToSvg();
            var document = XDocument.Parse(s);
            document.Root.Should().HaveElement(Name("path"))
                .Which
                .Should()
                .BeOfType<XElement>()
                .And.HaveAttribute("d", "M2500,5000 L5000,4000 L7500,5000A2500,2500 0 1,0 2500,5000 z")
                .And.HaveAttribute("stroke", "none")
                .And.HaveAttribute("fill", "red")
                .And.Match(xe => xe.Parent.Name == Name("svg"));
        }
    }
}
