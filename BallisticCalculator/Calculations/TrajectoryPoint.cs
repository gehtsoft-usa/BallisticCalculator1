using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    public class TrajectoryPoint
    {
        public TimeSpan Time { get; }
        public Measurement<DistanceUnit> Distance { get; }
        public Measurement<VelocityUnit> Velocity { get; }
        public double Mach { get; }
        public Measurement<DistanceUnit> Drop { get; }

        public Measurement<DistanceUnit> Windage { get; }

        public Measurement<EnergyUnit> Energy { get; }

        [JsonIgnore]
        public Measurement<AngularUnit> DropAdjustment { get; }

        [JsonIgnore]
        public Measurement<AngularUnit> WindageAdjustment { get; }

        public Measurement<WeightUnit> OptimalGameWeight { get; }

        public TrajectoryPoint(TimeSpan time, Measurement<WeightUnit> weight, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> windage)
            : this(time, distance, velocity, mach, drop, windage,
                  MeasurementMath.KineticEnergy(weight, velocity),
                  BallisticMath.OptimalGameWeight(weight, velocity))
        {

        }

        [JsonConstructor]
        public TrajectoryPoint(TimeSpan time, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> windage, Measurement<EnergyUnit> energy, 
                               Measurement<WeightUnit> optimalGameWeight)
        {
            Time = time;
            Distance = distance;
            Velocity = velocity;
            Drop = drop;
            DropAdjustment = MeasurementMath.Atan(Drop / Distance);

            Windage = windage;
            WindageAdjustment = MeasurementMath.Atan(Windage / Distance);
            Energy = energy;
            OptimalGameWeight = optimalGameWeight;
        }
    }
}
