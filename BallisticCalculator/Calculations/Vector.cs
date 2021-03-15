using Gehtsoft.Measurements;
using System;
using System.Runtime.CompilerServices;

namespace BallisticCalculator
{
    /// <summary>
    /// 3D vector structure with appropriate math operations implemented
    /// </summary>
    internal struct Vector<T>
        where T : Enum
    {
        /// <summary>
        /// X coordinate
        /// </summary>
        public Measurement<T> X { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        /// <summary>
        /// Y coordinate
        /// </summary>
        public Measurement<T> Y { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        /// <summary>
        /// Z coordinate
        /// </summary>
        public Measurement<T> Z { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        /// Parameterize constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Measurement<T> x, Measurement<T> y, Measurement<T> z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Copying constructor
        /// </summary>
        /// <param name="other"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Vector<T> other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        /// <summary>
        /// Returns a magnitude (length) of the vector
        /// </summary>
        public Measurement<T> Magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                T units = X.Unit;
                return new Measurement<T>(Math.Sqrt(X.Value * X.Value + Y.In(units) * Y.In(units) + Z.In(units) * Z.In(units)), units);
            }
        }

        /// <summary>
        /// Adds two vectors
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator +(Vector<T> a, Vector<T> b) => new Vector<T>(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>
        /// Multiples a vector to a constant
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(double a, Vector<T> b) => new Vector<T>(a * b.X, a * b.Y, a * b.Z);

        /// <summary>
        /// Multiples a vector to a constant
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(Vector<T> a, double b) => new Vector<T>(b * a.X, b * a.Y, b * a.Z);

        /// <summary>
        /// Subtracts on vector from another
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> a, Vector<T> b) => new Vector<T>(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        /// Inverts the vector
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> a) => new Vector<T>(-a.X, -a.Y, -a.Z);

        /// <summary>
        /// Finds distance between ends of two vectors.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Measurement<T> Distance(Vector<T> a, Vector<T> b) => (a - b).Magnitude;

        /// <summary>
        /// Normalizes vector
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector<T> Normalize()
        {
            var m = Magnitude;
            if (m.Value < 1e-7)
                return this;

            return (1.0 / Magnitude.Value) * this;
        }
    }
}
