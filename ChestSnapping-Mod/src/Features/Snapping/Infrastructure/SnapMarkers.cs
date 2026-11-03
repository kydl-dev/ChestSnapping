using UnityEngine;
using UnityEngine.Rendering;

namespace ChestSnapping
{
    /// <summary>
    /// Optional debug view (console: chestsnap_show). Draws a small sphere on every snap point of every
    /// chest that has them, so you can see what shift / grow / point adjustments actually do.
    /// Yellow = corners, cyan = face centers. Session only, nothing is saved.
    ///
    /// The marker is a child of the snap point object (not a direct child of the chest), so vanilla's
    /// snap point lookup, which only reads direct children, never sees it. It has no collider.
    /// </summary>
    internal static class SnapMarkers
    {
        internal const string MarkerName = "chestsnap_marker";

        private const float MarkerSize = 0.1f; // meters (sphere diameter)

        internal static bool Enabled { get; private set; }

        private static Mesh _sphere;
        private static Material _cornerMaterial;
        private static Material _faceMaterial;

        /// <summary>Turns markers on/off for everything currently loaded (new chests follow automatically).</summary>
        internal static void SetEnabled(bool on)
        {
            Enabled = on;

            foreach (Piece piece in UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None))
            {
                if (ChestSnapPoints.HasPoints(piece.transform))
                    Sync(piece.transform);
            }
        }

        /// <summary>Adds or removes the markers on one chest so they match <see cref="Enabled"/>.</summary>
        internal static void Sync(Transform chest)
        {
            for (int i = 0; i < chest.childCount; i++)
            {
                Transform point = chest.GetChild(i);
                int index = ChestSnapPoints.PointIndex(point);
                if (index < 0) continue;

                Transform marker = point.Find(MarkerName);

                if (Enabled && marker == null)
                    CreateMarker(point, index < 8);
                else if (!Enabled && marker != null)
                    UnityEngine.Object.Destroy(marker.gameObject);
            }
        }

        private static void CreateMarker(Transform point, bool isCorner)
        {
            EnsureAssets();

            var go = new GameObject(MarkerName);
            Transform t = go.transform;
            t.SetParent(point, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * MarkerSize;

            go.AddComponent<MeshFilter>().sharedMesh = _sphere;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = isCorner ? _cornerMaterial : _faceMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void EnsureAssets()
        {
            if (_sphere != null) return;

            // Borrow the built-in sphere mesh from a throw-away primitive.
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sphere = tmp.GetComponent<MeshFilter>().sharedMesh;
            Material fallback = tmp.GetComponent<MeshRenderer>().sharedMaterial;
            UnityEngine.Object.DestroyImmediate(tmp);

            _cornerMaterial = MakeMaterial(new Color(1f, 0.85f, 0.1f, 1f), fallback);
            _faceMaterial = MakeMaterial(new Color(0.2f, 0.9f, 1f, 1f), fallback);
        }

        private static Material MakeMaterial(Color color, Material fallback)
        {
            // Unlit shaders so the colors are readable in any lighting. Shaders can be stripped from
            // builds, so fall back to the primitive's own material if neither one is available.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = shader != null ? new Material(shader) : new Material(fallback);
            material.color = color;
            return material;
        }
    }
}
