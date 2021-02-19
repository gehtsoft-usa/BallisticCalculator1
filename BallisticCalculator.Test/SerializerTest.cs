using BallisticCalculator.Serialization;
using FluentAssertions;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace BallisticCalculator.Test
{
    public class SerializerTest
    {
        [BXmlElement("child")]
        public class ChildClass
        {
            [BXmlProperty("name")]
            public string Name { get; set; }

        }

        [BXmlElement("main")]
        public class MainClass
        {
            [BXmlProperty("name")]
            public string Name { get; set; }

            [BXmlProperty("subname", Optional = true)]
            public string SubName { get; set; }

            [BXmlProperty("int")]
            public int Int { get; set; }

            [BXmlProperty("subint", Optional = true)]
            public int? SubInt { get; set; }

            [BXmlProperty("coefficient")]
            public BallisticCoefficient BallisticCoefficient { get; set; }

            [BXmlProperty("length")]
            public Measurement<DistanceUnit> Length { get; set; }

            [BXmlProperty("real")]
            public double Real { get; set; }

            [BXmlProperty("bool")]
            public bool Bool { get; set; }

            [BXmlProperty("span")]
            public TimeSpan TimeSpan { get; set; }

            [BXmlProperty("date")]
            public DateTime DateTime { get; set; }

            [BXmlProperty(ChildElement = true, Optional = true)]
            public ChildClass Child { get; set; }
        }

        [Fact]
        public void SerializedXmlTest1()
        {
            BallisticXmlSerializer serializer = new BallisticXmlSerializer();

            MainClass main = new MainClass()
            {
                Name = "main name",
                SubName = "main sub name",
                Int = 123,
                SubInt = 456,
                BallisticCoefficient = new BallisticCoefficient(0.345, DragTableId.G1),
                Real = 1.234,
                Bool = true,
                Length = new Measurement<DistanceUnit>(1.23, DistanceUnit.Meter),
                TimeSpan = TimeSpan.FromMilliseconds(123.456),
                DateTime = new DateTime(2010, 5, 27, 11, 30, 25),
                Child = new ChildClass()
                {
                    Name = "child name"
                }
            };

            XmlElement element = serializer.Serialize(main);

            element.Should().NotBeNull();
            element.Name.Should().Be("main");
            element.Should().HaveAttribute("name", "main name");
            element.Should().HaveAttribute("subname", "main sub name");

            element.Should().HaveAttribute("int", "123");
            element.Should().HaveAttribute("subint", "456");


            element.Should().HaveAttribute("coefficient", "0.345G1");
            element.Should().HaveAttribute("length", "1.23m");
            
            element.Should().HaveAttribute("real", "1.234");
            element.Should().HaveAttribute("bool", "true");
            
            element.Should().HaveAttribute("span", "123.456");
            element.Should().HaveAttribute("date", "2010-05-27 11:30:25");

            element.Should().HaveElement("child");

            element.ChildNodes[0].Should().BeOfType<XmlElement>();
            (element.ChildNodes[0] as XmlElement).Name.Should().Be("child");
            (element.ChildNodes[0] as XmlElement).Should().HaveAttribute("name", "child name");

            var main1 = serializer.Deserialize<MainClass>(element);

            main1.Should().NotBeNull();

            main1.Name.Should().Be("main name");
            main1.SubName.Should().Be("main sub name");
            main1.Int.Should().Be(123);
            main1.SubInt.Should().Be(456);
            main1.Real.Should().Be(1.234);
            main1.Bool.Should().Be(true);
            main1.TimeSpan.TotalMilliseconds.Should().Be(123.456);
            main1.DateTime.Should().Be(new DateTime(2010, 5, 27, 11, 30, 25));
            main1.Length.Should().Be(new Measurement<DistanceUnit>(1.23, DistanceUnit.Meter));
            main1.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.345, DragTableId.G1));

            main1.Child.Should().NotBeNull();
            main1.Child.Name.Should().Be("child name");
        }
        
        [Fact]
        public void SerializedXmlTest2()
        {
            BallisticXmlSerializer serializer = new BallisticXmlSerializer();

            MainClass main = new MainClass()
            {
                Name = "main name",
                Int = 123,
                BallisticCoefficient = new BallisticCoefficient(0.345, DragTableId.G1),
                Real = 1.234,
                Bool = true,
                Length = new Measurement<DistanceUnit>(1.23, DistanceUnit.Meter),
                TimeSpan = TimeSpan.FromMilliseconds(123.456),
                DateTime = new DateTime(2010, 5, 27, 11, 30, 25),
            };

            XmlElement element = serializer.Serialize(main);

            element.Should().NotBeNull();
            element.Name.Should().Be("main");
            element.Should().HaveAttribute("name", "main name");
            element.Attributes["subname"].Should().BeNull();

            element.Should().HaveAttribute("int", "123");
            element.Attributes["subint"].Should().BeNull();
            

            element.Should().HaveAttribute("coefficient", "0.345G1");
            element.Should().HaveAttribute("length", "1.23m");

            element.Should().HaveAttribute("real", "1.234");
            element.Should().HaveAttribute("bool", "true");

            element.Should().HaveAttribute("span", "123.456");
            element.Should().HaveAttribute("date", "2010-05-27 11:30:25");
            element.ChildNodes.Should().BeEmpty();

            var main1 = serializer.Deserialize<MainClass>(element);

            main1.Should().NotBeNull();

            main1.Name.Should().Be("main name");
            main1.SubName.Should().BeNull();
            main1.Int.Should().Be(123);
            main1.SubInt.Should().BeNull();
            main1.Real.Should().Be(1.234);
            main1.Bool.Should().Be(true);
            main1.TimeSpan.TotalMilliseconds.Should().Be(123.456);
            main1.DateTime.Should().Be(new DateTime(2010, 5, 27, 11, 30, 25));
            main1.Length.Should().Be(new Measurement<DistanceUnit>(1.23, DistanceUnit.Meter));
            main1.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.345, DragTableId.G1));

            main1.Child.Should().BeNull();
        }

        [Fact]
        public void ReadLegacy_Imperial()
        {
            BallisticXmlSerializer serializer = new BallisticXmlSerializer();
            var entry = serializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G7\" bc=\"0.305\" bullet-weight=\"250.00000000gr\" muzzle-velocity=\"2960.00000000ft/s\" barrel-length=\"24.00000000in\" bullet-length=\"1.55000000in\" bullet-diameter=\"0.33800000in\" name=\".338 Lapua 250gr\" source=\"Lapua/Litz\" caliber=\".338 Lapua Magnum\" bullet-type=\"FMJ\" />", false);
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
            BallisticXmlSerializer serializer = new BallisticXmlSerializer();
            var entry = serializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" barrel-length=\"410.00000mm\" name=\"7N23\" source=\"GRAU\" caliber=\"7.62x39mm M43\" bullet-type=\"FMJ\" bullet-diameter=\"7.85mm\" bullet-length=\"26mm\" />", false);

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
            BallisticXmlSerializer serializer = new BallisticXmlSerializer();
            var entry = serializer.ReadLegacyAmmunitionLibraryEntry("<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" name=\"7N23\"  />", false);

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
        public void RoundTrip_Ammunition1()
        {
            Ammunition ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.295, DragTableId.G1),
                Weight = new Measurement<WeightUnit>(9.1, WeightUnit.Gram),
                MuzzleVelocity = new Measurement<VelocityUnit>(2956.5, VelocityUnit.FeetPerSecond),
                BulletDiameter = new Measurement<DistanceUnit>(0.224, DistanceUnit.Inch),
                BulletLength = new Measurement<DistanceUnit>(0.98, DistanceUnit.Inch)
            };

            BallisticXmlSerializer serializer = new BallisticXmlSerializer();

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
        public void RoundTrip_Ammunition2()
        {
            Ammunition ammo = new Ammunition()
            {
                BallisticCoefficient = new BallisticCoefficient(0.295, DragTableId.G1),
                Weight = new Measurement<WeightUnit>(9.1, WeightUnit.Gram),
                MuzzleVelocity = new Measurement<VelocityUnit>(2956.5, VelocityUnit.FeetPerSecond),
            };

            BallisticXmlSerializer serializer = new BallisticXmlSerializer();

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
        public void RoundTrip_AmmuntionLibraryEntry()
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

            BallisticXmlSerializer serializer = new BallisticXmlSerializer();

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
    }
}

