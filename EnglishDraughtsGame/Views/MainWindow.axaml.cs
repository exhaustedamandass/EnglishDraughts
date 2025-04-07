using Avalonia.Controls;
using EnglishDraughtsGame.ViewModels;

namespace EnglishDraughtsGame.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void OnMoveCompleted(object sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.OnPlayerMoveCompleted();
        }
    }
}