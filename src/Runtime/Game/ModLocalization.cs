using BlindSpire.Core.Status;
using STS2RitsuLib;
using STS2RitsuLib.Utils;

namespace BlindSpire.Game;

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
            modId: "BlindSpire",
            instanceName: "BlindSpire",
            resourceFolders: new[] { "BlindSpire.Localization" },
            fallbackLanguage: "eng");
    }

    public static string LowHpWarning => _i18n?.Get("low_hp_warning", LowHpHint.Message) ?? LowHpHint.Message;

    public static string UnknownCard => _i18n?.Get("unknown_card", "未知卡牌") ?? "未知卡牌";
}
