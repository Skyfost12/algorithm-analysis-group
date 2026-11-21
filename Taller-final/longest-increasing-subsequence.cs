public class Solution {
    public int LengthOfLIS(int[] nums) {
        int n = nums.Length;
        int[] dp = new int[n];

        // Base
        Array.Fill(dp, 1);

        int result = 1;
        for (int i = 1; i < n; i++) {
            for (int j = 0; j < i; j++) {
                if (nums[j] < nums[i]) {
                    dp[i] = Math.Max(dp[i], dp[j] + 1);
                }
            }
            result = Math.Max(result, dp[i]);
        }

        return result;
    }
}