using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator
{
    /// <summary>
    /// The data point of initial drag table
    /// </summary>
    public class DragTableDataPoint
    {
        /// <summary>
        /// Velocity in Mach (relative to speed of sound)
        /// </summary>
        public double Mach { get; }

        /// <summary>
        /// Drag coefficient
        /// </summary>
        public double DragCoefficient { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mach"></param>
        /// <param name="dragCoefficient"></param>
        public DragTableDataPoint(double mach, double dragCoefficient)
        {
            Mach = mach;
            DragCoefficient = dragCoefficient;
        }
    }
}
