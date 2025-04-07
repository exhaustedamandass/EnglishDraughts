namespace EnglishDraughtsGame.Models;

public class Piece : IBoardCell
{
    public Player Owner { get; private set; }
    public PieceType Type { get; private set; }

    public Piece(Player owner, PieceType type = PieceType.Man)
    {
        Owner = owner;
        Type = type;
    }

    // Crowns the piece, converting it to a King.
    public void Crown()
    {
        Type = PieceType.King;
    }

    public bool IsKing => Type == PieceType.King;
    
    public bool IsEmpty => false;
}