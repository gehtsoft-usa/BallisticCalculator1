using System;

namespace BallisticCalculator
{
    /// <summary>
    /// A drag table
    /// </summary>
    public abstract class DragTable
    {
        private static DragTable gG1;
        private static DragTable gG2;
        private static DragTable gG5;
        private static DragTable gG6;
        private static DragTable gG7;
        private static DragTable gG8;
        private static DragTable gGI;
        private static DragTable gGS;
        private static DragTable gRA4;

        /// <summary>
        /// Returns the drag table by its identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DragTable Get(DragTableId id)
        {
            return id switch
            {
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions
                DragTableId.G1 => gG1 ??= new G1DragTable(),
                DragTableId.G2 => gG2 ??= new G2DragTable(),
                DragTableId.G5 => gG5 ??= new G5DragTable(),
                DragTableId.G6 => gG6 ??= new G6DragTable(),
                DragTableId.G7 => gG7 ??= new G7DragTable(),
                DragTableId.G8 => gG8 ??= new G8DragTable(),
                DragTableId.GI => gGI ??= new GIDragTable(),
                DragTableId.GS => gGS ??= new GSDragTable(),
                DragTableId.RA4 => gRA4 ??= new RA4DragTable(),
                DragTableId.GC => throw new ArgumentException("Pass custom drag table directly to the target method", nameof(id)),
#pragma warning restore S1121 // Assignments should not be made from within sub-expressions
                _ => throw new ArgumentOutOfRangeException(nameof(id)),
            };
        }

        /// <summary>
        /// The identifier of a drag table
        /// </summary>
        public abstract DragTableId TableId { get; }

        /// <summary>
        /// Returns the number of drag table nodes
        /// </summary>
        public int Count => mNodes.Length;

        /// <summary>
        /// Returns the drag table node by its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public DragTableNode this[int index] => mNodes[index];

        private readonly DragTableNode[] mNodes;

        /// <summary>
        /// <para>Parameterized constructor</para>
        /// <para>This class based on original JavaScript solution by Alexandre Trofimov</para>
        /// </summary>
        /// <param name="points">The data points. The points must be pre-sorted in ascending order by Mach field</param>
        protected DragTable(DragTableDataPoint[] points)
        {
            int numpts = points.Length;
            mNodes = new DragTableNode[numpts];
            double rate = (points[1].DragCoefficient - points[0].DragCoefficient) / (points[1].Mach - points[0].Mach);
            mNodes[0] = new DragTableNode(points[0].Mach, points[0].DragCoefficient, 0, rate, points[0].DragCoefficient - points[0].Mach * rate, null);

            // rest as 2nd degree polynomials on three adjacent points
            for (int i = 1; i < numpts - 1; i++)
            {
                double x1 = points[i - 1].Mach;
                double x2 = points[i].Mach;
                double x3 = points[i + 1].Mach;

                double y1 = points[i - 1].DragCoefficient;
                double y2 = points[i].DragCoefficient;
                double y3 = points[i + 1].DragCoefficient;

                double a = ((y3 - y1) * (x2 - x1) - (y2 - y1) * (x3 - x1)) / ((x3 * x3 - x1 * x1) * (x2 - x1) - (x2 * x2 - x1 * x1) * (x3 - x1));
                double b = (y2 - y1 - a * (x2 * x2 - x1 * x1)) / (x2 - x1);
                double c = y1 - (a * x1 * x1 + b * x1);

                mNodes[i] = new DragTableNode(points[i].Mach, points[i].DragCoefficient, a, b, c, mNodes[i - 1]);
            }
            mNodes[numpts - 1] = new DragTableNode(points[numpts - 1].Mach, points[numpts - 1].DragCoefficient, 0, 0, points[numpts - 1].DragCoefficient, mNodes[numpts - 2]);
        }

        /// <summary>
        /// Finds the drag table node by velocity
        /// </summary>
        /// <param name="mach"></param>
        /// <returns></returns>
        internal DragTableNode Find(double mach)
        {
            int numpts = mNodes.Length;

            int mlo = 0;
            int mhi = numpts - 1;
            int mid;

            while ((mhi - mlo) > 1)
            {
                mid = (int)Math.Floor((mhi + mlo) / 2.0);
                if (mNodes[mid].Mach < mach)
                    mlo = mid;
                else
                    mhi = mid;
            }

            int m;
            if ((mNodes[mhi].Mach - mach) > (mach - mNodes[mlo].Mach))
                m = mlo;
            else
                m = mhi;

            return mNodes[m];
        }
    }
}
