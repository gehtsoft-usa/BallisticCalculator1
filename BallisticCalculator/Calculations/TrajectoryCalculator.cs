using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BallisticCalculator
{
    /// <summary>
    /// The calculator for the projectile trajectory
    /// </summary>
    public class TrajectoryCalculator
    {
        /// <summary>
        /// <para>The maximum step size of the calculation.</para>
        /// <para>The default value is 10cm</para>
        /// </summary>
        public Measurement<DistanceUnit> MaximumCalculationStepSize { get; set; } = new Measurement<DistanceUnit>(0.1, DistanceUnit.Meter);

        /// <summary>
        /// The maximum drop value to stop further calculation
        /// </summary>
        public static Measurement<DistanceUnit> MaximumDrop { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<DistanceUnit>(10000, DistanceUnit.Foot);

        /// <summary>
        /// The minimum velocity to stop the calculation
        /// </summary>
        public static Measurement<VelocityUnit> MinimumVelocity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<VelocityUnit>(50, VelocityUnit.FeetPerSecond);

        /// <summary>
        /// PIR = (PI/8)*(RHO0/144)
        /// </summary>
        private const double PIR = 2.08551e-04;

        private static DragTable ValidateDragTable(Ammunition ammunition, DragTable dragTable)
        {
            if (ammunition.BallisticCoefficient.Table == DragTableId.GC)
            {
                if (dragTable == null)
                    throw new ArgumentNullException(nameof(dragTable), "The drag table shoudn't be null if the ballistic coefficient is custom");
            }
            else
                dragTable = DragTable.Get(ammunition.BallisticCoefficient.Table);

            return dragTable;
        }

        /// <summary>
        /// Calculates the sight angle for the specified zero distance
        /// </summary>
        /// <param name="ammunition">The ammunition used to zero</param>
        /// <param name="rifle">The rifle zeroed</param>
        /// <param name="atmosphere">The atmosphere of the time of zeroing</param>
        /// <param name="dragTable">Custom dragTable (is DragTableId.GC is used)</param>
        /// <param name="accuracy">Accuracy of calculation. Default accuracy is 0.1mm</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Exception is thrown in case zeroing parameters cannot be found. Try to lower accuracy if you get this exception</exception>"
        public Measurement<AngularUnit> SightAngle(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere, DragTable dragTable = null, Measurement<DistanceUnit>? accuracy = null)
        {
            Measurement<DistanceUnit> rangeTo = rifle.Zero.Distance * 2;
            Measurement<DistanceUnit> step = rifle.Zero.Distance / 100;
            Measurement<DistanceUnit> calculationStep = GetCalculationStep(step);
            accuracy ??= new Measurement<DistanceUnit>(0.1, DistanceUnit.Millimeter);

            dragTable = ValidateDragTable(ammunition, dragTable);

            if (rifle.Zero.Atmosphere != null)
                atmosphere = rifle.Zero.Atmosphere;

            atmosphere ??= new Atmosphere();

            if (rifle.Zero.Ammunition != null)
                ammunition = rifle.Zero.Ammunition;

            Measurement<DistanceUnit> alt0 = atmosphere.Altitude;
            Measurement<DistanceUnit> altDelta = new Measurement<DistanceUnit>(1, DistanceUnit.Meter);
            double densityFactor = 0, drag;
            Measurement<VelocityUnit> mach = new Measurement<VelocityUnit>(0, VelocityUnit.MetersPerSecond);
            Measurement<DistanceUnit> verticalOffset = (rifle.Zero.VerticalOffset) ?? Measurement<DistanceUnit>.ZERO;

            var sightAngle = new Measurement<AngularUnit>(150, AngularUnit.MOA);
            var barrelAzimuth = new Measurement<AngularUnit>(0, AngularUnit.Radian);

            for (int approximation = 0; approximation < 100; approximation++)
            {
                var barrelElevation = sightAngle;

                Measurement<VelocityUnit> velocity = ammunition.MuzzleVelocity;
                TimeSpan time = new TimeSpan(0);

                //x - distance towards target,
                //y - drop and
                //z - windage
                var rangeVector = new Vector<DistanceUnit>(new Measurement<DistanceUnit>(0, DistanceUnit.Meter),
                    -rifle.Sight.SightHeight,
                    new Measurement<DistanceUnit>(0, DistanceUnit.Meter));

                var velocityVector = new Vector<VelocityUnit>(velocity * barrelElevation.Cos() * barrelAzimuth.Cos(),
                                                              velocity * barrelElevation.Sin(),
                                                              velocity * barrelElevation.Cos() * barrelAzimuth.Sin());

                Measurement<DistanceUnit> maximumRange = rangeTo;
                Measurement<DistanceUnit> lastAtAltitude = new Measurement<DistanceUnit>(-1000000, DistanceUnit.Meter);
                DragTableNode dragTableNode = null;

                double adjustBallisticFactorForVelocityUnits = Measurement<VelocityUnit>.Convert(1, velocity.Unit, VelocityUnit.FeetPerSecond);
                double ballisticFactor = 1 / ammunition.GetBallisticCoefficient();
                var accumulatedFactor = PIR * adjustBallisticFactorForVelocityUnits * ballisticFactor;

                var earthGravity = (new Measurement<VelocityUnit>(Measurement<AccelerationUnit>.Convert(1, AccelerationUnit.EarthGravity, AccelerationUnit.MeterPerSecondSquare),
                                                                  VelocityUnit.MetersPerSecond)).To(velocity.Unit);

                var alt = alt0;
                //run all the way down the range
                while (rangeVector.X <= maximumRange)
                {
                    //update density and Mach velocity each 10 feet
                    if (MeasurementMath.Abs(lastAtAltitude - alt) > altDelta)
                    {
                        atmosphere.AtAltitude(alt, out densityFactor, out mach);
                        lastAtAltitude = alt;
                    }

                    if (velocity < MinimumVelocity || rangeVector.Y < -MaximumDrop)
                        break;

                    TimeSpan deltaTime = BallisticMath.TravelTime(calculationStep, velocityVector.X);

                    double currentMach = velocity / mach;

                    //find Mach node for the first time
                    dragTableNode ??= dragTable.Find(currentMach);

                    //walk towards the beginning the table as velocity drops
                    while (dragTableNode.Previous != null && dragTableNode.Previous.Mach > currentMach)
                        dragTableNode = dragTableNode.Previous;

                    drag = accumulatedFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocity.Value;

                    velocityVector = new Vector<VelocityUnit>(
                        velocityVector.X - deltaTime.TotalSeconds * drag * velocityVector.X,
                        velocityVector.Y - deltaTime.TotalSeconds * drag * velocityVector.Y - earthGravity * deltaTime.TotalSeconds,
                        velocityVector.Z - deltaTime.TotalSeconds * drag * velocityVector.Z);

                    var deltaRangeVector = new Vector<DistanceUnit>(
                            new Measurement<DistanceUnit>(velocityVector.X.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                            new Measurement<DistanceUnit>(velocityVector.Y.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                            new Measurement<DistanceUnit>(velocityVector.Z.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter)
                            );

                    rangeVector += deltaRangeVector;
                    alt += deltaRangeVector.Y;

                    if (rangeVector.X >= rifle.Zero.Distance)
                    {
                        var match = rangeVector.Y - verticalOffset;
                        if (Math.Abs(match.In(DistanceUnit.Millimeter)) < accuracy.Value.In(DistanceUnit.Millimeter))
                            return sightAngle;

                        sightAngle += new Measurement<AngularUnit>(-match.In(DistanceUnit.Centimeter) / rifle.Zero.Distance.In(DistanceUnit.Meter) * 100, AngularUnit.CmPer100Meters);
                        break;
                    }

                    velocity = velocityVector.Magnitude;
                    time = time.Add(BallisticMath.TravelTime(deltaRangeVector.Magnitude, velocity));
                }
            }
            throw new InvalidOperationException("Cannot find zero parameters");
        }

        /// <summary>
        /// Calculates the trajectory for the specified parameters.
        /// </summary>
        /// <param name="ammunition"></param>
        /// <param name="rifle"></param>
        /// <param name="atmosphere"></param>
        /// <param name="shot"></param>
        /// <param name="wind"></param>
        /// <param name="dragTable">Custom drag table</param>
        /// <returns></returns>
        public TrajectoryPoint[] Calculate(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere, ShotParameters shot, Wind[] wind = null, DragTable dragTable = null)
        {
            Measurement<DistanceUnit> rangeTo = shot.MaximumDistance;
            Measurement<DistanceUnit> step = shot.Step;
            Measurement<DistanceUnit> calculationStep = GetCalculationStep(step);

            atmosphere ??= new Atmosphere();

            dragTable = ValidateDragTable(ammunition, dragTable);

            Measurement<DistanceUnit> alt0 = atmosphere.Altitude;
            Measurement<DistanceUnit> altDelta = new Measurement<DistanceUnit>(1, DistanceUnit.Meter);
            double densityFactor = 0, drag;
            Measurement<VelocityUnit> mach = new Measurement<VelocityUnit>(0, VelocityUnit.MetersPerSecond);

            double stabilityCoefficient = 1;
            bool calculateDrift;

            if (rifle.Rifling != null && ammunition.BulletDiameter != null && ammunition.BulletLength != null)
            {
                stabilityCoefficient = CalculateStabilityCoefficient(ammunition, rifle, atmosphere);
                calculateDrift = true;
            }
            else
            {
                calculateDrift = false;
            }

            TrajectoryPoint[] trajectoryPoints = new TrajectoryPoint[(int)(Math.Floor(rangeTo / step)) + 1];

            var barrelAzimuth = shot.BarrelAzymuth ?? new Measurement<AngularUnit>(0.0, AngularUnit.Radian);
            var barrelElevation = shot.SightAngle;
            
            if (shot.ShotAngle != null)
                barrelElevation += shot.ShotAngle.Value;

            var lineOfSight = shot.ShotAngle ?? new Measurement<AngularUnit>(0, AngularUnit.Radian);
            double lineOfSightTan = MeasurementMath.Tan(lineOfSight);
            double lineOfDepartureTan = MeasurementMath.Tan(barrelElevation);
            double lineOfSightCos = MeasurementMath.Cos(lineOfSight);

            Measurement<VelocityUnit> velocity = ammunition.MuzzleVelocity;
            TimeSpan time = new TimeSpan(0);

            int currentWind = 0;
            Measurement<DistanceUnit> nextWindRange = new Measurement<DistanceUnit>(1e7, DistanceUnit.Meter);
            Vector<VelocityUnit> windVector;
            if (wind == null || wind.Length < 1)
            {
                windVector = new Vector<VelocityUnit>();
            }
            else
            {
                if (wind.Length > 1 && wind[0].MaximumRange != null)
                    nextWindRange = wind[0].MaximumRange.Value;
                windVector = WindVector(shot, wind[0], velocity.Unit);
            }

            //x - distance towards target,
            //y - drop and
            //z - windage
            var rangeVector = new Vector<DistanceUnit>(new Measurement<DistanceUnit>(0, DistanceUnit.Meter),
                -rifle.Sight.SightHeight,
                new Measurement<DistanceUnit>(0, DistanceUnit.Meter));

            var velocityVector = new Vector<VelocityUnit>(velocity * barrelElevation.Cos() * barrelAzimuth.Cos(),
                                                          velocity * barrelElevation.Sin(),
                                                          velocity * barrelElevation.Cos() * barrelAzimuth.Sin());

            int currentItem = 0;
            Measurement<DistanceUnit> maximumRange = rangeTo + calculationStep;
            Measurement<DistanceUnit> nextRangeDistance = new Measurement<DistanceUnit>(0, DistanceUnit.Meter);

            Measurement<DistanceUnit> lastAtAltitude = new Measurement<DistanceUnit>(-1000000000, DistanceUnit.Meter);
            DragTableNode dragTableNode = null;

            double adjustBallisticFactorForVelocityUnits = Measurement<VelocityUnit>.Convert(1, velocity.Unit, VelocityUnit.FeetPerSecond);
            double ballisticFactor = 1 / ammunition.GetBallisticCoefficient();
            var accumulatedFactor = PIR * adjustBallisticFactorForVelocityUnits * ballisticFactor;

            var earthGravity = (new Measurement<VelocityUnit>(Measurement<AccelerationUnit>.Convert(1, AccelerationUnit.EarthGravity, AccelerationUnit.MeterPerSecondSquare),
                                                              VelocityUnit.MetersPerSecond)).To(velocity.Unit);

            var alt = alt0;
            var distance = new Measurement<DistanceUnit>(0, rangeVector.X.Unit);

            //run all the way down the range
            while (distance <= maximumRange)
            {
                //update density and Mach velocity each 10 feet of altitude
                if (MeasurementMath.Abs(lastAtAltitude - alt) > altDelta)
                {
                    atmosphere.AtAltitude(alt, out densityFactor, out mach);
                    lastAtAltitude = alt;
                }

                if (velocity < MinimumVelocity || rangeVector.Y < -MaximumDrop)
                    break;

                if (distance >= nextWindRange)
                {
                    currentWind++;
                    windVector = WindVector(shot, wind[currentWind], velocity.Unit);

                    if (currentWind == wind.Length - 1 || wind[currentWind].MaximumRange == null)
                        nextWindRange = new Measurement<DistanceUnit>(1e7, DistanceUnit.Meter);
                    else
                        nextWindRange = wind[currentWind].MaximumRange.Value;
                }

                if (distance >= nextRangeDistance)
                {
                    var windage = rangeVector.Z;
                    
                    if (calculateDrift)
                        windage += new Measurement<DistanceUnit>(1.25 * (stabilityCoefficient + 1.2) * Math.Pow(time.TotalSeconds, 1.83) * (rifle.Rifling.Direction == TwistDirection.Right ? -1 : 1), DistanceUnit.Inch);

                    var lineOfSightElevation = rangeVector.X * lineOfSightTan;
                    var lineOfDepartureElevation = rangeVector.X * lineOfDepartureTan - rifle.Sight.SightHeight;

                    trajectoryPoints[currentItem] = new TrajectoryPoint(
                        time: time,
                        weight: ammunition.Weight,
                        distance: distance,
                        velocity: velocity,
                        mach: velocity / mach,
                        drop: rangeVector.Y - lineOfSightElevation,
                        lineOfSightElevation: lineOfSightElevation,
                        lineOfDepartureElevation: lineOfDepartureElevation,
                        windage: windage);
                     nextRangeDistance += step;
                    currentItem++;                   
                    if (currentItem == trajectoryPoints.Length)
                        break;
                }

                TimeSpan deltaTime = BallisticMath.TravelTime(calculationStep, velocityVector.X);

                var velocityAdjusted = velocityVector - windVector;
                velocity = velocityAdjusted.Magnitude;
                double currentMach = velocity / mach;

                //find Mach node for the first time
                dragTableNode ??= dragTable.Find(currentMach);

                //walk towards the beginning the table as velocity drops
                while (dragTableNode.Mach > currentMach)
                    dragTableNode = dragTableNode.Previous;

                drag = accumulatedFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocity.Value;
                var factor = deltaTime.TotalSeconds * drag;

                velocityVector = new Vector<VelocityUnit>(
                    velocityVector.X - factor * velocityAdjusted.X,
                    velocityVector.Y - factor * velocityAdjusted.Y
                                     - earthGravity * deltaTime.TotalSeconds,
                    velocityVector.Z - factor * velocityAdjusted.Z
                 );

                var deltaRangeVector = new Vector<DistanceUnit>(
                        new Measurement<DistanceUnit>(velocityVector.X.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                        new Measurement<DistanceUnit>(velocityVector.Y.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                        new Measurement<DistanceUnit>(velocityVector.Z.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter));

                rangeVector += deltaRangeVector;
                distance = rangeVector.X / lineOfSightCos;
                alt += deltaRangeVector.Y;
                velocity = velocityVector.Magnitude;
                time = time.Add(BallisticMath.TravelTime(deltaRangeVector.Magnitude, velocity));
            }

            return trajectoryPoints;
        }

        internal Measurement<DistanceUnit> GetCalculationStep(Measurement<DistanceUnit> step)
        {
            step /= 2;      //do it twice for increased accuracy of velocity calculation and 10 times per step
            if (step > MaximumCalculationStepSize)
            {
                int stepOrder = (int)Math.Floor(Math.Log10(step.Value));
                int maximumOrder = (int)Math.Floor(Math.Log10(MaximumCalculationStepSize.In(step.Unit)));
                step /= Math.Pow(10, stepOrder - maximumOrder + 1);
            }
            return step;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<VelocityUnit> WindVector(ShotParameters shot, Wind wind, VelocityUnit units)
        {
            double sightCosine = shot.SightAngle.Cos();
            double sightSine = shot.SightAngle.Sin();
            double cantCosine = (shot.CantAngle ?? AngularUnit.Radian.New(0)).Cos();
            double cantSine = (shot.CantAngle ?? AngularUnit.Radian.New(0)).Sin();

            Measurement<VelocityUnit> rangeVelocity, crossComponent;

            if (wind != null)
            {
                rangeVelocity = (wind.Velocity * wind.Direction.Cos()).To(units);
                crossComponent = (wind.Velocity * wind.Direction.Sin()).To(units);
            }
            else
            {
                rangeVelocity = new Measurement<VelocityUnit>(0, units);
                crossComponent = new Measurement<VelocityUnit>(0, units);
            }

            Measurement<VelocityUnit> rangeFactor = -rangeVelocity * sightSine;

            return new Vector<VelocityUnit>(rangeVelocity * sightCosine, rangeFactor * cantCosine + crossComponent * cantSine, crossComponent * cantCosine - rangeFactor * cantSine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateStabilityCoefficient(Ammunition ammunitionInfo, Rifle rifleInfo, Atmosphere atmosphere)
        {
            double weight = ammunitionInfo.Weight.In(WeightUnit.Grain);
            double diameter = ammunitionInfo.BulletDiameter.Value.In(DistanceUnit.Inch);
            double twist = rifleInfo.Rifling.RiflingStep.In(DistanceUnit.Inch) / diameter;
            double length = ammunitionInfo.BulletLength.Value.In(DistanceUnit.Inch) / diameter;
            double sd = 30 * weight / (Math.Pow(twist, 2) * Math.Pow(diameter, 3) * length * (1 + Math.Pow(length, 2)));
            double fv = Math.Pow(ammunitionInfo.MuzzleVelocity.In(VelocityUnit.FeetPerSecond) / 2800, 1.0 / 3.0);
            double ftp = 1;

            if (atmosphere != null)
            {
                double ft = atmosphere.Temperature.In(TemperatureUnit.Fahrenheit);
                double pt = atmosphere.Pressure.In(PressureUnit.InchesOfMercury);
                ftp = ((ft + 460) / (59 + 460)) * (29.92 / pt);
            }
            return sd * fv * ftp;
        }
    }
}
