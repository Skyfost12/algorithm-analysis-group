public class Solution {
    public int CanCompleteCircuit(int[] gas, int[] cost) {
        int totalBalance = 0;
        int currentBalance = 0;

        int start = 0;
        for (int i = 0; i < gas.Length; i++) {
            int balance = gas[i] - cost[i];

            totalBalance += balance;
            currentBalance += balance;

            if (currentBalance < 0) {
                start = i + 1;
                currentBalance = 0;
            }
        }

        return totalBalance < 0 ? -1 : start;
    }
}