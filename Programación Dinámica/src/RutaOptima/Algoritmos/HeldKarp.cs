namespace RutaOptimaApi.Algoritmos;

/// <summary>
/// Resultado de Held-Karp. Se conserva TODA la tabla para poder mostrarla y explicar cada celda.
///
/// Los subconjuntos son máscaras de bits sobre las entregas: el bit b representa la parada b+1
/// (el depósito, parada 0, siempre es el origen y no forma parte del subconjunto).
///   Cost[S*M + (j-1)]   = menor costo de salir del depósito, visitar exactamente las paradas de S
///                         y terminar en la parada j (+∞ si j no está en S).
///   Parent[S*M + (j-1)] = parada visitada justo antes de j en ese camino óptimo (0 = el depósito).
/// </summary>
public sealed class HeldKarpResult
{
    public int Stops { get; }
    public int Deliveries => Stops - 1;
    public double[] Cost { get; }
    public sbyte[] Parent { get; }

    /// <summary>Recorrido óptimo: [0, a, b, ..., 0]. Vacío si no es factible (ver <see cref="IsFeasible"/>).</summary>
    public int[] Tour { get; }

    /// <summary>Costo del recorrido óptimo; +∞ si no existe ningún recorrido que use solo tramos con distancia finita.</summary>
    public double TotalCost { get; }

    /// <summary>
    /// false si no hay ningún recorrido [0, ..., 0] que pase por todas las entregas con distancias finitas
    /// (una parada aislada, no se puede volver al depósito, calles de un solo sentido...).
    /// En ese caso <see cref="Tour"/> está vacío y no se reconstruye nada.
    /// </summary>
    public bool IsFeasible => double.IsFinite(TotalCost);

    /// <summary>Estados (subconjunto, parada final) que se calcularon.</summary>
    public long StatesComputed { get; }

    /// <summary>Veces que se evaluó "dp[S sin j][k] + d(k, j)".</summary>
    public long TransitionsEvaluated { get; }

    internal HeldKarpResult(int stops, double[] cost, sbyte[] parent, int[] tour, double total, long states, long transitions)
    {
        Stops = stops;
        Cost = cost;
        Parent = parent;
        Tour = tour;
        TotalCost = total;
        StatesComputed = states;
        TransitionsEvaluated = transitions;
    }

    public double CostOf(int subset, int endStop) => Cost[subset * Deliveries + (endStop - 1)];
    public int ParentOf(int subset, int endStop) => Parent[subset * Deliveries + (endStop - 1)];
}

/// <summary>
/// Held-Karp: Programación Dinámica para el problema del viajante (TSP) con depósito.
///
/// PROBLEMA
///   Un repartidor sale del depósito (parada 0), debe visitar cada una de las otras paradas
///   exactamente una vez y volver al depósito. ¿Qué orden da el recorrido más corto?
///   Probar todos los órdenes cuesta (n-1)! rutas: inviable desde unas 13 entregas.
///
/// ESTADO
///   dp[S][j] = menor costo de un camino que sale del depósito, visita exactamente
///              las entregas del conjunto S y termina en la entrega j (j ∈ S).
///
/// RECURRENCIA
///   dp[S][j] = min sobre k ∈ S \ {j} de ( dp[S \ {j}][k] + d(k, j) )
///   (la parada anterior a j fue alguna k del resto del conjunto; se prueba cada una)
///
/// CASO BASE
///   dp[{j}][j] = d(0, j)      (ir directo del depósito a j)
///
/// RESPUESTA
///   min sobre j de ( dp[todas][j] + d(j, 0) )      (cerrar el recorrido volviendo al depósito)
///   El orden se reconstruye siguiendo la parada anterior (Parent) desde la mejor j.
///
/// DESEMPATES (resultado determinista): gana la k más pequeña; en la respuesta, la j más pequeña.
///
/// COMPLEJIDAD: tiempo O(n² · 2ⁿ), espacio O(n · 2ⁿ). Sirve también si d no es simétrica.
///
/// DISTANCIAS INFINITAS: d(a, b) = +∞ significa "no se puede ir de a a b". El algoritmo las acepta;
/// si ningún recorrido las evita, devuelve un resultado no factible (IsFeasible = false, Tour vacío)
/// en lugar de reconstruir un recorrido imposible.
/// </summary>
public static class HeldKarp
{
    /// <summary>Más paradas que esto consume demasiada memoria (≈ 2ⁿ·n números).</summary>
    public const int MaxStops = 20;

