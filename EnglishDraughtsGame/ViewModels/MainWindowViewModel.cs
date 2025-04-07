using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EnglishDraughtsGame.Helpers;
using EnglishDraughtsGame.Models;
using OpenAI.Chat;
using ReactiveUI;

namespace EnglishDraughtsGame.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    //--------------------------------------------------------------------------------
    // Constants
    //--------------------------------------------------------------------------------
    private const int DefaultBotTimeSeconds = 1;
    private const int MinTimeMs = 100;
    private const int MaxTimeMs = 10_000;
    private const string ModelName = "gpt-4o-mini";
    
    /// <summary>
    /// Special string used when the AI sees no moves.
    /// </summary>
    private const string NoMovesString = "NO MOVES";

    //--------------------------------------------------------------------------------
    // Fields
    //--------------------------------------------------------------------------------
    private readonly ChatClient _openAiClient;
    private readonly Player[] _playerOptions = [Player.White, Player.Red];
    
    private Game _game;
    private readonly Bot _bot;
    private Player _botPlayer = Player.White; // Default bot plays as White
    private TimeSpan _botTimeLimit = TimeSpan.FromSeconds(DefaultBotTimeSeconds);
    private Player _userPlayer = Player.Red;

    [ObservableProperty]
    private int _moveCount;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------
    public MainWindowViewModel()
    {
        // Create the OpenAI client with the chosen model name.
        _openAiClient = new ChatClient(model: ModelName, Environment.GetEnvironmentVariable("OPEN_AI_API_KEY") ?? "ENV_VAR_NOT_SET");
        
        // Initialize game objects.
        _game = new Game();
        _bot = new Bot(_botPlayer, _botTimeLimit);

        // Reactive command used to fetch an AI hint asynchronously.
        GetAiHintCommand = ReactiveCommand.CreateFromTask(
            GetAiHintCommandAsync,
            outputScheduler: RxApp.TaskpoolScheduler);
    }

    //--------------------------------------------------------------------------------
    // Properties
    //--------------------------------------------------------------------------------
    /// <summary>
    /// The list of log messages displayed in the UI.
    /// </summary>
    public ObservableCollection<string> AppLog { get; } = new();

    /// <summary>
    /// The set of Player options (e.g., White, Red) used for binding in the UI.
    /// </summary>
    public Player[] PlayerOptions => _playerOptions;

    /// <summary>
    /// The primary Game object used by the UI.
    /// </summary>
    public Game Game
    {
        get => _game;
        set => _game = value;
    }
    
    /// <summary>
    /// The current user player color (e.g., White or Red).
    /// Changing this forces the Bot to the opposite color.
    /// </summary>
    public Player UserPlayer
    {
        get => _userPlayer;
        set
        {
            if (SetProperty(ref _userPlayer, value))
            {
                // Force Bot to be the other side
                BotPlayer = (_userPlayer == Player.White)
                    ? Player.Red
                    : Player.White;
            }
        }  
    }
    
    /// <summary>
    /// The current Bot color. Changing this triggers a bot move if it's currently the bot's turn.
    /// </summary>
    public Player BotPlayer
    {
        get => _botPlayer;
        set
        {
            if (SetProperty(ref _botPlayer, value))
            {
                // Update Bot's player color if it exists
                _bot.BotPlayer = _botPlayer;
                
                // If it's the bot's turn and the game isn't over, make a bot move immediately
                if (_game.CurrentPlayer == _botPlayer && !_game.IsGameOver)
                {
                    MakeBotMove();
                }

                // Force a re-render of the board even if the bot doesn't move
                MoveCount++; // Forces BoardControl re-render
            }
        }
    }

    /// <summary>
    /// The time limit the Bot uses per move (as a TimeSpan).
    /// Ensures time limit is between 100ms and 10s.
    /// </summary>
    public TimeSpan BotTimeLimit
    {
        get => _botTimeLimit;
        set
        {
            int milliseconds = Math.Max(MinTimeMs, Math.Min(MaxTimeMs, (int)value.TotalMilliseconds));
            _botTimeLimit = TimeSpan.FromMilliseconds(milliseconds);
            
            // Update bot if enabled
            _bot.TimeLimit = _botTimeLimit;
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(BotTimeLimitMs));
        }
    }

    /// <summary>
    /// The time limit in milliseconds for the Bot, used for binding to a numeric control in the UI.
    /// </summary>
    public int BotTimeLimitMs
    {
        get => (int)_botTimeLimit.TotalMilliseconds;
        set
        {
            if (value != (int)_botTimeLimit.TotalMilliseconds)
            {
                BotTimeLimit = TimeSpan.FromMilliseconds(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Command used to request a hint from the AI.
    /// </summary>
    public ICommand GetAiHintCommand { get; }

    //--------------------------------------------------------------------------------
    // Public Methods
    //--------------------------------------------------------------------------------
    
    /// <summary>
    /// Call this method after a human player completes a move.
    /// If it's the bot's turn next, the bot moves automatically.
    /// </summary>
    public void OnPlayerMoveCompleted()
    {
        if (_game.CurrentPlayer == _botPlayer && !_game.IsGameOver)
        {
            MakeBotMove();
            MoveCount++;
        }
    }

    //--------------------------------------------------------------------------------
    // Private Methods
    //--------------------------------------------------------------------------------

    /// <summary>
    /// Appends a string to the main application log.
    /// </summary>
    private void AddToAppLog(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AppLog.Add(message);
        });
    }

    /// <summary>
    /// Checks if it's valid for the Bot to make a move right now.
    /// </summary>
    private bool CanMakeBotMove()
    {
        return !_game.IsGameOver && 
               _game.CurrentPlayer == _botPlayer;
    }

    /// <summary>
    /// Asks the Bot for its best move and applies that move to the Game.
    /// </summary>
    private void MakeBotMove()
    {
        if (!CanMakeBotMove()) return;
        
        Move bestMove = _bot.GetBestMove(_game);
        _game.MakeMove(bestMove);
        if (_game.IsGameOver)
        {
            AddToAppLog("Game Over! Bot made the last move.");
        }
        else
        {
            AddToAppLog("Bot made its move.");
        }
    }

    /// <summary>
    /// Called by GetAiHintCommand to asynchronously retrieve and parse an AI hint.
    /// </summary>
    private async Task GetAiHintCommandAsync()
    {
        // 1) Build the AI prompt
        string prompt = OpenAiHelper.GenerateAiPrompt(_game, _userPlayer);

        // 2) Send the prompt to the AI
        var completion = await _openAiClient.CompleteChatAsync(prompt);

        // 3) Parse the AI's response
        string aiResponse = completion.Value.Content[0].Text;
        Move? aiMove = OpenAiHelper.ParseAiMove(aiResponse, NoMovesString);

        // 4) Display or handle the AI's suggested move
        if (aiMove != null)
        {
            AddToAppLog("AI suggests move:");
            AddToAppLog($"{SquareString(aiMove.Start)} -> {string.Join(" -> ", aiMove.Sequence.Select(SquareString))}");
        }
        else
        {
            AddToAppLog("Could not parse a move from the AI's response.");
        }
    }

    /// <summary>
    /// Converts a Position to a string like "C3" for display/logging.
    /// Must stay in sync with ParseSquare (in the helper).
    /// </summary>
    private string SquareString(Position pos)
    {
        var file = (char)('A' + pos.Col);
        var rank = pos.Row + 1;
        return $"{file}{rank}";
    }
}