using EnglishDraughtsGame.Models;

namespace EnglishDraughtsGame.Test.Models;

[TestFixture]
public class GameTests
{
    [Test]
    public void Game_Constructor_InitializesDefaults()
    {
        // Arrange & Act
        var game = new Game();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(game.Board, Is.Not.Null, "Game should create a new Board.");
            Assert.That(game.CurrentPlayer, Is.EqualTo(Player.Red), "Default starting player should be Red.");
            Assert.That(game.IsGameOver, Is.False, "New game should not be over.");
        });
    }

    [Test]
    public void GetCurrentPlayerMoves_ReturnsMovesForCurrentPlayer()
    {
        // Arrange
        var game = new Game();
        // By default, CurrentPlayer = Red

        // Act
        var moves = game.GetCurrentPlayerMoves();

        // Assert
        Assert.NotNull(moves, "Should return a list of moves (possibly empty, but not null).");
        // Usually, on a standard 8x8 checkers setup, Red DOES have moves at the start.
        Assert.IsNotEmpty(moves, "On a normal initial board, Red should have some moves.");
    }

    [Test]
    public void MakeMove_InvalidMove_ReturnsFalseAndNoStateChange()
    {
        // Arrange
        var game = new Game();
        var invalidMove = new Move(new Position(7, 7));  // Some random position likely invalid.

        // Pre-check
        var oldCurrentPlayer = game.CurrentPlayer;
        var oldGameOver = game.IsGameOver;

        // Act
        bool result = game.MakeMove(invalidMove);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result, Is.False, "MakeMove should fail for an invalid move.");
            Assert.That(game.CurrentPlayer, Is.EqualTo(oldCurrentPlayer), "CurrentPlayer should remain unchanged.");
            Assert.That(game.IsGameOver, Is.EqualTo(oldGameOver), "IsGameOver should remain unchanged.");
        });
    }

    [Test]
    public void MakeMove_ValidMove_SucceedsAndSwitchesTurn()
    {
        // Arrange
        var game = new Game();

        // Typically, Red will have some immediate non-capturing moves at the start,
        // e.g., from row=2 to row=3 on dark squares. Let's just take the first valid move:
        var validMoves = game.GetCurrentPlayerMoves();
        Assert.IsNotEmpty(validMoves, "Expected at least one valid move for Red.");
        var firstMove = validMoves[0];

        // Act
        bool result = game.MakeMove(firstMove);

        // Assert
        Assert.True(result, "MakeMove should succeed for a valid move.");
        Assert.That(game.CurrentPlayer, Is.EqualTo(Player.White), "Turn should switch from Red to White.");
        Assert.False(game.IsGameOver, "Game should not be over after one valid move.");
    }

    [Test]
    public void MakeMove_WhenNoMovesAvailable_EndsGame()
    {
        // Arrange
        var game = new Game();

        // Forcing a scenario where the current player has no moves:
        // One approach is to remove all Red pieces or place them in blocked positions.
        // Let's remove all Red pieces:
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var cell = game.Board.GetCellAt(new Position(row, col));
                if (!cell.IsEmpty && cell is Piece piece && piece.Owner == Player.Red)
                {
                    game.Board.RemovePieceAt(new Position(row, col));
                }
            }
        }

        // Act
        // Attempting to make any move is moot—there's none. But let's call "GetCurrentPlayerMoves" first:
        var redMoves = game.GetCurrentPlayerMoves();
        Assert.IsEmpty(redMoves, "Red has no pieces, so no moves.");
        
        // Because the game checks for 'IsGameOver' after switching turn or making a move,
        // we can force a check by making a 'move' or by switching the turn artificially.

        // Let's try to call MakeMove with an invalid move (or any move) to trigger the logic:
        bool result = game.MakeMove(new Move(new Position(0, 0))); // guaranteed invalid

        // Assert
        Assert.False(result, "MakeMove fails with no moves available for Red.");
        // The game might set IsGameOver to true if the *current* player is checked post-turn.
        // However, the logic in CheckGameOver is triggered *after* a successful move,
        // so in this scenario, we haven't had a successful move. Another approach is to directly 
        // switch the turn and see if White has moves, etc.

        // Another approach: switch turn manually with a valid Red move, or remove White pieces. 
        // For demonstration, let's forcibly test the next turn scenario:

        // Simulate that Red is forced to pass or fails to move:
        // We can do that by forcibly toggling players:
        var current = game.CurrentPlayer;
        // If we wanted to strictly ensure IsGameOver gets set, we'd do a successful move for Red, 
        // then remove White's pieces, etc. 
        // For brevity, we'll rely on the logic that if the new current player has no moves, game is over.

        // We'll do it the simpler approach:
        // 1) Switch Turn with a valid Red move if possible, then remove White's pieces, call MakeMove again, etc.
        // But we've already removed Red's pieces, so let's just check the logic that if we 
        // remove White's pieces and the turn becomes White, it might set IsGameOver. 
        
        // Let's revert the board to re-check. We'll do a simpler test approach:
        var game2 = new Game();
        // remove White pieces:
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var cell = game2.Board.GetCellAt(new Position(row, col));
                if (!cell.IsEmpty && cell is Piece piece && piece.Owner == Player.White)
                {
                    game2.Board.RemovePieceAt(new Position(row, col));
                }
            }
        }
        // Now Red presumably has moves. Let's do one valid Red move:
        var redMoves2 = game2.GetCurrentPlayerMoves();
        Assert.IsNotEmpty(redMoves2, "Red should have moves in game2 after removing only White pieces.");
        game2.MakeMove(redMoves2[0]); // Succeeds, now it will become White's turn

        // White should have 0 moves => game over
        Assert.True(game2.IsGameOver, "Game should be over if White has no pieces (hence no moves).");
    }

    [Test]
    public void Clone_CreatesSeparateGameState()
    {
        // Arrange
        var original = new Game();
        original.CurrentPlayer = Player.White;

        // Act
        var clone = original.Clone();

        // Assert
        Assert.That(clone, Is.Not.SameAs(original), "Clone should produce a new Game instance.");
        Assert.That(clone.Board, Is.Not.SameAs(original.Board), "Boards should not be the same reference.");
        Assert.That(clone.CurrentPlayer, Is.EqualTo(original.CurrentPlayer), "Cloned game should preserve current player.");
        Assert.That(clone.IsGameOver, Is.EqualTo(original.IsGameOver), "Cloned game should preserve IsGameOver state.");
    }
}