using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// One point of the projectile trajectory
    /// </summary>
    [BXmlElement("point")]
    public class TrajectoryPoint
    {
        /// <summary>
        /// Time since start
        /// </summary>
        [BXmlProperty(Name = "time")]
        public TimeSpan Time { get; }
        /// <summary>
        /// Distance traveled
        /// </summary>
        [BXmlProperty(Name = "distance")]
        public Measurement<DistanceUnit> Distance { get; }
        /// <summary>
        /// Current velocity
        /// </summary>
        [BXmlProperty(Name = "velocity")]
        public Measurement<VelocityUnit> Velocity { get; }
        /// <summary>
        /// <para>Velocity in Mach</para>
        /// <para>The value shows proportion of the current velocity to the velocity of sound for the current atmosphere conditions</para>
        /// </summary>
        [BXmlProperty(Name = "mach")]
        public double Mach { get; }

        /// <summary>
        /// <para>Current drop</para>
        /// <para>The drop is vertical distance between trajectory and the line of sight.</para>
        /// </summary>
        [BXmlProperty(Name = "drop")]
        public Measurement<DistanceUnit> Drop { get; }

        /// <summary>
        /// <para>Current windage</para>
        /// <para>The windage is horizontal distance between trajectory and the line of sight</para>
        /// <para>The windage to the left is positive, to the right is negative</para>
        /// </summary>
        [BXmlProperty(Name = "windage")]
        public Measurement<DistanceUnit> Windage { get; }

        /// <summary>
        /// Projectile energy
        /// </summary>
        [BXmlProperty(Name = "energy")]
        public Measurement<EnergyUnit> Energy { get; }

        /// <summary>
        /// The difference between the line of sight and the altitude of the muzzle
        /// </summary>
        [BXmlProperty(Name = "lineOfSight")]
        public Measurement<DistanceUnit> LineOfSightElevation { get; }

        /// <summary>
        /// The difference between the line of departure and the altitude of the muzzle
        /// </summary>
        [BXmlProperty(Name = "lineOfDeparture")]
        public Measurement<DistanceUnit> LineOfDepartureElevation { get; }

        /// <summary>
        /// Vertical distance between trajectory and the line of departure.
        /// </summary>
        [JsonIgnore]
        public Measurement<DistanceUnit> DropVsLineOfDeparture => LineOfDepartureElevation - (Drop + LineOfSightElevation);

        /// <summary>
        /// Adjustment for drop in angular units
        /// </summary>
        [JsonIgnore]
        public Measurement<AngularUnit> DropAdjustment { get; }

        /// <summary>
        /// Adjustment for windage in angular units
        /// </summary>
        [JsonIgnore]
        public Measurement<AngularUnit> WindageAdjustment { get; }

        /// <summary>
        /// Optimal weight of the game
        /// </summary>
        [BXmlProperty(Name = "optimal-game-weight")]
        public Measurement<WeightUnit> OptimalGameWeight { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="time"></param>
        /// <param name="weight"></param>
        /// <param name="distance"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="windage"></param>
        public TrajectoryPoint(TimeSpan time, Measurement<WeightUnit> weight, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> windage)
            : this(time, distance, velocity, mach, drop, new Measurement<DistanceUnit>(0, DistanceUnit.Meter), new Measurement<DistanceUnit>(0, DistanceUnit.Meter), windage,
                  MeasurementMath.KineticEnergy(weight, velocity),
                  BallisticMath.OptimalGameWeight(weight, velocity))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="time"></param>
        /// <param name="weight"></param>
        /// <param name="distance"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="lineOfSightElevation"></param>
        /// <param name="lineOfDepartureElevation"></param>
        /// <param name="windage"></param>
        public TrajectoryPoint(TimeSpan time, Measurement<WeightUnit> weight, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop, 
                               Measurement<DistanceUnit> lineOfSightElevation, Measurement<DistanceUnit> lineOfDepartureElevation,
                               Measurement<DistanceUnit> windage)
            : this(time, distance, velocity, mach, drop, lineOfSightElevation, lineOfDepartureElevation, windage,
                  MeasurementMath.KineticEnergy(weight, velocity),
                  BallisticMath.OptimalGameWeight(weight, velocity))
        {
        }

        /// <summary>
        /// Constructor for backward compatibility 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="distance"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="windage"></param>
        /// <param name="energy"></param>
        /// <param name="optimalGameWeight"></param>
        public TrajectoryPoint(TimeSpan time, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> windage, Measurement<EnergyUnit> energy,
                               Measurement<WeightUnit> optimalGameWeight)
            : this(time, distance, velocity, mach, drop, Measurement<DistanceUnit>.ZERO, Measurement<DistanceUnit>.ZERO, windage, energy, optimalGameWeight)
        {
        }

        /// <summary>
        /// Constructor for serialization
        /// </summary>
        /// <param name="time"></param>
        /// <param name="distance"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="lineOfSightElevation"></param>
        /// <param name="lineOfDepartureElevation"></param>
        /// <param name="windage"></param>
        /// <param name="energy"></param>
        /// <param name="optimalGameWeight"></param>
        [JsonConstructor]
        [BXmlConstructor]
        public TrajectoryPoint(TimeSpan time, Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity, double mach, Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> lineOfSightElevation,
                               Measurement<DistanceUnit> lineOfDepartureElevation,
                               Measurement<DistanceUnit> windage, Measurement<EnergyUnit> energy,
                               Measurement<WeightUnit> optimalGameWeight)
        {
            Time = time;
            Distance = distance;
            Velocity = velocity;
            Drop = drop;
            LineOfSightElevation = lineOfSightElevation;
            LineOfDepartureElevation = lineOfDepartureElevation;
            if (Distance.Value > 0)
                DropAdjustment = MeasurementMath.Atan(Drop / Distance);
            else
                DropAdjustment = 0.As(AngularUnit.Radian);
            Mach = mach;

            Windage = windage;
            if (Distance.Value > 0)
                WindageAdjustment = MeasurementMath.Atan(Windage / Distance);
            else
                WindageAdjustment = 0.As(AngularUnit.Radian);
            Energy = energy;
            OptimalGameWeight = optimalGameWeight;
        }
    }
}
