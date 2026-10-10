namespace RutaOptima.Servicios {
    public static class CodigosError {
        public const string DistanciaNoDisponible = "distancia_no_disponible";

        public const string ParadaFueraDelMapa = "parada_fuera_del_mapa";

        public const string ParadaInalcanzable = "parada_inalcanzable";

        public const string SinRecorrido = "sin_recorrido";
    }

    public static class NombreParada {
        public static string De(int indice) => indice == 0 ? "el depósito" : $"la entrega {indice}";

        public static string Inicial(int indice) => indice == 0 ? "El depósito" : $"La entrega {indice}";

        public static string Lista(IEnumerable<int> indices) =>
            string.Join(", ", indices.Select(De));
    }

    public sealed class RutaNoFactibleException : Exception {
        public string Codigo { get; }
        public IReadOnlyList<int> Paradas { get; }

        public RutaNoFactibleException(string message, string codigo, IEnumerable<int> paradas) : base(message) {
            Codigo = codigo;
            Paradas = paradas.ToList();
        }
    }
}