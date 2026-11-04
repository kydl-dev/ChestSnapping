using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ChestSnapping;

internal struct SnapDef
{
    /// <summary>Localization key without the leading '$'.</summary>
    public string TokenKey;
    public string EnglishName;
    /// <summary>-1 / 0 / +1 per axis, multiplied with the bounds' half-size and added to the bounds' center.</summary>
    public Vector3 Factor;

    /// <summary>GameObject name. The leading '$' makes the vanilla message translate it.</summary>
    public string ObjectName { get { return "$" + TokenKey; } }
}

/// <summary>
/// The 14 snap points, in the order Q/E cycles through them (vanilla starts at "auto", then goes
/// 1, 2, 3 ... and wraps back to auto):
///   4 bottom corners, 4 top corners, then Bottom / Top / Back / Front / Left / Right centers.
///
/// Orientation is from the chest's own point of view: Front = +Z (transform.forward),
/// Right = +X, Left = -X. If a modded chest was modelled facing the other way the Front/Back (and
/// Left/Right) names are simply swapped - every point still exists.
/// </summary>
internal static class SnapLayout
{
    public static readonly SnapDef[] Points = Build();

    private static SnapDef[] Build()
    {
        var list = new List<SnapDef>();

        // Corners: bottom first, then top. Within a level: front-left, front-right, back-left, back-right.
        foreach (float y in new[] { -1f, 1f })
        {
            foreach (float z in new[] { 1f, -1f })
            {
                foreach (float x in new[] { -1f, 1f })
                {
                    string level = y < 0 ? "Bottom" : "Top";
                    string depth = z > 0 ? "Front" : "Back";
                    string side = x < 0 ? "Left" : "Right";
                    list.Add(Make(level + " " + depth + " " + side, new Vector3(x, y, z)));
                }
            }
        }

        // Face centers.
        list.Add(Make("Bottom Center", new Vector3(0, -1, 0)));
        list.Add(Make("Top Center", new Vector3(0, 1, 0)));
        list.Add(Make("Back Center", new Vector3(0, 0, -1)));
        list.Add(Make("Front Center", new Vector3(0, 0, 1)));
        list.Add(Make("Left Center", new Vector3(-1, 0, 0)));
        list.Add(Make("Right Center", new Vector3(1, 0, 0)));

        return list.ToArray();
    }

    private static SnapDef Make(string english, Vector3 factor)
    {
        return new SnapDef
        {
            EnglishName = english,
            TokenKey = "chestsnap_" + english.ToLowerInvariant().Replace(' ', '_'),
            Factor = factor
        };
    }

    /// <summary>Index into <see cref="Points"/> for a GameObject name like "$chestsnap_top_center", or -1.</summary>
    public static int IndexOfObjectName(string objectName)
    {
        for (int i = 0; i < Points.Length; i++)
            if (string.Equals(Points[i].ObjectName, objectName, StringComparison.Ordinal))
                return i;
        return -1;
    }

    /// <summary>
    /// Resolves "5", "p5", "top_front_left" or "Top Front Left" (case-insensitive) to an index into
    /// <see cref="Points"/>. Numbers are 1-based, matching the Q/E cycling order.
    /// </summary>
    public static bool TryFindPoint(string key, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(key)) return false;

        string k = key.Trim();

        string digits = (k.Length > 1 && (k[0] == 'p' || k[0] == 'P')) ? k.Substring(1) : k;
        int number;
        if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            if (number < 1 || number > Points.Length) return false;
            index = number - 1;
            return true;
        }

        string normalized = k.Replace(' ', '_').Replace('-', '_');
        for (int i = 0; i < Points.Length; i++)
        {
            if (string.Equals(Points[i].EnglishName.Replace(' ', '_'), normalized, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                return true;
            }
        }

        return false;
    }
}
