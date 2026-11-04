using HarmonyLib;

namespace ChestSnapping;

/// <summary>
/// Piece.Awake runs for placed pieces AND for the placement ghost, so both the thing you are
/// holding and the things already standing in the world get snap points. Doing it per instance
/// (instead of editing prefabs at startup) means modded chests registered late still work.
/// </summary>
[HarmonyPatch(typeof(Piece), "Awake")]
internal static class PieceAwakePatch
{
    private static void Postfix(Piece __instance)
    {
        ChestSnapPoints.Apply(__instance);
    }
}
