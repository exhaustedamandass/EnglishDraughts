using System;
using System.Collections.Generic;

namespace DraughtsGame.DataModels;

public class Board
{
    public const int Size = 8;
    private IBoardCell[,] _board;

    public Board()
    {
        _board = new IBoardCell[Size, Size];
        Initialize();
    }

    public int GetSize()
    {
        return _board.GetLength(0);
    }

    public void Initialize()
    {
        // Initialize every cell as empty.
        for (var row = 0; row < Size; row++)
        {
            for (var col = 0; col < Size; col++)
            {
                _board[row, col] = EmptyCell.Instance;
            }
        }

        // Place pieces on dark squares based on English draughts rules.
        // Assuming top-left (0,0) is dark.
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if ((row + col) % 2 == 0)
                {
                    if (row < 3)
                    {
                        _board[row, col] = new Piece(Player.Red);
                    }
                    else if (row > 4)
                    {
                        _board[row, col] = new Piece(Player.White);
                    }
                }
            }
        }
    }

    public IBoardCell GetCellAt(Position pos)
    {
        if (IsInsideBoard(pos))
            return _board[pos.Row, pos.Col];
        return null;
    }

    public void SetCellAt(Position pos, IBoardCell cell)
    {
        if (IsInsideBoard(pos))
            _board[pos.Row, pos.Col] = cell;
    }

    public void RemovePieceAt(Position pos)
    {
        if (IsInsideBoard(pos))
            _board[pos.Row, pos.Col] = EmptyCell.Instance;
    }

    public bool IsInsideBoard(Position pos)
    {
        return pos.Row >= 0 && pos.Row < Size && pos.Col >= 0 && pos.Col < Size;
    }

    // The rest of the methods such as GetValidMoves, ApplyMove, etc.
    // will now operate on IBoardCell and cast to Piece where needed.
    // For example:
    public List<Move> GetValidMoves(Player player)
    {
        List<Move> allMoves = new List<Move>();
        List<Move> captureMoves = new List<Move>();

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                Position pos = new Position(row, col);
                IBoardCell cell = GetCellAt(pos);
                if (!cell.IsEmpty && cell is Piece piece && piece.Owner == player)
                {
                    var moves = GetValidMovesForPiece(pos);
                    foreach (var move in moves)
                    {
                        if (move.CapturedPositions.Count > 0)
                            captureMoves.Add(move);
                        else
                            allMoves.Add(move);
                    }
                }
            }
        }

        return captureMoves.Count > 0 ? captureMoves : allMoves;
    }

    public List<Move> GetValidMovesForPiece(Position pos)
    {
        List<Move> moves = new List<Move>();
        IBoardCell cell = GetCellAt(pos);
        if (cell.IsEmpty || !(cell is Piece piece))
            return moves;

        int[] dirRows = piece.IsKing ? new int[] { -1, 1 } : (piece.Owner == Player.Red ? new int[] { 1 } : new int[] { -1 });
        int[] dirCols = new int[] { -1, 1 };

        List<Move> captureMoves = new List<Move>();
        bool[,] visited = new bool[Size, Size];
        RecursiveCapture(piece, pos, new Move(pos), captureMoves, visited);
        if (captureMoves.Count > 0)
            return captureMoves;

        foreach (int dr in dirRows)
        {
            foreach (int dc in dirCols)
            {
                Position newPos = new Position(pos.Row + dr, pos.Col + dc);
                if (IsInsideBoard(newPos) && GetCellAt(newPos).IsEmpty)
                {
                    Move move = new Move(pos);
                    move.AddStep(newPos);
                    moves.Add(move);
                }
            }
        }

        return moves;
    }

    private void RecursiveCapture(Piece piece, Position currentPos, Move currentMove, List<Move> captureMoves, bool[,] visited)
    {
        bool foundCapture = false;
        int[] drs = piece.IsKing ? new int[] { -1, 1 } : (piece.Owner == Player.Red ? new int[] { 1 } : new int[] { -1 });
        int[] dcs = new int[] { -1, 1 };

        foreach (int dr in drs)
        {
            foreach (int dc in dcs)
            {
                Position enemyPos = new Position(currentPos.Row + dr, currentPos.Col + dc);
                Position landingPos = new Position(currentPos.Row + 2 * dr, currentPos.Col + 2 * dc);
                if (IsInsideBoard(enemyPos) && IsInsideBoard(landingPos))
                {
                    IBoardCell enemyCell = GetCellAt(enemyPos);
                    if (!enemyCell.IsEmpty && enemyCell is Piece enemyPiece &&
                        enemyPiece.Owner != piece.Owner &&
                        !visited[enemyPos.Row, enemyPos.Col] &&
                        GetCellAt(landingPos).IsEmpty)
                    {
                        visited[enemyPos.Row, enemyPos.Col] = true;
                        Move newMove = currentMove.Clone();
                        newMove.AddStep(landingPos, enemyPos);

                        // Simulate the capture
                        Piece temp = (Piece)GetCellAt(currentPos);
                        RemovePieceAt(enemyPos);
                        RemovePieceAt(currentPos);
                        SetCellAt(landingPos, temp);

                        RecursiveCapture(piece, landingPos, newMove, captureMoves, visited);

                        // Restore board state
                        SetCellAt(currentPos, temp);
                        SetCellAt(enemyPos, enemyPiece);
                        RemovePieceAt(landingPos);

                        foundCapture = true;
                        visited[enemyPos.Row, enemyPos.Col] = false;
                    }
                }
            }
        }

        if (!foundCapture && currentMove.CapturedPositions.Count > 0)
        {
            captureMoves.Add(currentMove);
        }
    }

    public void ApplyMove(Move move)
    {
        Position start = move.Start;
        IBoardCell cell = GetCellAt(start);
        if (cell.IsEmpty || !(cell is Piece piece))
            throw new InvalidOperationException("No piece at the starting position");

        RemovePieceAt(start);
        foreach (var capPos in move.CapturedPositions)
        {
            RemovePieceAt(capPos);
        }
        Position endPos = move.End;
        SetCellAt(endPos, piece);

        if (!piece.IsKing)
        {
            if ((piece.Owner == Player.Red && endPos.Row == Size - 1) ||
                (piece.Owner == Player.White && endPos.Row == 0))
            {
                piece.Crown();
            }
        }
    }
    
    public Board Clone()
    {
        Board newBoard = new Board();
        // Reinitialize with empty cells first.
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                Position pos = new Position(row, col);
                var cell = this.GetCellAt(pos);
                if (!cell.IsEmpty && cell is Piece piece)
                {
                    // Create a new piece with the same properties.
                    newBoard.SetCellAt(pos, new Piece(piece.Owner, piece.Type));
                }
                else
                {
                    newBoard.SetCellAt(pos, EmptyCell.Instance);
                }
            }
        }
        return newBoard;
    }

}