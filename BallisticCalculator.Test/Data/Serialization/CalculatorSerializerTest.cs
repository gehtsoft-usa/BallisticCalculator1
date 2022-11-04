using BallisticCalculator.Serialization;
using FluentAssertions;
using Gehtsoft.Measurements;
using System;
using Xunit;

namespace BallisticCalculator.Test.Data.Serialization
{
    public class CalculatorSerializerTest
    {
        [Fact]
        public void Roundtrip_TrajectoryPoint()
        {
            var point = new TrajectoryPoint(TimeSpan.FromMilliseconds(0.5),
                new Measurement<WeightUnit>(55, WeightUnit.Grain),
                new Measurement<DistanceUnit>(100, DistanceUnit.Yard),
                new Measurement<VelocityUnit>(2430, VelocityUnit.FeetPerSecond),
                2.15937,
                new Measurement<DistanceUnit>(1.54, DistanceUnit.Inch),
                new Measurement<DistanceUnit>(-2.55, DistanceUnit.Inch));

            point.OptimalGameWeight.In(WeightUnit.Pound).Should().BeApproximately(65, 0.5);
            point.DropAdjustment.Should().Be(new Measurement<AngularUnit>(1.54, AngularUnit.InchesPer100Yards));
            point.WindageAdjustment.Should().Be(new Measurement<AngularUnit>(-2.55, AngularUnit.InchesPer100Yards));

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(point);
            var point2 = serializer.Deserialize<TrajectoryPoint>(xml);

            point2.Time.Should().Be(point.Time);
            point2.Distance.Should().Be(point.Distance);
            point2.Velocity.Should().Be(point.Velocity);
            point2.Mach.Should().Be(point.Mach);
            point2.Energy.Should().Be(point.Energy);
            point2.OptimalGameWeight.Should().Be(point.OptimalGameWeight);
            point2.Drop.Should().Be(point.Drop);
            point2.DropAdjustment.Should().Be(point.DropAdjustment);
            point2.Windage.Should().Be(point.Windage);
            point2.WindageAdjustment.Should().Be(point.WindageAdjustment);
        }

        [Fact]
        public void ReadLegacy_Imperial()
        {
            var entry = BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G7\" bc=\"0.305\" bullet-weight=\"250.00000000gr\" muzzle-velocity=\"2960.00000000ft/s\" barrel-length=\"24.00000000in\" bullet-length=\"1.55000000in\" bullet-diameter=\"0.33800000in\" name=\".338 Lapua 250gr\" source=\"Lapua/Litz\" caliber=\".338 Lapua Magnum\" bullet-type=\"FMJ\" />");
            entry.Should().NotBeNull();
            entry.Ammunition.Should().NotBeNull();

            entry.Name.Should().Be(".338 Lapua 250gr");
            entry.AmmunitionType.Should().Be("FMJ");
            entry.Source.Should().Be("Lapua/Litz");
            entry.Caliber.Should().Be(".338 Lapua Magnum");
            entry.BarrelLength.Should().Be(new Measurement<DistanceUnit>(24, DistanceUnit.Inch));

