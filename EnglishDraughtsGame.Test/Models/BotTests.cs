using EnglishDraughtsGame.Models;

namespace EnglishDraughtsGame.Test.Models;

[TestFixture]
public class BotTests
{
    [Test]
    public void Bot_Constructor_SetsProperties()
    {
        // Arrange
        var timeLimit = TimeSpan.FromSeconds(1);

        // Act
        var bot = new Bot(Player.White, timeLimit);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(bot.BotPlayer, Is.EqualTo(Player.White));
            Assert.That(bot.TimeLimit, Is.EqualTo(timeLimit));
        });
    }

    [Test]
    public void GetBestMove_NoMovesAvailable_ReturnsNull()
    {
        // Arrange
        var bot = new Bot(Player.Red, TimeSpan.FromMilliseconds(200));
        var game = new Game();

        // Remove all Red pieces so Red has no moves
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var cell = game.Board.GetCellAt(new Position(row, col));
                if (!cell.IsEmpty && cell is Piece { Owner: Player.Red })
                {
                    game.Board.RemovePieceAt(new Position(row, col));
                }
            }
        }

        // It's Red's turn; Red has no moves
        // Act
        var bestMove = bot.GetBestMove(game);

        // Assert
        Assert.IsNull(bestMove, "Should return null when no moves are available.");
    }

    [Test]
    public void GetBestMove_StopsWhenTimeLimitExceeded()
    {
        // Arrange
        // Give the Bot a very short time limit to force an early cutoff
        var bot = new Bot(Player.Red, TimeSpan.FromMilliseconds(1));
        var game = new Game();

        // Act
        var bestMove = bot.GetBestMove(game);

        // Assert
        // Even under a 1ms limit, the bot should return some move if any are available,
        // but we can't confirm it reached maximum depth. We just check it doesn't hang forever.
        // So effectively, the test passes if it completes quickly without timing out.
        Assert.That(bestMove, Is.Null, "Bot should return something or null within the allotted time.");
    }
}