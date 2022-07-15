﻿namespace BallisticCalculator
{
    internal class GIDragTable : DragTable
    {
        public override DragTableId TableId => DragTableId.GI;

        public static int PointCount => gDataPoints.Length;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
        {
           new DragTableDataPoint(0.00, 0.2282),
           new DragTableDataPoint(0.05, 0.2282),
           new DragTableDataPoint(0.10, 0.2282),
           new DragTableDataPoint(0.15, 0.2282),
           new DragTableDataPoint(0.20, 0.2282),
           new DragTableDataPoint(0.25, 0.2282),
           new DragTableDataPoint(0.30, 0.2282),
           new DragTableDataPoint(0.35, 0.2282),
           new DragTableDataPoint(0.40, 0.2282),
           new DragTableDataPoint(0.45, 0.2282),
           new DragTableDataPoint(0.50, 0.2282),
           new DragTableDataPoint(0.55, 0.2282),
           new DragTableDataPoint(0.60, 0.2282),
           new DragTableDataPoint(0.65, 0.2282),
           new DragTableDataPoint(0.70, 0.2282),
           new DragTableDataPoint(0.725, 0.2353),
           new DragTableDataPoint(0.75, 0.2434),
           new DragTableDataPoint(0.775, 0.2515),
           new DragTableDataPoint(0.80, 0.2596),
           new DragTableDataPoint(0.825, 0.2677),
           new DragTableDataPoint(0.85, 0.2759),
           new DragTableDataPoint(0.875, 0.2913),
           new DragTableDataPoint(0.90, 0.3170),
           new DragTableDataPoint(0.925, 0.3442),
           new DragTableDataPoint(0.95, 0.3728),
           new DragTableDataPoint(1.0, 0.4349),
           new DragTableDataPoint(1.05, 0.5034),
           new DragTableDataPoint(1.075, 0.5402),
           new DragTableDataPoint(1.10, 0.5756),
           new DragTableDataPoint(1.125, 0.5887),
           new DragTableDataPoint(1.15, 0.6018),
           new DragTableDataPoint(1.175, 0.6149),
           new DragTableDataPoint(1.20, 0.6279),
           new DragTableDataPoint(1.225, 0.6418),
           new DragTableDataPoint(1.25, 0.6423),
           new DragTableDataPoint(1.30, 0.6423),
           new DragTableDataPoint(1.35, 0.6423),
           new DragTableDataPoint(1.40, 0.6423),
           new DragTableDataPoint(1.45, 0.6423),
           new DragTableDataPoint(1.50, 0.6423),
           new DragTableDataPoint(1.55, 0.6423),
           new DragTableDataPoint(1.60, 0.6423),
           new DragTableDataPoint(1.625, 0.6407),
           new DragTableDataPoint(1.65, 0.6378),
           new DragTableDataPoint(1.70, 0.6321),
           new DragTableDataPoint(1.75, 0.6266),
           new DragTableDataPoint(1.80, 0.6213),
           new DragTableDataPoint(1.85, 0.6163),
           new DragTableDataPoint(1.90, 0.6113),
           new DragTableDataPoint(1.95, 0.6066),
           new DragTableDataPoint(2.00, 0.6020),
           new DragTableDataPoint(2.05, 0.5976),
           new DragTableDataPoint(2.10, 0.5933),
           new DragTableDataPoint(2.15, 0.5891),
           new DragTableDataPoint(2.20, 0.5850),
           new DragTableDataPoint(2.25, 0.5811),
           new DragTableDataPoint(2.30, 0.5773),
           new DragTableDataPoint(2.35, 0.5733),
           new DragTableDataPoint(2.40, 0.5679),
           new DragTableDataPoint(2.45, 0.5626),
           new DragTableDataPoint(2.50, 0.5576),
           new DragTableDataPoint(2.60, 0.5478),
           new DragTableDataPoint(2.70, 0.5386),
           new DragTableDataPoint(2.80, 0.5298),
           new DragTableDataPoint(2.90, 0.5215),
           new DragTableDataPoint(3.00, 0.5136),
           new DragTableDataPoint(3.10, 0.5061),
           new DragTableDataPoint(3.20, 0.4989),
           new DragTableDataPoint(3.30, 0.4921),
           new DragTableDataPoint(3.40, 0.4855),
           new DragTableDataPoint(3.50, 0.4792),
           new DragTableDataPoint(3.60, 0.4732),
           new DragTableDataPoint(3.70, 0.4674),
           new DragTableDataPoint(3.80, 0.4618),
           new DragTableDataPoint(3.90, 0.4564),
           new DragTableDataPoint(4.00, 0.4513),
           new DragTableDataPoint(4.20, 0.4415),
           new DragTableDataPoint(4.40, 0.4323),
           new DragTableDataPoint(4.60, 0.4238),
           new DragTableDataPoint(4.80, 0.4157),
           new DragTableDataPoint(5.00, 0.4082),
        };

        public GIDragTable() : base(gDataPoints)
        {
        }
    }
}
