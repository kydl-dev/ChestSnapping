using System;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;

namespace ChestSnapping
{
    internal static class ChestSnapPoints
    {
        private const string SnapTag = "snappoint"; // tag vanilla's Piece.GetSnapPoints looks for (direct children only)
        private const string NamePrefix = "$chestsnap_";

        private static readonly HashSet<string> WarnedPrefabs = new HashSet<string>();

        // ---------------------------------------------------------------- public API

        /// <summary>
        /// Adds or removes snap points on this piece depending on the current config.
        /// With <paramref name="refresh"/> set, points that already exist are also moved to match the
        /// current adjustments (used after config changes).
        /// </summary>
        internal static void Apply(Piece piece, bool refresh = false)
        {
            if (piece == null) return;

            try
            {
                string prefabName = GetPrefabName(piece.gameObject);
                bool want = Wants(piece.gameObject);
                bool has = HasPoints(piece.transform);

                if (want && !has)
                {
                    AddPoints(piece.transform, prefabName);
                    ModLog.Verbose("ChestSnapPoints: added snap points to " + prefabName);
                }
                else if (want && refresh)
                {
                    Reposition(piece.transform, prefabName);
                }
                else if (!want && has)
                {
                    RemovePoints(piece.transform);
                    ModLog.Verbose("ChestSnapPoints: removed snap points from " + prefabName);
                }

                if (want && SnapMarkers.Enabled) SnapMarkers.Sync(piece.transform);
            }
            catch (Exception e)
            {
                ModLog.Warn("ChestSnapPoints: could not update snap points on " + piece.name + ": " + e);
            }
        }

        /// <summary>Re-evaluates every loaded piece (used after config changes).</summary>
        internal static void RefreshAll()
        {
            foreach (Piece piece in UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None))
                Apply(piece, true);
        }

        /// <summary>Should this object (prefab asset or instance) get snap points under the current config?</summary>
        internal static bool Wants(GameObject go)
        {
            return Source(go) != null;
        }

        /// <summary>"default", "extra", "auto" or null (= no snap points).</summary>
        internal static string Source(GameObject go)
        {
            if (!ModSettings.PrefabsOn) return null;

            string name = GetPrefabName(go);

            if (PrefabSets.Excluded.Contains(name)) return null;

            if (PrefabSets.ExtraSet.Contains(name)) return "extra";
            if (PrefabSets.Included.Contains(name)) return "default";

            if (ModSettings.AutoDetectContainers.Value && IsPlainContainer(go)) return "auto";

            return null;
        }

        /// <summary>A Container on the root that is not a ship/cart (those have containers too).</summary>
        internal static bool IsPlainContainer(GameObject go)
        {
            return go.GetComponent<Container>() != null
                   && go.GetComponent<Ship>() == null
                   && go.GetComponent<Vagon>() == null;
        }

        internal static string GetPrefabName(GameObject go)
        {
            return PrefabNames.StripClone(go.name);
        }

        // ---------------------------------------------------------------- add / move / remove

        internal static bool HasPoints(Transform root)
        {
            return CountPoints(root) > 0;
        }

        /// <summary>How many of our snap points hang directly under this root.</summary>
        internal static int CountPoints(Transform root)
        {
            int n = 0;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).name.StartsWith(NamePrefix, StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>Index into <see cref="SnapLayout.Points"/> if this transform is one of our snap points, else -1.</summary>
        internal static int PointIndex(Transform t)
        {
            return t.name.StartsWith(NamePrefix, StringComparison.Ordinal)
                ? SnapLayout.IndexOfObjectName(t.name)
                : -1;
        }

        private static void AddPoints(Transform root, string prefabName)
        {
            Vector3[] positions;
            if (!TryComputePositions(root, prefabName, out positions)) return;

            for (int i = 0; i < SnapLayout.Points.Length; i++)
            {
                var go = new GameObject(SnapLayout.Points[i].ObjectName);
                go.tag = SnapTag;

                Transform t = go.transform;
                t.SetParent(root, false);
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                t.localPosition = positions[i];
            }
        }

        /// <summary>Moves the existing snap points of a chest to where the current adjustments say they belong.</summary>
        private static void Reposition(Transform root, string prefabName)
        {
            Vector3[] positions;
            if (!TryComputePositions(root, prefabName, out positions)) return;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                int index = PointIndex(child);
                if (index >= 0) child.localPosition = positions[index];
            }
        }

