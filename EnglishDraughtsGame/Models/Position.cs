namespace EnglishDraughtsGame.Models;

public struct Position(int row, int col)
{
    public int Row { get; set; } = row;
    public int Col { get; set; } = col;

    public override bool Equals(object? obj)
    {
        if (obj is Position other)
            return Row == other.Row && Col == other.Col;
        return false;
    }

    public override int GetHashCode() => Row * 31 + Col;

    public static bool operator ==(Position p1, Position p2)
    {
        return p1.Equals(p2);
    }

    public static bool operator !=(Position p1, Position p2)
    {
        return !p1.Equals(p2);
    }
}