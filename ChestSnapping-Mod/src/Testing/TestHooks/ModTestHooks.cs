using System;
using ChestSnapping.Bootstrap;
using KydlCore.Common;
using UnityEngine;

namespace ChestSnapping.Testing
{
    /// <summary>KydlCore D-084: hooks for DrakesTestLab (tests/TestLab/suites/ChestSnapping). They read the real prefabs of the loaded world.</summary>
    internal static class ModTestHooks
    {
        private static GameObject Prefab(string name)
        {
            GameObject p = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (p == null) throw new Exception("prefab " + name + " not found (no world loaded?)");
            return p;
        }

        /// <summary>Every test starts here (MIGRATION_DECISIONS C26): the player on the open arena spot 70 m from the lab start, facing +z, god mode on.</summary>
        [TestHook("chestsnapping.arena")]
        private static string Arena() { return TestArena.MovePlayer(); }

        /// <summary>Is the feature running (FeatureRegistry state after its Enable switches)? ids: snapping, adjustments.</summary>
        [TestHook("chestsnapping.featureRunning")]
        private static bool FeatureRunning(string id) { return ModInstaller.Current != null && ModInstaller.Current.Features.IsRunning(id); }

        /// <summary>Is this tab (settings) registered in the KY hub right now?</summary>
        [TestHook("chestsnapping.tabRegistered")]
        private static bool TabRegistered(string id) { return KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, id); }

        [TestHook("chestsnapping.selftest")]
        private static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// <summary>"default", "extra", "auto" or "none": does this prefab get snap points under the current config?</summary>
        [TestHook("chestsnapping.source")]
        private static string Source(string prefab) { return ChestSnapPoints.Source(Prefab(prefab)) ?? "none"; }

        /// <summary>
        /// Builds a placement ghost of the prefab (what the player holds while building; Piece.Awake runs) and counts its snap points.
        /// The ghost is destroyed again; nothing is placed in the world.
        /// </summary>
        [TestHook("chestsnapping.ghostPoints")]
        private static int GhostPoints(string prefab)
        {
            GameObject source = Prefab(prefab);
            GameObject ghost;
            ZNetView.m_forceDisableInit = true;
            try { ghost = UnityEngine.Object.Instantiate(source); }
            finally { ZNetView.m_forceDisableInit = false; }
            try { return ChestSnapPoints.CountPoints(ghost.transform); }
            finally { UnityEngine.Object.Destroy(ghost); }
        }

        /// <summary>
        /// How far the saved adjustment moves the first snap point on Y (metres, rounded to 3 decimals): applied position minus the
        /// plain measured one. 0 when there is no adjustment or [Offsets] Enable is off.
        /// </summary>
        [TestHook("chestsnapping.adjustDeltaY")]
        private static float AdjustDeltaY(string prefab)
        {
            GameObject p = Prefab(prefab);
            Bounds bounds;
            if (!ChestSnapPoints.TryGetLocalBounds(p.transform, out bounds)) throw new Exception("no bounds on " + prefab);
            Vector3[] plain = ChestSnapPoints.ComputePositions(bounds, null);
            Vector3[] applied = ChestSnapPoints.ComputePositions(bounds, SnapAdjustments.Active(prefab));
            return (float)Math.Round(applied[0].y - plain[0].y, 3);
        }
    }
}
