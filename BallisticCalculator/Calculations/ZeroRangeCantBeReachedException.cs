using System;

namespace BallisticCalculator
{
    /// <summary>
    /// <para>The exception which indicates that the rifle cannot be zeroed at the requested distance.</para>
    /// <para>The exception is thrown by
    /// [clink=BallisticCalculator.TrajectoryCalculator.CalculateZeroParameters.KD8]CalculateZeroParameters[/clink]
    /// when no barrel elevation puts the impact onto the aim point at the zero distance: either the projectile
    /// hits the ground or slows below the minimum velocity before the zero distance is reached, or the
    /// elevation required to reach the aim point makes the projectile fly over the zero point instead of
    /// crossing it, so the solve does not converge.</para>
    /// <para>The class is derived from [c]InvalidOperationException[/c], which was thrown before this
    /// exception was introduced, so the code which catches the base class keeps working.</para>
    /// </summary>
    public class ZeroRangeCantBeReachedException : InvalidOperationException
    {
        /// <summary>
        /// The message used when no explicit message is specified.
        /// </summary>
        public const string DefaultMessage = "The rifle cannot be zeroed at the specified distance";

        /// <summary>
        /// Constructor with the default message.
        /// </summary>
        public ZeroRangeCantBeReachedException()
            : base(DefaultMessage)
        {
        }

        /// <summary>
        /// Constructor with the specified message.
        /// </summary>
        /// <param name="message">The message which explains why the zero distance cannot be reached.</param>
        public ZeroRangeCantBeReachedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor with the specified message and the inner exception.
        /// </summary>
        /// <param name="message">The message which explains why the zero distance cannot be reached.</param>
        /// <param name="innerException">The exception which caused this exception.</param>
        public ZeroRangeCantBeReachedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
