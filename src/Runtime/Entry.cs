using BlindSpire.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace BlindSpire;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public static void Initialize()
    {
        RevealPersistence.Register();
        RunManager.Instance.RunStarted += RevealPersistence.OnRunStarted;

        var harmony = new Harmony("BlindSpire");
        harmony.PatchAll(typeof(Entry).Assembly);
    }
}
