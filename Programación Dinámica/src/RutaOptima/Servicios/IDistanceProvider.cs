namespace RutaOptima.Servicios {
    public readonly record struct GeoPoint(double Lat, double Lng);

    public sealed class DistanceUnavailableException : Exception {
        public string Codigo { get; }
        public IReadOnlyList<int> Paradas { get; }

        public DistanceUnavailableException(string message, string codigo = CodigosError.DistanciaNoDisponible,
            IEnumerable<int>? paradas = null) : base(message) {
            Codigo = codigo;
            Paradas = (paradas ?? Enumerable.Empty<int>()).ToList();
        }
    }

   
    public sealed class DistanceMatrix {
         
        public double[,] Distances { get; }

        public IReadOnlyList<GeoPoint> Stops { get; }

        public string Unit { get; }

        private readonly Func<int, int, IReadOnlyList<GeoPoint>> _geometry;

        public DistanceMatrix(double[,] distances, IReadOnlyList<GeoPoint> stops, string unit,
            Func<int, int, IReadOnlyList<GeoPoint>> geometry) {
            Distances = distances;
            Stops = stops;
            Unit = unit;
            _geometry = geometry;
        }

        public IReadOnlyList<GeoPoint> Geometry(int from, int to) => _geometry(from, to);
    }

    public interface IDistanceProvider {
         DistanceMatrix Build(IReadOnlyList<GeoPoint> points);
    }
}
