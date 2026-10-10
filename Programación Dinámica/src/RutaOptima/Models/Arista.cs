namespace RutaOptima.Models {
    public class Arista {
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public double Peso { get; set; }

        public bool Dirigida { get; set; } = false;
    }
}
