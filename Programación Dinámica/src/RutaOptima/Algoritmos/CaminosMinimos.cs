using RutaOptimaApi.Models;

namespace RutaOptimaApi.Algoritmos;

/// <summary>
/// Árbol de caminos mínimos desde un nodo origen hacia TODOS los demás nodos alcanzables.
/// </summary>
public sealed class ArbolCaminos
{
    public string Origen { get; }
    public IReadOnlyDictionary<string, double> Distancias { get; }
    private readonly Dictionary<string, string?> _predecesores;

    internal ArbolCaminos(string origen, Dictionary<string, double> distancias, Dictionary<string, string?> predecesores)
    {
        Origen = origen;
        Distancias = distancias;
        _predecesores = predecesores;
    }

    /// <summary>Distancia mínima al nodo (+∞ si no es alcanzable).</summary>
    public double DistanciaA(string destino) =>
        Distancias.TryGetValue(destino, out var d) ? d : double.PositiveInfinity;

    /// <summary>Ids de los nodos del camino mínimo origen → destino (vacío si no es alcanzable).</summary>
    public List<string> CaminoA(string destino)
    {
        var camino = new List<string>();
        if (double.IsPositiveInfinity(DistanciaA(destino))) return camino;

        string? actual = destino;
        while (actual != null)
        {
            camino.Add(actual);
            actual = _predecesores.GetValueOrDefault(actual);
        }
        camino.Reverse();
        return camino;
    }
}

/// <summary>
/// Dijkstra de un solo origen a todos los destinos (sin parar al llegar a uno concreto).
/// Es lo que necesita la matriz de distancias: una corrida por parada en vez de n² corridas.
/// </summary>
public static class CaminosMinimos
{
    public static ArbolCaminos DesdeOrigen(Grafo grafo, string origenId)
    {
        var distancias = new Dictionary<string, double> { [origenId] = 0 };
        var predecesores = new Dictionary<string, string?> { [origenId] = null };
        var visitados = new HashSet<string>();

        var cola = new PriorityQueue<string, double>();
        cola.Enqueue(origenId, 0);

        while (cola.Count > 0)
        {
            var actual = cola.Dequeue();
            if (!visitados.Add(actual)) continue;

            foreach (var (vecino, peso) in grafo.Vecinos(actual))
            {
                if (visitados.Contains(vecino)) continue;

                var nueva = distancias[actual] + peso;
                if (nueva < distancias.GetValueOrDefault(vecino, double.PositiveInfinity))
                {
                    distancias[vecino] = nueva;
                    predecesores[vecino] = actual;
                    cola.Enqueue(vecino, nueva);
                }
            }
        }

        return new ArbolCaminos(origenId, distancias, predecesores);
    }
}
