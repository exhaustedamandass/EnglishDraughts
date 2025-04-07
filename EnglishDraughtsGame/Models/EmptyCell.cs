namespace EnglishDraughtsGame.Models;

public class EmptyCell : IBoardCell
{
    public static readonly EmptyCell Instance = new EmptyCell();

    private EmptyCell() { }
    
    public bool IsEmpty => true;
}