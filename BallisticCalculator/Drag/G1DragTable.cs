namespace BallisticCalculator
{
    internal class G1DragTable : DragTable
    {
        public static int PointCount => gDataPoints.Length;

        public override DragTableId TableId => DragTableId.G1;

        public static DragTableDataPoint DataPoint(int index) => gDataPoints[index];

        private static readonly DragTableDataPoint[] gDataPoints = new DragTableDataPoint[]
        {
            new DragTableDataPoint(0.00, 0.2629),
            new DragTableDataPoint(0.05, 0.2558),
            new DragTableDataPoint(0.10, 0.2487),
            new DragTableDataPoint(0.15, 0.2413),
            new DragTableDataPoint(0.20, 0.2344),
            new DragTableDataPoint(0.25, 0.2278),
            new DragTableDataPoint(0.30, 0.2214),
            new DragTableDataPoint(0.35, 0.2155),
            new DragTableDataPoint(0.40, 0.2104),
            new DragTableDataPoint(0.45, 0.2061),
            new DragTableDataPoint(0.50, 0.2032),
            new DragTableDataPoint(0.55, 0.2020),
            new DragTableDataPoint(0.60, 0.2034),
            new DragTableDataPoint(0.70, 0.2165),
            new DragTableDataPoint(0.725, 0.2230),
            new DragTableDataPoint(0.75, 0.2313),
            new DragTableDataPoint(0.775, 0.2417),
            new DragTableDataPoint(0.80, 0.2546),
            new DragTableDataPoint(0.825, 0.2706),
            new DragTableDataPoint(0.85, 0.2901),
            new DragTableDataPoint(0.875, 0.3136),
            new DragTableDataPoint(0.90, 0.3415),
            new DragTableDataPoint(0.925, 0.3734),
            new DragTableDataPoint(0.95, 0.4084),
            new DragTableDataPoint(0.975, 0.4448),
            new DragTableDataPoint(1.0, 0.4805),
            new DragTableDataPoint(1.025, 0.5136),
            new DragTableDataPoint(1.05, 0.5427),
            new DragTableDataPoint(1.075, 0.5677),
            new DragTableDataPoint(1.10, 0.5883),
            new DragTableDataPoint(1.125, 0.6053),
            new DragTableDataPoint(1.15, 0.6191),
            new DragTableDataPoint(1.20, 0.6393),
            new DragTableDataPoint(1.25, 0.6518),
            new DragTableDataPoint(1.30, 0.6589),
            new DragTableDataPoint(1.35, 0.6621),
            new DragTableDataPoint(1.40, 0.6625),
            new DragTableDataPoint(1.45, 0.6607),
            new DragTableDataPoint(1.50, 0.6573),
            new DragTableDataPoint(1.55, 0.6528),
            new DragTableDataPoint(1.60, 0.6474),
            new DragTableDataPoint(1.65, 0.6413),
            new DragTableDataPoint(1.70, 0.6347),
            new DragTableDataPoint(1.75, 0.6280),
            new DragTableDataPoint(1.80, 0.6210),
            new DragTableDataPoint(1.85, 0.6141),
            new DragTableDataPoint(1.90, 0.6072),
            new DragTableDataPoint(1.95, 0.6003),
            new DragTableDataPoint(2.00, 0.5934),
            new DragTableDataPoint(2.05, 0.5867),
            new DragTableDataPoint(2.10, 0.5804),
            new DragTableDataPoint(2.15, 0.5743),
            new DragTableDataPoint(2.20, 0.5685),
            new DragTableDataPoint(2.25, 0.5630),
            new DragTableDataPoint(2.30, 0.5577),
            new DragTableDataPoint(2.35, 0.5527),
            new DragTableDataPoint(2.40, 0.5481),
            new DragTableDataPoint(2.45, 0.5438),
            new DragTableDataPoint(2.50, 0.5397),
            new DragTableDataPoint(2.60, 0.5325),
            new DragTableDataPoint(2.70, 0.5264),
            new DragTableDataPoint(2.80, 0.5211),
            new DragTableDataPoint(2.90, 0.5168),
            new DragTableDataPoint(3.00, 0.5133),
            new DragTableDataPoint(3.10, 0.5105),
            new DragTableDataPoint(3.20, 0.5084),
            new DragTableDataPoint(3.30, 0.5067),
            new DragTableDataPoint(3.40, 0.5054),
            new DragTableDataPoint(3.50, 0.5040),
            new DragTableDataPoint(3.60, 0.5030),
            new DragTableDataPoint(3.70, 0.5022),
            new DragTableDataPoint(3.80, 0.5016),
            new DragTableDataPoint(3.90, 0.5010),
            new DragTableDataPoint(4.00, 0.5006),
            new DragTableDataPoint(4.20, 0.4998),
            new DragTableDataPoint(4.40, 0.4995),
            new DragTableDataPoint(4.60, 0.4992),
            new DragTableDataPoint(4.80, 0.4990),
            new DragTableDataPoint(5.00, 0.4988)
        };

        public G1DragTable() : base(gDataPoints)
        {
        }
    }
}
