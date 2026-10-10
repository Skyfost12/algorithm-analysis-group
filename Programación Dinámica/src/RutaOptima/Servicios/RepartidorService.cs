using System.Diagnostics;
using RutaOptima.Algoritmos;
using RutaOptima.Models;

namespace RutaOptima.Servicios
{
    public sealed class SolicitudInvalidaException : Exception
    {
        public SolicitudInvalidaException(string message) : base(message) { }
    }

    public class RepartidorService
    {
        public const int MaxParadas = 18;

        public const int MaxEntregasTabla = 7;

        private readonly IDistanceProvider _distancias;

        public RepartidorService(IDistanceProvider distancias)
        {
            _distancias = distancias;
        }

        public RepartidorResultado Calcular(IReadOnlyList<ParadaSolicitud>? paradas)
        {
            if (paradas is null || paradas.Count == 0)
                throw new SolicitudInvalidaException("Debe indicar al menos el depósito (parada 0).");
            if (paradas.Count > MaxParadas)
                throw new SolicitudInvalidaException(
                    $"Máximo {MaxParadas} paradas (depósito + {MaxParadas - 1} entregas); recibió {paradas.Count}.");
            if (paradas.Any(p => !double.IsFinite(p.Lat) || !double.IsFinite(p.Lng) ||
                                 Math.Abs(p.Lat) > 90 || Math.Abs(p.Lng) > 180))
                throw new SolicitudInvalidaException("Hay coordenadas inválidas en las paradas.");

            var cronoMatriz = Stopwatch.StartNew();
            DistanceMatrix matriz = _distancias.Build(paradas.Select(p => new GeoPoint(p.Lat, p.Lng)).ToList());
            cronoMatriz.Stop();
            double[,] d = matriz.Distances;
            int n = paradas.Count;

            var cronoHk = Stopwatch.StartNew();
            HeldKarpResult hk = HeldKarp.Solve(d);
            cronoHk.Stop();

            if (!hk.IsFeasible)
                throw Diagnosticar(d);

            TourResult vecino = Reference.NearestNeighbor(d);

            double extra = hk.TotalCost > 0 ? (vecino.TotalCost - hk.TotalCost) / hk.TotalCost * 100 : 0;

            return new RepartidorResultado(
                Unidad: matriz.Unit,
                Paradas: matriz.Stops.Select((s, i) => new ParadaResuelta(i, s.Lat, s.Lng)).ToList(),
                Optima: ArmarRecorrido(matriz, hk.Tour, hk.TotalCost),
                VecinoCercano: ArmarRecorrido(matriz, vecino.Tour, vecino.TotalCost),
                PorcentajeExtraVecinoCercano: extra,
                Problema: new TamanoProblema(
                    Paradas: n,
                    Entregas: n - 1,
                    RutasPosibles: Factorial(n - 1),
                    EstadosDp: HeldKarp.CountStates(n),
                    TransicionesDp: HeldKarp.CountTransitions(n),
                    LlamadasRecursionSinMemo: Reference.CountNaiveCalls(n)),
                TiempoMatrizMs: cronoMatriz.Elapsed.TotalMilliseconds,
                TiempoHeldKarpMs: cronoHk.Elapsed.TotalMilliseconds,
                TablaDp: n - 1 >= 1 && n - 1 <= MaxEntregasTabla && TodasFinitas(d) ? ArmarTabla(matriz, hk) : null);
        }

        private static bool TodasFinitas(double[,] d)
        {
            foreach (double x in d)
                if (!double.IsFinite(x)) return false;
            return true;
        }

