namespace BallisticCalculator
{
    internal class G8DragTable : DragTable
    {
        public override DragTableId TableId => DragTableId.G8;

        public static int PointCount => gDataPoints.Length;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint []
        {
            new DragTableDataPoint(0.00, 0.2105),
            new DragTableDataPoint(0.05, 0.2105),
            new DragTableDataPoint(0.10, 0.2104),
            new DragTableDataPoint(0.15, 0.2104),
            new DragTableDataPoint(0.20, 0.2103),
            new DragTableDataPoint(0.25, 0.2103),
            new DragTableDataPoint(0.30, 0.2103),
            new DragTableDataPoint(0.35, 0.2103),
            new DragTableDataPoint(0.40, 0.2103),
            new DragTableDataPoint(0.45, 0.2102),
            new DragTableDataPoint(0.50, 0.2102),
            new DragTableDataPoint(0.55, 0.2102),
            new DragTableDataPoint(0.60, 0.2102),
            new DragTableDataPoint(0.65, 0.2102),
            new DragTableDataPoint(0.70, 0.2103),
            new DragTableDataPoint(0.75, 0.2103),
            new DragTableDataPoint(0.80, 0.2104),
            new DragTableDataPoint(0.825, 0.2104),
            new DragTableDataPoint(0.85, 0.2105),
            new DragTableDataPoint(0.875, 0.2106),
            new DragTableDataPoint(0.90, 0.2109),
            new DragTableDataPoint(0.925, 0.2183),
            new DragTableDataPoint(0.95, 0.2571),
            new DragTableDataPoint(0.975, 0.3358),
            new DragTableDataPoint(1.0, 0.4068),
            new DragTableDataPoint(1.025, 0.4378),
            new DragTableDataPoint(1.05, 0.4476),
            new DragTableDataPoint(1.075, 0.4493),
            new DragTableDataPoint(1.10, 0.4477),
            new DragTableDataPoint(1.125, 0.4450),
            new DragTableDataPoint(1.15, 0.4419),
            new DragTableDataPoint(1.20, 0.4353),
            new DragTableDataPoint(1.25, 0.4283),
            new DragTableDataPoint(1.30, 0.4208),
            new DragTableDataPoint(1.35, 0.4133),
            new DragTableDataPoint(1.40, 0.4059),
            new DragTableDataPoint(1.45, 0.3986),
            new DragTableDataPoint(1.50, 0.3915),
            new DragTableDataPoint(1.55, 0.3845),
            new DragTableDataPoint(1.60, 0.3777),
            new DragTableDataPoint(1.65, 0.3710),
            new DragTableDataPoint(1.70, 0.3645),
            new DragTableDataPoint(1.75, 0.3581),
            new DragTableDataPoint(1.80, 0.3519),
            new DragTableDataPoint(1.85, 0.3458),
            new DragTableDataPoint(1.90, 0.3400),
            new DragTableDataPoint(1.95, 0.3343),
            new DragTableDataPoint(2.00, 0.3288),
            new DragTableDataPoint(2.05, 0.3234),
            new DragTableDataPoint(2.10, 0.3182),
            new DragTableDataPoint(2.15, 0.3131),
            new DragTableDataPoint(2.20, 0.3081),
            new DragTableDataPoint(2.25, 0.3032),
            new DragTableDataPoint(2.30, 0.2983),
            new DragTableDataPoint(2.35, 0.2937),
            new DragTableDataPoint(2.40, 0.2891),
            new DragTableDataPoint(2.45, 0.2845),
            new DragTableDataPoint(2.50, 0.2802),
            new DragTableDataPoint(2.60, 0.2720),
            new DragTableDataPoint(2.70, 0.2642),
            new DragTableDataPoint(2.80, 0.2569),
            new DragTableDataPoint(2.90, 0.2499),
            new DragTableDataPoint(3.00, 0.2432),
            new DragTableDataPoint(3.10, 0.2368),
            new DragTableDataPoint(3.20, 0.2308),
            new DragTableDataPoint(3.30, 0.2251),
            new DragTableDataPoint(3.40, 0.2197),
            new DragTableDataPoint(3.50, 0.2147),
            new DragTableDataPoint(3.60, 0.2101),
            new DragTableDataPoint(3.70, 0.2058),
            new DragTableDataPoint(3.80, 0.2019),
            new DragTableDataPoint(3.90, 0.1983),
            new DragTableDataPoint(4.00, 0.1950),
            new DragTableDataPoint(4.20, 0.1890),
            new DragTableDataPoint(4.40, 0.1837),
            new DragTableDataPoint(4.60, 0.1791),
            new DragTableDataPoint(4.80, 0.1750),
            new DragTableDataPoint(5.00, 0.1713),
        };

        public G8DragTable() : base(gDataPoints)
        {
        }
    }
}