        private static void RemovePoints(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith(NamePrefix, StringComparison.Ordinal)) continue;

                // Destroy() is deferred to end of frame; un-tag first so Piece.GetSnapPoints ignores it immediately.
                child.gameObject.tag = "Untagged";
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        // ---------------------------------------------------------------- positions

        private static bool TryComputePositions(Transform root, string prefabName, out Vector3[] positions)
        {
            positions = null;

            Bounds bounds;
            if (!TryGetLocalBounds(root, out bounds))
            {
                if (WarnedPrefabs.Add(prefabName))
                    ModLog.Warn("ChestSnapPoints: no colliders or meshes found on '" + prefabName + "', cannot place snap points on it.");
                return false;
            }

            positions = ComputePositions(bounds, SnapAdjustments.Active(prefabName));
            return true;
        }

        /// <summary>
        /// Final local position of each of the 14 points: the box corner/face-center from the bounds,
        /// after the prefab's shift and grow, plus any extra offset for that single point.
        /// </summary>
        internal static Vector3[] ComputePositions(Bounds bounds, PrefabAdjust adj)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            if (adj != null)
            {
                center += adj.Shift;
                extents = Vector3.Max(extents + adj.Grow, Vector3.zero);
            }

            var result = new Vector3[SnapLayout.Points.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Vector3 p = center + Vector3.Scale(extents, SnapLayout.Points[i].Factor);

                Vector3 extra;
                if (adj != null && adj.Points.TryGetValue(i, out extra)) p += extra;

                result[i] = p;
            }

            return result;
        }

        // ---------------------------------------------------------------- bounds

        /// <summary>
        /// Axis-aligned bounds of the object's solid colliders, expressed in the ROOT's local space
        /// (so it is independent of where/how the chest is currently placed). Falls back to mesh
        /// bounds if the prefab has no non-trigger colliders.
        /// </summary>
        internal static bool TryGetLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default(Bounds);
            bool found = false;
            Matrix4x4 worldToRoot = root.worldToLocalMatrix;

            foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            {
                if (col == null || col.isTrigger) continue;

                Vector3 center, size;
                if (!TryGetColliderBox(col, out center, out size)) continue;

                Encapsulate(ref bounds, ref found, worldToRoot * col.transform.localToWorldMatrix, center, size);
            }

            if (found) return true;

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.gameObject.name == SnapMarkers.MarkerName) continue; // our own debug spheres

                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;

                Encapsulate(ref bounds, ref found, worldToRoot * mf.transform.localToWorldMatrix,
                    mesh.bounds.center, mesh.bounds.size);
            }

            return found;
        }

        private static bool TryGetColliderBox(Collider col, out Vector3 center, out Vector3 size)
        {
            var box = col as BoxCollider;
            if (box != null)
            {
                center = box.center;
                size = box.size;
                return true;
            }

            var sphere = col as SphereCollider;
            if (sphere != null)
            {
                center = sphere.center;
                size = Vector3.one * (sphere.radius * 2f);
                return true;
            }

            var capsule = col as CapsuleCollider;
            if (capsule != null)
            {
                float d = capsule.radius * 2f;
                float h = Mathf.Max(capsule.height, d);
                center = capsule.center;
                size = capsule.direction == 0 ? new Vector3(h, d, d)
                     : capsule.direction == 1 ? new Vector3(d, h, d)
                     : new Vector3(d, d, h);
                return true;
            }

            var mc = col as MeshCollider;
            if (mc != null && mc.sharedMesh != null)
            {
                center = mc.sharedMesh.bounds.center;
                size = mc.sharedMesh.bounds.size;
                return true;
            }

            center = Vector3.zero;
            size = Vector3.zero;
            return false;
        }

        /// <summary>Transforms the 8 corners of a local-space box by <paramref name="m"/> and grows the bounds.</summary>
        private static void Encapsulate(ref Bounds bounds, ref bool found, Matrix4x4 m, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = center + new Vector3(
                    (i & 1) == 0 ? -h.x : h.x,
                    (i & 2) == 0 ? -h.y : h.y,
                    (i & 4) == 0 ? -h.z : h.z);

                Vector3 p = m.MultiplyPoint3x4(corner);

                if (!found)
                {
                    bounds = new Bounds(p, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(p);
                }
            }
        }
    }
}
