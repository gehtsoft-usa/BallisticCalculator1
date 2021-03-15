namespace BallisticCalculator
{
    internal class G7DragTable : DragTable
    {
        public override DragTableId TableId => DragTableId.G7;

        public static int PointCount => gDataPoints.Length;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
        {
            new DragTableDataPoint(0.00, 0.1198),
            new DragTableDataPoint(0.05, 0.1197),
            new DragTableDataPoint(0.10, 0.1196),
            new DragTableDataPoint(0.15, 0.1194),
            new DragTableDataPoint(0.20, 0.1193),
            new DragTableDataPoint(0.25, 0.1194),
            new DragTableDataPoint(0.30, 0.1194),
            new DragTableDataPoint(0.35, 0.1194),
            new DragTableDataPoint(0.40, 0.1193),
            new DragTableDataPoint(0.45, 0.1193),
            new DragTableDataPoint(0.50, 0.1194),
            new DragTableDataPoint(0.55, 0.1193),
            new DragTableDataPoint(0.60, 0.1194),
            new DragTableDataPoint(0.65, 0.1197),
            new DragTableDataPoint(0.70, 0.1202),
            new DragTableDataPoint(0.725, 0.1207),
            new DragTableDataPoint(0.75, 0.1215),
            new DragTableDataPoint(0.775, 0.1226),
            new DragTableDataPoint(0.80, 0.1242),
            new DragTableDataPoint(0.825, 0.1266),
            new DragTableDataPoint(0.85, 0.1306),
            new DragTableDataPoint(0.875, 0.1368),
            new DragTableDataPoint(0.90, 0.1464),
            new DragTableDataPoint(0.925, 0.1660),
            new DragTableDataPoint(0.95, 0.2054),
            new DragTableDataPoint(0.975, 0.2993),
            new DragTableDataPoint(1.0, 0.3803),
            new DragTableDataPoint(1.025, 0.4015),
            new DragTableDataPoint(1.05, 0.4043),
            new DragTableDataPoint(1.075, 0.4034),
            new DragTableDataPoint(1.10, 0.4014),
            new DragTableDataPoint(1.125, 0.3987),
            new DragTableDataPoint(1.15, 0.3955),
            new DragTableDataPoint(1.20, 0.3884),
            new DragTableDataPoint(1.25, 0.3810),
            new DragTableDataPoint(1.30, 0.3732),
            new DragTableDataPoint(1.35, 0.3657),
            new DragTableDataPoint(1.40, 0.3580),
            new DragTableDataPoint(1.50, 0.3440),
            new DragTableDataPoint(1.55, 0.3376),
            new DragTableDataPoint(1.60, 0.3315),
            new DragTableDataPoint(1.65, 0.3260),
            new DragTableDataPoint(1.70, 0.3209),
            new DragTableDataPoint(1.75, 0.3160),
            new DragTableDataPoint(1.80, 0.3117),
            new DragTableDataPoint(1.85, 0.3078),
            new DragTableDataPoint(1.90, 0.3042),
            new DragTableDataPoint(1.95, 0.3010),
            new DragTableDataPoint(2.00, 0.2980),
            new DragTableDataPoint(2.05, 0.2951),
            new DragTableDataPoint(2.10, 0.2922),
            new DragTableDataPoint(2.15, 0.2892),
            new DragTableDataPoint(2.20, 0.2864),
            new DragTableDataPoint(2.25, 0.2835),
            new DragTableDataPoint(2.30, 0.2807),
            new DragTableDataPoint(2.35, 0.2779),
            new DragTableDataPoint(2.40, 0.2752),
            new DragTableDataPoint(2.45, 0.2725),
            new DragTableDataPoint(2.50, 0.2697),
            new DragTableDataPoint(2.55, 0.2670),
            new DragTableDataPoint(2.60, 0.2643),
            new DragTableDataPoint(2.65, 0.2615),
            new DragTableDataPoint(2.70, 0.2588),
            new DragTableDataPoint(2.75, 0.2561),
            new DragTableDataPoint(2.80, 0.2533),
            new DragTableDataPoint(2.85, 0.2506),
            new DragTableDataPoint(2.90, 0.2479),
            new DragTableDataPoint(2.95, 0.2451),
            new DragTableDataPoint(3.00, 0.2424),
            new DragTableDataPoint(3.10, 0.2368),
            new DragTableDataPoint(3.20, 0.2313),
            new DragTableDataPoint(3.30, 0.2258),
            new DragTableDataPoint(3.40, 0.2205),
            new DragTableDataPoint(3.50, 0.2154),
            new DragTableDataPoint(3.60, 0.2106),
            new DragTableDataPoint(3.70, 0.2060),
            new DragTableDataPoint(3.80, 0.2017),
            new DragTableDataPoint(3.90, 0.1975),
            new DragTableDataPoint(4.00, 0.1935),
            new DragTableDataPoint(4.20, 0.1861),
            new DragTableDataPoint(4.40, 0.1793),
            new DragTableDataPoint(4.60, 0.1730),
            new DragTableDataPoint(4.80, 0.1672),
            new DragTableDataPoint(5.00, 0.1618)
        };

        public G7DragTable() : base(gDataPoints)
        {
        }
    }
}
