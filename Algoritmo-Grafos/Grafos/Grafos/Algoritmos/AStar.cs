using Grafos.Models;
using System.Diagnostics;

namespace Grafos.Algoritmos;

/// <summary>
/// Algoritmo A*: igual que Dijkstra, pero prioriza los nodos usando
/// f(n) = g(n) + h(n), donde:
///   g(n) = costo real acumulado desde el origen hasta n
///   h(n) = estimación (heurística) del costo restante de n al destino
/// </summary>
public static class AStar
{
    public static ResultadoRuta CalcularRuta(Grafo grafo, string origenId, string destinoId)
    {
        var cronometro = Stopwatch.StartNew();

        var nodoDestino = grafo.Nodos[destinoId];

        var costoReal = new Dictionary<string, double>(); // g(n)
        var predecesores = new Dictionary<string, string?>();
        var visitados = new HashSet<string>();
        var ordenExploracion = new List<string>();

        foreach (var nodoId in grafo.Nodos.Keys)
            costoReal[nodoId] = double.PositiveInfinity;

        costoReal[origenId] = 0;
        predecesores[origenId] = null;

        var colaPrioridad = new PriorityQueue<string, double>();
        colaPrioridad.Enqueue(origenId, Heuristica(grafo.Nodos[origenId], nodoDestino));

        while (colaPrioridad.Count > 0)
        {
            var actual = colaPrioridad.Dequeue();

            if (visitados.Contains(actual))
                continue;

            visitados.Add(actual);
            ordenExploracion.Add(actual);

            if (actual == destinoId)
                break;

            foreach (var (vecino, peso) in grafo.Vecinos(actual))
            {
                if (visitados.Contains(vecino))
                    continue;

                var nuevoCostoReal = costoReal[actual] + peso;

                if (nuevoCostoReal < costoReal.GetValueOrDefault(vecino, double.PositiveInfinity))
                {
                    costoReal[vecino] = nuevoCostoReal;
                    predecesores[vecino] = actual;

                    var prioridad = nuevoCostoReal + Heuristica(grafo.Nodos[vecino], nodoDestino);
                    colaPrioridad.Enqueue(vecino, prioridad);
                }
            }
        }

        cronometro.Stop();

        var resultado = new ResultadoRuta
        {
            Algoritmo = "A*",
            NodosExplorados = ordenExploracion.Count,
            OrdenExploracion = ordenExploracion,
            TiempoCalculoMs = cronometro.ElapsedMilliseconds
        };

        if (!costoReal.ContainsKey(destinoId) || double.IsPositiveInfinity(costoReal[destinoId]))
        {
            resultado.Encontrada = false;
            return resultado;
        }

        resultado.Encontrada = true;
        resultado.DistanciaTotal = costoReal[destinoId];
        resultado.Ruta = ReconstruirRuta(grafo, predecesores, destinoId);

        return resultado;
    }

    /// <summary>
    /// Distancia Haversine entre dos coordenadas (lat/lng), en metros.
    /// Es la heurística h(n): la distancia "en línea recta" siempre es
    /// menor o igual a la distancia real por calles, por lo que nunca
    /// engaña al algoritmo para que se salte una ruta mejor.
    /// </summary>
    private static double Heuristica(Nodo a, Nodo b)
    {
        const double radioTierraMetros = 6_371_000;

        var lat1 = GradosARadianes(a.Lat);
        var lat2 = GradosARadianes(b.Lat);
        var deltaLat = GradosARadianes(b.Lat - a.Lat);
        var deltaLng = GradosARadianes(b.Lng - a.Lng);

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return radioTierraMetros * c;
    }

    private static double GradosARadianes(double grados) => grados * Math.PI / 180.0;

    private static List<Nodo> ReconstruirRuta(
        Grafo grafo,
        Dictionary<string, string?> predecesores,
        string destinoId)
    {
        var camino = new List<Nodo>();
        string? actual = destinoId;

        while (actual != null)
        {
            camino.Add(grafo.Nodos[actual]);
            actual = predecesores.GetValueOrDefault(actual);
        }

        camino.Reverse();
        return camino;
    }
}
