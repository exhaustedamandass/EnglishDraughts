using System.Collections.Generic;

namespace DraughtsGame.DataModels;

public class Move
{
    // The starting position of the move.
    public Position Start { get; set; }
    // The sequence of positions the piece moves to.
    public List<Position> Sequence { get; set; }
    // The positions of any captured enemy pieces.
    public List<Position> CapturedPositions { get; set; }
 
    public Move(Position start)
    {
        Start = start;
        Sequence = [];
        CapturedPositions = [];
    }

    // Allows cloning a move, which is useful when exploring multi-jump options.
    public Move Clone()
    {
        var newMove = new Move(Start);
        newMove.Sequence.AddRange(Sequence);
        newMove.CapturedPositions.AddRange(CapturedPositions);
        return newMove;
    }

    // Returns the final destination of the move.
    public Position End => Sequence.Count > 0 ? Sequence[^1] : Start;

    // Adds a step to the move, optionally including a captured enemy position.
    public void AddStep(Position pos, Position? captured = null)
    {
        Sequence.Add(pos);
        if (captured.HasValue)
            CapturedPositions.Add(captured.Value);
    }
}