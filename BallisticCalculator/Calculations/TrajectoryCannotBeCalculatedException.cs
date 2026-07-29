using System;

namespace BallisticCalculator
{
    /// <summary>
    /// <para>The exception which indicates that the trajectory cannot be calculated for the specified parameters.</para>
    /// <para>The exception is thrown by
    /// [clink=BallisticCalculator.TrajectoryCalculator.Calculate.LP7]Calculate[/clink] when the numerical
    /// integration cannot advance the projectile downrange: an integration step leaves the projectile
    /// where it was or moves it backwards, or the projectile velocity is no longer a finite number.
    /// Both conditions mean that the parameters are degenerate rather than merely extreme, the most
    /// common causes being a ballistic coefficient of zero and a drag table with unusable drag
    /// coefficients.</para>
    /// <para>The class is derived from [c]InvalidOperationException[/c], so the code which catches the
    /// base class keeps working.</para>
    /// </summary>
    public class TrajectoryCannotBeCalculatedException : InvalidOperationException
    {
        /// <summary>
        /// The message used when no explicit message is specified.
        /// </summary>
        public const string DefaultMessage = "The trajectory cannot be calculated for the specified parameters";

        /// <summary>
        /// Constructor with the default message.
        /// </summary>
        public TrajectoryCannotBeCalculatedException()
            : base(DefaultMessage)
        {
        }

        /// <summary>
        /// Constructor with the specified message.
        /// </summary>
        /// <param name="message">The message which explains why the trajectory cannot be calculated.</param>
        public TrajectoryCannotBeCalculatedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor with the specified message and the inner exception.
        /// </summary>
        /// <param name="message">The message which explains why the trajectory cannot be calculated.</param>
        /// <param name="innerException">The exception which caused this exception.</param>
        public TrajectoryCannotBeCalculatedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
