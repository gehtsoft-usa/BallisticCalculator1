using System;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Tools
{
    /// <summary>
    /// <para>Calculates the aim-off lead for a moving target.</para>
    /// <para>The target motion direction uses the same convention as a [clink=BallisticCalculator.Wind]Wind[/clink]
    /// direction: 0 degrees is straight along the line of sight (no lead), 90 degrees is a full crossing
    /// from the right and 270 degrees a full crossing from the left. Only the crossing component of the
    /// motion produces lead. The result follows the trajectory windage sign (positive left, negative right).</para>
    /// </summary>
    public static class MovingTargetLead
    {
        /// <summary>
        /// <para>The linear lead: how far to the side of the target's current position to aim.</para>
        /// <para>Equals the target crossing speed times the time of flight. Convert the result to the
        /// desired units with its In method.</para>
        /// </summary>
        /// <param name="targetSpeed">The target speed.</param>
        /// <param name="movingDirection">The target motion direction (same convention as wind).</param>
        /// <param name="timeOfFlight">The projectile time of flight to the target.</param>
        public static Measurement<DistanceUnit> Lead(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TimeSpan timeOfFlight)
        {
            double crossSpeedMps = targetSpeed.In(VelocityUnit.MetersPerSecond) * movingDirection.Sin();
            return new Measurement<DistanceUnit>(crossSpeedMps * timeOfFlight.TotalSeconds, DistanceUnit.Meter);
        }

        /// <summary>
        /// The linear lead at a [clink=BallisticCalculator.TrajectoryPoint]TrajectoryPoint[/clink], using its time of flight.
        /// </summary>
        /// <param name="targetSpeed">The target speed.</param>
        /// <param name="movingDirection">The target motion direction (same convention as wind).</param>
        /// <param name="point">The trajectory point at the target range.</param>
        public static Measurement<DistanceUnit> Lead(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TrajectoryPoint point)
        {
            ArgumentNullException.ThrowIfNull(point);
            return Lead(targetSpeed, movingDirection, point.Time);
        }

        /// <summary>
        /// <para>The angular lead: the sight hold-off for the moving target at the given range.</para>
        /// <para>Equals the arctangent of the linear lead over the range. Convert to MOA, Mil and so on with its In method.</para>
        /// </summary>
        /// <param name="targetSpeed">The target speed.</param>
        /// <param name="movingDirection">The target motion direction (same convention as wind).</param>
        /// <param name="timeOfFlight">The projectile time of flight to the target.</param>
        /// <param name="range">The range to the target.</param>
        public static Measurement<AngularUnit> LeadAngle(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TimeSpan timeOfFlight, Measurement<DistanceUnit> range)
        {
            return BallisticMath.CalculateAdjustment(Lead(targetSpeed, movingDirection, timeOfFlight), range);
        }

        /// <summary>
        /// The angular lead at a [clink=BallisticCalculator.TrajectoryPoint]TrajectoryPoint[/clink], using its time of flight and distance.
        /// </summary>
        /// <param name="targetSpeed">The target speed.</param>
        /// <param name="movingDirection">The target motion direction (same convention as wind).</param>
        /// <param name="point">The trajectory point at the target range.</param>
        public static Measurement<AngularUnit> LeadAngle(Measurement<VelocityUnit> targetSpeed, Measurement<AngularUnit> movingDirection, TrajectoryPoint point)
        {
            ArgumentNullException.ThrowIfNull(point);
            return LeadAngle(targetSpeed, movingDirection, point.Time, point.Distance);
        }
    }
}
