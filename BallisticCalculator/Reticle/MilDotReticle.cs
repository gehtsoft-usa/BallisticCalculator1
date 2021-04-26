using System;
using System.Collections.Generic;
using System.Text;
using BallisticCalculator.Reticle.Data;
using Gehtsoft.Measurements;

namespace BallisticCalculator.Reticle
{
    /// <summary>
    /// MilDot reticle definition
    /// </summary>
    public sealed class MilDotReticle : ReticleDefinition
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MilDotReticle() 
        {
            Name = "Mil-Dot Reticle";
            Size = new ReticlePosition(12, 12, AngularUnit.Mil); 
            Zero = new ReticlePosition(6, 6, AngularUnit.Mil);

            Elements.Add(new ReticleCircle() { Center = new ReticlePosition(0, 0, AngularUnit.Mil), Radius = AngularUnit.Mil.New(6), Color = "black", LineWidth = AngularUnit.Mil.New(0.01) });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(-5, 0, AngularUnit.Mil), End = new ReticlePosition(5, 0, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.01), Color = "black" });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, -5, AngularUnit.Mil), End = new ReticlePosition(0, 5, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.01), Color = "black" });

            Elements.Add(new ReticleLine() { Start = new ReticlePosition(-5, 0, AngularUnit.Mil), End = new ReticlePosition(-6, 0, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.2), Color = "black" });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(5, 0, AngularUnit.Mil), End = new ReticlePosition(6, 0, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.2), Color = "black" });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, -5, AngularUnit.Mil), End = new ReticlePosition(0, -6, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.2), Color = "black" });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, 5, AngularUnit.Mil), End = new ReticlePosition(0, 6, AngularUnit.Mil), LineWidth = AngularUnit.Mil.New(0.2), Color = "black" });

            for (int i = -4; i <= 4; i++)
            {
                if (i == 0)
                    continue;

                Elements.Add(new ReticleCircle() { Center = new ReticlePosition(i, 0, AngularUnit.Mil), Radius = AngularUnit.Mil.New(0.1), LineWidth = AngularUnit.Mil.New(0.01), Fill = true, Color = "black"});
                Elements.Add(new ReticleCircle() { Center = new ReticlePosition(0, i, AngularUnit.Mil), Radius = AngularUnit.Mil.New(0.1), LineWidth = AngularUnit.Mil.New(0.01), Fill = true, Color = "black"});
            }

            for (int i = -1; i >= -4; i--)
                BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint() { Position = new ReticlePosition(0, i, AngularUnit.Mil), TextOffset = AngularUnit.Mil.New(1), TextHeight = AngularUnit.Mil.New(0.3) });
        }
    }
}
