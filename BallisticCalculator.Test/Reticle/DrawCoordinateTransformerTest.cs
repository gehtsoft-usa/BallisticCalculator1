using BallisticCalculator.Reticle.Draw;
using FluentAssertions;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    public class DrawCoordinateTransformerTest
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
        [Theory]
        [InlineData(0, 0, 5, 5)]
        [InlineData(-2.5, 2.5, 0, 0)]
        [InlineData(2.5, -2.5, 10, 10)]
        [InlineData(0, -2.5, 5, 10)]
        [InlineData(1, 1, 7, 3)]
        public void XYTranslation(float sx, float sy, float dx, float dy)
        {
            CoordinateTranslator translator = new CoordinateTranslator(5f, 5f, 2.5f, 2.5f, 10f, 10f);

            translator.Transform(sx, sy, out float x, out float y);
            x.Should().BeApproximately(dx, 1e-5f);
            y.Should().BeApproximately(dy, 1e-5f);
        }

        // d(0,0)          s(0)
        //   +------------------------------+ sy(1.5)
        //   |              |               |
        //   |--------------+---------------| sy(0) dy(3)
        //   |              |               |
        //   |              |               |
        //   |              |               |
        //   |              |               |
        //   |              |               |
        //   +------------------------------+ sy(-3.5), sx(2.5)
        //                                    d(10,10)
        [Theory]
        [InlineData(-2.5, 1.5, 0, 0)]
        [InlineData(2.5, -3.5, 10, 10)]
        public void XYTranslation_OffCenter(float sx, float sy, float dx, float dy)
        {
            CoordinateTranslator translator = new CoordinateTranslator(5f, 5f, 2.5f, 1.5f, 10f, 10f);

            translator.Transform(sx, sy, out float x, out float y);
            x.Should().BeApproximately(dx, 1e-5f);
            y.Should().BeApproximately(dy, 1e-5f);
        }
    }
}

