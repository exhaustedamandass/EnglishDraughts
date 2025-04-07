using EnglishDraughtsGame.Models;
using EnglishDraughtsGame.ViewModels;
using Moq;
using OpenAI.Chat;

namespace EnglishDraughtsGame.Test.ViewModels;

[TestFixture]
    public class MainWindowViewModelTests
    {
        private MainWindowViewModel _viewModel;
        
        [SetUp]
        public void SetUp()
        {
            // In your real code, you might need to provide a mock or stub for ChatClient,
            // or handle it in the constructor. For a simple test, we can just instantiate directly:
            _viewModel = new MainWindowViewModel();
        }

        [Test]
        public void UserPlayer_SetToWhite_UpdatesBotPlayerToRed()
        {
            // Arrange
            _viewModel.UserPlayer = Player.Red;  // default is Red
            Assert.That(_viewModel.BotPlayer, Is.EqualTo(Player.White), 
                "Precondition: Bot should be White if user is Red.");
            
            // Act
            _viewModel.UserPlayer = Player.White;

            // Assert
            Assert.That(_viewModel.BotPlayer, Is.EqualTo(Player.Red), 
                "BotPlayer should flip to Red when UserPlayer is set to White.");
        }

        [Test]
        public void BotTimeLimit_SetOutOfRange_ClampsToMinMax()
        {
            // Act - set a value lower than 100ms
            _viewModel.BotTimeLimit = TimeSpan.FromMilliseconds(50);
            // Assert
            Assert.That(_viewModel.BotTimeLimit.TotalMilliseconds, Is.EqualTo(100), 
                "Should clamp to 100ms minimum.");

            // Act - set a value above 10000ms
            _viewModel.BotTimeLimit = TimeSpan.FromMilliseconds(15000);
            // Assert
            Assert.That(_viewModel.BotTimeLimit.TotalMilliseconds, Is.EqualTo(10000),
                "Should clamp to 10000ms maximum.");
        }

        [Test]
        public void OnPlayerMoveCompleted_BotMovesIfBotTurn()
        {
            // Arrange
            // Force scenario: It's Bot's turn (BotPlayer = White).
            _viewModel.BotPlayer = Player.White;
            // Make sure game is not over and it's White's turn:
            _viewModel.Game.CurrentPlayer = Player.White;
            
            // Act
            _viewModel.OnPlayerMoveCompleted();
            
            // Assert
            // We can't easily check the exact move if it's an internal Bot,
            // but we can check if MoveCount incremented as a sign that the bot moved:
            Assert.That(_viewModel.MoveCount, Is.EqualTo(1),
                "MoveCount should increment if the bot actually made a move.");
        }
    }