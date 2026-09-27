using Grafos.Service;
using Microsoft.AspNetCore.Mvc;
using RutaOptimaApi.Algoritmos;

namespace RutaOptimaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RutaController : ControllerBase
{
    private readonly GrafoService _grafoService;

    public RutaController(GrafoService grafoService)
    {
        _grafoService = grafoService;
    }

    /// <summary>
    /// GET /api/ruta/grafo
    /// Devuelve todos los nodos y aristas del grafo cargado, para que
    /// el frontend pueda dibujar el mapa completo.
    /// </summary>
    [HttpGet("grafo")]
    public IActionResult ObtenerGrafo()
    {
        var grafo = _grafoService.Grafo;

        var nodos = grafo.Nodos.Values.Select(n => new { n.Id, n.Nombre, n.Lat, n.Lng });

        var aristas = grafo.Nodos.Keys
            .SelectMany(origenId => grafo.Vecinos(origenId)
                .Select(v => new { Origen = origenId, Destino = v.Destino, Peso = v.Peso }))
            .ToList();

        return Ok(new
        {
            cantidadNodos = grafo.CantidadNodos,
            cantidadAristas = grafo.CantidadAristas,
            nodos,
            aristas
        });
    }

    /// <summary>
    /// GET /api/ruta?origen=N00&destino=N45&algoritmo=astar
    /// Calcula la ruta óptima entre dos nodos usando Dijkstra o A*.
    /// </summary>
    [HttpGet]
    public IActionResult CalcularRuta(
        [FromQuery] string origen,
        [FromQuery] string destino,
        [FromQuery] string algoritmo = "astar")
    {
        var grafo = _grafoService.Grafo;

        if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(destino))
            return BadRequest(new { error = "Debe indicar 'origen' y 'destino'." });

        if (!grafo.ExisteNodo(origen))
            return NotFound(new { error = $"El nodo origen '{origen}' no existe en el grafo." });

        if (!grafo.ExisteNodo(destino))
            return NotFound(new { error = $"El nodo destino '{destino}' no existe en el grafo." });

        var resultado = algoritmo.Trim().ToLowerInvariant() switch
        {
            "dijkstra" => Dijkstra.CalcularRuta(grafo, origen, destino),
            "astar" or "a*" => AStar.CalcularRuta(grafo, origen, destino),
            _ => null
        };

        if (resultado is null)
            return BadRequest(new { error = "Algoritmo inválido. Use 'dijkstra' o 'astar'." });

        if (!resultado.Encontrada)
            return NotFound(new { error = "No existe una ruta entre esos dos nodos.", resultado });

        return Ok(resultado);
    }

    /// <summary>
    /// GET /api/ruta/comparar?origen=N00&destino=N45
    /// Corre Dijkstra y A* sobre el mismo par de nodos y devuelve ambos resultados juntos
    /// </summary>
    [HttpGet("comparar")]
    public IActionResult Comparar([FromQuery] string origen, [FromQuery] string destino)
    {
        var grafo = _grafoService.Grafo;

        if (!grafo.ExisteNodo(origen) || !grafo.ExisteNodo(destino))
            return NotFound(new { error = "Origen o destino no existen en el grafo." });

        var dijkstra = Dijkstra.CalcularRuta(grafo, origen, destino);
        var astar = AStar.CalcularRuta(grafo, origen, destino);

        return Ok(new { dijkstra, astar });
    }
}
