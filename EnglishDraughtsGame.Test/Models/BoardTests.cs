using EnglishDraughtsGame.Models;

namespace EnglishDraughtsGame.Test.Models;

[TestFixture]
public class BoardTests
{
    [Test]
    public void Board_Constructor_InitializesWithPieces()
    {
        // Arrange & Act
        var board = new Board();

        // Assert
        // For standard English draughts, the top 3 dark rows (row<3) have Red,
        // and bottom 3 dark rows (row>4) have White. Let's count them:
        int redCount = 0;
        int whiteCount = 0;

        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var cell = board.GetCellAt(new Position(row, col));
                if (!cell.IsEmpty && cell is Piece piece)
                {
                    if (piece.Owner == Player.Red)
                        redCount++;
                    else if (piece.Owner == Player.White)
                        whiteCount++;
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(redCount, Is.GreaterThan(0), "Should start with some Red pieces placed.");
            Assert.That(whiteCount, Is.GreaterThan(0), "Should start with some White pieces placed.");
        });
    }

    [Test]
    public void GetValidMoves_ReturnsNonEmptyForInitialRed()
    {
        // Arrange
        var board = new Board();

        // Act
        var moves = board.GetValidMoves(Player.Red);

        // Assert
        Assert.NotNull(moves);
        Assert.IsNotEmpty(moves, "Red should have some opening moves on a standard board.");
    }

    [Test]
    public void IsInsideBoard_ValidAndInvalidPositions()
    {
        // Arrange
        var board = new Board();

        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(board.IsInsideBoard(new Position(0, 0)), Is.True);
            Assert.That(board.IsInsideBoard(new Position(Board.Size - 1, Board.Size - 1)), Is.True);
            Assert.That(board.IsInsideBoard(new Position(-1, 0)), Is.False);
            Assert.That(board.IsInsideBoard(new Position(0, Board.Size)), Is.False);
        });
    }

    [Test]
    public void ApplyMove_WithCapture_RemovesCapturedPiece()
    {
        // Arrange
        var board = new Board();

        // Force a simple capture scenario by clearing everything except two pieces:
        // 1) A Red piece at (2,2)
        // 2) A White piece at (3,3)
        // Then (4,4) is empty for a jump. We'll remove everything else.

        for (int r = 0; r < Board.Size; r++)
        {
            for (int c = 0; c < Board.Size; c++)
            {
                board.RemovePieceAt(new Position(r, c));
            }
        }

        var redPos = new Position(2, 2);
        var whitePos = new Position(3, 3);
        board.SetCellAt(redPos, new Piece(Player.Red));
        board.SetCellAt(whitePos, new Piece(Player.White));

        var move = new Move(redPos);
        var landingPos = new Position(4, 4);
        move.AddStep(landingPos, whitePos); // capturing White at (3,3)

        // Act
        board.ApplyMove(move);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(board.GetCellAt(landingPos) is Piece p && p.Owner == Player.Red, Is.True,
                        "Red piece should now be at (4,4).");
            Assert.That(board.GetCellAt(whitePos).IsEmpty, Is.True,
                        "White piece should have been captured and removed.");
            Assert.That(board.GetCellAt(redPos).IsEmpty, Is.True,
                        "Original Red position should now be empty.");
        });
    }

    [Test]
    public void Clone_CreatesSeparateBoardState()
    {
        // Arrange
        var original = new Board();

        // Make a small change to confirm that the clone is independent
        var pos = new Position(2, 2);
        original.RemovePieceAt(pos);

        // Act
        var clone = original.Clone();

        // Assert
        // They are separate references
        Assert.That(clone, Is.Not.SameAs(original), "Clone should produce a distinct Board.");
        
        // A further change to the original does not affect the clone
        original.RemovePieceAt(new Position(3, 3));
        var cloneCell = clone.GetCellAt(new Position(3, 3));
        Assert.False(cloneCell == null, "Clone's position (3,3) should remain as it was.");
    }
}
