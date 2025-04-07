using System.Collections.Generic;

namespace EnglishDraughtsGame.Models;

public class Game
{
    public Board Board { get; private set; }
    public Player CurrentPlayer { get; set; }
    public bool IsGameOver { get; private set; }

    public Game()
    {
        Board = new Board();
        CurrentPlayer = Player.Red; // Red typically starts.
        IsGameOver = false;
    }

    // Retrieves all valid moves for the current player.
    public List<Move> GetCurrentPlayerMoves()
    {
        return Board.GetValidMoves(CurrentPlayer);
    }

    // Attempts to make the given move. Returns false if the move is invalid.
    public bool MakeMove(Move move)
    {
        var validMoves = GetCurrentPlayerMoves();
        bool isValid = false;
        foreach (var validMove in validMoves)
        {
            if (MovesEqual(validMove, move))
            {
                isValid = true;
                break;
            }
        }

        if (!isValid)
            return false;

        Board.ApplyMove(move);
        SwitchTurn();
        CheckGameOver();
        
        return true;
    }

    private void SwitchTurn()
    {
        CurrentPlayer = CurrentPlayer == Player.Red ? Player.White : Player.Red;
    }

    private void CheckGameOver()
    {
        // If the current player has no valid moves, the game is over.
        if (Board.GetValidMoves(CurrentPlayer).Count == 0)
        {
            IsGameOver = true;
        }
    }

    // Helper method to compare two moves.
    private bool MovesEqual(Move move1, Move move2)
    {
        if (move1.Start != move2.Start)
            return false;
        if (move1.Sequence.Count != move2.Sequence.Count)
            return false;
        for (int i = 0; i < move1.Sequence.Count; i++)
        {
            if (move1.Sequence[i] != move2.Sequence[i])
                return false;
        }
        if (move1.CapturedPositions.Count != move2.CapturedPositions.Count)
            return false;
        for (int i = 0; i < move1.CapturedPositions.Count; i++)
        {
            if (move1.CapturedPositions[i] != move2.CapturedPositions[i])
                return false;
        }
        return true;
    }
    
    public Game Clone()
    {
        Game newGame = new Game();
        newGame.Board = Board.Clone();
        // Set the current player and game over flag.
        newGame.CurrentPlayer = CurrentPlayer;
        newGame.IsGameOver = IsGameOver;
        return newGame;
    }
}