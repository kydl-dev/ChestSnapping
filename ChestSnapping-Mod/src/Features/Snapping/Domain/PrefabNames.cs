using System;
using System.Collections.Generic;

namespace ChestSnapping
{
    /// <summary>Pure helpers for the prefab-name lists of the config (comma separated, case-insensitive). No game types.</summary>
    internal static class PrefabNames
    {
        private const string CloneSuffix = "(Clone)";

        /// <summary>Splits a comma / semicolon separated list into a case-insensitive set; blanks are dropped.</summary>
        /// <param name="csv">The config text, may be null or empty.</param>
        /// <returns>The trimmed, distinct names (never null).</returns>
        internal static HashSet<string> Parse(string csv)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(csv))
            {
                return set;
            }

            foreach (string part in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (name.Length > 0)
                {
                    set.Add(name);
                }
            }

            return set;
        }

        /// <summary>The prefab name of a GameObject name: "piece_chest_wood(Clone)" becomes "piece_chest_wood".</summary>
        /// <param name="objectName">The GameObject name.</param>
        /// <returns>The name without the "(Clone)" suffix.</returns>
        internal static string StripClone(string objectName)
        {
            if (objectName == null)
            {
                return string.Empty;
            }

            return objectName.EndsWith(CloneSuffix, StringComparison.Ordinal)
                ? objectName.Substring(0, objectName.Length - CloneSuffix.Length)
                : objectName;
        }

        /// <summary>Appends a name to a list text ("a, b" + "c" gives "a, b, c").</summary>
        /// <param name="csv">The current list text.</param>
        /// <param name="name">The name to add.</param>
        /// <returns>The new list text.</returns>
        internal static string Append(string csv, string name)
        {
            return string.IsNullOrWhiteSpace(csv) ? name : csv.TrimEnd(',', ' ') + ", " + name;
        }

        /// <summary>Removes a name (case-insensitive) from a list text.</summary>
        /// <param name="csv">The current list text.</param>
        /// <param name="name">The name to remove.</param>
        /// <returns>The new list text, names joined with ", ".</returns>
        internal static string Remove(string csv, string name)
        {
            var kept = new List<string>();
            if (!string.IsNullOrEmpty(csv))
            {
                foreach (string part in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string p = part.Trim();
                    if (p.Length > 0 && !string.Equals(p, name, StringComparison.OrdinalIgnoreCase))
                    {
                        kept.Add(p);
                    }
                }
            }

            return string.Join(", ", kept.ToArray());
        }
    }
}
