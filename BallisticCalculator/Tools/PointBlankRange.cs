using System;
using System.Linq;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// The aiming point within the vital zone, which fixes where the point-blank corridor sits relative to the line of sight.
    /// </summary>
    public enum PointBlankAim
    {
        /// <summary>The line of sight passes through the center of the vital zone (typical for optics).</summary>
        Center,
        /// <summary>The line of sight rests at the bottom of the vital zone (typical for iron sights).</summary>
        Bottom,
    }

    /// <summary>
    /// The point-blank range (danger space) analysis of a trajectory produced by [clink=BallisticCalculator.Tools.PointBlankRange]PointBlankRange[/clink].
    /// </summary>
    public sealed class PointBlankRangeResult
    {
        /// <summary>
        /// The near edge of the corridor: the first range at which the path is within half the vital zone of the line of sight.
        /// </summary>
        public Measurement<DistanceUnit> MinimumRange { get; }

        /// <summary>
        /// The far edge of the corridor (the maximum point-blank range): the last range still within the vital zone.
        /// </summary>
        public Measurement<DistanceUnit> MaximumRange { get; }

        /// <summary>
        /// The length of the point-blank corridor (the danger space for a dead-center hold).
        /// </summary>
        public Measurement<DistanceUnit> DangerSpace { get; }

        /// <summary>
        /// The highest point of the path above the line of sight (the maximum ordinate).
        /// </summary>
        public Measurement<DistanceUnit> MaximumOrdinate { get; }

        /// <summary>
        /// The range at which the maximum ordinate occurs.
        /// </summary>
        public Measurement<DistanceUnit> MaximumOrdinateRange { get; }

        /// <summary>
        /// The near range at which the ascending path crosses the line of sight, or null when it never does.
        /// </summary>
        public Measurement<DistanceUnit>? NearZero { get; }

        /// <summary>
        /// The far range at which the descending path crosses the line of sight, or null when it never does.
        /// </summary>
        public Measurement<DistanceUnit>? FarZero { get; }

        internal PointBlankRangeResult(Measurement<DistanceUnit> minimumRange, Measurement<DistanceUnit> maximumRange,
            Measurement<DistanceUnit> maximumOrdinate, Measurement<DistanceUnit> maximumOrdinateRange,
            Measurement<DistanceUnit>? nearZero, Measurement<DistanceUnit>? farZero)
        {
            MinimumRange = minimumRange;
            MaximumRange = maximumRange;
            DangerSpace = maximumRange - minimumRange;
            MaximumOrdinate = maximumOrdinate;
            MaximumOrdinateRange = maximumOrdinateRange;
            NearZero = nearZero;
            FarZero = farZero;
        }
    }

    /// <summary>
    /// <para>Finds the maximum point-blank range and danger space of a trajectory for a given vital zone.</para>
    /// <para>The corridor is the contiguous range span over which the path stays inside the vital zone, so a
    /// fixed hold still hits. Its position relative to the line of sight depends on the aim: centered on the
    /// sight line for optics, or resting at the bottom of the zone for iron sights. This is pure post-processing
    /// of the path (its Drop relative to the line of sight) at the trajectory's own resolution: compute the
    /// trajectory with the intended zero and the step that suits the needed accuracy. The far edge is the
    /// maximum point-blank range.</para>
    /// </summary>
    public static class PointBlankRange
    {
        /// <summary>
        /// <para>Analyzes a trajectory for its point-blank corridor against the given vital zone size and aim.</para>
        /// <para>The vital zone size is the full target height. With a center aim the corridor is half the zone
        /// above and below the line of sight; with a bottom aim it is the whole zone above the line of sight.
        /// The trajectory must extend past the corridor so the far edge can be found, and all ranges are reported
        /// at the resolution of the trajectory's output points.</para>
        /// </summary>
        /// <param name="trajectory">The computed trajectory (as returned by the calculator).</param>
        /// <param name="vitalZoneSize">The full vital zone height.</param>
        /// <param name="aim">Where the line of sight sits within the vital zone.</param>
        /// <returns>The point-blank range and danger space result.</returns>
        /// <exception cref="ArgumentNullException">The trajectory is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The vital zone size is not positive.</exception>
        /// <exception cref="InvalidOperationException">The trajectory never enters, or never leaves, the corridor (extend it or change the zone).</exception>
        public static PointBlankRangeResult Analyze(TrajectoryPoint[] trajectory, Measurement<DistanceUnit> vitalZoneSize, PointBlankAim aim = PointBlankAim.Center)
        {
            ArgumentNullException.ThrowIfNull(trajectory);
            double size = vitalZoneSize.In(DistanceUnit.Meter);
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(vitalZoneSize), "The vital zone size must be positive.");

            // Corridor bounds relative to the line of sight: center hold straddles it (±size/2),
            // bottom hold sits the line of sight at the base of the zone (0 .. +size).
            double lower = aim == PointBlankAim.Bottom ? 0.0 : -size / 2.0;
            double upper = aim == PointBlankAim.Bottom ? size : size / 2.0;

            // Non-null points: range and height above the line of sight (Drop, negative below), in meters.
            var points = trajectory.Where(p => p != null).ToArray();
            if (points.Length < 2)
                throw new InvalidOperationException("The trajectory has too few points to analyze.");
            double[] r = points.Select(p => p.Distance.In(DistanceUnit.Meter)).ToArray();
            double[] d = points.Select(p => p.Drop.In(DistanceUnit.Meter)).ToArray();

            int peak = IndexOfMax(d);
            if (d[peak] < lower)
                throw new InvalidOperationException("The trajectory never rises into the vital zone corridor.");

            int minIdx = FirstInside(d, lower, upper);
            if (minIdx < 0)
                throw new InvalidOperationException("The trajectory never enters the corridor.");

            int maxIdx = LastContiguousInside(d, minIdx, lower, upper);
            if (maxIdx == d.Length - 1)
                throw new InvalidOperationException("The trajectory does not extend past the point-blank corridor; compute it to a longer range.");

            return new PointBlankRangeResult(
                Meters(r[minIdx]), Meters(r[maxIdx]), Meters(d[peak]), Meters(r[peak]),
                nearZero: CrossingRange(r, d, 0, peak, rising: true),
                farZero: CrossingRange(r, d, peak + 1, d.Length - 1, rising: false));
        }

        private static int IndexOfMax(double[] values)
        {
            int max = 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] > values[max]) max = i;
            return max;
        }

        // First index whose value lies inside the corridor.
        private static int FirstInside(double[] d, double lower, double upper)
        {
            for (int i = 0; i < d.Length; i++)
                if (d[i] >= lower && d[i] <= upper) return i;
            return -1;
        }

        // Last index of the contiguous run inside the corridor that starts at 'start'.
        private static int LastContiguousInside(double[] d, int start, double lower, double upper)
        {
            int last = start;
            while (last + 1 < d.Length && d[last + 1] >= lower && d[last + 1] <= upper)
                last++;
            return last;
        }

        // First range in [from, to] where the path reaches the line of sight: rising (Drop at or above 0)
        // for the near crossing, falling (Drop below 0) for the far crossing; null if it never does.
        private static Measurement<DistanceUnit>? CrossingRange(double[] r, double[] d, int from, int to, bool rising)
        {
            for (int i = from; i <= to && i < d.Length; i++)
                if (rising ? d[i] >= 0 : d[i] < 0)
                    return Meters(r[i]);
            return null;
        }

        private static Measurement<DistanceUnit> Meters(double m) => new Measurement<DistanceUnit>(m, DistanceUnit.Meter);
    }
}
