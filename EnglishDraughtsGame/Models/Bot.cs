using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DraughtsGame.DataModels;

public class Bot
{
    public Player BotPlayer { get; set; }

    // Instead of a fixed depth, we use a time limit.
    public TimeSpan TimeLimit { get; set; }

    public Bot(Player botPlayer, TimeSpan timeLimit)
    {
        BotPlayer = botPlayer;
        TimeLimit = timeLimit;
    }

    /// <summary>
    /// Returns the best move computed within the given time limit.
    /// Uses iterative deepening to search deeper until time expires.
    /// </summary>
    public Move GetBestMove(Game game)
    {
        // Create a cancellation token that cancels after the time limit.
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.CancelAfter(TimeLimit);
            CancellationToken token = cancellationTokenSource.Token;

            Move bestMove = null;
            int bestScore = int.MinValue;
            int depth = 1;

            // Iterative deepening loop.
            while (!token.IsCancellationRequested)
            {
                List<Move> validMoves = game.Board.GetValidMoves(game.CurrentPlayer);
                if (validMoves.Count == 0)
                {
                    return null; // No moves available.
                }

                // Use a concurrent collection to store results from each move.
                var results = new ConcurrentBag<(Move move, int score)>();

                try
                {
                    // Evaluate each candidate move in parallel.
                    Parallel.ForEach(validMoves,
                        new ParallelOptions { CancellationToken = token },
                        move =>
                        {
                            // Clone the game state and apply the move.
                            Game clonedGame = game.Clone();
                            clonedGame.MakeMove(move);

                            // Evaluate this move using minimax to the current depth.
                            var result = Minimax(clonedGame, depth - 1, int.MinValue, int.MaxValue,
                                clonedGame.CurrentPlayer, token);
                            results.Add((move, result.score));
                        });
                }
                catch (OperationCanceledException)
                {
                    // Time is up; break out of the loop.
                    break;
                }

                // If no cancellation, update the best move from this iteration.
                foreach (var result in results)
                {
                    if (result.score > bestScore)
                    {
                        bestScore = result.score;
                        bestMove = result.move;
                    }
                }

                depth++; // Increase the depth for the next iteration.
            }

            return bestMove;
        }
    }

    /// <summary>
    /// Minimax algorithm with alpha-beta pruning.
    /// Uses a cancellation token to interrupt deep searches if time expires.
    /// Returns a tuple (score, move) for the given game state.
    /// </summary>
    private (int score, Move move) Minimax(Game game, int depth, int alpha, int beta, Player currentPlayer,
        CancellationToken token)
    {
        if (depth == 0 || game.IsGameOver || token.IsCancellationRequested)
        {
            return (Evaluate(game), null);
        }

        List<Move> validMoves = game.Board.GetValidMoves(currentPlayer);
        if (validMoves.Count == 0)
        {
            return (Evaluate(game), null);
        }

        Move bestMove = null;
        if (currentPlayer == BotPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in validMoves)
            {
                if (token.IsCancellationRequested)
                    break;

                Game clonedGame = game.Clone();
                clonedGame.MakeMove(move);
                int eval = Minimax(clonedGame, depth - 1, alpha, beta, clonedGame.CurrentPlayer, token).score;
                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break; // Beta cutoff.
            }

            return (maxEval, bestMove);
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in validMoves)
            {
                if (token.IsCancellationRequested)
                    break;

                Game clonedGame = game.Clone();
                clonedGame.MakeMove(move);
                int eval = Minimax(clonedGame, depth - 1, alpha, beta, clonedGame.CurrentPlayer, token).score;
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                }

                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break; // Alpha cutoff.
            }

            return (minEval, bestMove);
        }
    }

    /// <summary>
    /// A simple evaluation function that scores the board based on material.
    /// Each man is worth 1 point and each king is worth 2 points.
    /// </summary>
    private int Evaluate(Game game)
    {
        int score = 0;
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                Position pos = new Position(row, col);
                var cell = game.Board.GetCellAt(pos);
                if (!cell.IsEmpty && cell is Piece piece)
                {
                    int pieceValue = piece.IsKing ? 2 : 1;
                    // Score from the bot's perspective.
                    score += (piece.Owner == BotPlayer) ? pieceValue : -pieceValue;
                }
            }
        }

        return score;
    }
}