            entry.Ammunition.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.305, DragTableId.G7));
            entry.Ammunition.Weight.Should().Be(new Measurement<WeightUnit>(250, WeightUnit.Grain));
            entry.Ammunition.MuzzleVelocity.Should().Be(new Measurement<VelocityUnit>(2960, VelocityUnit.FeetPerSecond));
            entry.Ammunition.BulletLength.Should().Be(new Measurement<DistanceUnit>(1.55, DistanceUnit.Inch));
            entry.Ammunition.BulletDiameter.Should().Be(new Measurement<DistanceUnit>(0.338, DistanceUnit.Inch));
        }

        [Fact]
        public void ReadLegacy_Metric()
        {
            var entry = BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" barrel-length=\"410.00000mm\" name=\"7N23\" source=\"GRAU\" caliber=\"7.62x39mm M43\" bullet-type=\"FMJ\" bullet-diameter=\"7.85mm\" bullet-length=\"26mm\" />");

            entry.Should().NotBeNull();
            entry.Ammunition.Should().NotBeNull();

            entry.Name.Should().Be("7N23");
            entry.AmmunitionType.Should().Be("FMJ");
            entry.Source.Should().Be("GRAU");
            entry.Caliber.Should().Be("7.62x39mm M43");
            entry.BarrelLength.Should().Be(new Measurement<DistanceUnit>(410, DistanceUnit.Millimeter));

            entry.Ammunition.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.297, DragTableId.G1));
            entry.Ammunition.Weight.Should().Be(new Measurement<WeightUnit>(7.7, WeightUnit.Gram));
            entry.Ammunition.MuzzleVelocity.Should().Be(new Measurement<VelocityUnit>(730, VelocityUnit.MetersPerSecond));
            entry.Ammunition.BulletLength.Should().Be(new Measurement<DistanceUnit>(26, DistanceUnit.Millimeter));
            entry.Ammunition.BulletDiameter.Should().Be(new Measurement<DistanceUnit>(7.85, DistanceUnit.Millimeter));
        }

        [Fact]
        public void ReadLegacy_Incomplete()
        {
            var entry = BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" name=\"7N23\"  />");

            entry.Should().NotBeNull();
            entry.Ammunition.Should().NotBeNull();

            entry.Name.Should().Be("7N23");
            entry.AmmunitionType.Should().BeNull();
            entry.Source.Should().BeNull();
            entry.Caliber.Should().BeNull();
            entry.BarrelLength.Should().BeNull();

            entry.Ammunition.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.297, DragTableId.G1));
            entry.Ammunition.Weight.Should().Be(new Measurement<WeightUnit>(7.7, WeightUnit.Gram));
            entry.Ammunition.MuzzleVelocity.Should().Be(new Measurement<VelocityUnit>(730, VelocityUnit.MetersPerSecond));
            entry.Ammunition.BulletLength.Should().BeNull();
            entry.Ammunition.BulletDiameter.Should().BeNull();
        }

        [Fact]
        public void Roundtrip_Ammunition1()
        {
            Ammunition ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.295, DragTableId.G1),
                Weight = new Measurement<WeightUnit>(9.1, WeightUnit.Gram),
                MuzzleVelocity = new Measurement<VelocityUnit>(2956.5, VelocityUnit.FeetPerSecond),
                BulletDiameter = new Measurement<DistanceUnit>(0.224, DistanceUnit.Inch),
                BulletLength = new Measurement<DistanceUnit>(0.98, DistanceUnit.Inch)
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();

            var node = serializer.Serialize(ammo);
            node.Should().NotBeNull();

            var ammo1 = serializer.Deserialize<Ammunition>(node);
            ammo1.Should().NotBeNull();

            ammo1.BallisticCoefficient.Should().Be(ammo.BallisticCoefficient);
            ammo1.Weight.Should().Be(ammo.Weight);
            ammo1.MuzzleVelocity.Should().Be(ammo.MuzzleVelocity);
            ammo1.BulletDiameter.Should().Be(ammo.BulletDiameter);
            ammo1.BulletLength.Should().Be(ammo.BulletLength);
        }

        [Fact]
        public void Roundtrip_Ammunition2()
        {
            Ammunition ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.295, DragTableId.G1),
                Weight = new Measurement<WeightUnit>(9.1, WeightUnit.Gram),
                MuzzleVelocity = new Measurement<VelocityUnit>(2956.5, VelocityUnit.FeetPerSecond),
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();

            var node = serializer.Serialize(ammo);
            node.Should().NotBeNull();

            var ammo1 = serializer.Deserialize<Ammunition>(node);
            ammo1.Should().NotBeNull();

            ammo1.BallisticCoefficient.Should().Be(ammo.BallisticCoefficient);
            ammo1.Weight.Should().Be(ammo.Weight);
            ammo1.MuzzleVelocity.Should().Be(ammo.MuzzleVelocity);
            ammo1.BulletDiameter.Should().Be(ammo.BulletDiameter);
            ammo1.BulletLength.Should().Be(ammo.BulletLength);
        }

        [Fact]
        public void Roundtrip_AmmuntionLibraryEntry()
        {
            AmmunitionLibraryEntry entry = new AmmunitionLibraryEntry()
            {
                Name = "Entry name",
                Source = "Entry source",
                Caliber = "Entry caliber",
                AmmunitionType = "BTHP",
                BarrelLength = new Measurement<DistanceUnit>(410, DistanceUnit.Millimeter),
                Ammunition = new Ammunition()
                {
                    BallisticCoefficient = new BallisticCoefficient(0.295, DragTableId.G1),
                    Weight = new Measurement<WeightUnit>(9.1, WeightUnit.Gram),
                    MuzzleVelocity = new Measurement<VelocityUnit>(2956.5, VelocityUnit.FeetPerSecond),
                    BulletDiameter = new Measurement<DistanceUnit>(0.224, DistanceUnit.Inch),
                    BulletLength = new Measurement<DistanceUnit>(0.98, DistanceUnit.Inch)
                }
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();

            var node = serializer.Serialize(entry);
            node.Should().NotBeNull();

            var entry1 = serializer.Deserialize<AmmunitionLibraryEntry>(node);
            entry1.Should().NotBeNull();

            entry1.Name.Should().Be(entry.Name);
            entry1.Source.Should().Be(entry.Source);
            entry1.Caliber.Should().Be(entry.Caliber);
            entry1.AmmunitionType.Should().Be(entry.AmmunitionType);
            entry1.BarrelLength.Should().Be(entry.BarrelLength);

            entry1.Ammunition.BallisticCoefficient.Should().Be(entry.Ammunition.BallisticCoefficient);
            entry1.Ammunition.Weight.Should().Be(entry.Ammunition.Weight);
            entry1.Ammunition.MuzzleVelocity.Should().Be(entry.Ammunition.MuzzleVelocity);
            entry1.Ammunition.BulletDiameter.Should().Be(entry.Ammunition.BulletDiameter);
            entry1.Ammunition.BulletLength.Should().Be(entry.Ammunition.BulletLength);
        }

        [Fact]
        public void Roundtrip_Atmosphere()
        {
            var atmo = new Atmosphere(new Measurement<DistanceUnit>(123, DistanceUnit.Meter),
                new Measurement<PressureUnit>(30.02, PressureUnit.InchesOfMercury),
                new Measurement<TemperatureUnit>(16, TemperatureUnit.Celsius),
                0.57);

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(atmo);
            var atmo2 = serializer.Deserialize<Atmosphere>(xml);

            atmo2.Should().NotBeNull();
            atmo2.Altitude.Should().Be(atmo.Altitude);
            atmo2.Pressure.Should().Be(atmo.Pressure);
            atmo2.Temperature.Should().Be(atmo.Temperature);
            atmo2.Humidity.Should().Be(atmo.Humidity);
            atmo2.SoundVelocity.Should().Be(atmo.SoundVelocity);
            atmo2.Density.Should().Be(atmo.Density);
        }

        [Fact]
        public void Roundtrip_ZeroParam_NullableValues()
        {
            var zero = new ZeroingParameters()
            {
                Distance = DistanceUnit.Yard.New(100)
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(zero);
            var zero2 = serializer.Deserialize<ZeroingParameters>(xml);

            zero2.Should().NotBeNull();
            zero2.Atmosphere.Should().BeNull();
            zero2.Ammunition.Should().BeNull();
            zero2.VerticalOffset.Should().BeNull();
            zero2.Distance.Should().Be(zero.Distance);
        }

        [Fact]
        public void Roundtrip_ZeroParam_NoNullableValues()
        {
            var zero = new ZeroingParameters()
            {
                Distance = DistanceUnit.Yard.New(100),
                VerticalOffset = DistanceUnit.Inch.New(5)
                
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(zero);
            var zero2 = serializer.Deserialize<ZeroingParameters>(xml);

            zero2.Should().NotBeNull();
            zero2.Atmosphere.Should().BeNull();
            zero2.Ammunition.Should().BeNull();
            zero2.VerticalOffset.Should()
                .NotBeNull()
                .And.Be(zero.VerticalOffset);
            zero2.Distance.Should().Be(zero.Distance);
        }

        [Fact]
        public void Roundtrip_Rifle()
        {
            Rifle rifle = new Rifle()
            {
                Rifling = new Rifling()
                {
                    Direction = TwistDirection.Right,
                    RiflingStep = new Measurement<DistanceUnit>(12, DistanceUnit.Inch)
                },
                Sight = new Sight()
                {
                    SightHeight = new Measurement<DistanceUnit>(3.2, DistanceUnit.Inch),
                    VerticalClick = new Measurement<AngularUnit>(0.5, AngularUnit.MOA),
                    HorizontalClick = new Measurement<AngularUnit>(0.25, AngularUnit.MOA),
                },
                Zero = new ZeroingParameters()
                {
                    Ammunition = new Ammunition()
                    {
                        BallisticCoefficient = new BallisticCoefficient(0.375, DragTableId.G7),
                        MuzzleVelocity = new Measurement<VelocityUnit>(2700, VelocityUnit.FeetPerSecond),
                        Weight = new Measurement<WeightUnit>(168, WeightUnit.Grain)
                    },
                    Atmosphere = new Atmosphere(new Measurement<DistanceUnit>(123, DistanceUnit.Meter),
                           new Measurement<PressureUnit>(30.02, PressureUnit.InchesOfMercury),
                           new Measurement<TemperatureUnit>(16, TemperatureUnit.Celsius),
                           0.57),
                    Distance = new Measurement<DistanceUnit>(100, DistanceUnit.Yard)
                }
            };
            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(rifle);
            var rifle2 = serializer.Deserialize<Rifle>(xml);

            rifle2.Rifling.Should().NotBeNull();
            rifle2.Sight.Should().NotBeNull();
            rifle2.Zero.Should().NotBeNull();

            rifle2.Rifling.RiflingStep.Should().Be(rifle.Rifling.RiflingStep);
            rifle2.Rifling.Direction.Should().Be(rifle.Rifling.Direction);

            rifle2.Sight.SightHeight.Should().Be(rifle.Sight.SightHeight);
            rifle2.Sight.VerticalClick.Should().Be(rifle.Sight.VerticalClick);
            rifle2.Sight.HorizontalClick.Should().Be(rifle.Sight.HorizontalClick);

            rifle2.Zero.Ammunition.Should().NotBeNull();
            rifle2.Zero.Ammunition.BallisticCoefficient.Should().Be(rifle.Zero.Ammunition.BallisticCoefficient);
            rifle2.Zero.Ammunition.Weight.Should().Be(rifle.Zero.Ammunition.Weight);
            rifle2.Zero.Ammunition.MuzzleVelocity.Should().Be(rifle.Zero.Ammunition.MuzzleVelocity);

            rifle2.Zero.Distance.Should().Be(rifle.Zero.Distance);

            rifle2.Zero.Atmosphere.Should().NotBeNull();
            rifle2.Zero.Atmosphere.Altitude.Should().Be(rifle.Zero.Atmosphere.Altitude);
            rifle2.Zero.Atmosphere.Pressure.Should().Be(rifle.Zero.Atmosphere.Pressure);
            rifle2.Zero.Atmosphere.Temperature.Should().Be(rifle.Zero.Atmosphere.Temperature);
            rifle2.Zero.Atmosphere.Humidity.Should().Be(rifle.Zero.Atmosphere.Humidity);
            rifle2.Zero.Atmosphere.SoundVelocity.Should().Be(rifle.Zero.Atmosphere.SoundVelocity);
            rifle2.Zero.Atmosphere.Density.Should().Be(rifle.Zero.Atmosphere.Density);
        }

        [Fact]
        public void Roundtrip_Wind1()
        {
            var wind = new Wind()
            {
                Direction = new Measurement<AngularUnit>(45, AngularUnit.Degree),
                Velocity = new Measurement<VelocityUnit>(10, VelocityUnit.Knot),
                MaximumRange = new Measurement<DistanceUnit>(500, DistanceUnit.Yard),
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(wind);
            var wind2 = serializer.Deserialize<Wind>(xml);

            wind2.Direction.Should().Be(wind.Direction);
            wind2.Velocity.Should().Be(wind.Velocity);
            wind2.MaximumRange.Should().Be(wind.MaximumRange);
        }

        [Fact]
        public void Roundtrip_Wind2()
        {
            var wind = new Wind()
            {
                Direction = new Measurement<AngularUnit>(45, AngularUnit.Degree),
                Velocity = new Measurement<VelocityUnit>(10, VelocityUnit.Knot),
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(wind);
            var wind2 = serializer.Deserialize<Wind>(xml);

            wind2.Direction.Should().Be(wind.Direction);
            wind2.Velocity.Should().Be(wind.Velocity);
            wind2.MaximumRange.Should().BeNull();
        }
    }
}

