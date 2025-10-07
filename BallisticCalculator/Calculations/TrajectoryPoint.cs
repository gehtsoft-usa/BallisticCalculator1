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
        /// Distance traveled along line of sight
        /// </summary>
        [BXmlProperty(Name = "distance")]
        public Measurement<DistanceUnit> Distance { get; }

        /// <summary>
        /// Distance traveled along surface from the muzzle
        /// </summary>
        [BXmlProperty(Name = "distanceFlat")]
        public Measurement<DistanceUnit> DistanceFlat { get; }

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
        /// <para>Current drop relatively to line of sight</para>
        /// <para>The drop is vertical distance between trajectory and the line of sight.</para>
        /// </summary>
        [BXmlProperty(Name = "drop")]
        public Measurement<DistanceUnit> Drop { get; }

        /// <summary>
        /// <para>Current drop relatively to muzzle</para>
        /// <para>The drop is vertical distance between trajectory and the line of sight.</para>
        /// </summary>
        [BXmlProperty(Name = "dropFlat")]
        public Measurement<DistanceUnit> DropFlat { get; }


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
        /// Adjustment for drop in angular units
        /// </summary>
        [BXmlProperty(Name = "dropAdjustment")]
        public Measurement<AngularUnit> DropAdjustment { get; }

        /// <summary>
        /// Adjustment for windage in angular units
        /// </summary>
        [BXmlProperty(Name = "windageAdjustment")]
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
        public TrajectoryPoint(TimeSpan time,
                               Measurement<WeightUnit> weight,
                               Measurement<DistanceUnit> distance,
                               Measurement<VelocityUnit> velocity,
                               double mach,
                               Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> windage)
            : this(time, distance, distance, velocity, mach, drop, drop,
                  BallisticMath.CalculateAdjustment(drop, distance),
                  new Measurement<DistanceUnit>(0, DistanceUnit.Meter), new Measurement<DistanceUnit>(0, DistanceUnit.Meter), 
                  windage, BallisticMath.CalculateAdjustment(windage, distance),
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
        /// <param name="distanceFlat"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="dropFlat"></param>
        /// <param name="lineOfSightElevation"></param>
        /// <param name="lineOfDepartureElevation"></param>
        /// <param name="windage"></param>
        public TrajectoryPoint(TimeSpan time, Measurement<WeightUnit> weight,
                               Measurement<DistanceUnit> distance,
                               Measurement<DistanceUnit> distanceFlat,
                               Measurement<VelocityUnit> velocity, double mach,
                               Measurement<DistanceUnit> drop, Measurement<DistanceUnit> dropFlat,
                               Measurement<DistanceUnit> lineOfSightElevation, 
                               Measurement<DistanceUnit> lineOfDepartureElevation,
                               Measurement<DistanceUnit> windage)
            : this(time, distance, distanceFlat, velocity, mach, drop, dropFlat,
                  BallisticMath.CalculateAdjustment(drop, distance),
                  lineOfSightElevation, lineOfDepartureElevation, 
                  windage, BallisticMath.CalculateAdjustment(windage, distance),
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
            : this(time, distance, distance, velocity, mach, drop, drop,
                  BallisticMath.CalculateAdjustment(drop, distance),
                  Measurement<DistanceUnit>.ZERO, Measurement<DistanceUnit>.ZERO, 
                  windage, BallisticMath.CalculateAdjustment(windage, distance),
                  energy, optimalGameWeight)
        {
        }

        /// <summary>
        /// Constructor for serialization
        /// </summary>
        /// <param name="time"></param>
        /// <param name="distance"></param>
        /// <param name="distanceFlat"></param>
        /// <param name="velocity"></param>
        /// <param name="mach"></param>
        /// <param name="drop"></param>
        /// <param name="dropFlat"></param>
        /// <param name="dropAdjustment"></param>
        /// <param name="windageAdjustment"></param>
        /// <param name="lineOfSightElevation"></param>
        /// <param name="lineOfDepartureElevation"></param>
        /// <param name="windage"></param>
        /// <param name="energy"></param>
        /// <param name="optimalGameWeight"></param>
        [JsonConstructor]
        [BXmlConstructor]
        public TrajectoryPoint(TimeSpan time,
                               Measurement<DistanceUnit> distance,
                               Measurement<DistanceUnit> distanceFlat,
                               Measurement<VelocityUnit> velocity, 
                               double mach,
                               Measurement<DistanceUnit> drop,
                               Measurement<DistanceUnit> dropFlat,
                               Measurement<AngularUnit> dropAdjustment,
                               Measurement<DistanceUnit> lineOfSightElevation,
                               Measurement<DistanceUnit> lineOfDepartureElevation,
                               Measurement<DistanceUnit> windage,
                               Measurement<AngularUnit> windageAdjustment,
                               Measurement<EnergyUnit> energy,
                               Measurement<WeightUnit> optimalGameWeight)
        {
            Time = time;
            Distance = distance;
            DistanceFlat = distanceFlat;
            Mach = mach;
            Velocity = velocity;
            Drop = drop;
            DropFlat = dropFlat;
            LineOfSightElevation = lineOfSightElevation;
            LineOfDepartureElevation = lineOfDepartureElevation;
            DropAdjustment = dropAdjustment;
            Windage = windage;
            WindageAdjustment = windageAdjustment;
            Energy = energy;
            OptimalGameWeight = optimalGameWeight;
        }
    }
}