    public static HeldKarpResult Solve(double[,] dist)
    {
        int n = dist.GetLength(0);
        if (n < 1 || dist.GetLength(1) != n) throw new ArgumentException("La matriz de distancias debe ser cuadrada y no vacía.", nameof(dist));
        if (n > MaxStops) throw new ArgumentException($"Held-Karp admite como máximo {MaxStops} paradas (recibió {n}).", nameof(dist));

        int m = n - 1;                                   // entregas
        if (m == 0)
            return new HeldKarpResult(n, Array.Empty<double>(), Array.Empty<sbyte>(), new[] { 0 }, 0, 0, 0);

        int subsets = 1 << m;
        var cost = new double[subsets * m];
        var parent = new sbyte[subsets * m];
        Array.Fill(cost, double.PositiveInfinity);
        Array.Fill(parent, (sbyte)-1);

        long states = 0, transitions = 0;

        // Casos base: dp[{j}][j] = d(0, j)
        for (int j = 1; j <= m; j++)
        {
            int s = 1 << (j - 1);
            cost[s * m + (j - 1)] = dist[0, j];
            parent[s * m + (j - 1)] = 0;
            states++;
        }

        // Los subconjuntos se recorren en orden numérico: S sin j siempre es menor que S, ya está calculado.
        for (int s = 1; s < subsets; s++)
        {
            if ((s & (s - 1)) == 0) continue;            // un solo elemento: es caso base

            for (int j = 1; j <= m; j++)
            {
                int bitJ = 1 << (j - 1);
                if ((s & bitJ) == 0) continue;

                int rest = s ^ bitJ;
                double best = double.PositiveInfinity;
                int bestK = -1;

                for (int k = 1; k <= m; k++)
                {
                    if ((rest & (1 << (k - 1))) == 0) continue;
                    transitions++;
                    double c = cost[rest * m + (k - 1)] + dist[k, j];
                    if (c < best) { best = c; bestK = k; }
                }

                cost[s * m + (j - 1)] = best;
                parent[s * m + (j - 1)] = (sbyte)bestK;
                states++;
            }
        }

        // Cerrar el recorrido: volver al depósito desde la última entrega.
        int full = subsets - 1;
        double total = double.PositiveInfinity;
        int lastStop = 1;
        for (int j = 1; j <= m; j++)
        {
            double c = cost[full * m + (j - 1)] + dist[j, 0];
            if (c < total) { total = c; lastStop = j; }
        }

        // Si todas las opciones de cierre son +∞ no hay recorrido posible: no hay nada que reconstruir
        // (Parent quedó en -1 en las celdas sin ningún candidato finito).
        if (!double.IsFinite(total))
            return new HeldKarpResult(n, cost, parent, Array.Empty<int>(), double.PositiveInfinity, states, transitions);

        int[] inner = Backtrack(parent, m, full, lastStop);
        var tour = new int[m + 2];
        tour[0] = 0;
        Array.Copy(inner, 0, tour, 1, m);
        tour[m + 1] = 0;

        return new HeldKarpResult(n, cost, parent, tour, total, states, transitions);
    }

    /// <summary>
    /// Camino óptimo [0, ..., endStop] que visita exactamente las entregas de subset y termina en endStop.
    /// Es lo que representa cada celda de la tabla DP.
    /// </summary>
    public static int[] PathFor(HeldKarpResult result, int subset, int endStop)
    {
        int m = result.Deliveries;
        if (m == 0) return new[] { 0 };
        if (endStop < 1 || endStop > m || (subset & (1 << (endStop - 1))) == 0)
            throw new ArgumentException("La parada final debe pertenecer al subconjunto.");
        if (!double.IsFinite(result.CostOf(subset, endStop)))
            throw new ArgumentException("No existe un camino con distancias finitas para esa celda.");

        int[] inner = Backtrack(result.Parent, m, subset, endStop);
        var path = new int[inner.Length + 1];
        path[0] = 0;
        Array.Copy(inner, 0, path, 1, inner.Length);
        return path;
    }

    private static int[] Backtrack(sbyte[] parent, int m, int subset, int endStop)
    {
        var reversed = new List<int>();
        int s = subset, j = endStop;
        while (s != 0)
        {
            reversed.Add(j);
            int k = parent[s * m + (j - 1)];
            if (k < 0) throw new InvalidOperationException("Celda sin predecesor: no hay camino finito hasta ella.");
            s ^= 1 << (j - 1);
            j = k;
        }
        reversed.Reverse();
        return reversed.ToArray();
    }

    /// <summary>Cantidad de estados que calcula Held-Karp para n paradas: m · 2^(m-1), con m = n-1.</summary>
    public static double CountStates(int stops)
    {
        int m = stops - 1;
        return m <= 0 ? 0 : m * Math.Pow(2, m - 1);
    }

    /// <summary>Transiciones evaluadas: m(m-1) · 2^(m-2).</summary>
    public static double CountTransitions(int stops)
    {
        int m = stops - 1;
        return m < 2 ? 0 : (double)m * (m - 1) * Math.Pow(2, m - 2);
    }

    public static string SubsetLabel(int subset)
    {
        var items = new List<int>();
        for (int b = 0; b < 31; b++)
            if ((subset & (1 << b)) != 0) items.Add(b + 1);
        return "{" + string.Join(",", items) + "}";
    }

    public static int PopCount(int subset) => System.Numerics.BitOperations.PopCount((uint)subset);
}
