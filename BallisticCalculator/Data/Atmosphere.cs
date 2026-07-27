using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The specification of the current atmosphere conditions
    /// </summary>
    [BXmlElement("atmosphere")]
    public class Atmosphere
    {
        /// <summary>
        /// The height above the sea level
        /// </summary>
        [BXmlProperty("altitude")]
        public Measurement<DistanceUnit> Altitude { get; }
        /// <summary>
        /// The current air pressure
        /// </summary>
        [BXmlProperty("pressure")]
        public Measurement<PressureUnit> Pressure { get; }
        /// <summary>
        /// The current temperature
        /// </summary>
        [BXmlProperty("temperature")]
        public Measurement<TemperatureUnit> Temperature { get; }

        /// <summary>
        /// The current relative humidity, as a fraction from 0 to 1 rather than as a percentage.
        /// </summary>
        [BXmlProperty("humidity")]
        public double Humidity { get; }

        /// <summary>
        /// Mach velocity for the given atmospheric conditions
        /// </summary>
        [JsonIgnore]
        public Measurement<VelocityUnit> SoundVelocity { get; }

        /// <summary>
        /// Density of the atmosphere
        /// </summary>
        [JsonIgnore]
        public Measurement<DensityUnit> Density { get; }

        /// <summary>
        /// <para>The altitude in the standard atmosphere which has the same air density as this atmosphere.</para>
        /// <para>The density altitude summarizes into one number everything the air does to the drag, so two
        /// sets of conditions with the same density altitude produce the same trajectory. Air that is hot,
        /// high or humid raises it above the true altitude, dense air lowers it, and at sea level in
        /// conditions denser than standard it is negative.</para>
        /// <para>The value is derived from [c]Density[/c], so it accounts for the humidity as well as for the
        /// pressure and the temperature.</para>
        /// </summary>
        [JsonIgnore]
        public Measurement<DistanceUnit> DensityAltitude { get; }

        /// <summary>
        /// A standard density of the atmosphere
        /// </summary>
        public static Measurement<DensityUnit> StandardDensity { get; } = new Measurement<DensityUnit>(0.076474, DensityUnit.PoundsPerCubicFoot);

        /// <summary>
        /// <para>Create default atmosphere.</para>
        /// <para>The standard atmosphere is at sea level, 15C, the pressure of 1 atmosphere and 78% humidity</para>
        /// </summary>
        public Atmosphere() : this(new Measurement<DistanceUnit>(0, DistanceUnit.Meter),
                                   new Measurement<PressureUnit>(29.95, PressureUnit.InchesOfMercury),
                                   false,
                                   new Measurement<TemperatureUnit>(15, TemperatureUnit.Celsius),
                                   0.78)
        {
        }

        /// <summary>
        /// Parameterized constructor for the pressure at altitude
        /// </summary>
        /// <param name="altitude"></param>
        /// <param name="pressure"></param>
        /// <param name="temperature"></param>
        /// <param name="humidity">The relative humidity, as a fraction from 0 to 1</param>
        [JsonConstructor]
        [BXmlConstructorAttribute]
        public Atmosphere(Measurement<DistanceUnit> altitude, Measurement<PressureUnit> pressure, Measurement<TemperatureUnit> temperature, double humidity)
            : this(altitude, pressure, false, temperature, humidity)
        {
        }

        /// <summary>
        /// Parameterized constructor for the pressure at altitude or at sea level
        /// </summary>
        /// <param name="altitude"></param>
        /// <param name="pressure"></param>
        /// <param name="pressureAtSeaLevel"></param>
        /// <param name="temperature"></param>
        /// <param name="humidity">The relative humidity, as a fraction from 0 to 1</param>
        public Atmosphere(Measurement<DistanceUnit> altitude, Measurement<PressureUnit> pressure, bool pressureAtSeaLevel, Measurement<TemperatureUnit> temperature, double humidity)
        {
            Altitude = altitude;
            if (!pressureAtSeaLevel || altitude.Value <= 0)
                Pressure = pressure;
            else
                Pressure = new Measurement<PressureUnit>(CalculatePressure(pressure.In(PressureUnit.Bar), temperature.In(TemperatureUnit.Kelvin), 0, altitude.In(DistanceUnit.Meter)), PressureUnit.Bar);

            Temperature = temperature;
            Humidity = humidity;

            //the resolved Pressure, not the argument: when the caller supplied the sea level pressure the
            //density belongs to the station pressure at the altitude, which is what AtAltitude also uses
            double densityKgPerCubicMeter = CalculateDensity(temperature.In(TemperatureUnit.Kelvin), Pressure.In(PressureUnit.Pascal), humidity);
            Density = new Measurement<DensityUnit>(densityKgPerCubicMeter, DensityUnit.KilogramPerCubicMeter);
            SoundVelocity = new Measurement<VelocityUnit>(CalculateSoundVelocity(temperature.In(TemperatureUnit.Kelvin)), VelocityUnit.MetersPerSecond);
            DensityAltitude = new Measurement<DistanceUnit>(CalculateDensityAltitude(densityKgPerCubicMeter), DistanceUnit.Meter);
        }

        /// <summary>
        /// Creates an ICAO default atmosphere at the specified altitude
        /// </summary>
        /// <param name="altitude">The altitiude above sea level</param>
        /// <param name="humidity">The relative humidity (0...1)</param>
        /// <returns></returns>
        public static Atmosphere CreateICAOAtmosphere(Measurement<DistanceUnit> altitude, double humidity = 0)
        {
            double altitude1 = altitude.In(DistanceUnit.Meter);
            double pressure = Measurement<PressureUnit>.Convert(29.92, PressureUnit.InchesOfMercury, PressureUnit.Pascal);
            double temperature = Measurement<TemperatureUnit>.Convert(59, TemperatureUnit.Fahrenheit, TemperatureUnit.Kelvin);

            //the third argument is the base altitude of the standard atmosphere, which is the sea level
            return new Atmosphere(altitude, new Measurement<PressureUnit>(CalculatePressure(pressure, temperature, 0, altitude1), PressureUnit.Pascal), false,
                                  new Measurement<TemperatureUnit>(CalculateTemperature(temperature, 0, altitude1), TemperatureUnit.Kelvin), humidity);
        }

        // https://www.omnicalculator.com/physics/air-density
        // http://www.emd.dk/files/windpro/WindPRO_AirDensity.pdf
        //base value for saturated vapor pressure
        private const double ES0 = 6.1078;
        //Herman Wobus polynomial coefficient to calculate saturated vapor pressure
        private const double SVP_C0 = 0.99999683;
        private const double SVP_C1 = -0.90826951e-2;
        private const double SVP_C2 = 0.78736169e-4;
        private const double SVP_C3 = -0.61117958e-6;
        private const double SVP_C4 = 0.43884187e-8;
        private const double SVP_C5 = -0.29883885e-10;
        private const double SVP_C6 = 0.21874425e-12;
        private const double DRY_AIR_K = 287.058;
        private const double VAPOR_K = 461.495;

        /// <summary>
        /// Calculates 100% saturated vapor pressure for the specified temperature
        /// </summary>
        /// <param name="t">Temperature in degree of Celsius</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double SaturatedVapourPressure(double t)
        {
            double pt = SVP_C0 + t * (SVP_C1 + t * (SVP_C2 + t * (SVP_C3 + t * (SVP_C4 + t * (SVP_C5 + t * SVP_C6)))));
            return ES0 / Math.Pow(pt, 8);
        }

        /// <summary>
        /// Calculate density of atmosphere in given conditions
        /// </summary>
        /// <param name="temperature">Temperature in Kelvin</param>
        /// <param name="pressure">Pressure in pascals</param>
        /// <param name="humidity">Relative humidity (0 to 1)</param>
        /// <returns>The density in kg/m3</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateDensity(double temperature, double pressure, double humidity)
        {
            double t = temperature - 273.15;
            double tk = temperature;

            double vaporSaturation = SaturatedVapourPressure(t) * 100;
            double actualVapourPressure = vaporSaturation * humidity;
            double dryPressure = pressure - actualVapourPressure;

            double density = dryPressure / (DRY_AIR_K * tk) + actualVapourPressure / (VAPOR_K * tk);
            return density;
        }

        //https://www.grc.nasa.gov/www/BGH/isentrop.html

        /// <summary>
        /// Calculates velocity of the sound for the given temperature
        /// </summary>
        /// <param name="temperature">The temperature in Kelvins</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateSoundVelocity(double temperature)
        {
            return 331.3 * Math.Sqrt(temperature / 273.15);
        }

        //https://www.mide.com/air-pressure-at-altitude-calculator
        private const double TEMPERATURE_LAPSE = -0.0065;
        private const double GAS_CONSTANT = 8.31432;
        private const double G_CONSTANT = 9.80665;
        private const double AIR_MOLAR_MASS = 0.0289644;

        /// <summary>
        /// Calculate the pressure on the specified altitude with known pressure on the other altitude
        /// </summary>
        /// <param name="basePressure">Pressure at base level (pascal)</param>
        /// <param name="baseTemperature">Temperature at base level (kelvin)</param>
        /// <param name="baseAltitude">Height of the base level (meter)</param>
        /// <param name="altitude">Altitude to calculate (meter)</param>
        /// <returns>The pressure at the specified altitude in pascals</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculatePressure(double basePressure, double baseTemperature, double baseAltitude, double altitude)
        {
            const double exponent = -G_CONSTANT * AIR_MOLAR_MASS / (GAS_CONSTANT * TEMPERATURE_LAPSE);
            return basePressure * Math.Pow(1 + TEMPERATURE_LAPSE / baseTemperature * (altitude - baseAltitude), exponent);
        }

        //https://en.wikipedia.org/wiki/Density_altitude
        //the sea level base of the standard atmosphere, the same one CreateICAOAtmosphere starts from
        private const double STANDARD_SEA_LEVEL_TEMPERATURE = 288.15;

        //deliberately NOT StandardDensity: that is the drag normalization constant inherited with the
        //engine and it differs from this model's own sea level density by a hair. The density altitude
        //is defined as the altitude of the standard atmosphere with a matching density, so deriving the
        //base from the same constants the forward model uses is what makes the inverse exact
        private static readonly double StandardSeaLevelDensity = CalculateDensity(
            STANDARD_SEA_LEVEL_TEMPERATURE,
            Measurement<PressureUnit>.Convert(29.92, PressureUnit.InchesOfMercury, PressureUnit.Pascal),
            0);

        /// <summary>
        /// Calculate the altitude of the standard atmosphere which has the given air density
        /// </summary>
        /// <param name="density">The air density in kilograms per cubic meter</param>
        /// <returns>The altitude in meters, negative for air denser than the standard sea level air</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateDensityAltitude(double density)
        {
            //in the standard atmosphere the density follows the same barometric law as the pressure, one
            //power of the temperature lower, because the density is the pressure over the temperature:
            //  density(h) = density(0) * (1 + LAPSE * h / T0) ^ exponent
            //so inverting it for h turns a density ratio into an altitude
            const double exponent = -G_CONSTANT * AIR_MOLAR_MASS / (GAS_CONSTANT * TEMPERATURE_LAPSE) - 1;
            double ratio = density / StandardSeaLevelDensity;
            return STANDARD_SEA_LEVEL_TEMPERATURE / TEMPERATURE_LAPSE * (Math.Pow(ratio, 1 / exponent) - 1);
        }

        /// <summary>
        /// Calculate temperature at different altitude
        /// </summary>
        /// <param name="baseTemperature">Base temperature in kelvins</param>
        /// <param name="baseAltitude">Base altitude</param>
        /// <param name="altitude">Altitude to calculate</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateTemperature(double baseTemperature, double baseAltitude, double altitude)
        {
            return baseTemperature + TEMPERATURE_LAPSE * (altitude - baseAltitude);
        }

        private readonly double StandartDensity1 = StandardDensity.In(DensityUnit.KilogramPerCubicMeter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AtAltitude(Measurement<DistanceUnit> altitude, out double densityFactor, out Measurement<VelocityUnit> mach)
        {
            double a = altitude.In(DistanceUnit.Meter);
            var t = CalculateTemperature(Temperature.In(TemperatureUnit.Kelvin), Altitude.In(DistanceUnit.Meter), a);
            var p = CalculatePressure(Pressure.In(PressureUnit.Pascal), Temperature.In(TemperatureUnit.Kelvin), Altitude.In(DistanceUnit.Meter), a);
            var d = CalculateDensity(t, p, Humidity);
            densityFactor = d / StandartDensity1;
            mach = new Measurement<VelocityUnit>(CalculateSoundVelocity(t), VelocityUnit.MetersPerSecond);
        }
    }
}
