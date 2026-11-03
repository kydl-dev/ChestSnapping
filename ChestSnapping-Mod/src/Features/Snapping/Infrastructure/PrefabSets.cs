using System;
using System.Collections.Generic;

namespace ChestSnapping
{
    /// <summary>The parsed prefab lists of the config: built-in chests, extra chests, excluded chests.</summary>
    internal static class PrefabSets
    {
        /// <summary>Buildable containers that ship with the base game (prefab names, case-insensitive).</summary>
        internal static readonly string[] DefaultPrefabs =
        {
            "piece_chest_wood",       // Wood chest
            "piece_chest",            // Reinforced chest
            "piece_chest_private",    // Private chest
            "piece_chest_blackmetal", // Blackmetal chest
        };

        /// <summary>Default + extra prefabs.</summary>
        internal static HashSet<string> Included = new HashSet<string>(DefaultPrefabs, StringComparer.OrdinalIgnoreCase);

        /// <summary>The prefabs of the ExtraPrefabs config entry.</summary>
        internal static HashSet<string> ExtraSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The prefabs of the ExcludedPrefabs config entry.</summary>
        internal static HashSet<string> Excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Re-reads the config text (and the saved adjustments). Call after every config change.</summary>
        internal static void Rebuild()
        {
            ExtraSet = PrefabNames.Parse(ModSettings.ExtraPrefabs.Value);
            Excluded = PrefabNames.Parse(ModSettings.ExcludedPrefabs.Value);

            var all = new HashSet<string>(DefaultPrefabs, StringComparer.OrdinalIgnoreCase);
            all.UnionWith(ExtraSet);
            Included = all;

            SnapAdjustments.Load(ModSettings.Adjustments.Value);
        }
    }
}
