using System.Collections.Generic;
using Jotunn.Managers;

namespace ChestSnapping;

/// <summary>
/// The snap point GameObjects are named with "$tokens" so the vanilla "$msg_snapping &lt;name&gt;" message translates them.
/// Jotunn falls back to English for languages without a translation.
/// </summary>
internal static class SnapLocalization
{
    /// <summary>Registers the English names of the 14 snap points.</summary>
    internal static void Register()
    {
        var english = new Dictionary<string, string>();
        foreach (SnapDef def in SnapLayout.Points)
        {
            english[def.TokenKey] = def.EnglishName;
        }

        LocalizationManager.Instance.GetLocalization().AddTranslation("English", english);
    }
}
