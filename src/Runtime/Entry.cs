using BrainFog.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace BrainFog;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    private static void TryAttachDebugOverlay()
    {
        if (!ModRuntime.DebugEnabled || Godot.Engine.GetMainLoop() is not Godot.SceneTree tree || tree.Root == null)
        {
            return;
        }
        tree.Root.CallDeferred(Godot.Node.MethodName.AddChild, new Game.DebugOverlay { Name = "BrainFogDebugOverlay" });
    }

    private static void TryAttachTextBlurDriver()
    {
        if (Godot.Engine.GetMainLoop() is not Godot.SceneTree tree || tree.Root == null)
        {
            return;
        }
        tree.Root.CallDeferred(Godot.Node.MethodName.AddChild, new Game.GlobalTextBlurDriver { Name = "BrainFogTextBlurDriver" });
    }

    private static void TryAttachDifficultyPanel()
    {
        if (Godot.Engine.GetMainLoop() is not Godot.SceneTree tree || tree.Root == null)
        {
            return;
        }
        tree.Root.CallDeferred(Godot.Node.MethodName.AddChild, new Game.DifficultyPanel { Name = "BrainFogDifficultyPanelLayer" });
    }

    private static void TryAttachPlayCounterPanel()
    {
        if (Godot.Engine.GetMainLoop() is not Godot.SceneTree tree || tree.Root == null)
        {
            return;
        }
        tree.Root.CallDeferred(Godot.Node.MethodName.AddChild, new Game.PlayCounterPanel { Name = "BrainFogPlayCounterLayer" });
    }

    public static void Initialize()
    {
        try
        {
            ModLocalization.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error("[BrainFog] localization init failed: " + ex);
        }

        try
        {
            RevealPersistence.Register();
        }
        catch (Exception ex)
        {
            Log.Error("[BrainFog] persistence register failed: " + ex);
        }

        try
        {
            RunManager.Instance.RunStarted += RevealPersistence.OnRunStarted;
        }
        catch (Exception ex)
        {
            Log.Error("[BrainFog] run event subscription failed: " + ex);
        }

        try
        {
            var harmony = new Harmony("BrainFog");
            harmony.PatchAll(typeof(Entry).Assembly);
            var gameVersion = typeof(MegaCrit.Sts2.Core.Runs.RunManager).Assembly.GetName().Version?.ToString() ?? "unknown";
            var modVersion = typeof(Entry).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            Log.Info($"[BrainFog] loaded (v{modVersion}) against game assembly {gameVersion}");
            ModRuntime.DumpState("loaded");
            Game.DifficultyRuntime.Load();
            TryAttachTextBlurDriver();
            TryAttachDifficultyPanel();
            TryAttachDebugOverlay();
        }
        catch (Exception ex)
        {
            Log.Error("[BrainFog] Harmony patch application failed: " + ex);
        }
    }
}
