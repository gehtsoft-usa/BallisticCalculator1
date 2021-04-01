using BallisticCalculator.Reticle.Draw;
using FluentAssertions;
using Xunit;

namespace BallisticCalculator.Test
{
    public class DrawCoordinateTransformerTest
    {
        [Fact]
        public void Test1()
        {
            // d(0,0)          s(0)
            //   +------------------------------+ sy(2.5)
            //   |              |               |
            //   |              |               |
            //   |              |               |
            //   |--------------+---------------| sy(0) dy(5)
            //   |              |               |
            //   |              |               |
            //   |              |               |
            //   +------------------------------+ sy(-2.5), sx(2.5)
            //                                  d(10,10)



            CoordinateTranslator translator = new CoordinateTranslator(5f, 5f, 2.5f, 2.5f, 10f, 10f);

            float x, y;
            translator.Transform(0, 0, out x, out y);
            x.Should().BeApproximately(5f, 1e-5f);
            y.Should().BeApproximately(5f, 1e-5f);

            translator.Transform(-2.5f, 2.5f, out x, out y);
            x.Should().BeApproximately(0f, 1e-5f);
            y.Should().BeApproximately(0f, 1e-5f);

            translator.Transform(2.5f, -2.5f, out x, out y);
            x.Should().BeApproximately(10f, 1e-5f);
            y.Should().BeApproximately(10f, 1e-5f);

            translator.Transform(0f, 2.5f, out x, out y);
            x.Should().BeApproximately(5f, 1e-5f);
            y.Should().BeApproximately(0f, 1e-5f);

            translator.Transform(0f, -2.5f, out x, out y);
            x.Should().BeApproximately(5f, 1e-5f);
            y.Should().BeApproximately(10f, 1e-5f);

            translator.Transform(1f, 1f, out x, out y);
            x.Should().BeApproximately(7f, 1e-5f);
            y.Should().BeApproximately(3f, 1e-5f);
        }

    }
}

