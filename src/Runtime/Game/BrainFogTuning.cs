namespace BrainFog.Game;

/// <summary>
/// Central visual tuning knobs (in-game adjustable by editing one place).
/// </summary>
internal static class BrainFogTuning
{
    // Enemy breathing box.
    public static readonly Godot.Color EnemyBoxColor = new(0.06f, 0.06f, 0.09f, 0.85f);
    public const float EnemyBoxAlphaMin = 0.55f;
    public const float EnemyBoxAlphaMax = 0.90f;

    // Card fog.
    public static readonly Godot.Color CardFogColor = Godot.Colors.Black;

    // "About to be forgotten" dim overlay (bad memory hint).
    public static readonly Godot.Color CardDimColor = new(0f, 0f, 0f, 0.5f);

    // Low-HP warning color.
    public static readonly Godot.Color LowHpColor = Godot.Colors.Red;
}
