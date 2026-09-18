namespace BlindSpire.Core.Reveal;

/// <summary>How much the player knows about one specific card instance.</summary>
public enum CardKnowledge
{
    /// <summary>Initial state: the whole face is black fog.</summary>
    Unknown = 0,

    /// <summary>Face permanently revealed for this run (played once, or upgraded).</summary>
    Revealed = 1,
}
