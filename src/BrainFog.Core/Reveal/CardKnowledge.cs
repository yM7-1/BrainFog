namespace BrainFog.Core.Reveal;

/// <summary>How much the player knows about one card definition.</summary>
public enum CardKnowledge
{
    /// <summary>Initial state: the whole face is black fog.</summary>
    Unknown = 0,

    /// <summary>Face permanently revealed for this run (any copy played, or any copy upgraded).</summary>
    Revealed = 1,
}
