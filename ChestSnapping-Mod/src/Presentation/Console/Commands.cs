using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jotunn.Entities;
using Kydl.Common;
using UnityEngine;

namespace ChestSnapping
{
    /// <summary>Shared helpers for the chestsnap_* console commands.</summary>
    internal static class CommandUtil
    {
        internal static void Print(string text)
        {
            global::Console.instance.Print(text);
        }

        /// <summary>Accepts "0.05" and "0,05", whatever the OS language is.</summary>
        internal static bool TryParseFloat(string s, out float value)
        {
            return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Reads three numbers from args[start..start+2].</summary>
        internal static bool TryParseVector(string[] args, int start, out Vector3 v)
        {
            v = Vector3.zero;
            if (args == null || args.Length < start + 3) return false;

            float x, y, z;
            if (!TryParseFloat(args[start], out x) ||
                !TryParseFloat(args[start + 1], out y) ||
                !TryParseFloat(args[start + 2], out z))
                return false;

            v = new Vector3(x, y, z);
            return true;
        }

        internal static string Fmt(Vector3 v)
        {
            return "(" + SnapAdjustments.Num(v.x) + ", " + SnapAdjustments.Num(v.y) + ", " + SnapAdjustments.Num(v.z) + ")";
        }

        /// <summary>Returns the prefab's real spelling if it is loaded (names are matched case-insensitively).</summary>
        internal static string ResolvePrefabName(string typed)
        {
            typed = typed.Trim();

            if (ZNetScene.instance != null)
            {
                foreach (GameObject p in ZNetScene.instance.m_prefabs)
                    if (p != null && string.Equals(p.name, typed, StringComparison.OrdinalIgnoreCase))
                        return p.name;
            }

            return typed;
        }

        /// <summary>Every buildable container prefab that is currently loaded (for tab completion).</summary>
        internal static List<string> ContainerPrefabNames()
        {
            if (ZNetScene.instance == null) return new List<string>();

            return ZNetScene.instance.m_prefabs
                .Where(p => p != null && p.GetComponent<Piece>() != null && p.GetComponent<Container>() != null)
                .Select(p => p.name)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Tells the user when an adjustment is saved for a prefab that does not (yet) get snap points.</summary>
        internal static void NoteIfInactive(string name)
        {
            if (!ModSettings.PrefabsOn)
                Print("Note: snap points are switched off right now ([General] EnableAll / [Prefabs] Enable).");
            else if (!ModSettings.OffsetsOn)
                Print("Note: adjustments are switched off right now ([Offsets] Enable); they are saved but not applied.");

            if (ZNetScene.instance != null)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(name);

                if (prefab == null)
                    Print("Note: no prefab named '" + name + "' is loaded right now - check the spelling.");
                else if (ChestSnapPoints.Source(prefab) == null)
                    Print("Note: '" + name + "' has no snap points yet. Run: chestsnap_add " + name);

                return;
            }

            if (!PrefabSets.Included.Contains(name))
                Print("Note: '" + name + "' has no snap points yet. Run: chestsnap_add " + name);
        }
    }

    /// <summary>chestsnap_list [filter] - shows buildable container prefabs and whether they currently get snap points.</summary>
    internal class ListContainersCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_list"; } }

        public override string Help
        {
            get { return "Lists buildable container prefabs and whether they have snap points. Usage: chestsnap_list [name filter]"; }
        }

        public override void Run(string[] args)
        {
            if (ZNetScene.instance == null)
            {
                CommandUtil.Print("Load into a world first (prefabs are not available in the main menu).");
                return;
            }

            string filter = args != null && args.Length > 0 ? args[0] : null;

            var rows = new List<string>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<Piece>() == null) continue;      // only things you can build
                if (prefab.GetComponent<Container>() == null) continue;  // only things that store items

                if (!string.IsNullOrEmpty(filter) &&
                    prefab.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                string source = ChestSnapPoints.Source(prefab);
                string state = source != null ? "snap points (" + source + ")" : "-";
                string note = ChestSnapPoints.IsPlainContainer(prefab) ? "" : "  [ship/cart]";
                string adjusted = SnapAdjustments.Get(prefab.name) != null ? "  [adjusted]" : "";

                rows.Add(prefab.name.PadRight(32) + state + note + adjusted);
            }

