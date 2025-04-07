using System;
using System.Text;
using System.Text.RegularExpressions;
using DraughtsGame.DataModels;

namespace EnglishDraughtsGame.Helpers;

public static class OpenAiHelper
{
    //--------------------------------------------------------------------------------
    // Constants
    //--------------------------------------------------------------------------------
    private const string InstructionsBlock = @"
You are an AI that plays English Draughts (Checkers) on an 8x8 board. Follow these instructions exactly:

Board Setup:
The board is 8x8, with rows numbered 0 (top) to 7 (bottom) and columns A–H.
Red pieces: 'R' for men, 'RK' for kings. White pieces: 'W' for men, 'WK' for kings. '.' for empty squares.

Movement and Capture Rules:
• Regular men move diagonally forward 1 square (Red upward, White downward). Kings move diagonally any direction 1 square.
• Captures (jumps) skip over an adjacent opponent piece to a vacant square beyond.
• Regular men capture forward only; kings capture in any diagonal direction.
• Forced capture: If any jump is available, it must be taken. If multiple jumps, choose the sequence capturing the most pieces (tie-break is free choice).
• Promotion: A man reaching the opponent’s back row is immediately promoted to a king, potentially continuing to capture as a king if more jumps are available.

Task Requirements:
• It is currently Red’s turn.
• You must choose exactly ONE move that follows all the rules above.
• Choose the move that will maximize Red's chances of winning (i.e., the 'best move to win the game').
• If no moves exist, output exactly 'NO MOVES' with no extra text.
• Format: if a single-jump or single-step move is used, for instance 'C3->D4'. If there are multiple jumps, chain them, e.g. 'C3->E5->G7'.
";

    //--------------------------------------------------------------------------------
    // Public Methods
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Generates the prompt to send to OpenAI, describing the current game state and expected AI response format.
    /// </summary>
    public static string GenerateAiPrompt(Game game, Player userPlayer)
    {
        var sb = new StringBuilder();
        sb.AppendLine(InstructionsBlock.Trim());
        sb.AppendLine("Current Board Configuration:");
        
        // Add row-by-row board data
        int size = game.Board.GetSize(); // typically 8
        for (int row = 0; row < size; row++)
        {
            var rowBuilder = new StringBuilder();
            for (int col = 0; col < size; col++)
            {
                var pos = new Position(row, col);
                var cell = game.Board.GetCellAt(pos);
                if (cell is Piece piece && !cell.IsEmpty)
                {
                    rowBuilder.Append(FormatPiece(piece));
                }
                else
                {
                    rowBuilder.Append('.');
                }
            }
            sb.AppendLine(rowBuilder.ToString());
        }

        // Final instruction line
        sb.AppendLine();
        sb.AppendLine("Choose the best single move for Red to increase their chances of winning, or 'NO MOVES' if none are available. Output no additional commentary.");

        return sb.ToString();
    }

    /// <summary>
    /// Parses the AI's text response (e.g., 'C3->D4' or 'NO MOVES') into a Move object.
    /// </summary>
    /// <param name="aiText">The text response from the AI.</param>
    /// <param name="noMovesString">The special 'no moves' string.</param>
    /// <returns>A valid Move or null if no move can be parsed.</returns>
    public static Move? ParseAiMove(string aiText, string noMovesString)
    {
        string cleanText = aiText.Trim().ToUpperInvariant();

        // If AI outputs "NO MOVES", we return null to indicate no moves found
        if (cleanText == noMovesString.ToUpperInvariant())
        {
            return null;
        }

        // Validate the format (e.g. "C3->E5->G7")
        var pattern = @"^([A-H][1-8])(?:->[A-H][1-8])*$";
        if (!Regex.IsMatch(cleanText, pattern))
        {
            return null;
        }

        // Split by '->' to get squares
        var squares = cleanText.Split("->", StringSplitOptions.RemoveEmptyEntries);
        if (squares.Length < 1)
        {
            return null;
        }

        // Construct the Move
        var startPos = ParseSquare(squares[0]);
        var move = new Move(startPos);
        for (int i = 1; i < squares.Length; i++)
        {
            move.AddStep(ParseSquare(squares[i]));
        }

        return move;
    }

    //--------------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Converts a Piece to its board-character representation (e.g., 'R' or 'RK').
    /// </summary>
    private static string FormatPiece(Piece piece)
    {
        if (piece.Owner == Player.Red)
        {
            return piece.IsKing ? "RK" : "R";
        }
        else
        {
            return piece.IsKing ? "WK" : "W";
        }
    }

    /// <summary>
    /// Converts a notation like "C3" to a Position (row, column).
    /// Must stay in sync with any corresponding 'SquareString(...)' method in the ViewModel.
    /// </summary>
    private static Position ParseSquare(string square)
    {
        square = square.Trim().ToUpperInvariant();
        if (square.Length < 2)
            throw new ArgumentException("Invalid square notation.", nameof(square));

        char file = square[0];                      
        int rank = int.Parse(square.Substring(1));  

        int col = file - 'A';  
        int row = rank - 1;    
        return new Position(row, col);
    }
}