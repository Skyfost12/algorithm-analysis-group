public class Solution {
    public int NetworkDelayTime(int[][] times, int n, int k) {
        var graph = new List<(int node, int weight)>[n + 1];

        for (int i = 1; i <= n; i++)
            graph[i] = new();

        foreach (var time in times)
            graph[time[0]].Add((time[1], time[2]));

        int[] distances = Enumerable.Repeat(int.MaxValue, n + 1).ToArray();
        distances[k] = 0;

        var heap = new PriorityQueue<int, int>();
        heap.Enqueue(k, 0);

        while (heap.Count > 0) {
            heap.TryDequeue(out int current, out int currentDist);

            if (currentDist > distances[current]) continue;

            foreach (var (neighbor, weight) in graph[current]) {
                int newDist = currentDist + weight;

                // if distance is smaller
                if (newDist < distances[neighbor]) {
                    distances[neighbor] = newDist;
                    heap.Enqueue(neighbor, newDist);
                }
            }
        }

        int result = 0;
        for (int i = 1; i <= n; i++) {
            if (distances[i] == int.MaxValue) return -1;

            result = Math.Max(result, distances[i]);
        }

        return result;
    }
}
