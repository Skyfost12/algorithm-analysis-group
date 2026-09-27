using Grafos.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grafos.Service {
    public class GrafoService {
        public Grafo Grafo { get; }

        public GrafoService(IWebHostEnvironment env) {
            var ruta = Path.Combine(env.ContentRootPath, "Data", "grafo_barrio.json");
            Grafo = CargarDesdeJson(ruta);
        }

        private static Grafo CargarDesdeJson(string rutaArchivo) {
            var json = File.ReadAllText(rutaArchivo);
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var datos = JsonSerializer.Deserialize<GrafoJson>(json, opciones)
                        ?? throw new InvalidOperationException("No se pudo leer el archivo del grafo.");

            var grafo = new Grafo();

            foreach (var n in datos.Nodos)
                grafo.AgregarNodo(new Nodo { Id = n.Id, Nombre = n.Nombre, Lat = n.Lat, Lng = n.Lng });

            foreach (var a in datos.Aristas)
                grafo.AgregarArista(new Arista {
                    Origen = a.Origen,
                    Destino = a.Destino,
                    Peso = a.Peso,
                    Dirigida = a.Dirigida
                });

            return grafo;
        }

        private class GrafoJson {
            [JsonPropertyName("nodos")]
            public List<NodoJson> Nodos { get; set; } = new();

            [JsonPropertyName("aristas")]
            public List<AristaJson> Aristas { get; set; } = new();
        }

        private class NodoJson {
            public string Id { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        private class AristaJson {
            public string Origen { get; set; } = string.Empty;
            public string Destino { get; set; } = string.Empty;
            public double Peso { get; set; }
            public bool Dirigida { get; set; }
        }
    }
}
