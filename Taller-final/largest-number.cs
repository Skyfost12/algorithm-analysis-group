public class Solution {
    public string LargestNumber(int[] nums) {
        string[] numbers = nums.Select(n => n.ToString()).ToArray();

        Array.Sort(numbers, (a, b) =>
            string.Compare(b + a, a + b, StringComparison.Ordinal));

        // if the first one is zero, the others are too
        if (numbers[0] == "0") return "0";

        return string.Concat(numbers);
    }
}
