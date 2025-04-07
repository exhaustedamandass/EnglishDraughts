# JetBrains Test Task: Draughts Game
## Project Structure

This project follows the MVVM (Model-View-ViewModel) architecture pattern to ensure a clear separation of concerns and improve maintainability.

```
├── App.axaml
├── App.axaml.cs
├── Controls
│   └── BoardControl.cs     -> Rendering/Highlighting
├── EnglishDraughtsGame.csproj
├── Helpers
│   └── OpenAiHelper.cs -> Prompt for LLM generation
├── Models
│   ├── Board.cs 
│   ├── Bot.cs
│   ├── EmptyCell.cs
│   ├── Game.cs         -> Main Game logic
│   ├── IBoardCell.cs
│   ├── Move.cs
│   ├── Piece.cs
│   ├── PieceType.cs
│   ├── Player.cs
│   └── Position.cs
├── Program.cs
├── ViewLocator.cs
├── ViewModels
│   ├── MainWindowViewModel.cs  -> Main Functionality
│   └── ViewModelBase.cs
├── Views
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs

```

## Features
Main UI Overview
An intuitive interface to play and interact with the game.

![UI overview](https://github.com/exhaustedamandass/EnglishDraughts/blob/main/Assets/overview.png)

### Choosing a Side to Play On
Select whether to play as white or black.

![DropDown menu to choose side](https://github.com/exhaustedamandass/EnglishDraughts/blob/main/Assets/selectSide.png)

### Changing Bot Move Time
Set the thinking time for the bot.

![Bot Time Slider](https://github.com/exhaustedamandass/EnglishDraughts/blob/main/Assets/botTimer.png)

### Highlighting Possible Moves
All possible moves are visually highlighted for the currently selected piece.

![Highlighting](https://github.com/exhaustedamandass/EnglishDraughts/blob/main/Assets/highlighting.png)

### Get AI Hint Functionality
Leverage OpenAI's GPT-4o-mini model to receive smart move suggestions in real-time.

![logging](https://github.com/exhaustedamandass/EnglishDraughts/blob/main/Assets/logging.png)

## Used Technologies
Component	Technology
UI	Avalonia.UI
LLM Hint System	OpenAI GPT-4o-mini
Core	.NET
Testing  NUnit 3

## Quick Start Guide

```
git clone https://github.com/exhaustedamandass/EnglishDraughts.git
```

**Important!** Before running, set up the environment variable with the OpenAI API key

1) Open Run/Debug Configurations:

2) Select your project’s run configuration in the left panel.

3) Locate “Environment variables” in the configuration settings.

4) Add your variable "OPEN_AI_API_KEY"
