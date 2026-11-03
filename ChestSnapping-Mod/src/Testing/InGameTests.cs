using System.Linq;
using KydlCore.Common;
using UnityEngine;

namespace ChestSnapping.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  run when the main menu is reached;   World_*  run 5 s after a world is loaded.
    /// Each returns "PASS: ..." / "FAIL: ..." / "SKIP: ..." with the numbers that decided it. Read-only: nothing is changed.
    /// </summary>
    internal static class InGameTests
    {
        private static string Startup_ConfigBound()
        {
            if (ModSettings.EnableAll == null || ModSettings.DebugLog == null) return "FAIL: ModSettings.Bind did not run (EnableAll / DebugLog null)";
            string r = ModSettings.On
                ? "PASS: EnableAll on, snapping " + ModSettings.PrefabsOn + ", adjustments " + ModSettings.OffsetsOn + ", debug log " + ModSettings.DebugLog.Value
                : "SKIP: EnableAll is off in the config";
            ModLog.Verbose("InGameTests: Startup_ConfigBound -> " + r);
            return r;
        }

        private static string World_DefaultChestsExist()
        {
            if (ZNetScene.instance == null) return "SKIP: no world";
            var missing = PrefabSets.DefaultPrefabs.Where(n => ZNetScene.instance.GetPrefab(n) == null).ToList();
            string r = missing.Count == 0
                ? "PASS: all " + PrefabSets.DefaultPrefabs.Length + " built-in chest prefabs exist"
                : "FAIL: missing built-in chest prefab(s): " + string.Join(", ", missing.ToArray()) + " (update DefaultPrefabs)";
            ModLog.Verbose("InGameTests: World_DefaultChestsExist -> " + r);
            return r;
        }

        private static string World_SnapPointsOnWoodChest()
        {
            if (ZNetScene.instance == null) return "SKIP: no world";
            if (!ModSettings.PrefabsOn) return "SKIP: snap points are switched off";
            GameObject chest = ZNetScene.instance.GetPrefab("piece_chest_wood");
            if (chest == null) return "SKIP: piece_chest_wood not loaded";
            Bounds b;
            string r = ChestSnapPoints.TryGetLocalBounds(chest.transform, out b)
                ? "PASS: wood chest box " + b.size + " has " + SnapLayout.Points.Length + " snap points to place"
                : "FAIL: no colliders or meshes on piece_chest_wood";
            ModLog.Verbose("InGameTests: World_SnapPointsOnWoodChest -> " + r);
            return r;
        }
    }
}
