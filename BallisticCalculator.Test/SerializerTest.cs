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

    }
}

