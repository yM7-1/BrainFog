using BlindSpire.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace BlindSpire;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public static void Initialize()
    {
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
        }
        catch (Exception ex)
        {
            Log.Error("[BlindSpire] Harmony patch application failed: " + ex);
        }
    }
}
