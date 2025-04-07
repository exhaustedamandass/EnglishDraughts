using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EnglishDraughtsGame.Models;

namespace EnglishDraughtsGame.Controls;

public class BoardControl : Control
{
    //--------------------------------------------------------------------------------
    // Constants and Readonly Fields
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Semi-transparent highlight color for destination squares.
    /// </summary>
    private static readonly SolidColorBrush HighlightBrush =
        new SolidColorBrush(Color.FromArgb(128, 144, 238, 144));

    /// <summary>
    /// The ratio of the square's minimum dimension used as the piece radius.
    /// </summary>
    private const double PieceRadiusFactor = 0.4;

    //--------------------------------------------------------------------------------
    // Avalonia Properties
    //--------------------------------------------------------------------------------

    /// <summary>
    /// The main <see cref="Game"/> displayed by this control.
    /// </summary>
    public static readonly StyledProperty<Game> GameProperty =
        AvaloniaProperty.Register<BoardControl, Game>(nameof(Game));

    /// <summary>
    /// A property used to trigger re-rendering of the board.
    /// For example, after a move is made.
    /// </summary>
    public static readonly DirectProperty<BoardControl, int> MoveCountProperty =
        AvaloniaProperty.RegisterDirect<BoardControl, int>(
            nameof(MoveCount),
            o => o.MoveCount,
            (o, v) => o.MoveCount = v);

    //--------------------------------------------------------------------------------
    // Backing Fields
    //--------------------------------------------------------------------------------
    private int _moveCount;
    private Position? _selectedCell;

    //--------------------------------------------------------------------------------
    // Constructor / Static Constructor
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Static constructor to register that changes in <see cref="MoveCountProperty"/>
    /// cause this control to redraw.
    /// </summary>
    static BoardControl()
    {
        AffectsRender<BoardControl>(MoveCountProperty);
    }

    //--------------------------------------------------------------------------------
    // Public Events
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Raised after a successful move has been completed on the board.
    /// </summary>
    public event EventHandler MoveCompleted;

    //--------------------------------------------------------------------------------
    // Public Properties
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the <see cref="Game"/> to be rendered.
    /// </summary>
    public Game Game
    {
        get => GetValue(GameProperty);
        set => SetValue(GameProperty, value);
    }

    /// <summary>
    /// Gets or sets the move count, used to force re-rendering when updated.
    /// </summary>
    public int MoveCount
    {
        get => _moveCount;
        set => SetAndRaise(MoveCountProperty, ref _moveCount, value);
    }

    //--------------------------------------------------------------------------------
    // Public Override Methods
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Renders the board, pieces, and highlights.
    /// </summary>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // If there's no game or no board, exit early
        if (Game?.Board == null)
            return;

        // Delegate the drawing to a helper class for clarity
        BoardDrawingHelper.RenderBoard(
            context,
            Game.Board,
            _selectedCell,
            Bounds.Width,
            Bounds.Height,
            HighlightBrush,
            PieceRadiusFactor
        );
    }

    //--------------------------------------------------------------------------------
    // Protected Overrides
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Handles clicks (presses) on the board for piece selection or move execution.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Game?.Board == null)
            return;

        // Determine which cell was clicked
        var point = e.GetPosition(this);
        var squareWidth = Bounds.Width / Board.Size;
        var squareHeight = Bounds.Height / Board.Size;
        var col = (int)(point.X / squareWidth);
        var row = (int)(point.Y / squareHeight);
        var clickedPos = new Position(row, col);

        // Handle selection and moves
        HandleCellClick(clickedPos);
    }

    //--------------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Invokes the <see cref="MoveCompleted"/> event.
    /// </summary>
    private void OnMoveCompleted() => MoveCompleted?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Handles logic for selecting a piece or executing a move based on the clicked position.
    /// </summary>
    /// <param name="clickedPos">The position on the board that was clicked.</param>
    private void HandleCellClick(Position clickedPos)
    {
        var cell = Game.Board.GetCellAt(clickedPos);

        // If nothing is selected yet, try to select the current player's piece
        if (!_selectedCell.HasValue)
        {
            if (cell.IsEmpty || cell is not Piece piece || piece.Owner != Game.CurrentPlayer) return;
            _selectedCell = clickedPos;
            InvalidateVisual();
            return;
        }

        // If the clicked cell is the same as selected cell, deselect
        if (_selectedCell.Value == clickedPos)
        {
            _selectedCell = null;
            InvalidateVisual();
            return;
        }

        // Try to find a valid move matching this click
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
            var success = Game.MakeMove(moveToExecute);
            if (!success) return;
            // Clear selection after a successful move
            _selectedCell = null;
            InvalidateVisual();
            OnMoveCompleted();
        }
        else
        {
            // If the clicked cell belongs to current player's piece, change selection
            if (!cell.IsEmpty && cell is Piece piece && piece.Owner == Game.CurrentPlayer)
            {
                _selectedCell = clickedPos;
            }
            else
            {
                // Otherwise clear selection
                _selectedCell = null;
            }
            InvalidateVisual();
        }
    }
}

