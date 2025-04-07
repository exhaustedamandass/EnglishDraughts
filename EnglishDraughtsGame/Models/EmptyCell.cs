namespace DraughtsGame.DataModels;

public class EmptyCell : IBoardCell
{
    public static readonly EmptyCell Instance = new EmptyCell();

    private EmptyCell() { }
    
    public bool IsEmpty => true;
}