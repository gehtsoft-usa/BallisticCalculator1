using System;
using System.Collections.Generic;
using System.Linq;
using Gehtsoft.Measurements;

namespace BallisticCalculator
{
    /// <summary>
    /// <para>Synthesizes a custom drag table from a standard base drag curve and an effective ballistic-coefficient-vs-Mach profile.</para>
    /// <para>The synthesized curve is the projectile's own drag coefficient, [c]Cd(M) = Cd_base(M) / BC(M) * SD[/c],
    /// where [c]BC(M)[/c] is interpolated from the supplied knots and [c]SD[/c] is the sectional density of the
    /// bullet. That is the same quantity a drg file stores, so the result is interchangeable with a table loaded by
    /// [clink=BallisticCalculator.DrgDragTable]DrgDragTable[/clink] or built by
    /// [clink=BallisticCalculator.Tools.RadarDragTableFactory]RadarDragTableFactory[/clink]: it survives a
    /// [c]Save[/c] and [c]Open[/c] round trip unchanged, and it is run with the form factor of one that the
    /// factory writes into the ammunition it hands back.</para>
    /// </summary>
    public static class DrgDragTableFactory
    {
        /// <summary>
        /// Builds a custom drag table from a base curve and a Mach-to-BC profile.
        /// </summary>
        /// <param name="ammunition">The ammunition metadata to attach to the resulting table. The bullet weight and
        /// diameter are required, because they set the scale of the drag curve. The ballistic coefficient is not an
        /// input: the factory replaces it with a form factor of one on the custom table, which is the value the
        /// synthesized curve has to be run with.</param>
        /// <param name="baseTable">The standard drag curve to scale, for example G1 or G7. It must not be GC.</param>
        /// <param name="bcCurve">The Mach-to-effective-BC knots. Order does not matter and at least one knot is required. BC is interpolated linearly between knots and held flat beyond the end knots.</param>
        /// <returns>A custom drag table on the base curve's Mach grid.</returns>
        /// <exception cref="ArgumentNullException">The ammunition or the BC curve is null.</exception>
        /// <exception cref="ArgumentException">The base table is GC, there are no knots, a knot has a non-positive BC, or the ammunition has no positive weight and diameter.</exception>
        public static DrgDragTable Build(AmmunitionLibraryEntry ammunition, DragTableId baseTable, IEnumerable<BcAtMach> bcCurve)
        {
            ArgumentNullException.ThrowIfNull(ammunition);
            ArgumentNullException.ThrowIfNull(bcCurve);
            if (baseTable == DragTableId.GC)
                throw new ArgumentException("The base table must be a standard drag curve, not GC", nameof(baseTable));

            var knots = bcCurve.OrderBy(k => k.Mach).ToArray();
            if (knots.Length < 1)
                throw new ArgumentException("At least one BC knot is required", nameof(bcCurve));
            foreach (var k in knots)
                if (k.Bc <= 0)
                    throw new ArgumentException("The ballistic coefficient must be positive", nameof(bcCurve));

            //the sectional density puts the curve on the physical scale that a drg file uses, so the weight
            //and the diameter are as much an input as the BC profile itself
            if (ammunition.Ammunition == null)
                throw new ArgumentException("The ammunition of the library entry is required", nameof(ammunition));
            double weightGrains = ammunition.Ammunition.Weight.In(WeightUnit.Grain);
            double diameterInch = ammunition.Ammunition.BulletDiameter?.In(DistanceUnit.Inch) ?? 0.0;
            if (weightGrains <= 0)
                throw new ArgumentException("The bullet weight must be positive", nameof(ammunition));
            if (diameterInch <= 0)
                throw new ArgumentException("The bullet diameter is required to scale the drag curve", nameof(ammunition));
            double sectionalDensity = weightGrains / 7000.0 / (diameterInch * diameterInch);

            var baseCurve = DragTable.Get(baseTable);
            var points = new DragTableDataPoint[baseCurve.Count];
            for (int i = 0; i < baseCurve.Count; i++)
            {
                double mach = baseCurve[i].Mach;
                double bc = InterpolateBc(knots, mach);
                points[i] = new DragTableDataPoint(mach, baseCurve[i].DragCoefficient / bc * sectionalDensity);
            }

            //the table now holds the projectile's own drag coefficient, so it pairs with a form factor of one,
            //exactly as a loaded drg file does. Stamping it here is what keeps the pair impossible to mismatch.
            ammunition.Ammunition.BallisticCoefficient =
                new BallisticCoefficient(1, DragTableId.GC, BallisticCoefficientValueType.FormFactor);
            if (string.IsNullOrWhiteSpace(ammunition.Source))
                ammunition.Source = "bc curve";

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
