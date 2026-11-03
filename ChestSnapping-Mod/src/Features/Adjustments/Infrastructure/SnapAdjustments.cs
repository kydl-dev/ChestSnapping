using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kydl.Common;
using UnityEngine;

namespace ChestSnapping
{
    /// <summary>
    /// Per-prefab tweaks applied on top of the automatically computed snap point positions.
    /// All values are in meters, in the chest's own local axes (+X right, +Y up, +Z front).
    /// </summary>
    internal sealed class PrefabAdjust
    {
        /// <summary>Moves the whole snap box, i.e. all 14 points together.</summary>
        public Vector3 Shift;

        /// <summary>
        /// Added to the box's half-size on each axis. +0.05 makes the box 10 cm bigger along that axis,
        /// negative values shrink it (handy for chests with an oversized invisible collider).
        /// </summary>
        public Vector3 Grow;

        /// <summary>Extra offset for single points, keyed by index into <see cref="SnapLayout.Points"/>.</summary>
        public readonly Dictionary<int, Vector3> Points = new Dictionary<int, Vector3>();

        public bool IsEmpty
        {
            get { return Shift == Vector3.zero && Grow == Vector3.zero && Points.Count == 0; }
        }
    }

    /// <summary>
    /// Holds the parsed <c>PrefabAdjustments</c> config value and writes it back when the console
    /// commands change something.
    ///
    /// Text format (entries separated by ';', one entry per prefab):
    ///   MyMod_Crate: shift=0,0.05,0 grow=-0.02,0,-0.02 p5=0,0.1,0; OtherChest: grow=0.1,0,0
    /// Numbers always use '.' as decimal separator, regardless of the OS language.
    /// </summary>
    internal static class SnapAdjustments
    {
        private static Dictionary<string, PrefabAdjust> _map =
            new Dictionary<string, PrefabAdjust>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The adjustment that is applied to chests: the saved one, or null when [Offsets] Enable (or EnableAll) is off.</summary>
        internal static PrefabAdjust Active(string prefab)
        {
            return ModSettings.OffsetsOn ? Get(prefab) : null;
        }

        /// <summary>The saved adjustment for this prefab, or null if there is none.</summary>
        internal static PrefabAdjust Get(string prefab)
        {
            PrefabAdjust adj;
            return _map.TryGetValue(prefab, out adj) ? adj : null;
        }

        // ---------------------------------------------------------------- editing (console commands)

        /// <summary>Applies <paramref name="change"/> to the prefab's adjustment and saves it to the config.</summary>
        internal static void Edit(string prefab, Action<PrefabAdjust> change)
        {
            PrefabAdjust adj = Get(prefab) ?? new PrefabAdjust();
            change(adj);

            if (adj.IsEmpty) _map.Remove(prefab);
            else _map[prefab] = adj;

            Save();
        }

        private static void Save()
        {
            // Setting the value raises SettingChanged, which makes the plugin re-read it and
            // move the snap points of all loaded chests on the next frame.
            ModSettings.Adjustments.Value = Serialize(_map);
        }

        // ---------------------------------------------------------------- parsing

        internal static void Load(string text)
        {
            var map = new Dictionary<string, PrefabAdjust>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(text))
            {
                foreach (string entry in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    int colon = entry.IndexOf(':');
                    string name = colon > 0 ? entry.Substring(0, colon).Trim() : "";
                    if (name.Length == 0)
                    {
                        ModLog.Warn(
                            "PrefabAdjustments: ignoring '" + entry.Trim() + "' (expected 'prefab: shift=x,y,z ...').");
                        continue;
                    }

                    var adj = new PrefabAdjust();
                    string[] tokens = entry.Substring(colon + 1)
                        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string token in tokens)
                        ParseToken(name, token, adj);

                    if (!adj.IsEmpty) map[name] = adj;
                }
            }

            _map = map;
        }

        private static void ParseToken(string prefab, string token, PrefabAdjust adj)
        {
            int eq = token.IndexOf('=');
            Vector3 v;

            if (eq <= 0 || !TryParseVector(token.Substring(eq + 1), out v))
            {
                ModLog.Warn(
                    "PrefabAdjustments: ignoring '" + token + "' for " + prefab + " (expected key=x,y,z).");
                return;
            }

            string key = token.Substring(0, eq).Trim().ToLowerInvariant();

            if (key == "shift")
            {
                adj.Shift = v;
            }
            else if (key == "grow")
            {
                adj.Grow = v;
            }
            else
            {
                int index;
                if (SnapLayout.TryFindPoint(key, out index))
                {
                    adj.Points[index] = v;
                }
                else
                {
                    ModLog.Warn(
                        "PrefabAdjustments: unknown key '" + key + "' for " + prefab +
                        " (use shift, grow, or p1..p" + SnapLayout.Points.Length + ").");
                }
            }
        }

        private static bool TryParseVector(string csv, out Vector3 v)
        {
            v = Vector3.zero;

            string[] parts = csv.Split(',');
            if (parts.Length != 3) return false;

            float x, y, z;
            if (!TryParseFloat(parts[0], out x) ||
                !TryParseFloat(parts[1], out y) ||
                !TryParseFloat(parts[2], out z))
                return false;

            v = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseFloat(string s, out float value)
        {
            return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        // ---------------------------------------------------------------- serializing

        private static string Serialize(Dictionary<string, PrefabAdjust> map)
        {
            var names = new List<string>(map.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);

            var entries = new List<string>();
            foreach (string name in names)
            {
                PrefabAdjust adj = map[name];
                var sb = new StringBuilder(name).Append(':');

                if (adj.Shift != Vector3.zero) sb.Append(" shift=").Append(Csv(adj.Shift));
                if (adj.Grow != Vector3.zero) sb.Append(" grow=").Append(Csv(adj.Grow));

                var indices = new List<int>(adj.Points.Keys);
                indices.Sort();
                foreach (int i in indices)
                    sb.Append(" p").Append(i + 1).Append('=').Append(Csv(adj.Points[i]));

                entries.Add(sb.ToString());
            }

            return string.Join("; ", entries.ToArray());
        }

        private static string Csv(Vector3 v)
        {
            return Num(v.x) + "," + Num(v.y) + "," + Num(v.z);
        }

        /// <summary>Invariant-culture number with at most 4 decimals and no "-0".</summary>
        internal static string Num(float f)
        {
            if (f == 0f) f = 0f; // turns -0 into +0
            return f.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
