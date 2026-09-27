
namespace Grafos.Models {
    public class Grafo {
        public Dictionary<string, Nodo> Nodos { get; set; } = new();
        private readonly Dictionary<string, List<(string Destino, double Peso)>> _adyacencia = new();

        public void AgregarNodo(Nodo nodo) {
            Nodos[nodo.Id] = nodo;
            if (!_adyacencia.ContainsKey(nodo.Id))
                _adyacencia[nodo.Id] = new List<(string Destino, double Peso)>();
        }

        public void AgregarArista(Arista arista) {
            if (!_adyacencia.ContainsKey(arista.Origen))
                _adyacencia[arista.Origen] = new List<(string, double)>();

            _adyacencia[arista.Origen].Add((arista.Destino, arista.Peso));

            if (!arista.Dirigida) {
                if (!_adyacencia.ContainsKey(arista.Destino))
                    _adyacencia[arista.Destino] = new List<(string, double)>();

                _adyacencia[arista.Destino].Add((arista.Origen, arista.Peso));
            }
        }

        public IReadOnlyList<(string Destino, double Peso)> Vecinos(string nodoId) {
            return _adyacencia.TryGetValue(nodoId, out var lista)
                ? lista
                : new List<(string, double)>();
        }

        public bool ExisteNodo(string id) => Nodos.ContainsKey(id);

        public int CantidadNodos => Nodos.Count;

        public int CantidadAristas => _adyacencia.Values.Sum(l => l.Count);
    }
}