        private static RutaNoFactibleException Diagnosticar(double[,] d)
        {
            int n = d.GetLength(0);
            bool[] desdeDeposito = Alcanzables(d, adelante: true);
            bool[] haciaDeposito = Alcanzables(d, adelante: false);

            var sinLlegada = Enumerable.Range(1, n - 1).Where(j => !desdeDeposito[j]).ToList();
            var sinVuelta = Enumerable.Range(1, n - 1).Where(j => desdeDeposito[j] && !haciaDeposito[j]).ToList();

            if (sinLlegada.Count == 0 && sinVuelta.Count == 0)
                return new RutaNoFactibleException(
                    "No existe ningún recorrido que pase por todas las paradas y vuelva al depósito con las calles disponibles.",
                    CodigosError.SinRecorrido, Array.Empty<int>());

            var partes = new List<string>();
            if (sinLlegada.Count > 0)
                partes.Add($"{Capital(NombreParada.Lista(sinLlegada))} " +
                           (sinLlegada.Count == 1 ? "no es alcanzable" : "no son alcanzables") +
                           " por las calles desde el depósito.");
            if (sinVuelta.Count > 0)
                partes.Add($"Desde {NombreParada.Lista(sinVuelta)} no se puede volver al depósito por las calles " +
                           "(¿calle de un solo sentido?).");

            return new RutaNoFactibleException(string.Join(" ", partes), CodigosError.ParadaInalcanzable,
                sinLlegada.Concat(sinVuelta).OrderBy(j => j));
        }

        private static string Capital(string texto) => char.ToUpperInvariant(texto[0]) + texto[1..];

        private static bool[] Alcanzables(double[,] d, bool adelante)
        {
            int n = d.GetLength(0);
            var visto = new bool[n];
            var pila = new Stack<int>();
            visto[0] = true;
            pila.Push(0);
            while (pila.Count > 0)
            {
                int a = pila.Pop();
                for (int b = 0; b < n; b++)
                {
                    if (visto[b]) continue;
                    double tramo = adelante ? d[a, b] : d[b, a];
                    if (double.IsFinite(tramo)) { visto[b] = true; pila.Push(b); }
                }
            }
            return visto;
        }

        private static Recorrido ArmarRecorrido(DistanceMatrix matriz, int[] tour, double total)
        {
            var tramos = new List<Tramo>();
            for (int i = 1; i < tour.Length; i++)
                tramos.Add(new Tramo(tour[i - 1], tour[i], matriz.Distances[tour[i - 1], tour[i]],
                    Polilinea(matriz, tour[i - 1], tour[i])));
            return new Recorrido(tour, total, tramos);
        }

        private static double[][] Polilinea(DistanceMatrix matriz, int desde, int hasta) =>
            matriz.Geometry(desde, hasta).Select(p => new[] { p.Lat, p.Lng }).ToArray();

        private static TablaDp ArmarTabla(DistanceMatrix matriz, HeldKarpResult hk)
        {
            double[,] d = matriz.Distances;
            int m = hk.Deliveries;
            
            var optimas = new HashSet<(int, int)>();
            int acumulado = 0;
            for (int t = 1; t <= m; t++)
            {
                acumulado |= 1 << (hk.Tour[t] - 1);
                optimas.Add((acumulado, hk.Tour[t]));
            }

            var filas = new List<FilaDp>();
            foreach (int s in Enumerable.Range(1, (1 << m) - 1).OrderBy(HeldKarp.PopCount).ThenBy(s => s))
            {
                var celdas = new List<CeldaDp>();
                for (int j = 1; j <= m; j++)
                {
                    if ((s & (1 << (j - 1))) == 0) continue;

                    int resto = s ^ (1 << (j - 1));
                    CandidatoDp[]? candidatos = null;
                    if (resto != 0)
                    {
                        candidatos = Enumerable.Range(1, m)
                            .Where(k => (resto & (1 << (k - 1))) != 0)
                            .Select(k => new CandidatoDp(k, hk.CostOf(resto, k), d[k, j], hk.CostOf(resto, k) + d[k, j]))
                            .ToArray();
                    }

                    celdas.Add(new CeldaDp(j, hk.CostOf(s, j), hk.ParentOf(s, j), optimas.Contains((s, j)),
                        candidatos, HeldKarp.PathFor(hk, s, j)));
                }
                filas.Add(new FilaDp(s, HeldKarp.SubsetLabel(s), HeldKarp.PopCount(s), celdas));
            }

            var geometrias = Enumerable.Range(0, m + 1)
                .Select(i => Enumerable.Range(0, m + 1)
                    .Select(j => i == j ? null : Polilinea(matriz, i, j))
                    .ToList())
                .ToList();

            return new TablaDp(m, filas, geometrias);
        }

        private static double Factorial(int k)
        {
            double f = 1;
            for (int i = 2; i <= k; i++) f *= i;
            return f;
        }
    }
}
