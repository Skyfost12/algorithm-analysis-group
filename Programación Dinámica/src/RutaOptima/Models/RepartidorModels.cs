namespace RutaOptima.Models;

// Contratos JSON del endpoint POST /api/repartidor/ruta (ruta óptima de repartidor con Held-Karp).

/// <summary>Una parada pedida por el cliente. La parada 0 es el depósito.</summary>
public record ParadaSolicitud(double Lat, double Lng);

public record RepartidorSolicitud(List<ParadaSolicitud>? Paradas);

/// <summary>Posición de la parada ya ajustada a la calle más cercana.</summary>
public record ParadaResuelta(int Indice, double Lat, double Lng);

/// <summary>Un tramo del recorrido entre dos paradas consecutivas, con su polilínea por calles ([lat, lng]).</summary>
public record Tramo(int Desde, int Hasta, double Distancia, double[][] Geometria);

/// <summary>Un recorrido completo [0, ..., 0] con el largo de cada tramo.</summary>
public record Recorrido(int[] Paradas, double Distancia, List<Tramo> Tramos);

/// <summary>Tamaño del problema: fuerza bruta frente a lo que realmente calcula la programación dinámica.</summary>
public record TamanoProblema(
    int Paradas,
    int Entregas,
    double RutasPosibles,
    double EstadosDp,
    double TransicionesDp,
    double LlamadasRecursionSinMemo);

public record CandidatoDp(int K, double DpResto, double DistanciaKJ, double Suma);

/// <summary>Una celda dp[S][j] con su recurrencia desarrollada con números reales.</summary>
public record CeldaDp(int J, double Valor, int Padre, bool EnRutaOptima, CandidatoDp[]? Candidatos, int[] Camino);

public record FilaDp(int Mascara, string Etiqueta, int Tamano, List<CeldaDp> Celdas);

public record TablaDp(int Entregas, List<FilaDp> Filas, List<List<double[][]?>> GeometriaPares);

public record RepartidorResultado(
    string Unidad,
    List<ParadaResuelta> Paradas,
    Recorrido Optima,
    Recorrido VecinoCercano,
    double PorcentajeExtraVecinoCercano,
    TamanoProblema Problema,
    double TiempoMatrizMs,
    double TiempoHeldKarpMs,
    TablaDp? TablaDp);
