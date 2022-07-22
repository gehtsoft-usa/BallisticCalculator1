using BallisticCalculator.Serialization;
using FluentAssertions;
using Gehtsoft.Measurements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Xunit;

namespace BallisticCalculator.Test.Data.Serialization
{
    public class SerializerTest
    {
        [BXmlElement("child")]
        public class ChildClass
        {
            [BXmlProperty("name")]
            public string Name { get; set; }

            public ChildClass()
            {
            }

            public ChildClass(string name)
            {
                Name = name;
            }
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

        [BXmlElement("collector")]
        public class CollectorClass
        {
            [BXmlProperty(Name = "array", Collection = true)]
            public ChildClass[] Array { get; set; }

            [BXmlProperty(Name = "list", Collection = true)]
            public List<ChildClass> List { get; set; }

            [BXmlProperty(Name = "list1", Collection = true)]
            public List<ChildClass> List1 { get; } = new List<ChildClass>();

            [BXmlProperty(Name = "array2", Collection = true, Optional = true)]
            public ChildClass[] Array2 { get; set; }
        }

        [BXmlSelect(typeof(Implementation1), typeof(Implementation2))]
        public interface IClassInterface
        {
        }

        [BXmlElement("impl1")]
        public class Implementation1 : IClassInterface
        {
            [BXmlProperty("name")]
            public string Name { get; set; }
        }

        [BXmlElement("impl2")]
        public class Implementation2 : IClassInterface
        {
            [BXmlProperty("id")]
            public int ID { get; set; }
        }

        [BXmlElement("container")]
        public class InterfaceContainer
        {
            [BXmlProperty(Name = "property", ChildElement = true, Optional = true)]
            public IClassInterface Property { get; set; }

            [BXmlProperty(Name = "collection", Collection = true)]
            public List<IClassInterface> Elements { get; } = new List<IClassInterface>();
        }

        [BXmlElement("flatenning-container")]
        public class FlatteningContainer
        {
            [BXmlProperty(Name = "value1", ChildElement = true, FlattenChild = true)]
            public Implementation1 Value1 { get; set; }

            [BXmlProperty(Name = "value2", ChildElement = true, FlattenChild = true)]
            public Implementation2 Value2 { get; set; }
        }

