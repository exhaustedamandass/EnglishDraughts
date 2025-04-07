using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DraughtsGame.DataModels;

namespace EnglishDraughtsGame.Controls;

public class BoardControl : Control
{
    public static readonly StyledProperty<Game> GameProperty =
        AvaloniaProperty.Register<BoardControl, Game>(nameof(Game));

    public Game Game
    {
        get => GetValue(GameProperty);
        set => SetValue(GameProperty, value);
    }

    static BoardControl()
    {
        AffectsRender<BoardControl>(MoveCountProperty);
    }

    private int _moveCount = 0;
    
    public int MoveCount
    {
        get => _moveCount;
        set => SetAndRaise(MoveCountProperty, ref _moveCount, value);
    }
    
    public static readonly DirectProperty<BoardControl, int> MoveCountProperty = 
        AvaloniaProperty.RegisterDirect<BoardControl, int>(
            nameof(MoveCount),
            o => o.MoveCount,
            (o, v) => o.MoveCount = v);
    
    
    // Holds the currently selected cell (if any).
    private Position? _selectedCell;

    public event EventHandler MoveCompleted;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Game?.Board == null)
            return;

        // Determine the size of each square.
        double squareWidth = Bounds.Width / Board.Size;
        double squareHeight = Bounds.Height / Board.Size;

        // Draw board squares and pieces.
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                Rect squareRect = new Rect(col * squareWidth, row * squareHeight, squareWidth, squareHeight);
                bool isDarkSquare = (row + col) % 2 == 0;
                var squareBrush = isDarkSquare ? Brushes.SaddleBrown : Brushes.BurlyWood;
                context.FillRectangle(squareBrush, squareRect);

                Position pos = new Position(row, col);
                var cell = Game.Board.GetCellAt(pos);
                if (!cell.IsEmpty && cell is Piece piece)
                {
                    // Draw the piece as a circle.
                    var center = new Point(squareRect.X + squareRect.Width / 2, squareRect.Y + squareRect.Height / 2);
                    double radius = Math.Min(squareWidth, squareHeight) * 0.4;
                    IBrush pieceBrush = piece.Owner == Player.Red ? Brushes.Red : Brushes.White;
                    var pieceRect = new Rect(
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2);

                    // Use FillRectangle with a corner radius equal to the circle's radius.
                    context.FillRectangle(pieceBrush, pieceRect, (float)radius);

                    if (piece.IsKing)
                    {
                        context.DrawEllipse(Brushes.Red,new Pen(Brushes.Gold, 3), center, radius, radius);
                    }
                }
            }
        }

        // If a piece is selected, highlight its cell and all possible destination cells.
        if (!_selectedCell.HasValue) return;
        // Highlight selected cell.
        int selRow = _selectedCell.Value.Row;
        int selCol = _selectedCell.Value.Col;
        Rect selRect = new Rect(selCol * squareWidth, selRow * squareHeight, squareWidth, squareHeight);
        var selectionPen = new Pen(Brushes.Yellow, 3);
        context.DrawRectangle(selectionPen, selRect);

        // Get possible moves for the selected piece.
        var moves = Game.Board.GetValidMovesForPiece(_selectedCell.Value);
        var destinations = new HashSet<Position>();
        foreach (var move in moves)
        {
            if (move.Sequence.Count > 0)
            {
                // For highlighting, we consider only the immediate destination.
                destinations.Add(move.Sequence[0]);
            }
        }

        // Highlight possible destination squares.
        foreach (var dest in destinations)
        {
            Rect destRect = new Rect(dest.Col * squareWidth, dest.Row * squareHeight, squareWidth, squareHeight);
            // Using a semi-transparent light green for highlighting.
            var highlightBrush = new SolidColorBrush(Color.FromArgb(128, 144, 238, 144));
            context.FillRectangle(highlightBrush, destRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (Game == null || Game.Board == null)
            return;

        // Determine which board cell was clicked.
        var point = e.GetPosition(this);
        double squareWidth = Bounds.Width / Board.Size;
        double squareHeight = Bounds.Height / Board.Size;
        int col = (int)(point.X / squareWidth);
        int row = (int)(point.Y / squareHeight);
        Position clickedPos = new Position(row, col);

        var cell = Game.Board.GetCellAt(clickedPos);

        if (!_selectedCell.HasValue)
        {
            // Select a piece if it's the current player's.
            if (!cell.IsEmpty && cell is Piece piece && piece.Owner == Game.CurrentPlayer)
            {
                _selectedCell = clickedPos;
                InvalidateVisual();
            }
        }
        else
        {
            // Deselect if clicking the already selected cell.
            if (_selectedCell.Value == clickedPos)
            {
                _selectedCell = null;
                InvalidateVisual();
                return;
            }
            else
            {
                // Check if the clicked cell is one of the valid moves.
                var moves = Game.Board.GetValidMovesForPiece(_selectedCell.Value);
                Move moveToExecute = null;
                foreach (var move in moves)
                {
                    if (move.Sequence.Count > 0 && move.Sequence[0] == clickedPos)
                    {
                        moveToExecute = move;
                        break;
                    }
                }
                if (moveToExecute != null)
                {
                    bool success = Game.MakeMove(moveToExecute);
                    if (success)
                    {
                        // Clear selection after a successful move.
                        _selectedCell = null;
                        InvalidateVisual();
                        OnMoveCompleted();
                    }
                }
                else
                {
                    // If the clicked cell has a current player's piece, update selection.
                    if (!cell.IsEmpty && cell is Piece piece && piece.Owner == Game.CurrentPlayer)
                    {
                        _selectedCell = clickedPos;
                        InvalidateVisual();
                    }
                    else
                    {
                        // Otherwise, clear the selection.
                        _selectedCell = null;
                        InvalidateVisual();
                    }
                }
            }
        }
    }

    private void OnMoveCompleted()
    {
        MoveCompleted?.Invoke(this, EventArgs.Empty);
    }
}