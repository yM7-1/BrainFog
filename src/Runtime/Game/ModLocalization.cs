using BrainFog.Core.Status;
using STS2RitsuLib;
using STS2RitsuLib.Utils;

namespace BrainFog.Game;

/// <summary>
/// Mod strings via RitsuLib I18N (embedded zhs/eng tables), with the literal
/// Chinese message as the last-resort fallback.
/// </summary>
internal static class ModLocalization
{
    private static I18N? _i18n;

    public static void Initialize()
    {
        _i18n ??= RitsuLibFramework.CreateModLocalizationWithFallback(
            modId: "BrainFog",
            instanceName: "BrainFog",
            resourceFolders: new[] { "BrainFog.Localization" },
            fallbackLanguage: "eng");
    }

    public static string LowHpWarning => _i18n?.Get("low_hp_warning", LowHpHint.Message) ?? LowHpHint.Message;

    public static string UnknownCard => _i18n?.Get("unknown_card", "未知卡牌") ?? "未知卡牌";

    public static string UnknownRelic => _i18n?.Get("unknown_relic", "未知遗物") ?? "未知遗物";

    public static string HpSnapshotTag => _i18n?.Get("hp_snapshot_tag", "上次休息时的状态") ?? "上次休息时的状态";

    public static string HpSnapshotHint =>
        _i18n?.Get("hp_snapshot_hint", "该状态并非当前真实状态，请在火堆休息获得最新状态信息")
        ?? "该状态并非当前真实状态，请在火堆休息获得最新状态信息";
}