/// <summary>
/// A static helper class that encapsulates the board rendering logic.
/// </summary>
internal static class BoardDrawingHelper
{
    /// <summary>
    /// Draws the entire board, pieces, and highlights (including selected piece and possible moves).
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="board">The <see cref="Board"/> to render.</param>
    /// <param name="selectedCell">An optional position of the currently selected piece.</param>
    /// <param name="totalWidth">Total width available for drawing.</param>
    /// <param name="totalHeight">Total height available for drawing.</param>
    /// <param name="highlightBrush">Brush used for highlighting available moves.</param>
    /// <param name="pieceRadiusFactor">Multiplier for how large pieces should be within a square.</param>
    public static void RenderBoard(
        DrawingContext context,
        Board board,
        Position? selectedCell,
        double totalWidth,
        double totalHeight,
        IBrush highlightBrush,
        double pieceRadiusFactor)
    {
        var squareWidth = totalWidth / Board.Size;
        var squareHeight = totalHeight / Board.Size;

        // Draw squares and pieces
        for (var row = 0; row < Board.Size; row++)
        {
            for (var col = 0; col < Board.Size; col++)
            {
                var squareRect = GetSquareRect(row, col, squareWidth, squareHeight);
                var isDarkSquare = ((row + col) % 2 == 0);

                // Fill the square
                var squareBrush = isDarkSquare ? Brushes.SaddleBrown : Brushes.BurlyWood;
                context.FillRectangle(squareBrush, squareRect);

                // Draw a piece if present
                var pos = new Position(row, col);
                var cell = board.GetCellAt(pos);
                if (cell is Piece piece && !cell.IsEmpty)
                {
                    DrawPiece(context, piece, squareRect, pieceRadiusFactor);
                }
            }
        }

        // If a piece is selected, highlight that square + possible moves
        if (selectedCell.HasValue)
        {
            HighlightSelectedAndDestinations(context, board, selectedCell.Value, squareWidth, squareHeight, highlightBrush);
        }
    }

    /// <summary>
    /// Highlights the selected cell and the immediate destinations for the selected piece.
    /// </summary>
    private static void HighlightSelectedAndDestinations(
        DrawingContext context,
        Board board,
        Position selectedCell,
        double squareWidth,
        double squareHeight,
        IBrush highlightBrush)
    {
        // 1) Draw a yellow rectangle around the selected cell
        var selRect = GetSquareRect(selectedCell.Row, selectedCell.Col, squareWidth, squareHeight);
        var selectionPen = new Pen(Brushes.Yellow, 3);
        context.DrawRectangle(selectionPen, selRect);

        // 2) Gather immediate destinations
        var moves = board.GetValidMovesForPiece(selectedCell);
        var destinations = new HashSet<Position>();
        foreach (var move in moves)
        {
            if (move.Sequence.Count > 0)
            {
                // Only highlight the first step of each move
                destinations.Add(move.Sequence[0]);
            }
        }

        // 3) Fill those squares with a semi-transparent highlight
        foreach (var dest in destinations)
        {
            var destRect = GetSquareRect(dest.Row, dest.Col, squareWidth, squareHeight);
            context.FillRectangle(highlightBrush, destRect);
        }
    }

    /// <summary>
    /// Draws a piece (circle) within the specified square rectangle.
    /// </summary>
    private static void DrawPiece(
        DrawingContext context,
        Piece piece,
        Rect squareRect,
        double pieceRadiusFactor)
    {
        // Calculate circle center & radius
        var center = GetSquareCenter(squareRect);
        var radius = Math.Min(squareRect.Width, squareRect.Height) * pieceRadiusFactor;

        // Fill the circular piece
        IBrush pieceBrush = (piece.Owner == Player.Red) ? Brushes.Red : Brushes.White;
        var pieceRect = new Rect(
            center.X - radius,
            center.Y - radius,
            radius * 2,
            radius * 2);
        context.FillRectangle(pieceBrush, pieceRect, (float)radius);

        // If it's a king, draw a ring around it
        if (!piece.IsKing) return;
        var kingPen = new Pen(Brushes.Gold, 3);
        context.DrawEllipse(Brushes.Red, kingPen, center, radius, radius);
    }

    /// <summary>
    /// Returns the <see cref="Rect"/> for a given row and column on the board.
    /// </summary>
    private static Rect GetSquareRect(int row, int col, double squareWidth, double squareHeight)
    {
        return new Rect(col * squareWidth, row * squareHeight, squareWidth, squareHeight);
    }

    /// <summary>
    /// Computes the center point of a square <see cref="Rect"/>.
    /// </summary>
    private static Point GetSquareCenter(Rect squareRect)
    {
        var centerX = squareRect.X + (squareRect.Width / 2);
        var centerY = squareRect.Y + (squareRect.Height / 2);
        return new Point(centerX, centerY);
    }
}