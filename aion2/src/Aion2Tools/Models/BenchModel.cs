namespace Aion2Tools.Models;

/// <summary>A character left out of a composition, and why.</summary>
public class BenchModel
{
    public CharacterData Character { get; }

    public string Reason { get; }

    public BenchModel(CharacterData character, string reason)
    {
        Character = character;
        Reason = reason;
    }
}
