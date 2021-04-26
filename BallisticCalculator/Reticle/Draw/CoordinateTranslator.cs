using System;
using System.Collections.Generic;
using System.Text;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle.Draw
{
    internal class CoordinateTranslator
    {
        private readonly float mScaleX, mScaleY, mZeroX, mZeroY, mSourceHeight;

        /// <summary>
        /// Constructor (real numbers)
        /// </summary>
        /// <param name="sourceWidth">The width of the source image</param>
        /// <param name="sourceHeight">The height of the source image</param>
        /// <param name="sourceZeroX">The x offset of the zero from top-left corer on the source image</param>
        /// <param name="sourceZeroY">The y offset of the zero from top-left corer on the source image</param>
        /// <param name="destinationWidth">The width of the destination image</param>
        /// <param name="destinationHeight">The height of the destination image</param>

        public CoordinateTranslator(float sourceWidth, float sourceHeight, float sourceZeroX, float sourceZeroY,
                                    float destinationWidth, float destinationHeight)
        {
            mSourceHeight = sourceHeight;
            mScaleX = destinationWidth / sourceWidth;
            mScaleY = destinationHeight / sourceHeight;
            mZeroX = sourceZeroX;
            mZeroY = sourceZeroY;
        }

        /// <summary>
        /// Constructor (angular units)
        /// </summary>
        /// <param name="sourceWidth">The width of the source image</param>
        /// <param name="sourceHeight">The height of the source image</param>
        /// <param name="sourceZeroX">The x offset of the zero from top-left corer on the source image</param>
        /// <param name="sourceZeroY">The y offset of the zero from top-left corer on the source image</param>
        /// <param name="destinationWidth">The width of the destination image</param>
        /// <param name="destinationHeight">The height of the destination image</param>
        public CoordinateTranslator(Measurement<AngularUnit> sourceWidth, Measurement<AngularUnit> sourceHeight, Measurement<AngularUnit> sourceZeroX, Measurement<AngularUnit> sourceZeroY,
                                    float destinationWidth, float destinationHeight)
            : this((float)sourceWidth.In(AngularUnit.Mil), (float)sourceHeight.In(AngularUnit.Mil),
                   (float)sourceZeroX.In(AngularUnit.Mil), (float)sourceZeroY.In(AngularUnit.Mil),
                   destinationWidth, destinationHeight)
        {

        }

        /// <summary>
        /// Transform coordinates from the source system to destination system
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Transform(float sx, float sy, out float x, out float y)
        {
            x = (sx + mZeroX) * mScaleX;
            y = (mSourceHeight - (sy + mZeroY)) * mScaleY;
        }

        /// <summary>
        /// Transform coordinates from the source system to destination system (angular units)
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Transform(Measurement<AngularUnit> sx, Measurement<AngularUnit> sy, out float x, out float y)
            => Transform((float)sx.In(AngularUnit.Mil), (float)sy.In(AngularUnit.Mil), out x, out y);

        /// <summary>
        /// Transforms just X coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float TransformX(float x)
        {
            Transform(x, 0, out float sx, out _);
            return sx;
        }

        /// <summary>
        /// Transform linear measurement
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float TransformL(float x)
        {
            return x * mScaleX;
        }

        /// <summary>
        /// Transforms just X coordinate (angular units)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>

        public float TransformX(Measurement<AngularUnit>? x) => x == null ? 1 : TransformX((float)x.Value.In(AngularUnit.Mil));

        /// <summary>
        /// Transform linear value
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public float TransformL(Measurement<AngularUnit>? x) => x == null ? 1 : TransformL((float)x.Value.In(AngularUnit.Mil));

        /// <summary>
        /// Transforms just Y coordinate
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public float TransformY(float y)
        {
            Transform(0, y, out _, out float sy);
            return sy;
        }

        /// <summary>
        /// Transforms just Y coordinate (angular units)
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>

        public float TransformY(Measurement<AngularUnit>? y) => y == null ? 1 : TransformY((float)y.Value.In(AngularUnit.Mil));

    }
}