        [Fact]
        public void SerializedXmlTest1()
        {
            SerializerRoundtrip serializer = new SerializerRoundtrip();

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
            SerializerRoundtrip serializer = new SerializerRoundtrip();

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
            element.ChildNodes.Count.Should().Be(0);

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
        public void TestCollector1()
        {
            CollectorClass collector = new CollectorClass()
            {
                Array = new ChildClass[] { new ChildClass("array1_1"), new ChildClass("array1_2") },
                List = new List<ChildClass>() { new ChildClass("list_1"), new ChildClass("list_2"), new ChildClass("list_3") },
                Array2 = new ChildClass[] { new ChildClass("array2_1"), new ChildClass("array2_2") },
            };

            collector.List1.AddRange(new ChildClass[] { new ChildClass("list1_1"), new ChildClass("list1_2"), new ChildClass("list1_3") });

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(collector);
            var collector2 = serializer.Deserialize<CollectorClass>(xml);

            collector2.Array.Should().NotBeNull();
            collector2.Array2.Should().NotBeNull();
            collector2.List.Should().NotBeNull();
            collector2.List1.Should().NotBeNull();

            collector2.Array.Should().HaveCount(collector.Array.Length);
            collector2.Array2.Should().HaveCount(collector.Array2.Length);
            collector2.List.Should().HaveCount(collector.List.Count);

            collector2.Array[0].Name.Should().Be(collector.Array[0].Name);
            collector2.Array[1].Name.Should().Be(collector.Array[1].Name);

            collector2.Array2[0].Name.Should().Be(collector.Array2[0].Name);
            collector2.Array2[1].Name.Should().Be(collector.Array2[1].Name);

            collector2.List[0].Name.Should().Be(collector.List[0].Name);
            collector2.List[1].Name.Should().Be(collector.List[1].Name);
            collector2.List[2].Name.Should().Be(collector.List[2].Name);

            collector2.List1[0].Name.Should().Be(collector.List1[0].Name);
            collector2.List1[1].Name.Should().Be(collector.List1[1].Name);
            collector2.List1[2].Name.Should().Be(collector.List1[2].Name);
        }

        [Fact]
        public void TestCollector2()
        {
            CollectorClass collector = new CollectorClass()
            {
                Array = Array.Empty<ChildClass>(),
                List = new List<ChildClass>(),
                Array2 = null,
            };

            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(collector);
            var collector2 = serializer.Deserialize<CollectorClass>(xml);

            collector2.Array.Should().NotBeNull();
            collector2.Array2.Should().BeNull();
            collector2.List.Should().NotBeNull();

            collector2.Array.Should().HaveCount(0);
            collector2.List.Should().HaveCount(0);
        }

        [Fact]
        public void TestSelect()
        {
            {
                var container = new InterfaceContainer()
                {
                    Property = new Implementation1() { Name = "Text1" }
                };

                SerializerRoundtrip serializer = new SerializerRoundtrip();
                var xml = serializer.Serialize(container);
                var container2 = serializer.Deserialize<InterfaceContainer>(xml);

                container2.Property.Should().NotBeNull();
                container2.Property.Should().BeOfType<Implementation1>();
                (container2.Property as Implementation1).Name.Should().Be("Text1");
            }

            {
                var container = new InterfaceContainer()
                {
                    Property = new Implementation2() { ID = 10 }
                };

                SerializerRoundtrip serializer = new SerializerRoundtrip();
                var xml = serializer.Serialize(container);
                var container2 = serializer.Deserialize<InterfaceContainer>(xml);

                container2.Property.Should().NotBeNull();
                container2.Property.Should().BeOfType<Implementation2>();
                (container2.Property as Implementation2).ID.Should().Be(10);
            }

            {
                var container = new InterfaceContainer()
                {
                    Property = null
                };

                container.Elements.AddRange(new IClassInterface[]
                {
                    new Implementation1() {Name = "Item1" },
                    new Implementation2() {ID = 1},
                    new Implementation1() {Name = "Item2" },
                    new Implementation2() {ID = 2},
                });

                SerializerRoundtrip serializer = new SerializerRoundtrip();
                var xml = serializer.Serialize(container);
                var container2 = serializer.Deserialize<InterfaceContainer>(xml);

                container2.Property.Should().BeNull();
                container2.Elements.Should().NotBeNull();
                container2.Elements.Should().HaveCount(4);
                container2.Elements[0].Should().BeOfType(typeof(Implementation1));
                (container2.Elements[0] as Implementation1).Name.Should().Be("Item1");
                container2.Elements[1].Should().BeOfType(typeof(Implementation2));
                (container2.Elements[1] as Implementation2).ID.Should().Be(1);
                container2.Elements[2].Should().BeOfType(typeof(Implementation1));
                (container2.Elements[2] as Implementation1).Name.Should().Be("Item2");
                container2.Elements[3].Should().BeOfType(typeof(Implementation2));
                (container2.Elements[3] as Implementation2).ID.Should().Be(2);
            }
        }

        [Fact]
        public void FlatenningContainer()
        {
            FlatteningContainer container = new FlatteningContainer()
            {
                Value1 = new Implementation1() { Name = "123" },
                Value2 = new Implementation2() { ID = 456 },
            };
            SerializerRoundtrip serializer = new SerializerRoundtrip();
            var xml = serializer.Serialize(container);
            xml.ChildNodes.Count.Should().Be(0);
            var container2 = serializer.Deserialize<FlatteningContainer>(xml);
            container2.Value1.Name.Should().Be("123");
            container2.Value2.ID.Should().Be(456);
        }

        [Fact]
        public void SerializeStream1()
        {
            FlatteningContainer container = new FlatteningContainer()
            {
                Value1 = new Implementation1() { Name = "123" },
                Value2 = new Implementation2() { ID = 456 },
            };

            using MemoryStream ms = new MemoryStream();
            BallisticXmlSerializer.SerializeToStream(container, ms);

            using MemoryStream ms1 = new MemoryStream(ms.ToArray());
            var container1 = BallisticXmlDeserializer.ReadFromStream<FlatteningContainer>(ms1);

            container1.Value1?.Name.Should().Be("123");
            container1.Value2?.ID.Should().Be(456);
        }

        [Fact]
        public void SerializeStream2()
        {
            FlatteningContainer container = new FlatteningContainer()
            {
                Value1 = new Implementation1() { Name = "123" },
                Value2 = new Implementation2() { ID = 456 },
            };

            using MemoryStream ms = new MemoryStream();
            container.BallisticXmlSerialize(ms);

            using MemoryStream ms1 = new MemoryStream(ms.ToArray());
            var container1 = ms1.BallisticXmlDeserialize<FlatteningContainer>();

            container1.Value1?.Name.Should().Be("123");
            container1.Value2?.ID.Should().Be(456);
        }

        [Fact]
        public void SerializeToFile()
        {
            var tempFileName = Path.GetTempFileName();
            try
            {
                FlatteningContainer container = new FlatteningContainer()
                {
                    Value1 = new Implementation1() { Name = "123" },
                    Value2 = new Implementation2() { ID = 456 },
                };
                BallisticXmlSerializer.SerializeToFile(container, tempFileName);

                File.Exists(tempFileName).Should().BeTrue();

                var container1 = BallisticXmlDeserializer.ReadFromFile<FlatteningContainer>(tempFileName);
                container1.Value1?.Name.Should().Be("123");
                container1.Value2?.ID.Should().Be(456);
            }
            finally
            {
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
                File.Exists(tempFileName).Should().BeFalse();
            }
        }
    }
}
