using FluentAssertions;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BallisticCalculator.Test.Data
{
    public class AtmosphereTest
    {
        [Theory]
        [InlineData(59, TemperatureUnit.Fahrenheit, 29.95, PressureUnit.InchesOfMercury, 0, 1.2261, DensityUnit.KilogramPerCubicMeter)]
        [InlineData(59, TemperatureUnit.Fahrenheit, 29.95, PressureUnit.InchesOfMercury, 0.78, 1.2201, DensityUnit.KilogramPerCubicMeter)]
        [InlineData(75, TemperatureUnit.Fahrenheit, 31.07, PressureUnit.InchesOfMercury, 0.78, 1.2237, DensityUnit.KilogramPerCubicMeter)]
        [InlineData(10, TemperatureUnit.Fahrenheit, 28, PressureUnit.InchesOfMercury, 0.3, 1.2656, DensityUnit.KilogramPerCubicMeter)]

        public void Density(double temperature, TemperatureUnit temperatureUnit, double pressure, PressureUnit pressureUnit, double humidity, double expected, DensityUnit expectedUnit)
        {
            var atmosphere = new Atmosphere(new Measurement<DistanceUnit>(0, DistanceUnit.Foot),
                                            new Measurement<PressureUnit>(pressure, pressureUnit),
                                            true,
                                            new Measurement<TemperatureUnit>(temperature, temperatureUnit),
                                            humidity);
            atmosphere.Density.In(expectedUnit).Should().BeApproximately(expected, 1e-4);
        }

        [Theory]
        [InlineData(32, TemperatureUnit.Fahrenheit, 29.95, PressureUnit.InchesOfMercury, 0, 1086, VelocityUnit.FeetPerSecond)]
        [InlineData(59, TemperatureUnit.Fahrenheit, 29.95, PressureUnit.InchesOfMercury, 0, 1225, VelocityUnit.KilometersPerHour)]
        [InlineData(0, TemperatureUnit.Celsius, 29.95, PressureUnit.InchesOfMercury, 0, 331, VelocityUnit.MetersPerSecond)]

        public void SoundVelocity(double temperature, TemperatureUnit temperatureUnit, double pressure, PressureUnit pressureUnit, double humidity, double expected, VelocityUnit expectedUnit)
        {
            var atmosphere = new Atmosphere(new Measurement<DistanceUnit>(0, DistanceUnit.Foot),
                                            new Measurement<PressureUnit>(pressure, pressureUnit),
                                            true,
                                            new Measurement<TemperatureUnit>(temperature, temperatureUnit),
                                            humidity);
            atmosphere.SoundVelocity.In(expectedUnit).Should().BeApproximately(expected, 1);
        }

        [Theory]
        [InlineData(15, 101325, 1000, 89874.57)]
        [InlineData(25, 101325, 200, 99024.40)]
        public void ShiftPressure(double temperature, double pressure, double altitude, double expected)
        {
            var atmosphere = new Atmosphere(new Measurement<DistanceUnit>(altitude, DistanceUnit.Meter),
                                            new Measurement<PressureUnit>(pressure, PressureUnit.Pascal),
                                            true,
                                            new Measurement<TemperatureUnit>(temperature, TemperatureUnit.Celsius),
                                            0);

            atmosphere.Pressure.In(PressureUnit.Pascal).Should().BeApproximately(expected, 0.1);
        }

        [Theory]
        [InlineData(1000, 89870, 8.5)]
        public void Icao(double altitude, double pressure, double temperature)
        {
            var atmosphere = Atmosphere.CreateICAOAtmosphere(new Measurement<DistanceUnit>(altitude, DistanceUnit.Meter));
            atmosphere.Pressure.In(PressureUnit.Pascal).Should().BeApproximately(pressure, 1);
            atmosphere.Temperature.In(TemperatureUnit.Celsius).Should().BeApproximately(temperature, 0.1);
        }

        [Fact]
        public void SaveAndRestore()
        {
            var atmo1 = new Atmosphere(new Measurement<DistanceUnit>(1234, DistanceUnit.Foot),
                    new Measurement<PressureUnit>(31.02, PressureUnit.InchesOfMercury), false,
                    new Measurement<TemperatureUnit>(21, TemperatureUnit.Celsius), 0.65);

            string v = JsonSerializer.Serialize(atmo1);

            var atmo2 = JsonSerializer.Deserialize<Atmosphere>(v);
            atmo2.Pressure.Should().Be(atmo1.Pressure);
            atmo2.Temperature.Should().Be(atmo1.Temperature);
            atmo2.Altitude.Should().Be(atmo1.Altitude);
            atmo2.Humidity.Should().Be(atmo2.Humidity);
        }
    }
}
