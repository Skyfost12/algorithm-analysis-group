public class Solution {
    public int MinCostConnectPoints(int[][] points) {
        int n = points.Length;

        int[] minCost = Enumerable.Repeat(int.MaxValue, n).ToArray();
        minCost[0] = 0;

        bool[] visited = new bool[n];
        int totalCost = 0;
        for (int i = 0; i < n; i++) {
            int current = -1;
            for (int j = 0; j < n; j++) {
                if (!visited[j] &&
                    (current == -1 || minCost[j] < minCost[current])) {
                    current = j;
                }
            }

            visited[current] = true;
            totalCost += minCost[current];
            for (int j = 0; j < n; j++) {
                if (!visited[j]) {
                    int distance = Math.Abs(points[current][0] - points[j][0]) +
                        Math.Abs(points[current][1] - points[j][1]);

                    minCost[j] = Math.Min(minCost[j], distance);
                }
            }
        }

        return totalCost;
    }
}
