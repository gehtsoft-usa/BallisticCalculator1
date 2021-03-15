namespace BallisticCalculator
{
    internal class G6DragTable : DragTable
    {
        public override DragTableId TableId => DragTableId.G6;

        public static int PointCount => gDataPoints.Length;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
        {
            new DragTableDataPoint(0.00, 0.2617),
            new DragTableDataPoint(0.05, 0.2553),
            new DragTableDataPoint(0.10, 0.2491),
            new DragTableDataPoint(0.15, 0.2432),
            new DragTableDataPoint(0.20, 0.2376),
            new DragTableDataPoint(0.25, 0.2324),
            new DragTableDataPoint(0.30, 0.2278),
            new DragTableDataPoint(0.35, 0.2238),
            new DragTableDataPoint(0.40, 0.2205),
            new DragTableDataPoint(0.45, 0.2177),
            new DragTableDataPoint(0.50, 0.2155),
            new DragTableDataPoint(0.55, 0.2138),
            new DragTableDataPoint(0.60, 0.2126),
            new DragTableDataPoint(0.65, 0.2121),
            new DragTableDataPoint(0.70, 0.2122),
            new DragTableDataPoint(0.75, 0.2132),
            new DragTableDataPoint(0.80, 0.2154),
            new DragTableDataPoint(0.85, 0.2194),
            new DragTableDataPoint(0.875, 0.2229),
            new DragTableDataPoint(0.90, 0.2297),
            new DragTableDataPoint(0.925, 0.2449),
            new DragTableDataPoint(0.95, 0.2732),
            new DragTableDataPoint(0.975, 0.3141),
            new DragTableDataPoint(1.0, 0.3597),
            new DragTableDataPoint(1.025, 0.3994),
            new DragTableDataPoint(1.05, 0.4261),
            new DragTableDataPoint(1.075, 0.4402),
            new DragTableDataPoint(1.10, 0.4465),
            new DragTableDataPoint(1.125, 0.4490),
            new DragTableDataPoint(1.15, 0.4497),
            new DragTableDataPoint(1.175, 0.4494),
            new DragTableDataPoint(1.20, 0.4482),
            new DragTableDataPoint(1.225, 0.4464),
            new DragTableDataPoint(1.25, 0.4441),
            new DragTableDataPoint(1.30, 0.4390),
            new DragTableDataPoint(1.35, 0.4336),
            new DragTableDataPoint(1.40, 0.4279),
            new DragTableDataPoint(1.45, 0.4221),
            new DragTableDataPoint(1.50, 0.4162),
            new DragTableDataPoint(1.55, 0.4102),
            new DragTableDataPoint(1.60, 0.4042),
            new DragTableDataPoint(1.65, 0.3981),
            new DragTableDataPoint(1.70, 0.3919),
            new DragTableDataPoint(1.75, 0.3855),
            new DragTableDataPoint(1.80, 0.3788),
            new DragTableDataPoint(1.85, 0.3721),
            new DragTableDataPoint(1.90, 0.3652),
            new DragTableDataPoint(1.95, 0.3583),
            new DragTableDataPoint(2.00, 0.3515),
            new DragTableDataPoint(2.05, 0.3447),
            new DragTableDataPoint(2.10, 0.3381),
            new DragTableDataPoint(2.15, 0.3314),
            new DragTableDataPoint(2.20, 0.3249),
            new DragTableDataPoint(2.25, 0.3185),
            new DragTableDataPoint(2.30, 0.3122),
            new DragTableDataPoint(2.35, 0.3060),
            new DragTableDataPoint(2.40, 0.3000),
            new DragTableDataPoint(2.45, 0.2941),
            new DragTableDataPoint(2.50, 0.2883),
            new DragTableDataPoint(2.60, 0.2772),
            new DragTableDataPoint(2.70, 0.2668),
            new DragTableDataPoint(2.80, 0.2574),
            new DragTableDataPoint(2.90, 0.2487),
            new DragTableDataPoint(3.00, 0.2407),
            new DragTableDataPoint(3.10, 0.2333),
            new DragTableDataPoint(3.20, 0.2265),
            new DragTableDataPoint(3.30, 0.2202),
            new DragTableDataPoint(3.40, 0.2144),
            new DragTableDataPoint(3.50, 0.2089),
            new DragTableDataPoint(3.60, 0.2039),
            new DragTableDataPoint(3.70, 0.1991),
            new DragTableDataPoint(3.80, 0.1947),
            new DragTableDataPoint(3.90, 0.1905),
            new DragTableDataPoint(4.00, 0.1866),
            new DragTableDataPoint(4.20, 0.1794),
            new DragTableDataPoint(4.40, 0.1730),
            new DragTableDataPoint(4.60, 0.1673),
            new DragTableDataPoint(4.80, 0.1621),
            new DragTableDataPoint(5.00, 0.1574),
        };

        public G6DragTable() : base(gDataPoints)
        {
        }
    }
}
