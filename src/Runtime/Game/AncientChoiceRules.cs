using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace BrainFog.Game;

/// <summary>
/// Boss/act-start Ancient relic choices stay visible (spec 0.03 i), but Neow's
/// starting three-choice is an event relic grant and stays hidden (spec 3.1/0.03 i).
/// </summary>
internal static class AncientChoiceRules
{
    public static bool StaysVisible(EventModel? eventModel) =>
        eventModel is AncientEventModel && eventModel is not Neow;
}
