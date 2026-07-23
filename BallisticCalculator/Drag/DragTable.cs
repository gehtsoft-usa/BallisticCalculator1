using System;

namespace BallisticCalculator
{
    /// <summary>
    /// A drag table
    /// </summary>
    public abstract class DragTable
    {
        // Standard drag tables are immutable once built (the node graph is constructed in the ctor
        // and never mutated), so they are cached as process-wide singletons. Lazy<T> gives
        // thread-safe one-time initialization with lock-free reads afterwards, so Get is safe to
        // call concurrently (see the thread-safety note on TrajectoryCalculator).
        private static readonly Lazy<DragTable> gG1 = new Lazy<DragTable>(() => new G1DragTable());
        private static readonly Lazy<DragTable> gG2 = new Lazy<DragTable>(() => new G2DragTable());
        private static readonly Lazy<DragTable> gG5 = new Lazy<DragTable>(() => new G5DragTable());
        private static readonly Lazy<DragTable> gG6 = new Lazy<DragTable>(() => new G6DragTable());
        private static readonly Lazy<DragTable> gG7 = new Lazy<DragTable>(() => new G7DragTable());
        private static readonly Lazy<DragTable> gG8 = new Lazy<DragTable>(() => new G8DragTable());
        private static readonly Lazy<DragTable> gGI = new Lazy<DragTable>(() => new GIDragTable());
        private static readonly Lazy<DragTable> gGS = new Lazy<DragTable>(() => new GSDragTable());
        private static readonly Lazy<DragTable> gRA4 = new Lazy<DragTable>(() => new RA4DragTable());

        /// <summary>
        /// Returns the drag table by its identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DragTable Get(DragTableId id)
        {
            return id switch
            {
                DragTableId.G1 => gG1.Value,
                DragTableId.G2 => gG2.Value,
                DragTableId.G5 => gG5.Value,
                DragTableId.G6 => gG6.Value,
                DragTableId.G7 => gG7.Value,
                DragTableId.G8 => gG8.Value,
                DragTableId.GI => gGI.Value,
                DragTableId.GS => gGS.Value,
                DragTableId.RA4 => gRA4.Value,
                DragTableId.GC => throw new ArgumentException("Pass custom drag table directly to the target method", nameof(id)),
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
