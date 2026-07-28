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
        private const string BLACK = "black";
        private const string TITLE = "Mil-Dot Reticle";

        /// <summary>
        /// Constructor
        /// </summary>
        public MilDotReticle() 
        {
            Name = TITLE;
            Size = new ReticlePosition(12, 12, AngularUnit.MRad); 
            Zero = new ReticlePosition(6, 6, AngularUnit.MRad);

            Elements.Add(new ReticleCircle() { Center = new ReticlePosition(0, 0, AngularUnit.MRad), Radius = AngularUnit.MRad.New(6), Color = BLACK, LineWidth = AngularUnit.MRad.New(0.01) });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(-5, 0, AngularUnit.MRad), End = new ReticlePosition(5, 0, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.01), Color = BLACK });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, -5, AngularUnit.MRad), End = new ReticlePosition(0, 5, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.01), Color = BLACK });

            Elements.Add(new ReticleLine() { Start = new ReticlePosition(-5, 0, AngularUnit.MRad), End = new ReticlePosition(-6, 0, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.2), Color = BLACK });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(5, 0, AngularUnit.MRad), End = new ReticlePosition(6, 0, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.2), Color = BLACK });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, -5, AngularUnit.MRad), End = new ReticlePosition(0, -6, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.2), Color = BLACK });
            Elements.Add(new ReticleLine() { Start = new ReticlePosition(0, 5, AngularUnit.MRad), End = new ReticlePosition(0, 6, AngularUnit.MRad), LineWidth = AngularUnit.MRad.New(0.2), Color = BLACK });

            for (int i = -4; i <= 4; i++)
            {
                if (i == 0)
                    continue;

                Elements.Add(new ReticleCircle() { Center = new ReticlePosition(i, 0, AngularUnit.MRad), Radius = AngularUnit.MRad.New(0.1), LineWidth = AngularUnit.MRad.New(0.01), Fill = true, Color = BLACK});
                Elements.Add(new ReticleCircle() { Center = new ReticlePosition(0, i, AngularUnit.MRad), Radius = AngularUnit.MRad.New(0.1), LineWidth = AngularUnit.MRad.New(0.01), Fill = true, Color = BLACK});
            }

            for (int i = -1; i >= -4; i--)
                BulletDropCompensator.Add(new ReticleBulletDropCompensatorPoint() { Position = new ReticlePosition(0, i, AngularUnit.MRad), TextOffset = AngularUnit.MRad.New(1), TextHeight = AngularUnit.MRad.New(0.3) });
        }
    }
}
