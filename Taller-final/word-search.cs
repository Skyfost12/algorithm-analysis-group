public class Solution {
    public bool Exist(char[][] board, string word) {
        for (int i = 0; i < board.Length; i++) {
            for (int j = 0; j < board[0].Length; j++) {
                if (Backtrack(board, word, i, j, 0))
                    return true;
            }
        }

        return false;
    }

    private bool Backtrack(
        char[][] board,
        string word,
        int row,
        int col,
        int index) {
        if (index == word.Length)
            return true;

        if (row < 0 || col < 0 ||
            row >= board.Length || col >= board[0].Length ||
            board[row][col] != word[index])
            return false;

        // mark cell as used
        char temp = board[row][col];
        board[row][col] = '*';

        bool found =
            Backtrack(board, word, row - 1, col, index + 1) ||
            Backtrack(board, word, row + 1, col, index + 1) ||
            Backtrack(board, word, row, col - 1, index + 1) ||
            Backtrack(board, word, row, col + 1, index + 1);

        // Remove mark
        board[row][col] = temp;

        return found;
    }
}