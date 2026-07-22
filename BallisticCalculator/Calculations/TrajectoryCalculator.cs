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

        //sonar: disable cognitive complexity warning. the suggested simplification will affect performance
        #pragma warning disable S3776
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

            // Pre-compute all conversion factors outside the approximation loop
            VelocityUnit velUnit = ammunition.MuzzleVelocity.Unit;
            double vel0 = ammunition.MuzzleVelocity.Value;                      // velUnit
            double sightHeightMeters = rifle.Sight.SightHeight.In(DistanceUnit.Meter);
            double calcStepMeters = calculationStep.In(DistanceUnit.Meter);
            double maximumRangeMeters = rangeTo.In(DistanceUnit.Meter);
            double zeroDistanceMeters = rifle.Zero.Distance.In(DistanceUnit.Meter);
            double verticalOffsetMeters = ((rifle.Zero.VerticalOffset) ?? Measurement<DistanceUnit>.ZERO).In(DistanceUnit.Meter);
            double accuracyMillimeters = accuracy.Value.In(DistanceUnit.Millimeter);

            double maximumDropMeters = MaximumDrop.In(DistanceUnit.Meter);
            double minimumVelocity = MinimumVelocity.In(velUnit);

            // velUnit->m/s divisor: use division (not multiply by reciprocal) to match
            // Measurement<T>.In(MPS) which internally divides by this constant
            double mpsToVel = Measurement<VelocityUnit>.Convert(1, VelocityUnit.MetersPerSecond, velUnit);

            // Altitude tracked in native unit for FP accuracy
            DistanceUnit altUnit = atmosphere.Altitude.Unit;
            double alt0Value = atmosphere.Altitude.Value;                        // altUnit
            double meterToAltUnit = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Meter, altUnit);
            double alt0Meters = atmosphere.Altitude.In(DistanceUnit.Meter);

            // Drag factor
            double adjustToFps = Measurement<VelocityUnit>.Convert(1, velUnit, VelocityUnit.FeetPerSecond);
            double ballisticFactor = 1.0 / ammunition.GetBallisticCoefficient();
            double accumulatedFactor = PIR * adjustToFps * ballisticFactor;

            // Gravity in velUnit per second
            double earthGravity = Measurement<VelocityUnit>.Convert(
                Measurement<AccelerationUnit>.Convert(1, AccelerationUnit.EarthGravity, AccelerationUnit.MeterPerSecondSquare),
                VelocityUnit.MetersPerSecond, velUnit);

            // Conversion: meters -> millimeters for accuracy check
            double meterToMm = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Meter, DistanceUnit.Millimeter);
            // Conversion: meters -> centimeters for angle adjustment
            double meterToCm = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Meter, DistanceUnit.Centimeter);

            var sightAngle = new Measurement<AngularUnit>(150, AngularUnit.MOA);

            for (int approximation = 0; approximation < 100; approximation++)
            {
                double barrelElevCos = sightAngle.Cos();
                double barrelElevSin = sightAngle.Sin();

                // Velocity vector in velUnit; barrelAzimuth=0 so cos=1, sin=0, vz=0
                double vx = vel0 * barrelElevCos;
                double vy = vel0 * barrelElevSin;

                // Range vector in meters
                double rx = 0, ry = -sightHeightMeters;

                // Altitude tracking
                double altValue = alt0Value;                                     // altUnit
                double altMeters = alt0Meters;                                   // meters (shadow for threshold)
                double lastAtAltMeters = -1e9;

                double densityFactor = 0;
                double machInVelUnit = 0;
                DragTableNode dragTableNode = null;

                while (rx <= maximumRangeMeters)
                {
                    if (Math.Abs(altMeters - lastAtAltMeters) > 1.0)
                    {
                        atmosphere.AtAltitude(new Measurement<DistanceUnit>(altValue, altUnit), out densityFactor, out Measurement<VelocityUnit> machMeasurement);
                        machInVelUnit = machMeasurement.In(velUnit);
                        lastAtAltMeters = altMeters;
                    }

                    double velocityMag = Math.Sqrt(vx * vx + vy * vy);
                    if (velocityMag < minimumVelocity || ry < -maximumDropMeters)
                        break;

                    // dt must go through TimeSpan for tick-precision truncation
                    double dt = TimeSpan.FromSeconds(calcStepMeters / (vx / mpsToVel)).TotalSeconds;

                    double currentMach = velocityMag / machInVelUnit;

                    dragTableNode ??= dragTable.Find(currentMach);

                    while (dragTableNode.Mach > currentMach)
                        dragTableNode = dragTableNode.Previous;
                    while (dragTableNode.Next != null && dragTableNode.Next.Mach <= currentMach)
                        dragTableNode = dragTableNode.Next;

                    double drag = accumulatedFactor * densityFactor * dragTableNode.CalculateDrag(currentMach) * velocityMag;
                    double factor = dt * drag;

                    vx = vx - factor * vx;
                    vy = vy - factor * vy - earthGravity * dt;

                    double drx = vx / mpsToVel * dt;                            // meters
                    double dry = vy / mpsToVel * dt;                            // meters

                    rx += drx;
                    ry += dry;

                    altValue += dry * meterToAltUnit;                            // altUnit
                    altMeters += dry;                                            // meters

                    if (rx >= zeroDistanceMeters)
                    {
                        double matchMeters = ry - verticalOffsetMeters;
                        if (Math.Abs(matchMeters * meterToMm) < accuracyMillimeters)
                            return sightAngle;

                        sightAngle += new Measurement<AngularUnit>(-matchMeters * meterToCm / zeroDistanceMeters * 100, AngularUnit.CmPer100Meters);
                        break;
                    }
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
            double barrelAzSin = barrelAzimuth.Sin();

            // Velocity vector: x = towards target, y = vertical, z = lateral — in velUnit.
            // Azimuth no longer tilts the muzzle vector into z: the bullet is always integrated
            // along x, and BarrelAzimuth becomes a pure scalar into the Coriolis terms (see the
            // Coriolis block below). This also removes the old azimuth-90 divide-by-zero, where
            // vx (and thus dt = step/vx) collapsed when the line of fire pointed along z
            // (CORIOLIS.md §3).
            double vx = vel0 * barrelElevCos;
            double vy = vel0 * barrelElevSin;
            double vz = 0;

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

            // Drag: PIR * (velUnit->fps factor) / BC — combines all constant multipliers
            double adjustToFps = Measurement<VelocityUnit>.Convert(1, velUnit, VelocityUnit.FeetPerSecond);
            double ballisticFactor = 1.0 / ammunition.GetBallisticCoefficient();
            double accumulatedFactor = PIR * adjustToFps * ballisticFactor;

            // Gravity acceleration in velUnit per second (e.g. ~32.17 ft/s^2)
            double earthGravity = Measurement<VelocityUnit>.Convert(
                Measurement<AccelerationUnit>.Convert(1, AccelerationUnit.EarthGravity, AccelerationUnit.MeterPerSecondSquare),
                VelocityUnit.MetersPerSecond, velUnit);

            double maximumRangeMeters = rangeToMeters + calcStepMeters / lineOfSightCos;
            double maximumDropMeters = MaximumDrop.In(DistanceUnit.Meter);
            double minimumVelocity = MinimumVelocity.In(velUnit);       // velUnit

            // Altitude is tracked in its original unit (e.g. Feet) as a raw double.
            // This preserves the per-step meter->altUnit FP conversion pattern from the
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
            double timeSeconds = 0;     // accumulated flight time in seconds (double; converted to TimeSpan at output)

            atmosphere.AtAltitude(new Measurement<DistanceUnit>(altValue, altUnit), out densityFactor, out Measurement<VelocityUnit> machMeasurement);
            machInVelUnit = machMeasurement.In(velUnit);

            // Pre-compute drift constants
            int driftDirection = 0;
            if (calculateDrift)
                driftDirection = rifle.Rifling.Direction == TwistDirection.Right ? -1 : 1;
            double driftFactor = calculateDrift ? 1.25 * (stabilityCoefficient + 1.2) : 0;
            double inchToMeter = Measurement<DistanceUnit>.Convert(1, DistanceUnit.Inch, DistanceUnit.Meter);

            // Pre-compute Coriolis / Eötvös constants (CORIOLIS.md §4/§5.2). Earth rotation is
            // applied as per-output-point closed-form corrections that match the field references
            // (Ballistic Explorer + Kestrel), not as a per-step -2Ω×v integration — so it does not
            // perturb time-of-flight, velocity, or range.
            bool coriolis = shot.Latitude != null;
            const double coriolisOmega = 7.2921159e-5;                  // Earth rotation, rad/s
            double sinLat = coriolis ? shot.Latitude.Value.Sin() : 0;
            double cosLat = coriolis ? shot.Latitude.Value.Cos() : 0;
            // Horizontal (windage) coefficient: Δwindage_m = coriolisHCoef * Range_m * TOF_s,
            // deflecting right in the N hemisphere (sign carried by sin(latitude)); azimuth-independent.
            double coriolisHCoef = coriolisOmega * sinLat;
            // Vertical Eötvös as a gravity ratio g_eff/g (dimensionless, constant per shot):
            // East (sin az > 0) lifts the bullet (less drop), West lowers it (more drop).
            double v0mps = ammunition.MuzzleVelocity.In(VelocityUnit.MetersPerSecond);
            double coriolisVRatio = coriolis
                ? 1.0 - 2.0 * coriolisOmega * cosLat * barrelAzSin * v0mps / 9.80665
                : 1.0;

            // Pre-compute aerodynamic (crosswind) jump — Litz, Applied Ballistics Eq 5.4
            // (CLAUDE/AERO_JUMP.md). A horizontal crosswind gives a spin-stabilized bullet a
            // vertical come-up imparted at the muzzle, then constant in angle at all ranges:
            //   Y[MOA per mph] = 0.01*Sg - 0.0024*L_calibers + 0.032
            // Gated exactly like spin drift (needs Sg + twist + bullet dims). Up for a wind from
            // the right with a right-hand twist (sign flips for left twist / wind from the left).
            double aeroJumpAngleRad = 0;
            if (calculateDrift && wind != null && wind.Length >= 1)
            {
                double lengthCalibers = ammunition.BulletLength.Value.In(DistanceUnit.Inch)
                                      / ammunition.BulletDiameter.Value.In(DistanceUnit.Inch);
                double aeroJumpMoaPerMph = 0.01 * stabilityCoefficient - 0.0024 * lengthCalibers + 0.032;
                // Muzzle crosswind, positive = from the right (wind Direction 90°); the jump is
                // imparted at the muzzle, so use the first wind zone.
                double crossFromRightMph = (wind[0].Velocity * wind[0].Direction.Sin()).In(VelocityUnit.MilesPerHour);
                int verticalJumpSign = rifle.Rifling.Direction == TwistDirection.Right ? 1 : -1;
                double aeroJumpMoa = aeroJumpMoaPerMph * crossFromRightMph * verticalJumpSign;
                aeroJumpAngleRad = Measurement<AngularUnit>.Convert(aeroJumpMoa, AngularUnit.MOA, AngularUnit.Radian);
            }

            // velUnit->m/s divisor: use division (not multiply by reciprocal) to match
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
                        windage_m += driftFactor * Math.Pow(timeSeconds, 1.83) * driftDirection * inchToMeter * lineOfSightCos;

                    var distanceMeas = new Measurement<DistanceUnit>(distanceMeters, DistanceUnit.Meter);

                    // Effective vertical position. Coriolis/Eötvös modifies gravity, which acts only
                    // on the *gravitational fall* — the deviation below the vacuum (no-gravity) bore
                    // line — not on the launch/sight geometry. So scale the fall by g_eff/g and
                    // rebuild, leaving the bore line (hence the exact −sightHeight muzzle drop where
                    // fall = 0) untouched. Integrator state (ry) is never mutated. CORIOLIS.md §4/§5.2.
                    double ryEffective = ry;
                    if (coriolis)
                    {
                        // Horizontal deflects right in the N hemisphere; our Windage is left +,
                        // right − ⇒ subtract. Azimuth-independent (∝ sin φ only).
                        windage_m -= coriolisHCoef * distanceMeters * timeSeconds;
                        // Vacuum bore line at rx (== LineOfDepartureElevation): the trajectory with no gravity.
                        double boreLine = rx * lineOfDepartureTan - sightHeightMeters;
                        ryEffective = boreLine + (ry - boreLine) * coriolisVRatio;
                    }

                    double dropFlat_m = ryEffective;    // drop vs the muzzle (vertical frame)
                    double drop_m = ryEffective;        // drop vs the line of sight (perpendicular)
                    if (hasShotAngle)
                    {
                        // Drop for inclined fire is measured perpendicular to the line of sight.
                        // The sightHeight is added in the vertical frame but the final subtraction
                        // is in the rotated frame; the resulting sightHeight*(cos-1) term is
                        // intentional: it pins the muzzle drop to exactly -sightHeight (matching the
                        // reference model), while every other point uses the perpendicular rotation.
                        // Verified best-of-3 conventions against the reference (a7): dropping this
                        // term worsens accuracy and breaks the exact muzzle match. Do not "simplify".
                        double y = ryEffective + sightHeightMeters;
                        double y_rotated = -rx * lineOfSightSin + y * lineOfSightCos;
                        drop_m = y_rotated - sightHeightMeters;
                    }

                    // Aerodynamic (crosswind) jump: a constant angle ⇒ a vertical offset linear in
                    // range (positive = up ⇒ less drop). Independent of and additive to the Eötvös
                    // scaling above and to spin drift (which is horizontal). CLAUDE/AERO_JUMP.md.
                    if (aeroJumpAngleRad != 0)
                    {
                        drop_m += aeroJumpAngleRad * distanceMeters;
                        dropFlat_m += aeroJumpAngleRad * rx;
                    }

                    var windageMeas = new Measurement<DistanceUnit>(windage_m, DistanceUnit.Meter);
                    var dropMeas = new Measurement<DistanceUnit>(drop_m, DistanceUnit.Meter);
                    var dropAdjustment = BallisticMath.CalculateAdjustment(dropMeas, distanceMeas);
                    var windageAdjustment = BallisticMath.CalculateAdjustment(windageMeas, distanceMeas);

                    var velocityOut = new Measurement<VelocityUnit>(velocityMag, velUnit);

                    trajectoryPoints[currentItem] = new TrajectoryPoint(
                        time: TimeSpan.FromSeconds(timeSeconds),
                        distance: distanceMeas,
                        distanceFlat: new Measurement<DistanceUnit>(rx, DistanceUnit.Meter),
                        velocity: velocityOut,
                        mach: velocityMag / machInVelUnit,
                        drop: dropMeas,
                        dropFlat: new Measurement<DistanceUnit>(dropFlat_m, DistanceUnit.Meter),
                        dropAdjustment: dropAdjustment,
                        windageAdjustment: windageAdjustment,
                        lineOfSightElevation: new Measurement<DistanceUnit>(rx * lineOfSightTan, DistanceUnit.Meter),
                        lineOfDepartureElevation: new Measurement<DistanceUnit>(rx * lineOfDepartureTan - sightHeightMeters, DistanceUnit.Meter),
                        windage: windageMeas,
                        energy: MeasurementMath.KineticEnergy(ammunition.Weight, velocityOut),
                        optimalGameWeight: BallisticMath.OptimalGameWeight(ammunition.Weight, velocityOut),
                        // Downrange gyroscopic stability: Sg grows as (v0/v)^1.25 because spin decays
                        // more slowly than velocity (PLAN0 b5). Null unless drift/stability inputs exist.
                        gyroscopicStability: calculateDrift
                            ? stabilityCoefficient * Math.Pow(vel0 / velocityMag, 1.25)
                            : (double?)null);

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
                while (dragTableNode.Next != null && dragTableNode.Next.Mach <= currentMach)
                    dragTableNode = dragTableNode.Next;

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
                timeSeconds += dt;
            }

            return trajectoryPoints;
        }
        #pragma warning restore S3776

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
