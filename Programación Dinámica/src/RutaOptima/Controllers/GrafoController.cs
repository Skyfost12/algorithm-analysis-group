using Microsoft.AspNetCore.Mvc;
using RutaOptima.Servicios;

namespace RutaOptima.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrafoController : ControllerBase
    {
        private readonly GrafoService _grafoService;

        public GrafoController(GrafoService grafoService)
        {
            _grafoService = grafoService;
        }

        [HttpGet]
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
    }
}
