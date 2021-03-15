using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;

namespace BallisticCalculator
{
    internal static class BallisticMath
    {
        public static Measurement<WeightUnit> OptimalGameWeight(Measurement<WeightUnit> weight, Measurement<VelocityUnit> velocity)
            => new Measurement<WeightUnit>(Math.Pow(weight.In(WeightUnit.Grain), 2) * Math.Pow(velocity.In(VelocityUnit.FeetPerSecond), 3) * 1.5e-12, WeightUnit.Pound);

        public static TimeSpan TravelTime(Measurement<DistanceUnit> distance, Measurement<VelocityUnit> velocity)
            => TimeSpan.FromSeconds(distance.In(DistanceUnit.Meter) / velocity.In(VelocityUnit.MetersPerSecond));
    }
}
