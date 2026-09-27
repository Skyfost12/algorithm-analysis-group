using System.Diagnostics;
using Grafos.Models;

namespace Grafos.Algoritmos;

/// <summary>
/// Algoritmo de Dijkstra: encuentra el camino más corto desde un nodo
/// origen a un nodo destino en un grafo con pesos no negativos.
///
/// </summary>
public static class Dijkstra
{
    public static ResultadoRuta CalcularRuta(Grafo grafo, string origenId, string destinoId)
    {
        var cronometro = Stopwatch.StartNew();

        var distancias = new Dictionary<string, double>();
        var predecesores = new Dictionary<string, string?>();
        var visitados = new HashSet<string>();
        var ordenExploracion = new List<string>();

        foreach (var nodoId in grafo.Nodos.Keys)
            distancias[nodoId] = double.PositiveInfinity;

        distancias[origenId] = 0;
        predecesores[origenId] = null;

        // PriorityQueue<TElement, TPriority>: extrae siempre el elemento
        // con menor prioridad (aquí, menor distancia acumulada).
        var colaPrioridad = new PriorityQueue<string, double>();
        colaPrioridad.Enqueue(origenId, 0);

        while (colaPrioridad.Count > 0)
        {
            var actual = colaPrioridad.Dequeue();

            if (visitados.Contains(actual))
                continue;

            visitados.Add(actual);
            ordenExploracion.Add(actual);

            if (actual == destinoId)
                break; // ya encontramos el destino con distancia mínima

            foreach (var (vecino, peso) in grafo.Vecinos(actual))
            {
                if (visitados.Contains(vecino))
                    continue;

                var nuevaDistancia = distancias[actual] + peso;

                if (nuevaDistancia < distancias.GetValueOrDefault(vecino, double.PositiveInfinity))
                {
                    distancias[vecino] = nuevaDistancia;
                    predecesores[vecino] = actual;
                    colaPrioridad.Enqueue(vecino, nuevaDistancia);
                }
            }
        }

        cronometro.Stop();

        var resultado = new ResultadoRuta
        {
            Algoritmo = "Dijkstra",
            NodosExplorados = ordenExploracion.Count,
            OrdenExploracion = ordenExploracion,
            TiempoCalculoMs = cronometro.ElapsedMilliseconds
        };

        if (!distancias.ContainsKey(destinoId) || double.IsPositiveInfinity(distancias[destinoId]))
        {
            resultado.Encontrada = false;
            return resultado;
        }

        resultado.Encontrada = true;
        resultado.DistanciaTotal = distancias[destinoId];
        resultado.Ruta = ReconstruirRuta(grafo, predecesores, destinoId);

        return resultado;
    }

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
