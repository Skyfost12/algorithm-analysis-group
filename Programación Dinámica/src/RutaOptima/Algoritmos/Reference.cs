namespace RutaOptima.Algoritmos;

/// <summary>Resultado de un algoritmo que devuelve un recorrido [0, ..., 0] y su largo.</summary>
public sealed record TourResult(int[] Tour, double TotalCost);

/// <summary>
/// Referencias para comparar contra Held-Karp: la misma recurrencia SIN tabla,
/// fuerza bruta sobre todos los órdenes, y la heurística del vecino más cercano.
/// </summary>
public static class Reference
{
    /// <summary>
    /// Recursión sin memoización (top-down): recalcula los mismos subproblemas una y otra vez.
    /// `calls` cuenta las llamadas hechas a la función recursiva.
    /// </summary>
    public static double NaiveRecursive(double[,] dist, out long calls)
    {
        int n = dist.GetLength(0);
        int m = n - 1;
        long count = 0;
        if (m == 0) { calls = 0; return 0; }

        double F(int s, int j)
        {
            count++;
            int bitJ = 1 << (j - 1);
            if (s == bitJ) return dist[0, j];
            int rest = s ^ bitJ;
            double best = double.PositiveInfinity;
            for (int k = 1; k <= m; k++)
                if ((rest & (1 << (k - 1))) != 0)
                    best = Math.Min(best, F(rest, k) + dist[k, j]);
            return best;
        }

        int full = (1 << m) - 1;
        double total = double.PositiveInfinity;
        for (int j = 1; j <= m; j++)
            total = Math.Min(total, F(full, j) + dist[j, 0]);

        calls = count;
        return total;
    }

    /// <summary>Llamadas que haría NaiveRecursive (se cuentan con otra recurrencia): C(1)=1, C(s)=1+(s-1)·C(s-1); total = m·C(m).</summary>
    public static double CountNaiveCalls(int stops)
    {
        int m = stops - 1;
        if (m <= 0) return 0;
        double c = 1;
        for (int s = 2; s <= m; s++) c = 1 + (s - 1) * c;
        return m * c;
    }

    /// <summary>Fuerza bruta: prueba los (n-1)! órdenes posibles. `routes` cuenta las rutas completas evaluadas.</summary>
    public static TourResult BruteForce(double[,] dist, out long routes)
    {
        int n = dist.GetLength(0);
        int m = n - 1;
        long count = 0;
        var current = new int[n + 1];
        var best = new int[n + 1];
        double bestCost = double.PositiveInfinity;
        var used = new bool[n];

        void Walk(int depth, int last, double sum)
        {
            if (depth == m)
            {
                count++;
                double total = sum + dist[last, 0];
                if (total < bestCost)
                {
                    bestCost = total;
                    Array.Copy(current, best, n + 1);
                    best[n] = 0;
                }
                return;
            }
            for (int c = 1; c <= m; c++)
            {
                if (used[c]) continue;
                used[c] = true;
                current[depth + 1] = c;
                Walk(depth + 1, c, sum + dist[last, c]);
                used[c] = false;
            }
        }

        current[0] = 0;
        Walk(0, 0, 0);

        routes = count;
        if (m == 0) return new TourResult(new[] { 0 }, 0);
        best[0] = 0;
        return new TourResult(best[..(n + 1)], bestCost);
    }

    /// <summary>Vecino más cercano: desde donde está, ir siempre a la parada pendiente más próxima. Rápido pero no óptimo.</summary>
    public static TourResult NearestNeighbor(double[,] dist)
    {
        int n = dist.GetLength(0);
        if (n == 1) return new TourResult(new[] { 0 }, 0);

        var tour = new List<int> { 0 };
        var visited = new bool[n];
        visited[0] = true;
        int cur = 0;
        double total = 0;

        for (int step = 1; step < n; step++)
        {
            int next = -1;
            for (int c = 1; c < n; c++)
                if (!visited[c] && (next < 0 || dist[cur, c] < dist[cur, next])) next = c;
            visited[next] = true;
            total += dist[cur, next];
            tour.Add(next);
            cur = next;
        }
        total += dist[cur, 0];
        tour.Add(0);
        return new TourResult(tour.ToArray(), total);
    }

    /// <summary>Largo de un recorrido dado.</summary>
    public static double TourLength(double[,] dist, IReadOnlyList<int> tour)
    {
        double total = 0;
        for (int i = 1; i < tour.Count; i++) total += dist[tour[i - 1], tour[i]];
        return total;
    }
}
