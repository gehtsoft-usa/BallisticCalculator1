using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BallisticCalculator
{
    /// <summary>
    /// Numerical integration scheme used when calculating a trajectory.
    /// </summary>
    public enum IntegrationMethod
    {
        /// <summary>Semi-implicit (symplectic) Euler, the original scheme.</summary>
        Euler,
        /// <summary>Midpoint (second-order Runge-Kutta): two drag evaluations per step, supporting a coarser step.</summary>
        MidpointRK2,
    }

    /// <summary>
    /// <para>The calculator for the projectile trajectory.</para>
    /// <para>Thread safety: a single instance is safe for concurrent use from multiple threads,
    /// provided its configuration ([clink=BallisticCalculator.TrajectoryCalculator.Integrator.5vC]Integrator[/clink]
    /// and [clink=BallisticCalculator.TrajectoryCalculator.MaximumCalculationStepSize.fdE]MaximumCalculationStepSize[/clink])
    /// is not changed while calculations are running. Configure the instance once, then share it:
    /// the calculation methods keep no per-call state on the instance, all inputs are treated as
    /// read-only, and the standard drag tables are cached as thread-safe singletons. To vary the
    /// configuration per thread, use a separate instance per thread instead.</para>
    /// </summary>
    public class TrajectoryCalculator
    {
        /// <summary>
        /// <para>The maximum step size of the calculation.</para>
        /// <para>The default value is 1 m. Paired with the default midpoint integrator this runs about
        /// ten times coarser than the historical 10 cm Euler cap at equal accuracy. Set
        /// [clink=BallisticCalculator.TrajectoryCalculator.Integrator.5vC]Integrator[/clink] to
        /// [clink=BallisticCalculator.IntegrationMethod.Euler.75B]Euler[/clink] if you lower this for a fine Euler run.</para>
        /// </summary>
        public Measurement<DistanceUnit> MaximumCalculationStepSize { get; set; } = new Measurement<DistanceUnit>(1.0, DistanceUnit.Meter);

        /// <summary>
        /// <para>Integration scheme used when calculating a trajectory, and by the zeroing solve that drives it.</para>
        /// <para>Defaults to [clink=BallisticCalculator.IntegrationMethod.MidpointRK2.6S7]MidpointRK2[/clink]:
        /// two drag evaluations per step buy second-order accuracy, so the step can be about ten times
        /// coarser for a net speedup at equal accuracy. Use
        /// [clink=BallisticCalculator.IntegrationMethod.Euler.75B]Euler[/clink] for the historical
        /// semi-implicit scheme.</para>
        /// </summary>
        public IntegrationMethod Integrator { get; set; } = IntegrationMethod.MidpointRK2;

        /// <summary>
        /// The maximum drop value to stop further calculation
        /// </summary>
        public static Measurement<DistanceUnit> MaximumDrop { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<DistanceUnit>(10000, DistanceUnit.Foot);

        /// <summary>
        /// The minimum velocity to stop the calculation
        /// </summary>
        public static Measurement<VelocityUnit> MinimumVelocity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = new Measurement<VelocityUnit>(50, VelocityUnit.FeetPerSecond);

        /// <summary>
        /// PIR = (PI/8)*(RHO0/144). Internal so drag-model tools (radar curve derivation) can invert the same drag law.
        /// </summary>
        internal const double PIR = 2.08551e-04;

        /// <summary>
        /// Line-of-sight tolerance (m) for emitting an output row: a step is shortened to land on
        /// the requested range, and this lets the row emit when it lands a hair short (velocity
        /// decay within the shortened step). 1 mm — far below any output tolerance, negligible at
        /// the fine step the historical engine used.
        /// </summary>
        private const double RangeEmitEpsilonMeters = 1e-3;

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
        /// <para>Calculates the vertical and horizontal barrel adjustments that zero the rifle at the zero distance.</para>
        /// <para>The impact is placed on the aim point, offset by the zeroing [c]VerticalOffset[/c] and
        /// [c]HorizontalOffset[/c] when set. This drives the full
        /// [clink=BallisticCalculator.TrajectoryCalculator.Calculate.LP7]Calculate[/clink] trajectory, so spin
        /// drift, Coriolis and aerodynamic jump are all accounted for in the zero.</para>
        /// <para>Wind is optional: pass [c]wind[/c] to zero under a known condition, folding in its deflection
        /// and crosswind aerodynamic jump, or omit it to zero in calm air. The optional [c]shot[/c] contributes
        /// only its shot angle, azimuth and latitude.</para>
        /// </summary>
        /// <param name="ammunition">Ammunition (overridden by the zeroing ammunition when set).</param>
        /// <param name="atmosphere">Atmosphere (overridden by the zeroing atmosphere when set).</param>
        /// <param name="rifle">The rifle (sight height and rifling).</param>
        /// <param name="zero">The zeroing parameters (distance and impact offsets).</param>
        /// <param name="shot">Optional shot parameters; only the shot angle, azimuth and latitude are used.</param>
        /// <param name="wind">Optional wind at zeroing; when not set, zeroing is done in calm air.</param>
        /// <param name="dragTable">Custom drag table (required when the ballistic coefficient table is GC).</param>
        /// <param name="accuracy">Solve accuracy; default 0.1 mm.</param>
        /// <returns>The calculated zeroing adjustments, ready for the shot's [clink=BallisticCalculator.ShotParameters.Apply.JkC]Apply[/clink] method.</returns>
        /// <exception cref="InvalidOperationException">The projectile cannot reach the zero distance, or the solve did not converge.</exception>
        public ZeroCalculatedParameters CalculateZeroParameters(Ammunition ammunition, Atmosphere atmosphere, Rifle rifle, ZeroingParameters zero, ShotParameters shot = null, Wind[] wind = null, DragTable dragTable = null, Measurement<DistanceUnit>? accuracy = null)
        {
            ArgumentNullException.ThrowIfNull(ammunition);
            ArgumentNullException.ThrowIfNull(rifle);
            ArgumentNullException.ThrowIfNull(zero);

            var zeroAmmunition = zero.Ammunition ?? ammunition;
            var zeroAtmosphere = zero.Atmosphere ?? atmosphere ?? new Atmosphere();
            double accuracyMillimeters = (accuracy ?? new Measurement<DistanceUnit>(0.1, DistanceUnit.Millimeter)).In(DistanceUnit.Millimeter);

            var verticalTarget = zero.VerticalOffset ?? Measurement<DistanceUnit>.ZERO;
            var horizontalTarget = zero.HorizontalOffset ?? Measurement<DistanceUnit>.ZERO;
            double zeroDistanceMeters = zero.Distance.In(DistanceUnit.Meter);

            // Single output row exactly at the zero distance (the range clamp lands it there).
            var solveShot = new ShotParameters
            {
                Step = zero.Distance,
                MaximumDistance = zero.Distance,
                ZeroDropAdjustment = Measurement<AngularUnit>.ZERO,
                ShotAngle = shot?.ShotAngle,
                BarrelAzimuth = shot?.BarrelAzimuth,
                Latitude = shot?.Latitude,
            };

            var dropAdjustment = Measurement<AngularUnit>.ZERO;
            Measurement<AngularUnit>? windageAdjustment = null;   // stays null when there is no horizontal effect to correct

            for (int approximation = 0; approximation < 100; approximation++)
            {
                solveShot.ZeroDropAdjustment = dropAdjustment;
                solveShot.ZeroWindageAdjustment = windageAdjustment;

                // Full trajectory — spin drift, Coriolis, wind deflection and aero jump are applied by Calculate.
                var trajectory = Calculate(zeroAmmunition, rifle, zeroAtmosphere, solveShot, wind, dragTable);

                TrajectoryPoint impact = null;
                for (int k = trajectory.Length - 1; k >= 0; k--)
                {
                    if (trajectory[k] != null)
                    {
                        impact = trajectory[k];
                        break;
                    }
                }
                if (impact == null || impact.Distance.In(DistanceUnit.Meter) < zeroDistanceMeters - 0.01)
                    throw new InvalidOperationException("The projectile cannot reach the zero distance");

                // Miss at the zero distance: +vertical ⇒ impact below target ⇒ raise; +horizontal ⇒
                // impact right of target (windage is left +) ⇒ tilt left. Newton step: the linear miss
                // over the zero distance is (to first order) the exact angular correction.
                var verticalMiss = verticalTarget - impact.Drop;
                var horizontalMiss = horizontalTarget - impact.Windage;

                bool verticalOk = verticalMiss.Abs().In(DistanceUnit.Millimeter) < accuracyMillimeters;
                bool horizontalOk = horizontalMiss.Abs().In(DistanceUnit.Millimeter) < accuracyMillimeters;
                if (verticalOk && horizontalOk)
                    return new ZeroCalculatedParameters(dropAdjustment, windageAdjustment);

                if (!verticalOk)
                    dropAdjustment += BallisticMath.CalculateAdjustment(verticalMiss, zero.Distance);
                if (!horizontalOk)
                    windageAdjustment = (windageAdjustment ?? Measurement<AngularUnit>.ZERO) + BallisticMath.CalculateAdjustment(horizontalMiss, zero.Distance);
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

            // Accumulate the dialed sight settings into the initial barrel orientation.
            // Vertical: zero elevation + per-shot elevation clicks + line-of-sight incline.
            var barrelElevation = shot.ZeroDropAdjustment;
            if (shot.ShotDropAdjustment != null)
                barrelElevation += shot.ShotDropAdjustment.Value;
            bool hasShotAngle = shot.ShotAngle != null;
            if (hasShotAngle)
                barrelElevation += shot.ShotAngle.Value;

            // Horizontal: zero windage + per-shot windage clicks. Positive tilts the barrel left
            // (seeds +vz below), producing positive (left) Windage — so a +N windage adjustment
            // cancels a −N (right) drift, mirroring how the vertical elevation cancels drop.
            // Zero/absent ⇒ windageSin == 0 ⇒ vz stays 0 ⇒ bit-identical to the pre-windage engine.
            var windageAngle = shot.ZeroWindageAdjustment ?? new Measurement<AngularUnit>(0, AngularUnit.Radian);
            if (shot.ShotWindageAdjustment != null)
                windageAngle += shot.ShotWindageAdjustment.Value;

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
            double windageCos = windageAngle.Cos();
            double windageSin = windageAngle.Sin();

            // Velocity vector: x = towards target, y = vertical, z = lateral — in velUnit.
            // Elevation raises vy; the dialed windage angle rotates the (vx, vz) plane so a left
            // tilt (positive windageAngle) seeds +vz. BarrelAzimuth (compass bearing) still does
            // NOT tilt the muzzle vector — the bullet is integrated along x and azimuth is a pure
            // scalar into the Coriolis terms (CORIOLIS.md §3); that also avoids the old azimuth-90
            // divide-by-zero where vx (and dt = step/vx) collapsed along z. With no windage dialed
            // (windageSin == 0) vz stays 0 and the seed is bit-identical to the pre-windage engine.
            double vx = vel0 * barrelElevCos * windageCos;
            double vy = vel0 * barrelElevSin;
            double vz = vel0 * barrelElevCos * windageSin;

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

                if (distanceMeters >= nextRangeDistMeters - RangeEmitEpsilonMeters)
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

                // Shorten only the sub-step that would cross the next output range so the row is
                // emitted (near-)exactly at the requested distance rather than up to one coarse
                // step past it — the coarse step would otherwise break the "point at the requested
                // range" output contract (and inflate that row's drop). Between output points the
                // full coarse step is used, so this costs ~one short step per row. Harmless at the
                // fine step the historical engine used (the crossing sub-step was already short).
                double effectiveCalcStep = calcStepMeters;
                double stepToNextRange = (nextRangeDistMeters - distanceMeters) * lineOfSightCos;
                if (stepToNextRange > 0 && stepToNextRange < effectiveCalcStep)
                    effectiveCalcStep = stepToNextRange;

                // dt must go through TimeSpan to match the original's tick-precision truncation.
                // Derived from the current vx so the along-bore advance per step ≈ effectiveCalcStep
                // for both integrators.
                double dt = TimeSpan.FromSeconds(effectiveCalcStep / (vx / mpsToVel)).TotalSeconds;

                double dry;
                if (Integrator == IntegrationMethod.Euler)
                {
                    // Semi-implicit (symplectic) Euler — position uses the just-updated velocity.
                    // Wind-adjusted velocity vector (velUnit)
                    double vax = vx - wx;
                    double vay = vy - wy;
                    double vaz = vz - wz;

                    // Drag lookup by Mach number
                    double velocityAdj = Math.Sqrt(vax * vax + vay * vay + vaz * vaz);
                    double currentMach = velocityAdj / machInVelUnit;

                    dragTableNode ??= dragTable.Find(currentMach);

                    // Clamp to the end nodes if the Mach is outside the table's range (custom/drg tables
                    // do not span down to Mach 0 like the standard curves). Bit-identical for standard
                    // tables, whose floor node is Mach 0, so the guard never fires.
                    while (dragTableNode.Previous != null && dragTableNode.Mach > currentMach)
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
                    dry = vy / mpsToVel * dt;                              // meters
                    double drz = vz / mpsToVel * dt;                       // meters

                    rx += drx;
                    ry += dry;
                    rz += drz;
                }
                else
                {
                    // Midpoint (2nd-order Runge-Kutta): evaluate the drag+gravity acceleration at
                    // the start (a1), step the velocity a half-dt to the midpoint, re-evaluate (a2),
                    // then advance velocity by a2·dt and position by the midpoint velocity·dt. Two
                    // drag evaluations per step buy 2nd-order accuracy, so the step can be ~10×
                    // coarser at equal error. Density/Mach are held at the loop-top altitude (they
                    // change negligibly across one step). PLAN1 §1.1.
                    Acceleration(vx, vy, vz, wx, wy, wz, accumulatedFactor, densityFactor,
                        machInVelUnit, earthGravity, dragTable, ref dragTableNode,
                        out double a1x, out double a1y, out double a1z);

                    double half = dt * 0.5;
                    double vmx = vx + a1x * half;
                    double vmy = vy + a1y * half;
                    double vmz = vz + a1z * half;

                    Acceleration(vmx, vmy, vmz, wx, wy, wz, accumulatedFactor, densityFactor,
                        machInVelUnit, earthGravity, dragTable, ref dragTableNode,
                        out double a2x, out double a2y, out double a2z);

                    vx += a2x * dt;                                        // velUnit
                    vy += a2y * dt;                                        // velUnit
                    vz += a2z * dt;                                        // velUnit

                    // Position advances by the midpoint velocity (the RK2 slope for r' = v).
                    rx += vmx / mpsToVel * dt;                             // meters
                    dry = vmy / mpsToVel * dt;                             // meters
                    rz += vmz / mpsToVel * dt;                             // meters
                    ry += dry;
                }

                distanceMeters = rx / lineOfSightCos;
                altValue += dry * meterToAltUnit;                       // altUnit (matches Measurement operator+ FP path)
                altMeters += dry;                                       // meters (shadow for threshold check)

                velocityMag = Math.Sqrt(vx * vx + vy * vy + vz * vz);  // velUnit (reused at top of next iteration)
                timeSeconds += dt;
            }

            return trajectoryPoints;
        }
        #pragma warning restore S3776

        internal Measurement<DistanceUnit> GetCalculationStep(Measurement<DistanceUnit> step) =>
            GetCalculationStep(step, MaximumCalculationStepSize);

        internal static Measurement<DistanceUnit> GetCalculationStep(Measurement<DistanceUnit> step, Measurement<DistanceUnit> maximumStep)
        {
            step /= 2;      //do it twice for increased accuracy of velocity calculation and 10 times per step
            if (step > maximumStep)
            {
                int stepOrder = (int)Math.Floor(Math.Log10(step.Value));
                int maximumOrder = (int)Math.Floor(Math.Log10(maximumStep.In(step.Unit)));
                step /= Math.Pow(10, stepOrder - maximumOrder + 1);
            }
            return step;
        }

        /// <summary>
        /// Drag + gravity acceleration (velUnit per second) at a given velocity, walking the shared
        /// drag node to the air-relative Mach. Used by the midpoint (RK2) integrator, which needs
        /// the acceleration evaluated at two velocities per step. Density and speed-of-sound are
        /// passed in (held at the loop-top altitude, as for Euler).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Acceleration(double vx, double vy, double vz,
            double wx, double wy, double wz,
            double accumulatedFactor, double densityFactor, double machInVelUnit,
            double earthGravity, DragTable dragTable, ref DragTableNode node,
            out double ax, out double ay, out double az)
        {
            double vax = vx - wx;
            double vay = vy - wy;
            double vaz = vz - wz;

            double velocityAdj = Math.Sqrt(vax * vax + vay * vay + vaz * vaz);
            double mach = velocityAdj / machInVelUnit;

            node ??= dragTable.Find(mach);
            while (node.Previous != null && node.Mach > mach)
                node = node.Previous;
            while (node.Next != null && node.Next.Mach <= mach)
                node = node.Next;

            double drag = accumulatedFactor * densityFactor * node.CalculateDrag(mach) * velocityAdj;
            ax = -drag * vax;
            ay = -drag * vay - earthGravity;
            az = -drag * vaz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WindVectorRaw(ShotParameters shot, Wind wind, VelocityUnit units, out double wx, out double wy, out double wz)
        {
            var shotAngle = shot.ZeroDropAdjustment;
            if (shot.ShotDropAdjustment != null)
                shotAngle += shot.ShotDropAdjustment.Value;
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
