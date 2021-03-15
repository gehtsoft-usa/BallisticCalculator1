namespace BallisticCalculator
{
    internal class G5DragTable : DragTable
    {
        public override DragTableId TableId => DragTableId.G5;

        public static int PointCount => gDataPoints.Length;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
        {
            new DragTableDataPoint(0.00, 0.1710),
            new DragTableDataPoint(0.05, 0.1719),
            new DragTableDataPoint(0.10, 0.1727),
            new DragTableDataPoint(0.15, 0.1732),
            new DragTableDataPoint(0.20, 0.1734),
            new DragTableDataPoint(0.25, 0.1730),
            new DragTableDataPoint(0.30, 0.1718),
            new DragTableDataPoint(0.35, 0.1696),
            new DragTableDataPoint(0.40, 0.1668),
            new DragTableDataPoint(0.45, 0.1637),
            new DragTableDataPoint(0.50, 0.1603),
            new DragTableDataPoint(0.55, 0.1566),
            new DragTableDataPoint(0.60, 0.1529),
            new DragTableDataPoint(0.65, 0.1497),
            new DragTableDataPoint(0.70, 0.1473),
            new DragTableDataPoint(0.75, 0.1463),
            new DragTableDataPoint(0.80, 0.1489),
            new DragTableDataPoint(0.85, 0.1583),
            new DragTableDataPoint(0.875, 0.1672),
            new DragTableDataPoint(0.90, 0.1815),
            new DragTableDataPoint(0.925, 0.2051),
            new DragTableDataPoint(0.95, 0.2413),
            new DragTableDataPoint(0.975, 0.2884),
            new DragTableDataPoint(1.0, 0.3379),
            new DragTableDataPoint(1.025, 0.3785),
            new DragTableDataPoint(1.05, 0.4032),
            new DragTableDataPoint(1.075, 0.4147),
            new DragTableDataPoint(1.10, 0.4201),
            new DragTableDataPoint(1.15, 0.4278),
            new DragTableDataPoint(1.20, 0.4338),
            new DragTableDataPoint(1.25, 0.4373),
            new DragTableDataPoint(1.30, 0.4392),
            new DragTableDataPoint(1.35, 0.4403),
            new DragTableDataPoint(1.40, 0.4406),
            new DragTableDataPoint(1.45, 0.4401),
            new DragTableDataPoint(1.50, 0.4386),
            new DragTableDataPoint(1.55, 0.4362),
            new DragTableDataPoint(1.60, 0.4328),
            new DragTableDataPoint(1.65, 0.4286),
            new DragTableDataPoint(1.70, 0.4237),
            new DragTableDataPoint(1.75, 0.4182),
            new DragTableDataPoint(1.80, 0.4121),
            new DragTableDataPoint(1.85, 0.4057),
            new DragTableDataPoint(1.90, 0.3991),
            new DragTableDataPoint(1.95, 0.3926),
            new DragTableDataPoint(2.00, 0.3861),
            new DragTableDataPoint(2.05, 0.3800),
            new DragTableDataPoint(2.10, 0.3741),
            new DragTableDataPoint(2.15, 0.3684),
            new DragTableDataPoint(2.20, 0.3630),
            new DragTableDataPoint(2.25, 0.3578),
            new DragTableDataPoint(2.30, 0.3529),
            new DragTableDataPoint(2.35, 0.3481),
            new DragTableDataPoint(2.40, 0.3435),
            new DragTableDataPoint(2.45, 0.3391),
            new DragTableDataPoint(2.50, 0.3349),
            new DragTableDataPoint(2.60, 0.3269),
            new DragTableDataPoint(2.70, 0.3194),
            new DragTableDataPoint(2.80, 0.3125),
            new DragTableDataPoint(2.90, 0.3060),
            new DragTableDataPoint(3.00, 0.2999),
            new DragTableDataPoint(3.10, 0.2942),
            new DragTableDataPoint(3.20, 0.2889),
            new DragTableDataPoint(3.30, 0.2838),
            new DragTableDataPoint(3.40, 0.2790),
            new DragTableDataPoint(3.50, 0.2745),
            new DragTableDataPoint(3.60, 0.2703),
            new DragTableDataPoint(3.70, 0.2662),
            new DragTableDataPoint(3.80, 0.2624),
            new DragTableDataPoint(3.90, 0.2588),
            new DragTableDataPoint(4.00, 0.2553),
            new DragTableDataPoint(4.20, 0.2488),
            new DragTableDataPoint(4.40, 0.2429),
            new DragTableDataPoint(4.60, 0.2376),
            new DragTableDataPoint(4.80, 0.2326),
            new DragTableDataPoint(5.00, 0.2280),
        };

        public G5DragTable() : base(gDataPoints)
        {
        }
    }
}
