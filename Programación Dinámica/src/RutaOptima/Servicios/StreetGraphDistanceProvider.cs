using RutaOptima.Algoritmos;
using RutaOptima.Models;

namespace RutaOptima.Servicios {
    public class StreetGraphDistanceProvider : IDistanceProvider {
        
        public const double MaxSnapMeters = 300;

        private readonly Grafo _grafo;

        public StreetGraphDistanceProvider(GrafoService grafoService)
            : this(grafoService.Grafo) {
        }

        public StreetGraphDistanceProvider(Grafo grafo) {
            _grafo = grafo;
        }

        public DistanceMatrix Build(IReadOnlyList<GeoPoint> points) {
            int n = points.Count;

            var nodeIds = new string[n];
            var stops = new GeoPoint[n];
            var lejos = new List<(int Indice, double Metros)>();
            for (int i = 0; i < n; i++) {
                var (nodo, metros) = NearestNode(points[i]);
                if (metros > MaxSnapMeters) lejos.Add((i, metros));
                nodeIds[i] = nodo.Id;
                stops[i] = new GeoPoint(nodo.Lat, nodo.Lng);
            }
                        
            if (lejos.Count > 0) {                
                var partes = lejos.Select((l, k) =>
                    $"{(k == 0 ? NombreParada.Inicial(l.Indice) : NombreParada.De(l.Indice))} está a {l.Metros:0} m").ToList();
                var detalle = partes.Count == 1
                    ? partes[0]
                    : string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[^1];
                throw new DistanceUnavailableException(
                    $"{detalle} de la calle más cercana (máximo {MaxSnapMeters:0} m): " +
                    (lejos.Count == 1 ? "queda fuera del mapa." : "quedan fuera del mapa."),
                    CodigosError.ParadaFueraDelMapa,
                    lejos.Select(l => l.Indice));
            }

            var trees = new Dictionary<string, ArbolCaminos>();
            foreach (var id in nodeIds.Distinct())
                trees[id] = CaminosMinimos.DesdeOrigen(_grafo, id);

            var d = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) {
                    if (i == j) continue;
                    d[i, j] = trees[nodeIds[i]].DistanciaA(nodeIds[j]);
                }

            IReadOnlyList<GeoPoint> Geometry(int from, int to) =>
                trees[nodeIds[from]].CaminoA(nodeIds[to])
                    .Select(id => _grafo.Nodos[id])
                    .Select(nodo => new GeoPoint(nodo.Lat, nodo.Lng))
                    .ToList();

            return new DistanceMatrix(d, stops, "m", Geometry);
        }

        private (Nodo Nodo, double Metros) NearestNode(GeoPoint p) {
            Nodo? best = null;
            double bestMeters = double.PositiveInfinity;
            foreach (var nodo in _grafo.Nodos.Values) {
                var m = Geo.Haversine(p.Lat, p.Lng, nodo.Lat, nodo.Lng);
                if (m < bestMeters) { bestMeters = m; best = nodo; }
            }
            return (best ?? throw new DistanceUnavailableException("El grafo de calles está vacío."), bestMeters);
        }
    }
}
