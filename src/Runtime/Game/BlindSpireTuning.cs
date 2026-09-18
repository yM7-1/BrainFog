namespace BlindSpire.Game;

/// <summary>
/// Central visual tuning knobs (in-game adjustable by editing one place).
/// </summary>
internal static class BlindSpireTuning
{
    // Combat vision mask (screen-space, radius as fraction of viewport height).
    public const float VisionHoleRadius = 0.30f;
    public const float VisionSoftness = 0.12f;

    // Enemy breathing box.
    public static readonly Godot.Color EnemyBoxColor = new(0.06f, 0.06f, 0.09f, 0.85f);
    public const float EnemyBoxAlphaMin = 0.55f;
    public const float EnemyBoxAlphaMax = 0.90f;

    // Card fog.
    public static readonly Godot.Color CardFogColor = Godot.Colors.Black;

    // Low-HP warning color.
    public static readonly Godot.Color LowHpColor = Godot.Colors.Red;
}
