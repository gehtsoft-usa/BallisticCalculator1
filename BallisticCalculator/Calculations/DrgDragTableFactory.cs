using System;
using System.Collections.Generic;
using System.Linq;

namespace BallisticCalculator
{
    /// <summary>
    /// <para>Synthesizes a custom (<see cref="DragTableId.GC"/>) drag table from a standard base
    /// drag curve and an effective ballistic-coefficient-vs-Mach profile.</para>
    /// <para>The synthesized curve is <c>Cd_custom(M) = Cd_base(M) / BC(M)</c>, where <c>BC(M)</c>
    /// is interpolated from the supplied knots. The resulting table must be run with an
    /// ammunition whose ballistic coefficient is <c>new BallisticCoefficient(1.0, DragTableId.GC)</c>
    /// so the engine applies exactly the intended per-Mach drag.</para>
    /// </summary>
    public static class DrgDragTableFactory
    {
        /// <summary>
        /// Builds a custom drag table from a base curve and a Mach-&gt;BC profile.
        /// </summary>
        /// <param name="ammunition">Ammunition metadata (name, weight, diameter) attached to the
        /// resulting table; supplied by the caller.</param>
        /// <param name="baseTable">The standard drag curve to scale (e.g. G1 or G7). Must not be GC.</param>
        /// <param name="bcCurve">The Mach-&gt;effective-BC knots. Order does not matter; at least one
        /// knot is required. BC is interpolated piecewise-linearly between knots and held flat
        /// beyond the end knots.</param>
        /// <returns>A custom <see cref="DrgDragTable"/> on the base curve's Mach grid.</returns>
        public static DrgDragTable Build(AmmunitionLibraryEntry ammunition, DragTableId baseTable, IEnumerable<BcAtMach> bcCurve)
        {
            if (ammunition == null)
                throw new ArgumentNullException(nameof(ammunition));
            if (bcCurve == null)
                throw new ArgumentNullException(nameof(bcCurve));
            if (baseTable == DragTableId.GC)
                throw new ArgumentException("The base table must be a standard drag curve, not GC", nameof(baseTable));

            var knots = bcCurve.OrderBy(k => k.Mach).ToArray();
            if (knots.Length < 1)
                throw new ArgumentException("At least one BC knot is required", nameof(bcCurve));
            foreach (var k in knots)
                if (k.Bc <= 0)
                    throw new ArgumentException("The ballistic coefficient must be positive", nameof(bcCurve));

            var baseCurve = DragTable.Get(baseTable);
            var points = new DragTableDataPoint[baseCurve.Count];
            for (int i = 0; i < baseCurve.Count; i++)
            {
                double mach = baseCurve[i].Mach;
                double bc = InterpolateBc(knots, mach);
                points[i] = new DragTableDataPoint(mach, baseCurve[i].DragCoefficient / bc);
            }

            return new DrgDragTable(points, ammunition);
        }

        // Piecewise-linear BC(M); flat extrapolation beyond the end knots.
        private static double InterpolateBc(BcAtMach[] knots, double mach)
        {
            if (mach <= knots[0].Mach)
                return knots[0].Bc;
            if (mach >= knots[knots.Length - 1].Mach)
                return knots[knots.Length - 1].Bc;

            for (int i = 1; i < knots.Length; i++)
            {
                if (mach <= knots[i].Mach)
                {
                    BcAtMach a = knots[i - 1];
                    BcAtMach b = knots[i];
                    double t = (mach - a.Mach) / (b.Mach - a.Mach);
                    return a.Bc + t * (b.Bc - a.Bc);
                }
            }
            return knots[knots.Length - 1].Bc;
        }
    }
}
