using Microsoft.AspNetCore.Mvc;
using RutaOptima.Models;
using RutaOptima.Servicios;

namespace RutaOptima.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RepartidorController : ControllerBase
    {
        private readonly RepartidorService _repartidor;

        public RepartidorController(RepartidorService repartidor)
        {
            _repartidor = repartidor;
        }

        /// <summary>
        /// POST /api/repartidor/ruta
        /// Recibe el depósito (paradas[0]) y las entregas (paradas[1..]) como lat/lng y devuelve la ruta
        /// óptima por calles calculada con Held-Karp, la del vecino más cercano para comparar, el tamaño del
        /// problema y, con hasta 7 entregas, la tabla DP completa.
        /// </summary>
        [HttpPost("ruta")]
        public IActionResult CalcularRuta([FromBody] RepartidorSolicitud solicitud)
        {
            try
            {
                return Ok(_repartidor.Calcular(solicitud.Paradas));
            }
            catch (SolicitudInvalidaException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (DistanceUnavailableException ex)
            {
                return UnprocessableEntity(new { error = ex.Message, codigo = ex.Codigo, paradas = ex.Paradas });
            }
            catch (RutaNoFactibleException ex)
            {
                return UnprocessableEntity(new { error = ex.Message, codigo = ex.Codigo, paradas = ex.Paradas });
            }
        }
    }
}
