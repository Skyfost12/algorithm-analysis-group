using Grafos.Models;

namespace RutaOptimaApi.Models;

/// <summary>
/// Resultado que devuelve la API tras calcular la ruta óptima.
/// Incluye no solo la ruta final, sino también el orden en que
/// el algoritmo fue explorando nodos (útil para animar en el mapa).
/// </summary>
public class ResultadoRuta
{
    public bool Encontrada { get; set; }
    public string Algoritmo { get; set; } = string.Empty;
    public List<Nodo> Ruta { get; set; } = new();
    public double DistanciaTotal { get; set; }
    public int NodosExplorados { get; set; }

    /// <summary>
    /// Orden en que se visitaron/expandieron los nodos.
    /// Sirve para animar la "búsqueda" en el frontend antes
    /// de mostrar la ruta final.
    /// </summary>
    public List<string> OrdenExploracion { get; set; } = new();

    public long TiempoCalculoMs { get; set; }
}