            rows.Sort(StringComparer.OrdinalIgnoreCase);

            CommandUtil.Print("Buildable containers (" + rows.Count + "):");
            foreach (string row in rows) CommandUtil.Print("  " + row);
            CommandUtil.Print("Add one with: chestsnap_add <prefab_name>");
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames();
        }
    }

    /// <summary>chestsnap_add &lt;prefab&gt; - appends a prefab to the ExtraPrefabs config entry (saved to the .cfg).</summary>
    internal class AddPrefabCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_add"; } }

        public override string Help
        {
            get { return "Adds a chest/container prefab to the snapping list (saved in the config). Usage: chestsnap_add <prefab_name>"; }
        }

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                CommandUtil.Print("Usage: chestsnap_add <prefab_name>   (see chestsnap_list)");
                return;
            }

            string name = CommandUtil.ResolvePrefabName(args[0]);

            if (PrefabSets.Included.Contains(name))
            {
                CommandUtil.Print("'" + name + "' already gets snap points.");
                return;
            }

            if (ZNetScene.instance != null && ZNetScene.instance.GetPrefab(name) == null)
                CommandUtil.Print("Warning: no prefab named '" + name + "' is currently loaded. Adding it anyway (names are case-insensitive).");

            ModSettings.ExtraPrefabs.Value = PrefabNames.Append(ModSettings.ExtraPrefabs.Value, name);

            CommandUtil.Print("Added '" + name + "'. Existing chests update right away.");
            CommandUtil.Print("Tune where the points sit with chestsnap_show on / chestsnap_grow / chestsnap_shift / chestsnap_point.");
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames()
                .Where(n => !PrefabSets.Included.Contains(n))
                .ToList();
        }
    }

    /// <summary>chestsnap_remove &lt;prefab&gt; - takes a prefab back out of the ExtraPrefabs config entry.</summary>
    internal class RemovePrefabCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_remove"; } }

        public override string Help
        {
            get { return "Removes a prefab you added with chestsnap_add (or typed into ExtraPrefabs). Usage: chestsnap_remove <prefab_name>"; }
        }

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                CommandUtil.Print("Usage: chestsnap_remove <prefab_name>");
                return;
            }

            string name = CommandUtil.ResolvePrefabName(args[0]);

            if (!PrefabSets.ExtraSet.Contains(name))
            {
                if (PrefabSets.Included.Contains(name))
                    CommandUtil.Print("'" + name + "' is a built-in chest. To turn it off, put it in ExcludedPrefabs in the config.");
                else
                    CommandUtil.Print("'" + name + "' is not on the extra list.");
                return;
            }

            ModSettings.ExtraPrefabs.Value = PrefabNames.Remove(ModSettings.ExtraPrefabs.Value, name);

            CommandUtil.Print("Removed '" + name + "'. Existing chests update right away.");
            if (SnapAdjustments.Get(name) != null)
                CommandUtil.Print("Its saved adjustments are kept; clear them with: chestsnap_reset " + name);
        }

        public override List<string> CommandOptionList()
        {
            return PrefabSets.ExtraSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    // ------------------------------------------------------------------------------------------
    // Adjusting where the snap points sit
    // ------------------------------------------------------------------------------------------

    /// <summary>Common code for chestsnap_shift and chestsnap_grow (both take a prefab and three numbers).</summary>
    internal abstract class BoxAdjustCommand : ConsoleCommand
    {
        protected abstract string What { get; }
        protected abstract string Explanation { get; }
        protected abstract void Set(PrefabAdjust adj, Vector3 value);

        public override string Help
        {
            get { return Explanation + " Usage: " + Name + " <prefab_name> <x> <y> <z>   (meters; 0 0 0 clears it)"; }
        }

        public override void Run(string[] args)
        {
            Vector3 v = Vector3.zero;

            if (args == null || args.Length < 4 || !CommandUtil.TryParseVector(args, 1, out v))
            {
                CommandUtil.Print("Usage: " + Name + " <prefab_name> <x> <y> <z>   (meters in the chest's own axes: +X right, +Y up, +Z front)");
                CommandUtil.Print(Explanation);
                return;
            }

            string name = CommandUtil.ResolvePrefabName(args[0]);
            Vector3 value = v;
            SnapAdjustments.Edit(name, a => Set(a, value));

            CommandUtil.Print(name + ": " + What + (value == Vector3.zero ? " cleared." : " = " + CommandUtil.Fmt(value) + "."));
            CommandUtil.NoteIfInactive(name);
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames();
        }
    }

    /// <summary>chestsnap_shift &lt;prefab&gt; x y z - moves all 14 points together.</summary>
    internal class ShiftCommand : BoxAdjustCommand
    {
        public override string Name { get { return "chestsnap_shift"; } }
        protected override string What { get { return "shift"; } }
        protected override string Explanation
        {
            get { return "Moves all snap points of a chest together (use when the chest's box is off-center)."; }
        }
        protected override void Set(PrefabAdjust adj, Vector3 value) { adj.Shift = value; }
    }

    /// <summary>chestsnap_grow &lt;prefab&gt; x y z - makes the snap box bigger/smaller.</summary>
    internal class GrowCommand : BoxAdjustCommand
    {
        public override string Name { get { return "chestsnap_grow"; } }
        protected override string What { get { return "grow"; } }
        protected override string Explanation
        {
            get { return "Adds to the snap box's half-size per axis: 0.05 = 10 cm bigger on that axis, negative shrinks it (use when the collider is oversized)."; }
        }
        protected override void Set(PrefabAdjust adj, Vector3 value) { adj.Grow = value; }
    }

    /// <summary>chestsnap_point &lt;prefab&gt; &lt;point&gt; x y z - offsets a single snap point.</summary>
    internal class PointCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_point"; } }

        public override string Help
        {
            get
            {
                return "Offsets one snap point of a chest. <point> is 1-14 (the Q/E order) or a name like top_front_left. " +
                       "Usage: chestsnap_point <prefab_name> <point> <x> <y> <z>   (meters; 0 0 0 clears it)";
            }
        }

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 5)
            {
                CommandUtil.Print("Usage: chestsnap_point <prefab_name> <point> <x> <y> <z>");
                CommandUtil.Print("<point> is 1-14 (Q/E order) or a name such as top_front_left. See: chestsnap_info <prefab_name>");
                return;
            }

            int index;
            if (!SnapLayout.TryFindPoint(args[1], out index))
            {
                CommandUtil.Print("Unknown point '" + args[1] + "'. Use 1-14 or a name such as top_front_left (see chestsnap_info).");
                return;
            }

            Vector3 value;
            if (!CommandUtil.TryParseVector(args, 2, out value))
            {
                CommandUtil.Print("Could not read x y z as numbers.");
                return;
            }

            string name = CommandUtil.ResolvePrefabName(args[0]);
            int idx = index;
            Vector3 offset = value;

            SnapAdjustments.Edit(name, a =>
            {
                if (offset == Vector3.zero) a.Points.Remove(idx);
                else a.Points[idx] = offset;
            });

            string label = (idx + 1) + " (" + SnapLayout.Points[idx].EnglishName + ")";
            CommandUtil.Print(name + ": point " + label + (offset == Vector3.zero ? " offset cleared." : " offset = " + CommandUtil.Fmt(offset) + "."));
            CommandUtil.NoteIfInactive(name);
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames();
        }
    }

    /// <summary>chestsnap_reset &lt;prefab&gt; [point] - clears adjustments again.</summary>
    internal class ResetCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_reset"; } }

        public override string Help
        {
            get { return "Clears the saved adjustments of a chest, or only those of one point. Usage: chestsnap_reset <prefab_name> [point]"; }
        }

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                CommandUtil.Print("Usage: chestsnap_reset <prefab_name> [point]");
                return;
            }

            string name = CommandUtil.ResolvePrefabName(args[0]);

            if (SnapAdjustments.Get(name) == null)
            {
                CommandUtil.Print("'" + name + "' has no saved adjustments.");
                return;
            }

            if (args.Length >= 2)
            {
                int index;
                if (!SnapLayout.TryFindPoint(args[1], out index))
                {
                    CommandUtil.Print("Unknown point '" + args[1] + "'. Use 1-14 or a name such as top_front_left.");
                    return;
                }

                int idx = index;
                SnapAdjustments.Edit(name, a => a.Points.Remove(idx));
                CommandUtil.Print(name + ": offset of point " + (idx + 1) + " (" + SnapLayout.Points[idx].EnglishName + ") cleared.");
            }
            else
            {
                SnapAdjustments.Edit(name, a =>
                {
                    a.Shift = Vector3.zero;
                    a.Grow = Vector3.zero;
                    a.Points.Clear();
                });
                CommandUtil.Print(name + ": all adjustments cleared.");
            }
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames();
        }
    }

    /// <summary>chestsnap_info &lt;prefab&gt; - prints the measured box, the adjustments and where each point ends up.</summary>
    internal class InfoCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_info"; } }

        public override string Help
        {
            get { return "Shows a chest's measured snap box, its adjustments and the final position of each snap point. Usage: chestsnap_info <prefab_name>"; }
        }

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                CommandUtil.Print("Usage: chestsnap_info <prefab_name>");
                return;
            }

            if (ZNetScene.instance == null)
            {
                CommandUtil.Print("Load into a world first (prefabs are not available in the main menu).");
                return;
            }

            string typed = CommandUtil.ResolvePrefabName(args[0]);
            GameObject prefab = ZNetScene.instance.GetPrefab(typed);
            if (prefab == null)
            {
                CommandUtil.Print("No prefab named '" + typed + "' is loaded. Try chestsnap_list.");
                return;
            }

            string name = prefab.name;
            CommandUtil.Print(name + "  -  snap points: " + (ChestSnapPoints.Source(prefab) ?? "none (not on the list)"));

            Bounds bounds;
            if (!ChestSnapPoints.TryGetLocalBounds(prefab.transform, out bounds))
            {
                CommandUtil.Print("  No colliders or meshes found, so no snap points can be placed.");
                return;
            }

            PrefabAdjust adj = ChestSnapPoints_Adjust(name);

            CommandUtil.Print("  Measured box: center " + CommandUtil.Fmt(bounds.center) + "  size " + CommandUtil.Fmt(bounds.size));
            CommandUtil.Print("  shift " + CommandUtil.Fmt(adj.Shift) + "   grow " + CommandUtil.Fmt(adj.Grow));
            CommandUtil.Print("  Local positions (after adjustments):");

            Vector3[] positions = ChestSnapPoints.ComputePositions(bounds, SnapAdjustments.Active(name));
            for (int i = 0; i < positions.Length; i++)
            {
                string line = "  " + (i + 1) + "  " + SnapLayout.Points[i].EnglishName + "  " + CommandUtil.Fmt(positions[i]);

                Vector3 extra;
                if (adj.Points.TryGetValue(i, out extra))
                    line += "   [point offset " + CommandUtil.Fmt(extra) + "]";

                CommandUtil.Print(line);
            }
        }

        private static PrefabAdjust ChestSnapPoints_Adjust(string name)
        {
            return SnapAdjustments.Get(name) ?? new PrefabAdjust();
        }

        public override List<string> CommandOptionList()
        {
            return CommandUtil.ContainerPrefabNames();
        }
    }

    /// <summary>chestsnap_show [on|off] - draws a sphere on every snap point so adjustments are visible.</summary>
    internal class ShowCommand : ConsoleCommand
    {
        public override string Name { get { return "chestsnap_show"; } }

        public override string Help
        {
            get { return "Shows/hides small spheres on all chest snap points (yellow = corners, cyan = face centers). Usage: chestsnap_show [on|off]"; }
        }

        public override void Run(string[] args)
        {
            bool on = !SnapMarkers.Enabled;

            if (args != null && args.Length > 0)
            {
                string a = args[0].Trim().ToLowerInvariant();
                if (a == "on" || a == "1" || a == "true") on = true;
                else if (a == "off" || a == "0" || a == "false") on = false;
                else
                {
                    CommandUtil.Print("Usage: chestsnap_show [on|off]");
                    return;
                }
            }

            SnapMarkers.SetEnabled(on);
            CommandUtil.Print("Snap point markers " + (on ? "on" : "off") + ".");
        }

        public override List<string> CommandOptionList()
        {
            return new List<string> { "on", "off" };
        }
    }
}
