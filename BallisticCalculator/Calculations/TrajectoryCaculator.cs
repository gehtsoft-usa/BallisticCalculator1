using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;



namespace BallisticCalculator
{
    public class TrajectoryCaculator
    {
        public Measurement<DistanceUnit> MaximumCalculationStepSize { get; set; } = new Measurement<DistanceUnit>(0.1, DistanceUnit.Meter);
        public static Measurement<DistanceUnit> MaximumDrop { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }  = new Measurement<DistanceUnit>(10000, DistanceUnit.Foot);
        public static Measurement<VelocityUnit> MinimumVelocity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<VelocityUnit>(50, VelocityUnit.FeetPerSecond);


        public Measurement<AngularUnit> SightAngle(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere)
        {
            Measurement<DistanceUnit> rangeTo = rifle.Zero.Distance * 2;
            Measurement<DistanceUnit> step = rifle.Zero.Distance / 100;
            Measurement<DistanceUnit> calculationStep = GetCalculationStep(step);

            if (rifle.Zero.Atmosphere != null)
                atmosphere = rifle.Zero.Atmosphere;

            if (atmosphere == null)
                atmosphere = new Atmosphere();

            if (rifle.Zero.Ammunition != null)
                ammunition = rifle.Zero.Ammunition;

            Measurement<DistanceUnit> alt0 = atmosphere.Altitude;
            Measurement<DistanceUnit> altDelta = new Measurement<DistanceUnit>(1, DistanceUnit.Meter);
            double densityFactor = 0, drag;
            Measurement<VelocityUnit> mach = new Measurement<VelocityUnit>(0, VelocityUnit.MetersPerSecond);

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

                var velocityVector = new Vector<VelocityUnit>(velocity * MeasurementMath.Cos(barrelElevation) * MeasurementMath.Cos(barrelAzimuth),
                                                              velocity * MeasurementMath.Sin(barrelElevation),
                                                              velocity * MeasurementMath.Cos(barrelElevation) * MeasurementMath.Sin(barrelAzimuth));

                Measurement<DistanceUnit> maximumRange = rangeTo;
                Measurement<DistanceUnit> lastAtAltitude = new Measurement<DistanceUnit>(-1000000, DistanceUnit.Meter);
                DragTableNode dragTableNode = null;
                var earthGravity = new Measurement<AccelerationUnit>(1, AccelerationUnit.EarthGravity);

                double ballisicFactor = 2.08551e-04 / ammunition.BallisticCoefficient.Value;

                //run all the way down the range
                while (rangeVector.X <= maximumRange)
                {
                    Measurement<DistanceUnit> alt = alt0 + rangeVector.Y;

                    //update density and mach velocity each 10 feet
                    if (MeasurementMath.Abs(lastAtAltitude - alt) > altDelta)
                    {
                        atmosphere.AtAltitude(alt, out densityFactor, out mach);
                        lastAtAltitude = alt;
                    }

                    if (velocity < MinimumVelocity || rangeVector.Y < -MaximumDrop)
                        break;

                    TimeSpan deltaTime = BallisticMath.TravelTime(calculationStep, velocityVector.X);

                    double currentMach = velocity / mach;

                    //find mach node for the first time
                    if (dragTableNode == null)
                        dragTableNode = DragTable.Get(ammunition.BallisticCoefficient.Table).Find(currentMach);

                    //walk towards the beginning the table as velocity drops
                    while (dragTableNode.Previous.Mach > currentMach)
                        dragTableNode = dragTableNode.Previous;

                    drag = ballisicFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocity.In(VelocityUnit.FeetPerSecond);

                    velocityVector = new Vector<VelocityUnit>(
                        velocityVector.X - deltaTime.TotalSeconds * drag * velocityVector.X.To(VelocityUnit.FeetPerSecond),
                        velocityVector.Y - deltaTime.TotalSeconds * drag * velocityVector.Y.To(VelocityUnit.FeetPerSecond)
                                         - new Measurement<VelocityUnit>(deltaTime.TotalSeconds * earthGravity.In(AccelerationUnit.MeterPerSecondSquare), 
                                                                         VelocityUnit.MetersPerSecond),
                        velocityVector.Z - deltaTime.TotalSeconds * drag * velocityVector.Z.To(VelocityUnit.FeetPerSecond));


                    var deltaRangeVector = new Vector<DistanceUnit>(calculationStep,
                            new Measurement<DistanceUnit>(velocityVector.Y.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                            new Measurement<DistanceUnit>(velocityVector.Z.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter));

                    rangeVector = rangeVector + deltaRangeVector;
                    
                    if (rangeVector.X >= rifle.Zero.Distance)
                    {
                        if (Math.Abs(rangeVector.Y.In(DistanceUnit.Millimeter)) < 1)
                            return sightAngle;

                        sightAngle += new Measurement<AngularUnit>(-rangeVector.Y.In(DistanceUnit.Centimeter) / rifle.Zero.Distance.In(DistanceUnit.Meter) * 100, AngularUnit.CmPer100Meters);
                        break;
                    }

                    velocity = velocityVector.Magnitude;
                    time = time.Add(BallisticMath.TravelTime(deltaRangeVector.Magnitude, velocity));
                }
            }
            throw new InvalidOperationException("Cannot find zero parameters");

        }
        public TrajectoryPoint[] Calculate(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere, ShotParameters shot, Wind[] wind)
        {
            Measurement<DistanceUnit> rangeTo = shot.MaximumDistance;
            Measurement<DistanceUnit> step = shot.Step;
            Measurement<DistanceUnit> calculationStep = GetCalculationStep(step);

            if (atmosphere == null)
                atmosphere = new Atmosphere();

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
                calculateDrift = false;

            TrajectoryPoint[] trajectoryPoints = null;
            trajectoryPoints = new TrajectoryPoint[(int)(Math.Floor(rangeTo / step)) + 1];

            var barrelAzimuth = new Measurement<AngularUnit>(0.0, AngularUnit.Radian);
            var barrelElevation = shot.SightAngle;
            if (shot.ShotAngle != null)
                barrelElevation += shot.ShotAngle.Value;

            int currentWind = 0;
            Measurement<DistanceUnit> nextWindRange = new Measurement<DistanceUnit>(1e7, DistanceUnit.Meter);
            Vector<VelocityUnit> windVector;
            if (wind == null || wind.Length < 0)
                windVector = new Vector<VelocityUnit>();
            else
            {
                if (wind.Length > 1 && wind[0].MaximumRange != null)
                    nextWindRange = wind[0].MaximumRange.Value;
                windVector = WindVector(shot, wind[0]);
            }


            Measurement<VelocityUnit> velocity = ammunition.MuzzleVelocity;
            TimeSpan time = new TimeSpan(0);

            //x - distance towards target,
            //y - drop and
            //z - windage
            var rangeVector = new Vector<DistanceUnit>(new Measurement<DistanceUnit>(0, DistanceUnit.Meter), 
                -rifle.Sight.SightHeight, 
                new Measurement<DistanceUnit>(0, DistanceUnit.Meter));
            
            var velocityVector = new Vector<VelocityUnit>(velocity * MeasurementMath.Cos(barrelElevation) * MeasurementMath.Cos(barrelAzimuth),
                                                          velocity * MeasurementMath.Sin(barrelElevation),
                                                          velocity * MeasurementMath.Cos(barrelElevation) * MeasurementMath.Sin(barrelAzimuth));

            int currentItem = 0;
            Measurement<DistanceUnit> maximumRange = rangeTo;
            Measurement<DistanceUnit> nextRangeDistance = new Measurement<DistanceUnit>(0, DistanceUnit.Meter);

            Measurement<DistanceUnit> lastAtAltitude = new Measurement<DistanceUnit>(-1000000, DistanceUnit.Meter);
            DragTableNode dragTableNode = null;
            var earthGravity = new Measurement<AccelerationUnit>(1, AccelerationUnit.EarthGravity);

            double ballisicFactor = 2.08551e-04 / ammunition.BallisticCoefficient.Value;

            //run all the way down the range
            while (rangeVector.X <= maximumRange + calculationStep)
            {
                Measurement<DistanceUnit> alt = alt0 + rangeVector.Y;
                
                //update density and mach velocity each 10 feet
                if (MeasurementMath.Abs(lastAtAltitude - alt) > altDelta)
                {
                    atmosphere.AtAltitude(alt, out densityFactor, out mach);
                    lastAtAltitude = alt;
                }

                if (velocity < MinimumVelocity || rangeVector.Y < -MaximumDrop)
                    break;

                if (rangeVector.X >= nextWindRange)
                {
                    currentWind++;
                    windVector = WindVector(shot, wind[currentWind]);

                    if (currentWind == wind.Length - 1 || wind[currentWind].MaximumRange == null)
                        nextWindRange = new Measurement<DistanceUnit>(1e7, DistanceUnit.Meter);
                    else
                        nextWindRange = wind[currentWind].MaximumRange.Value;
                }

                if (rangeVector.X >= nextRangeDistance)
                {
                    var windage = rangeVector.Z;
                    if (calculateDrift)
                        windage += new Measurement<DistanceUnit>((1.25 * (stabilityCoefficient + 1.2) * Math.Pow(time.TotalSeconds, 1.83) * (rifle.Rifling.Direction == TwistDirection.Right ? -1 : 1)), DistanceUnit.Inch);

                    trajectoryPoints[currentItem] = new TrajectoryPoint(
                        time: time,
                        
                        distance: rangeVector.X,
                        drop: rangeVector.Y, 
                        windage: windage, 

                        weight: ammunition.Weight,
                        velocity: velocity,
                        
                        mach: velocity / mach
                        );
                    nextRangeDistance += step;
                    currentItem++;
                    if (currentItem == trajectoryPoints.Length)
                        break;
                }

                TimeSpan deltaTime = BallisticMath.TravelTime(calculationStep, velocityVector.X);

                var velocityAdjusted = velocityVector - windVector;
                velocity = velocityAdjusted.Magnitude;
                double currentMach = velocity / mach;

                //find mach node for the first time
                if (dragTableNode == null)
                    dragTableNode = DragTable.Get(ammunition.BallisticCoefficient.Table).Find(currentMach);

                //walk towards the beginning the table as velocity drops
                while (dragTableNode.Mach > currentMach)
                    dragTableNode = dragTableNode.Previous;

                drag = ballisicFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocity.In(VelocityUnit.FeetPerSecond);

                velocityVector = new Vector<VelocityUnit>(
                    velocityVector.X - deltaTime.TotalSeconds * drag * velocityAdjusted.X.To(VelocityUnit.FeetPerSecond),
                    velocityVector.Y - deltaTime.TotalSeconds * drag * velocityAdjusted.Y.To(VelocityUnit.FeetPerSecond)
                                     - new Measurement<VelocityUnit>(deltaTime.TotalSeconds * earthGravity.In(AccelerationUnit.MeterPerSecondSquare),
                                                                     VelocityUnit.MetersPerSecond),
                    velocityVector.Z - deltaTime.TotalSeconds * drag * velocityAdjusted.Z.To(VelocityUnit.FeetPerSecond));


                var deltaRangeVector = new Vector<DistanceUnit>(calculationStep,
                        new Measurement<DistanceUnit>(velocityVector.Y.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter),
                        new Measurement<DistanceUnit>(velocityVector.Z.In(VelocityUnit.MetersPerSecond) * deltaTime.TotalSeconds, DistanceUnit.Meter));

                rangeVector = rangeVector + deltaRangeVector;
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
                step = step / Math.Pow(10, stepOrder - maximumOrder + 1);
            }
            return step;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<VelocityUnit> WindVector(ShotParameters shot, Wind wind)
        {
            double sightCosine = MeasurementMath.Cos(shot.SightAngle);
            double sightSine = MeasurementMath.Sin(shot.SightAngle);
            double cantCosine = MeasurementMath.Cos(shot.CantAngle ?? new Measurement<AngularUnit>(0, AngularUnit.Radian));
            double cantSine = MeasurementMath.Sin(shot.CantAngle ?? new Measurement<AngularUnit>(0, AngularUnit.Radian));
            
            Measurement<VelocityUnit> rangeVelocity, crossComponent;

            if (wind != null)
            {
                rangeVelocity = wind.Velocity * MeasurementMath.Cos(wind.Direction);
                crossComponent = wind.Velocity * MeasurementMath.Sin(wind.Direction);
            }
            else
            {
                rangeVelocity = wind.Velocity;
                crossComponent = new Measurement<VelocityUnit>(0, VelocityUnit.MetersPerSecond);
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
