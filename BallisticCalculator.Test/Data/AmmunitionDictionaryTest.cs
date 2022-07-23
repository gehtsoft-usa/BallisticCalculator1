using BallisticCalculator.Data.Dictionary;
using FluentAssertions;
using Gehtsoft.Measurements;
using Xunit;
using System.Linq;

namespace BallisticCalculator.Test.Data
{
    public class AmmunitionDictionaryTest
    {
        [Theory]
        [InlineData("a", "b", "a", "b", true)]
        [InlineData("a", "b", "c", "b", false)]
        [InlineData("a", "b", "a", "c", false)]
        [InlineData("a", "b", "c", "d", false)]
        public void AmmunitionType_EqualityAndHashCodeTest(string a1, string a2, string b1, string b2, bool equals)
        {
            var a = new AmmunitionType(a1, a2);
            var b = new AmmunitionType(b1, b2);

            a.Equals(b).Should().Be(equals);

            if (equals)
                a.GetHashCode().Should().Be(b.GetHashCode());
            else
                a.GetHashCode().Should().NotBe(b.GetHashCode());
        }

        [Fact]
        public void AmmunitionType_DefaultFactory()
        {
            var dictionary = AmmunitionTypeFactory.Create();

            dictionary.Should().NotBeEmpty();
            dictionary.Should().BeInAscendingOrder(t => t.Abbreviation);
            dictionary.Should().Contain(t => t.Abbreviation == "FMJ");
            dictionary.Should().Contain(t => t.Abbreviation == "JHP");

            dictionary[0].Abbreviation.Should().Be("AP");
            dictionary[0].Name.Should().Be("Armor Piercing");
            dictionary[^1].Abbreviation.Should().Be(dictionary.Max(t => t.Abbreviation));
        }


        private static AmmunitionCaliber AmmunitionCaliber_Construct(AmmunitionCaliberType t, string group, string diameter, string name, string altname)
        {
            Measurement<DistanceUnit>? g = null;
            

            if (group != null)
                g = new Measurement<DistanceUnit>(group);

            Measurement<DistanceUnit> d = new Measurement<DistanceUnit>(diameter);

            return new AmmunitionCaliber(t, g, d, name, altname);
        }

        [Theory]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "", true)]
        [InlineData(AmmunitionCaliberType.Pistol, null, "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Rifle, "10mm", "10.5mm", "name", "", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "9mm", "10.5mm", "name", "", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "10mm", "10.6mm", "name", "", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name1", "", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "",
                    AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "name1", false)]
        [InlineData(AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "name1",
                    AmmunitionCaliberType.Pistol, "10mm", "10.5mm", "name", "name2", false)]
        public void AmmunitionCaliber_EqualityAndHashCodeTest(AmmunitionCaliberType t1, string group1, string diameter1, string name1, string altname1,
                                                              AmmunitionCaliberType t2, string group2, string diameter2, string name2, string altname2, bool equals)
        {
            var a1 = AmmunitionCaliber_Construct(t1, group1, diameter1, name1, altname1);
            var a2 = AmmunitionCaliber_Construct(t2, group2, diameter2, name2, altname2);

            a1.Equals(a2).Should().Be(equals);
            a2.Equals(a1).Should().Be(equals);

            if (equals)
                a1.GetHashCode().Should().Be(a2.GetHashCode());
            else
                a1.GetHashCode().Should().NotBe(a2.GetHashCode());
        }

        [Fact]
        public void AmmunitionCaliber_DefaultFactory()
        {
            var dictionary = AmmunitionCaliberFactory.Create();
            dictionary.Should().NotBeEmpty();

            var ammo = dictionary.Find(c => c.Is("12ga"));
            
            ammo.TypeOfAmmunition.Should().Be(AmmunitionCaliberType.Shotgun);
            ammo.CaliberGroup.Should().BeNull();
            ammo.BulletDiameter.Should().Be(18.53.As(DistanceUnit.Millimeter));
            ammo.Name.Should().Be("12ga");
            ammo.AlternativeNames.Should().BeEmpty();

            ammo = dictionary.Find(c => c.Is(".32 WCF"));
            ammo.TypeOfAmmunition.Should().Be(AmmunitionCaliberType.Pistol);
            ammo.CaliberGroup.Should().NotBeNull();
            ammo.CaliberGroup.Should().Be(7.As(DistanceUnit.Millimeter));
            ammo.BulletDiameter.Should().Be(0.312.As(DistanceUnit.Inch));
            ammo.AlternativeNames.Should().Contain(".32-20 Winchester");
            ammo.AlternativeNames.Should().Contain(".32-20 Marlin");

            var ammo1 = dictionary.Find(c => c.Is(".32-20 Marlin"));
            ammo1.Should().BeSameAs(ammo);
        }
    }
}

