using System;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// <para>Converts a ballistic coefficient from one standard drag table to another (for example G1 to G7).</para>
    /// <para>The conversion is table-driven and velocity-aware: the same projectile experiences the same
    /// physical drag regardless of the reference used, so at a given Mach number
    /// [c]BC_target = BC_source * Cd_target(M) / Cd_source(M)[/c], where the Cd values come from the two
    /// standard drag curves. Because the G1 and G7 curves have different shapes across Mach, a single
    /// converted number is only exact at the reference velocity it was computed for; pass the velocity
    /// (or Mach) representative of the intended range band.</para>
    /// <para>Accuracy against manufacturer-published G1/G7 pairs is within about 1 percent at
    /// supersonic velocities (Mach 1.8 to 2.5, where the G1/G7 drag ratio is near 2.0), and degrades
    /// toward the transonic region (near Mach 1.3, roughly 9 percent low) where the two reference
    /// curves diverge in shape. Use a supersonic reference velocity for the best result.</para>
    /// </summary>
    public static class BallisticCoefficientConverter
    {
        /// <summary>
        /// Converts a ballistic coefficient to another drag table at the specified reference Mach number.
        /// </summary>
        /// <param name="source">The source ballistic coefficient. Must be a coefficient (not a form factor) and not a custom (GC) table.</param>
        /// <param name="targetTable">The drag table to convert to. Must not be GC.</param>
        /// <param name="referenceMach">The Mach number at which the two curves are matched. Must be greater than 0.</param>
        /// <returns>The equivalent ballistic coefficient expressed against the target table at that Mach.</returns>
        public static BallisticCoefficient Convert(BallisticCoefficient source, DragTableId targetTable, double referenceMach)
        {
            if (source.ValueType != BallisticCoefficientValueType.Coefficient)
                throw new ArgumentException("Only a coefficient (not a form factor) can be converted between tables", nameof(source));
            if (source.Table == DragTableId.GC || targetTable == DragTableId.GC)
                throw new ArgumentException("Conversion requires standard drag tables on both sides; the custom GC table has no fixed curve", nameof(targetTable));
            if (referenceMach <= 0)
                throw new ArgumentOutOfRangeException(nameof(referenceMach), "The reference Mach number must be greater than zero");

            if (source.Table == targetTable)
                return source;

            double cdSource = DragTable.Get(source.Table).Find(referenceMach).CalculateDrag(referenceMach);
            double cdTarget = DragTable.Get(targetTable).Find(referenceMach).CalculateDrag(referenceMach);

            double value = source.Value * cdTarget / cdSource;
            return new BallisticCoefficient(value, targetTable);
        }

        /// <summary>
        /// Converts a ballistic coefficient to another drag table at the specified reference velocity.
        /// </summary>
        /// <param name="source">The source ballistic coefficient. Must be a coefficient (not a form factor) and not a custom (GC) table.</param>
        /// <param name="targetTable">The drag table to convert to. Must not be GC.</param>
        /// <param name="referenceVelocity">The velocity representative of the intended range band. Must be greater than 0.</param>
        /// <param name="atmosphere">The atmosphere whose speed of sound sets the Mach number. When null, the standard atmosphere is used.</param>
        /// <returns>The equivalent ballistic coefficient expressed against the target table at that velocity.</returns>
        public static BallisticCoefficient Convert(BallisticCoefficient source, DragTableId targetTable,
            Measurement<VelocityUnit> referenceVelocity, Atmosphere atmosphere = null)
        {
            if (referenceVelocity.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(referenceVelocity), "The reference velocity must be greater than zero");

            atmosphere ??= new Atmosphere();
            double mach = referenceVelocity.In(VelocityUnit.MetersPerSecond)
                        / atmosphere.SoundVelocity.In(VelocityUnit.MetersPerSecond);
            return Convert(source, targetTable, mach);
        }
    }
}
