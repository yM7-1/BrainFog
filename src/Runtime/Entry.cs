using BlindSpire.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace BlindSpire;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    private static void TryAttachDebugOverlay()
    {
        if (!ModRuntime.DebugEnabled || Godot.Engine.GetMainLoop() is not Godot.SceneTree tree || tree.Root == null)
        {
            return;
        }
        tree.Root.CallDeferred(Godot.Node.MethodName.AddChild, new Game.DebugOverlay { Name = "BlindSpireDebugOverlay" });
    }

    public static void Initialize()
    {
        try
        {
            ModLocalization.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error("[BlindSpire] localization init failed: " + ex);
        }

        try
        {
            RevealPersistence.Register();
        }
        catch (Exception ex)
        {
            Log.Error("[BlindSpire] persistence register failed: " + ex);
        }

        try
        {
            RunManager.Instance.RunStarted += RevealPersistence.OnRunStarted;
        }
        catch (Exception ex)
        {
            Log.Error("[BlindSpire] run event subscription failed: " + ex);
        }

        try
        {
            var harmony = new Harmony("BlindSpire");
            harmony.PatchAll(typeof(Entry).Assembly);
            Log.Info("[BlindSpire] loaded (v0.1.0)");
            ModRuntime.DumpState("loaded");
            TryAttachDebugOverlay();
        }
        catch (Exception ex)
        {
            Log.Error("[BlindSpire] Harmony patch application failed: " + ex);
        }
    }
}
