using System;
using System.IO;
using System.Text;
using System.Xml;
using AwesomeAssertions;
using BallisticCalculator.Reticle.Data;
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Data.Serialization
{
    /// <summary>
    /// Covers the stream/file legacy-entry readers and the error paths of the
    /// BXml deserializer that the happy-path round-trip tests never exercise.
    /// </summary>
    public class LegacyDeserializerTest
    {
        private const string ValidLegacy =
            "<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" name=\"7N23\" />";

        private static void AssertValid(AmmunitionLibraryEntry entry)
        {
            entry.Should().NotBeNull();
            entry.Name.Should().Be("7N23");
            entry.Ammunition.BallisticCoefficient.Should().Be(new BallisticCoefficient(0.297, DragTableId.G1));
            entry.Ammunition.Weight.Should().Be(new Measurement<WeightUnit>(7.7, WeightUnit.Gram));
            entry.Ammunition.MuzzleVelocity.Should().Be(new Measurement<VelocityUnit>(730, VelocityUnit.MetersPerSecond));
        }

        [Fact]
        public void ReadLegacy_FromStream()
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(ValidLegacy));
            AssertValid(BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntryFromStream(ms));
        }

        [Fact]
        public void ReadLegacy_FromFile()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, ValidLegacy);
                AssertValid(BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntryFromFile(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ReadLegacy_NullElement_Throws()
        {
            ((Action)(() => BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry((XmlElement)null)))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ReadLegacy_MissingRequiredAttribute_Throws()
        {
            // 'name' is required (not optional) - dropping it must fail.
            ((Action)(() => BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry(
                "<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" />")))
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ReadLegacy_UnparseableBallisticCoefficient_Throws()
        {
            ((Action)(() => BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry(
                "<ammo-info-ex table=\"G1\" bc=\"not-a-number\" bullet-weight=\"7.70000g\" muzzle-velocity=\"730.00000m/s\" name=\"x\" />")))
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ReadLegacy_UnparseableMeasurement_Throws()
        {
            ((Action)(() => BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntry(
                "<ammo-info-ex table=\"G1\" bc=\"0.297\" bullet-weight=\"garbage\" muzzle-velocity=\"730.00000m/s\" name=\"x\" />")))
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Deserialize_UnmatchedElementType_ThrowsWithElementPath()
        {
            var document = new XmlDocument();
            document.LoadXml("<root><unknown-element /></root>");
            var element = document.DocumentElement.FirstChild as XmlElement;

            var deserializer = new BallisticXmlDeserializer();

            ((Action)(() => deserializer.Deserialize(element, new[] { typeof(ReticleLine) })))
                .Should().Throw<ArgumentException>()
                .WithMessage("*root*unknown-element*");
        }
    }
}
