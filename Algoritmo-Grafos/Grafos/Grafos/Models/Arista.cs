namespace Grafos.Models {
    public class Arista {
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public double Peso { get; set; }

        /// <summary>
        /// Indica si la arista es dirigida o no. 
        /// Si es dirigida, el flujo de información solo puede ir desde el nodo de origen al nodo de destino. 
        /// Si no es dirigida, el flujo de información puede ir en ambas direcciones entre los nodos.
        /// </summary>
        public bool Dirigida { get; set; }
    }
}
