using System;
using AwesomeAssertions;
using BallisticCalculator.Reticle.Data;
using Gehtsoft.Measurements;
using Xunit;

namespace BallisticCalculator.Test.Reticle
{
    /// <summary>
    /// Covers Clone() of the individual reticle elements and the reticle collections,
    /// plus the collection-specific operations (Sort, Swap).
    /// </summary>
    public class CollectionAndClone
    {
        [Fact]
        public void Rectangle_Clone_IsDeepCopy()
        {
            var r1 = new ReticleRectangle()
            {
                TopLeft = new ReticlePosition(1, 2, AngularUnit.Mil),
                Size = new ReticlePosition(3, 4, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.1),
                Color = "green",
                Fill = true,
            };

            var r2 = r1.Clone() as ReticleRectangle;

            r2.Should().NotBeNull();
            r2.Should().NotBeSameAs(r1);
            r2.Should().Be(r1);
            r2.TopLeft.Should().NotBeSameAs(r1.TopLeft);
            r2.Size.Should().NotBeSameAs(r1.Size);
        }

        [Fact]
        public void Text_Clone_IsDeepCopy()
        {
            var t1 = new ReticleText()
            {
                Position = new ReticlePosition(1, 2, AngularUnit.Mil),
                TextHeight = AngularUnit.Mil.New(1),
                Text = "abc",
                Color = "red",
                Anchor = TextAnchor.Center,
            };

            var t2 = t1.Clone() as ReticleText;

            t2.Should().NotBeNull();
            t2.Should().NotBeSameAs(t1);
            t2.Should().Be(t1);
            t2.Anchor.Should().Be(TextAnchor.Center);
            t2.Position.Should().NotBeSameAs(t1.Position);
        }

        [Fact]
        public void Line_Clone_IsDeepCopy()
        {
            var l1 = new ReticleLine()
            {
                Start = new ReticlePosition(1, 2, AngularUnit.Mil),
                End = new ReticlePosition(3, 4, AngularUnit.Mil),
                LineWidth = AngularUnit.Mil.New(0.2),
                Color = "blue",
            };

            var l2 = l1.Clone() as ReticleLine;

            l2.Should().NotBeNull();
            l2.Should().NotBeSameAs(l1);
            l2.Should().Be(l1);
            l2.Start.Should().NotBeSameAs(l1.Start);
            l2.End.Should().NotBeSameAs(l1.End);
        }

        [Fact]
        public void BulletDropCompensatorCollection_Sort_OrdersByDescendingY()
        {
            var collection = new ReticleBulletDropCompensatorPointCollection();
            collection.Add(new ReticleBulletDropCompensatorPoint() { Position = new ReticlePosition(0, -3, AngularUnit.Mil) });
            collection.Add(new ReticleBulletDropCompensatorPoint() { Position = new ReticlePosition(0, -1, AngularUnit.Mil) });
            collection.Add(new ReticleBulletDropCompensatorPoint() { Position = new ReticlePosition(0, -2, AngularUnit.Mil) });

            collection.Sort();

            collection[0].Position.Y.Should().Be(AngularUnit.Mil.New(-1));
            collection[1].Position.Y.Should().Be(AngularUnit.Mil.New(-2));
            collection[2].Position.Y.Should().Be(AngularUnit.Mil.New(-3));
        }

        [Fact]
        public void BulletDropCompensatorCollection_Clone_IsDeepCopy()
        {
            var c1 = new ReticleBulletDropCompensatorPointCollection();
            c1.Add(new ReticleBulletDropCompensatorPoint()
            {
                Position = new ReticlePosition(0, -5, AngularUnit.Mil),
                TextOffset = AngularUnit.Mil.New(1),
                TextHeight = AngularUnit.Mil.New(0.5),
            });

            var c2 = c1.Clone();
            var c3 = ((ICloneable)c1).Clone() as ReticleBulletDropCompensatorPointCollection;

            c2.Should().NotBeSameAs(c1);
            c2.Count.Should().Be(1);
            c2[0].Should().Be(c1[0]);
            c2[0].Should().NotBeSameAs(c1[0]);

            c3.Should().NotBeNull();
            c3.Count.Should().Be(1);
            c3[0].Should().Be(c1[0]);
        }

        [Fact]
        public void PathElementsCollection_Swap_ExchangesElements()
        {
            var collection = new ReticlePathElementsCollection();
            var a = new ReticlePathElementMoveTo() { Position = new ReticlePosition(1, 1, AngularUnit.Mil) };
            var b = new ReticlePathElementLineTo() { Position = new ReticlePosition(2, 2, AngularUnit.Mil) };
            collection.Add(a);
            collection.Add(b);

            collection.Swap(0, 1);

            collection[0].Should().BeSameAs(b);
            collection[1].Should().BeSameAs(a);
        }

        [Fact]
        public void PathElementsCollection_Clone_IsDeepCopy()
        {
            var c1 = new ReticlePathElementsCollection();
            c1.Add(new ReticlePathElementMoveTo() { Position = new ReticlePosition(1, 1, AngularUnit.Mil) });
            c1.Add(new ReticlePathElementLineTo() { Position = new ReticlePosition(2, 2, AngularUnit.Mil) });

            var c2 = c1.Clone();
            var c3 = ((ICloneable)c1).Clone() as ReticlePathElementsCollection;

            c2.Should().NotBeSameAs(c1);
            c2.Count.Should().Be(2);
            c2[0].Should().Be(c1[0]);
            c2[0].Should().NotBeSameAs(c1[0]);

            c3.Should().NotBeNull();
            c3.Count.Should().Be(2);
        }
    }
}
