namespace Clc.BibDedupe.Web.Models;

public class PairDecision
{
    private BibDupePair _pair = new();

    public BibDupePair Pair
    {
        get => _pair;
        set => _pair = value ?? new BibDupePair();
    }

    public BibDupePairAction Action { get; set; }

    public PairDecision Clone() => new()
    {
        Pair = Pair.Clone(),
        Action = Action
    };
}
