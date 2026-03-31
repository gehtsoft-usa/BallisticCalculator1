using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BallisticCalculator
{
    public class TrajectoryCalculator1
    {
        public Measurement<DistanceUnit> MaximumCalculationStepSize { get; set; } = new Measurement<DistanceUnit>(0.1, DistanceUnit.Meter);

        public static Measurement<DistanceUnit> MaximumDrop { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<DistanceUnit>(10000, DistanceUnit.Foot);

        public static Measurement<VelocityUnit> MinimumVelocity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<VelocityUnit>(50, VelocityUnit.FeetPerSecond);

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

        #pragma warning disable S3776
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
                while (rangeVector.X <= maximumRange)
                {
                    if (MeasurementMath.Abs(lastAtAltitude - alt) > altDelta)
                    {
                        atmosphere.AtAltitude(alt, out densityFactor, out mach);
                        lastAtAltitude = alt;
                    }

                    if (velocity < MinimumVelocity || rangeVector.Y < -MaximumDrop)
                        break;

                    TimeSpan deltaTime = BallisticMath.TravelTime(calculationStep, velocityVector.X);

                    double currentMach = velocity / mach;

                    dragTableNode ??= dragTable.Find(currentMach);

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

        public TrajectoryPoint[] Calculate(Ammunition ammunition, Rifle rifle, Atmosphere atmosphere, ShotParameters shot, Wind[] wind = null, DragTable dragTable = null)
        {
            Measurement<DistanceUnit> rangeTo = shot.MaximumDistance;
            Measurement<DistanceUnit> step = shot.Step;
            Measurement<DistanceUnit> calculationStep = GetCalculationStep(step);

            atmosphere ??= new Atmosphere();
            dragTable = ValidateDragTable(ammunition, dragTable);

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

            var barrelAzimuth = shot.BarrelAzimuth ?? new Measurement<AngularUnit>(0.0, AngularUnit.Radian);
            var barrelElevation = shot.SightAngle;
            bool hasShotAngle = shot.ShotAngle != null;
            if (hasShotAngle)
                barrelElevation += shot.ShotAngle.Value;
            var lineOfSight = shot.ShotAngle ?? new Measurement<AngularUnit>(0, AngularUnit.Radian);
            double lineOfSightTan = MeasurementMath.Tan(lineOfSight);
            double lineOfDepartureTan = MeasurementMath.Tan(barrelElevation);
            double lineOfSightCos = MeasurementMath.Cos(lineOfSight);
            double lineOfSightSin = MeasurementMath.Sin(lineOfSight);

            //
            // Performance-critical section: all Measurement<T>/Vector<T> operations are replaced
            // with raw doubles to avoid unit-conversion overhead in the hot loop.
            //
            // Unit conventions:
            //   velocity (vx, vy, vz, wx, wy, wz) — in velUnit (muzzle velocity's native unit, e.g. ft/s)
            //   position (rx, ry, rz)              — in meters
            //   altitude (altValue)                 — in altUnit (atmosphere's native unit, e.g. ft)
            //   time (dt)                           — in seconds, truncated to TimeSpan tick precision
            //

            VelocityUnit velUnit = ammunition.MuzzleVelocity.Unit;
            double vel0 = ammunition.MuzzleVelocity.Value;                      // velUnit
            double sightHeightMeters = rifle.Sight.SightHeight.In(DistanceUnit.Meter);
            double calcStepMeters = calculationStep.In(DistanceUnit.Meter);
            double stepMeters = step.In(DistanceUnit.Meter);
            double rangeToMeters = rangeTo.In(DistanceUnit.Meter);

            double barrelElevCos = barrelElevation.Cos();
            double barrelElevSin = barrelElevation.Sin();
            double barrelAzCos = barrelAzimuth.Cos();
            double barrelAzSin = barrelAzimuth.Sin();

            // Velocity vector: x = towards target, y = vertical, z = lateral — in velUnit
            double vx = vel0 * barrelElevCos * barrelAzCos;
            double vy = vel0 * barrelElevSin;
            double vz = vel0 * barrelElevCos * barrelAzSin;

            // Range vector: x = towards target, y = drop, z = windage — in meters
            double rx = 0, ry = -sightHeightMeters, rz = 0;

            // Wind vector — in velUnit
            double wx = 0, wy = 0, wz = 0;
            int currentWind = 0;
            double nextWindRangeMeters = 1e7;

            if (wind != null && wind.Length >= 1)
            {
                if (wind.Length > 1 && wind[0].MaximumRange != null)
                    nextWindRangeMeters = wind[0].MaximumRange.Value.In(DistanceUnit.Meter);
                WindVectorRaw(shot, wind[0], velUnit, out wx, out wy, out wz);
            }

            // Drag: PIR * (velUnit→fps factor) / BC — combines all constant multipliers
            double adjustToFps = Measurement<VelocityUnit>.Convert(1, velUnit, VelocityUnit.FeetPerSecond);
            double ballisticFactor = 1.0 / ammunition.GetBallisticCoefficient();
            double accumulatedFactor = PIR * adjustToFps * ballisticFactor;

            // Gravity acceleration in velUnit per second (e.g. ~32.17 ft/s²)
            double earthGravity = Measurement<VelocityUnit>.Convert(
                Measurement<AccelerationUnit>.Convert(1, AccelerationUnit.EarthGravity, AccelerationUnit.MeterPerSecondSquare),
                VelocityUnit.MetersPerSecond, velUnit);

            double maximumRangeMeters = rangeToMeters + calcStepMeters;
            double maximumDropMeters = MaximumDrop.In(DistanceUnit.Meter);
            double minimumVelocity = MinimumVelocity.In(velUnit);       // velUnit

            // Altitude is tracked in its original unit (e.g. Feet) as a raw double.
            // This preserves the per-step meter→altUnit FP conversion pattern from the
            // original Measurement<T> arithmetic, which is required for 1e-7 accuracy.
            DistanceUnit altUnit = atmosphere.Altitude.Unit;
            double altValue = atmosphere.Altitude.Value;                 // altUnit
            double meterToAltUnit = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Meter, altUnit);
            double altMeters = atmosphere.Altitude.In(DistanceUnit.Meter);  // shadow for fast 1m threshold check
            double lastAtAltMeters = -1e9;

            double distanceMeters = 0;                                  // line-of-sight distance, meters
            double nextRangeDistMeters = 0;                             // next output point distance, meters
            double densityFactor = 0;
            double machInVelUnit = 0;                                   // speed of sound in velUnit

            DragTableNode dragTableNode = null;
            int currentItem = 0;
            TimeSpan time = new TimeSpan(0);

            atmosphere.AtAltitude(new Measurement<DistanceUnit>(altValue, altUnit), out densityFactor, out Measurement<VelocityUnit> machMeasurement);
            machInVelUnit = machMeasurement.In(velUnit);

            // Pre-compute drift constants
            int driftDirection = calculateDrift ? (rifle.Rifling.Direction == TwistDirection.Right ? -1 : 1) : 0;
            double driftFactor = calculateDrift ? 1.25 * (stabilityCoefficient + 1.2) : 0;
            double inchToMeter = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Inch, DistanceUnit.Meter);

            // velUnit→m/s divisor: use division (not multiply by reciprocal) to match
            // Measurement<T>.In(MPS) which internally divides by this constant
            double mpsToVel = Measurement<VelocityUnit>.Convert(1, VelocityUnit.MetersPerSecond, velUnit);

            double velocityMag = Math.Sqrt(vx * vx + vy * vy + vz * vz);

            while (distanceMeters <= maximumRangeMeters)
            {
                if (Math.Abs(altMeters - lastAtAltMeters) > 1.0)
                {
                    atmosphere.AtAltitude(new Measurement<DistanceUnit>(altValue, altUnit), out densityFactor, out machMeasurement);
                    machInVelUnit = machMeasurement.In(velUnit);
                    lastAtAltMeters = altMeters;
                }

                if (velocityMag < minimumVelocity || ry < -maximumDropMeters)
                    break;

                if (rx >= nextWindRangeMeters)
                {
                    currentWind++;
                    WindVectorRaw(shot, wind[currentWind], velUnit, out wx, out wy, out wz);

                    if (currentWind == wind.Length - 1 || wind[currentWind].MaximumRange == null)
                        nextWindRangeMeters = 1e7;
                    else
                        nextWindRangeMeters = wind[currentWind].MaximumRange.Value.In(DistanceUnit.Meter);
                }

                if (distanceMeters >= nextRangeDistMeters)
                {
                    double windage_m = rz;
                    if (calculateDrift)
                        windage_m += driftFactor * Math.Pow(time.TotalSeconds, 1.83) * driftDirection * inchToMeter;

                    var windageMeas = new Measurement<DistanceUnit>(windage_m, DistanceUnit.Meter);
                    var distanceMeas = new Measurement<DistanceUnit>(distanceMeters, DistanceUnit.Meter);

                    double drop_m = ry;
                    if (hasShotAngle)
                    {
                        double y = ry + sightHeightMeters;
                        double y_rotated = -rx * lineOfSightSin + y * lineOfSightCos;
                        drop_m = y_rotated - sightHeightMeters;
                    }

                    var dropMeas = new Measurement<DistanceUnit>(drop_m, DistanceUnit.Meter);
                    var dropAdjustment = BallisticMath.CalculateAdjustment(dropMeas, distanceMeas);
                    var windageAdjustment = BallisticMath.CalculateAdjustment(windageMeas, distanceMeas);

                    var velocityOut = new Measurement<VelocityUnit>(velocityMag, velUnit);

                    trajectoryPoints[currentItem] = new TrajectoryPoint(
                        time: time,
                        distance: distanceMeas,
                        distanceFlat: new Measurement<DistanceUnit>(rx, DistanceUnit.Meter),
                        velocity: velocityOut,
                        mach: velocityMag / machInVelUnit,
                        drop: dropMeas,
                        dropFlat: new Measurement<DistanceUnit>(ry, DistanceUnit.Meter),
                        dropAdjustment: dropAdjustment,
                        windageAdjustment: windageAdjustment,
                        lineOfSightElevation: new Measurement<DistanceUnit>(rx * lineOfSightTan, DistanceUnit.Meter),
                        lineOfDepartureElevation: new Measurement<DistanceUnit>(rx * lineOfDepartureTan - sightHeightMeters, DistanceUnit.Meter),
                        windage: windageMeas,
                        energy: MeasurementMath.KineticEnergy(ammunition.Weight, velocityOut),
                        optimalGameWeight: BallisticMath.OptimalGameWeight(ammunition.Weight, velocityOut));

                    nextRangeDistMeters += stepMeters;
                    currentItem++;
                    if (currentItem == trajectoryPoints.Length)
                        break;
                }

                // --- Physics integration step ---

                // dt must go through TimeSpan to match the original's tick-precision truncation
                double dt = TimeSpan.FromSeconds(calcStepMeters / (vx / mpsToVel)).TotalSeconds;

                // Wind-adjusted velocity vector (velUnit)
                double vax = vx - wx;
                double vay = vy - wy;
                double vaz = vz - wz;

                // Drag lookup by Mach number
                double velocityAdj = Math.Sqrt(vax * vax + vay * vay + vaz * vaz);
                double currentMach = velocityAdj / machInVelUnit;

                dragTableNode ??= dragTable.Find(currentMach);

                while (dragTableNode.Mach > currentMach)
                    dragTableNode = dragTableNode.Previous;

                // Apply drag deceleration and gravity
                double drag = accumulatedFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocityAdj;
                double factor = dt * drag;

                vx = vx - factor * vax;                                 // velUnit
                vy = vy - factor * vay - earthGravity * dt;             // velUnit (gravity in velUnit/s)
                vz = vz - factor * vaz;                                 // velUnit

                // Position delta: convert velocity to m/s via division, then multiply by dt
                double drx = vx / mpsToVel * dt;                       // meters
                double dry = vy / mpsToVel * dt;                       // meters
                double drz = vz / mpsToVel * dt;                       // meters

                rx += drx;
                ry += dry;
                rz += drz;

                distanceMeters = rx / lineOfSightCos;
                altValue += dry * meterToAltUnit;                       // altUnit (matches Measurement operator+ FP path)
                altMeters += dry;                                       // meters (shadow for threshold check)

                velocityMag = Math.Sqrt(vx * vx + vy * vy + vz * vz);  // velUnit (reused at top of next iteration)
                double drMag = Math.Sqrt(drx * drx + dry * dry + drz * drz);
                time = time.Add(TimeSpan.FromSeconds(drMag / (velocityMag / mpsToVel)));
            }

            return trajectoryPoints;
        }
        #pragma warning restore S3776

        internal Measurement<DistanceUnit> GetCalculationStep(Measurement<DistanceUnit> step)
        {
            step /= 2;
            if (step > MaximumCalculationStepSize)
            {
                int stepOrder = (int)Math.Floor(Math.Log10(step.Value));
                int maximumOrder = (int)Math.Floor(Math.Log10(MaximumCalculationStepSize.In(step.Unit)));
                step /= Math.Pow(10, stepOrder - maximumOrder + 1);
            }
            return step;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WindVectorRaw(ShotParameters shot, Wind wind, VelocityUnit units, out double wx, out double wy, out double wz)
        {
            var shotAngle = shot.SightAngle;
            if (shot.ShotAngle != null)
                shotAngle += shot.ShotAngle.Value;

            double sightCosine = shotAngle.Cos();
            double sightSine = shotAngle.Sin();
            double cantCosine = (shot.CantAngle ?? AngularUnit.Radian.New(0)).Cos();
            double cantSine = (shot.CantAngle ?? AngularUnit.Radian.New(0)).Sin();

            double rangeVelocity, crossComponent;

            if (wind != null)
            {
                rangeVelocity = (wind.Velocity * wind.Direction.Cos()).In(units);
                crossComponent = (wind.Velocity * wind.Direction.Sin()).In(units);
            }
            else
            {
                rangeVelocity = 0;
                crossComponent = 0;
            }

            double rangeFactor = -rangeVelocity * sightSine;

            wx = rangeVelocity * sightCosine;
            wy = rangeFactor * cantCosine + crossComponent * cantSine;
            wz = crossComponent * cantCosine - rangeFactor * cantSine;
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
