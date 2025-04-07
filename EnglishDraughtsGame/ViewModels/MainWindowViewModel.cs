using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DraughtsGame.DataModels;
using OpenAI.Chat;
using ReactiveUI;

namespace EnglishDraughtsGame.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ChatClient _client;
    private Game game;
    private Bot bot;
    private Player botPlayer = Player.White; // Default bot plays as white
    private TimeSpan botTimeLimit = TimeSpan.FromSeconds(1); // Default time limit: 1 second
    private Player userPlayer = Player.Red;
    private readonly Player[] playerOptions = [Player.White, Player.Red];
    
    [ObservableProperty] private int _moveCount;
    
    public ObservableCollection<string> AppLog { get; } = [];

    private void AddToAppLog(string result)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AppLog.Add(result);
        });
    }
    
    public Player[] PlayerOptions => playerOptions;

    public Game Game
    {
        get => game;
        set => game = value;
    }
    
    public Player UserPlayer
    {
        get => userPlayer;
        set
        {
            if (SetProperty(ref userPlayer, value))
            {
                // Force Bot to be the other side
                BotPlayer = (userPlayer == Player.White)
                    ? Player.Red
                    : Player.White;
            }
        }  
    }
    
    public Player BotPlayer
    {
        get => botPlayer;
        set
        {
            if (SetProperty(ref botPlayer, value))
            {
                // Update Bot if it exists
                bot.BotPlayer = botPlayer;
                
                if (Game.CurrentPlayer == botPlayer && !Game.IsGameOver)
                {
                    MakeBotMove();
                    MoveCount++; // This ensures BoardControl re-renders
                }
                else
                {
                    // Force a re-render of the board even if the bot doesn't move
                    MoveCount++;
                }
            }
        }
    }
    
    public TimeSpan BotTimeLimit
    {
        get => botTimeLimit;
        set
        {
            // Ensure time limit is between 100ms and 10s
            int milliseconds = Math.Max(100, Math.Min(10000, (int)value.TotalMilliseconds));
            botTimeLimit = TimeSpan.FromMilliseconds(milliseconds);
            
            // Update bot if enabled
            bot.TimeLimit = botTimeLimit;
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(BotTimeLimitMs));
        }
    }
    
    // Property for binding to the numeric input in milliseconds
    public int BotTimeLimitMs
    {
        get => (int)botTimeLimit.TotalMilliseconds;
        set
        {
            // If the incoming value is different, update BotTimeLimit
            if (value != (int)botTimeLimit.TotalMilliseconds)
            {
                BotTimeLimit = TimeSpan.FromMilliseconds(value);
                // Raise property-changed if you're not using [ObservableProperty]
                OnPropertyChanged();
            }
        }
    }
    
    public ICommand GetAiHintCommand { get; }
    
    public MainWindowViewModel()
    {
        GetAiHintCommand = ReactiveCommand.CreateFromTask(
            GetAiHintCommandAsync,
            outputScheduler: RxApp.TaskpoolScheduler);
        
        _client = new ChatClient(model : "gpt-4o-mini", Environment.GetEnvironmentVariable("OPEN_AI_API_KEY"));
        
        Game = new Game();
        bot = new Bot(BotPlayer, BotTimeLimit);
    }
    
    private bool CanMakeBotMove()
    {
        return !Game.IsGameOver && 
               Game.CurrentPlayer == BotPlayer;
    }
    
    private void MakeBotMove()
    {
        if (!CanMakeBotMove()) return;
        
        // Get and make the bot's move
        Move bestMove = bot.GetBestMove(Game);
        Game.MakeMove(bestMove);
    }
    
    // TODO: move public methods up from private ones
    // Call this method after a player makes a move
    public void OnPlayerMoveCompleted()
    {
        // If it's the bot's turn, make a move automatically
        if (Game.CurrentPlayer == BotPlayer && !Game.IsGameOver)
        {
            MakeBotMove();
                
            MoveCount++;
        }
    }
    
    private async Task GetAiHintCommandAsync()
    {
        var prompt = GenerateAiPrompt();
        
        var completion = await _client.CompleteChatAsync(prompt);
        
        var aiMove = ParseAiMove(completion.Value.Content[0].Text);
        
        // (optional) Display the move, or do something else with it
        if (aiMove != null)
        {
            AddToAppLog("AI suggests move:");
            AddToAppLog(
                $"{SquareString(aiMove.Start)} -> {string.Join(" -> ", aiMove.Sequence.Select(SquareString))}"
            );
        }
        else
        {
            AddToAppLog("Could not parse a move from the AI's response.");
        }
    }

    private string GenerateAiPrompt()
    {
        var sb = new StringBuilder();
        var possibleMovesStringBuilder = new StringBuilder();

        // 1. Add your standard instructions about board setup, rules, etc.
        sb.AppendLine("You are an AI that plays English Draughts (Checkers) on an 8x8 board. Follow these instructions exactly:");
        sb.AppendLine();
        sb.AppendLine("Board Setup:");
        sb.AppendLine("The board is 8x8, with rows numbered 0 (top) to 7 (bottom) and columns A–H.");
        sb.AppendLine("Red pieces: 'R' for men, 'RK' for kings. White pieces: 'W' for men, 'WK' for kings. '.' for empty squares.");
        sb.AppendLine();
        sb.AppendLine("Movement and Capture Rules:");
        sb.AppendLine("• Regular men move diagonally forward 1 square (Red upward, White downward). Kings move diagonally any direction 1 square.");
        sb.AppendLine("• Captures (jumps) skip over an adjacent opponent piece to a vacant square beyond.");
        sb.AppendLine("• Regular men capture forward only; kings capture in any diagonal direction.");
        sb.AppendLine("• Forced capture: If any jump is available, it must be taken. If multiple jumps, choose the sequence capturing the most pieces (tie-break is free choice).");
        sb.AppendLine("• Promotion: A man reaching the opponent’s back row is immediately promoted to a king, potentially continuing to capture as a king if more jumps are available.");
        sb.AppendLine();
        sb.AppendLine("Task Requirements:");
        sb.AppendLine("• It is currently Red’s turn.");
        sb.AppendLine("• You must choose exactly ONE move that follows all the rules above.");
        sb.AppendLine("• Choose the move that will maximize Red's chances of winning (i.e., the 'best move to win the game').");
        sb.AppendLine("• If no moves exist, output exactly 'NO MOVES' with no extra text.");
        sb.AppendLine("• Format: if a single-jump or single-step move is used, for instance 'C3->D4'. If there are multiple jumps, chain them, e.g. 'C3->E5->G7'.");
        sb.AppendLine();
        sb.AppendLine("Current Board Configuration:");

        // 2. Describe the board (row by row, top=0 to bottom=7).
        var size = game.Board.GetSize(); // typically 8
        for (var row = 0; row < size; row++)
        {
            var rowBuilder = new StringBuilder();
            for (var col = 0; col < size; col++)
            {
                var pos = new Position(row, col);
                var cell = game.Board.GetCellAt(pos);
                if (cell is Piece p && !cell.IsEmpty)
                {
                    // Implementation of the TODO:
                    if (p.Owner == Player.Red)
                    {
                        
                        rowBuilder.Append(p.IsKing ? "RK" : "R");
                    }
                    else
                    {
                        rowBuilder.Append(p.IsKing ? "WK" : "W");
                    }

                    if (p.Owner == UserPlayer)
                    {
                        possibleMovesStringBuilder = GetPossibleMovesToStringBuilder(pos, possibleMovesStringBuilder);
                    }
                }
                else
                {
                    // Empty square
                    rowBuilder.Append('.');
                }
            }
            sb.AppendLine(rowBuilder.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("Choose the best single move for Red to increase their chances of winning, or 'NO MOVES' if none are available. Output no additional commentary.");

        // 3. Concatenate the main prompt with the possible-moves info before returning
        var finalPrompt = sb.ToString() + Environment.NewLine + possibleMovesStringBuilder.ToString();
        return finalPrompt;
    }

    public StringBuilder GetPossibleMovesToStringBuilder(Position pos, StringBuilder sb)
    {
        var validMoves = game.Board.GetValidMovesForPiece(pos);
        if (validMoves?.Count > 0)
        {
            sb.AppendLine($"Piece at {FormatSquare(pos)} has {validMoves.Count} possible move(s):");
            foreach (var move in validMoves)
            {
                sb.AppendLine("  " + MoveToNotation(move));
            }
        }
        else
        {
            sb.AppendLine($"Piece at {FormatSquare(pos)} has NO possible moves.");
        }

        return sb;
    }

    /// <summary>
    /// Converts a Move into a notation string like "C3->D4->F5".
    /// </summary>
    private string MoveToNotation(Move move)
    {
        var squares = new List<string> { FormatSquare(move.Start) };
        squares.AddRange(move.Sequence.Select(pos => FormatSquare(pos)));
        return string.Join("->", squares);
    }

    /// <summary>
    /// Formats a Position (row,col) into a user-friendly string like "C3".
    /// </summary>
    private string FormatSquare(Position pos)
    {
        char file = (char)('A' + pos.Col);
        int rank = pos.Row + 1;
        return $"{file}{rank}";
    }

    /// <summary>
    /// Takes an AI response (like "C3->D4" or "C3->E5->G7" or "NO MOVES")
    /// and converts it to a Move object. Returns null if no valid move exists.
    /// </summary>
    private Move? ParseAiMove(string aiText)
    {
        // 1) Standardize the text (trim whitespace, etc.)
        var cleanText = aiText.Trim().ToUpperInvariant();

        // 2) If the LLM says "NO MOVES", return null
        if (cleanText == "NO MOVES")
        {
            return null;
        }

        // 3) Attempt to find a move pattern (e.g., "C3->D4->F5")
        //    Modify the board notation pattern to match your style:
        //    - valid squares A-H, 1-8
        //    - multiple steps separated by '->'
        // e.g.  ^               start of string
        //       ([A-H][1-8])   a single square
        //       (?:->[A-H][1-8])*    zero or more '->Square'
        //       $               end of string
        var pattern = @"^([A-H][1-8])(?:->[A-H][1-8])*$";
        var match = Regex.Match(cleanText, pattern);

        if (!match.Success)
        {
            // The LLM output doesn't match a known move pattern
            return null;
        }

        // 4) Split on '->' to get each square
        var squares = cleanText.Split("->", StringSplitOptions.RemoveEmptyEntries);

        if (squares.Length < 1)
        {
            // At least a start is needed
            return null;
        }

        // 5) First square is Start
        var startPos = ParseSquare(squares[0]);
        var move = new Move(startPos);

        // 6) Each subsequent square is a step
        for (int i = 1; i < squares.Length; i++)
        {
            move.AddStep(ParseSquare(squares[i]));
        }

        // 7) Return the constructed Move
        return move;
    }

    /// <summary>
    /// Converts something like "C3" to Position(row,col).
    /// Adjust as needed for your own coordinate system.
    /// </summary>
    private Position ParseSquare(string square)
    {
        // Example approach: 'A' -> col 0, 'B' -> col 1, etc.
        // '1' -> row 0, '2' -> row 1, etc.
        // For "C3" => col=2, row=2 (0-based).
        square = square.Trim().ToUpperInvariant();
        if (square.Length < 2) throw new ArgumentException("Invalid square string.", nameof(square));

        char file = square[0];                     // e.g. 'C'
        int rank = int.Parse(square.Substring(1)); // e.g. '3' => 3

        int col = file - 'A';  // 'C' - 'A' = 2
        int row = rank - 1;    //  3 => 2 (0-based)
        return new Position(row, col);
    }

    /// <summary>
    /// Helper method to show a Position like "C3" for debugging or printing.
    /// </summary>
    private string SquareString(Position pos)
    {
        // col=2 => 'C'; row=2 => '3'
        // Make sure it matches ParseSquare in reverse.
        char file = (char)('A' + pos.Col);
        int rank = pos.Row + 1;
        return $"{file}{rank}";
    }
}