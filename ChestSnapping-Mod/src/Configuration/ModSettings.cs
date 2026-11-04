using BepInEx.Configuration;

namespace ChestSnapping;

/// <summary>
/// Player config (BepInEx entries, bound once from Plugin.Awake). Everything here is something a player may change.
/// Two functionalities, each with its own switch: [Prefabs] (which chests get snap points) and [Offsets] (where the points sit).
/// </summary>
internal static class ModSettings
{
    // [General]
    public static ConfigEntry<bool> EnableAll;

    // [Prefabs]
    public static ConfigEntry<bool> PrefabsEnable;
    public static ConfigEntry<string> ExtraPrefabs;
    public static ConfigEntry<string> ExcludedPrefabs;
    public static ConfigEntry<bool> AutoDetectContainers;

    // [Offsets]
    public static ConfigEntry<bool> OffsetsEnable;
    public static ConfigEntry<string> Adjustments;

    // [DebugLog]
    public static ConfigEntry<bool> DebugLog;

    /// <summary>Is the mod switched on ([General] EnableAll)? Off means inert: no snap points on any chest.</summary>
    public static bool On { get { return EnableAll == null || EnableAll.Value; } }

    /// <summary>Do chests get snap points? EnableAll and [Prefabs] Enable.</summary>
    public static bool PrefabsOn { get { return Functions.IsOn(On, PrefabsEnable == null || PrefabsEnable.Value); } }

    /// <summary>Are the saved point adjustments applied? EnableAll and [Offsets] Enable.</summary>
    public static bool OffsetsOn { get { return Functions.IsOn(On, OffsetsEnable == null || OffsetsEnable.Value); } }

    /// <summary>Binds every entry. Called once, first thing after the log is ready.</summary>
    /// <param name="config">The plugin's config file.</param>
    internal static void Bind(ConfigFile config)
    {
        EnableAll = config.Bind("General", "EnableAll", true,
            "Switches the whole mod on or off while playing. Off removes the snap points from every chest.");

        PrefabsEnable = config.Bind("Prefabs", "Enable", true,
            "Give chests snap points so they take part in Valheim's snap system (Q / E while placing). Off removes the snap points.");

        ExtraPrefabs = config.Bind("Prefabs", "ExtraPrefabs", "",
            "Comma-separated prefab names of additional chests/containers that should get snap points " +
            "(modded chests, or vanilla chests added in a future update). " +
            "Example: piece_chest_custom, MyMod_BigCrate. " +
            "Run the console command 'chestsnap_list' to see candidate prefab names, " +
            "or 'chestsnap_add <prefab>' to add one without editing this file.");

        ExcludedPrefabs = config.Bind("Prefabs", "ExcludedPrefabs", "",
            "Comma-separated prefab names that should NOT get snap points, even if they are in the built-in list " +
            "or matched by AutoDetectContainers.");

        AutoDetectContainers = config.Bind("Prefabs", "AutoDetectContainers", false,
            "If true, every buildable piece that has a Container component on its root object (and is not a ship or cart) " +
            "automatically gets snap points. Handy for modded chests, but may also catch things you do not " +
            "consider a chest - use ExcludedPrefabs to opt those out.");

        OffsetsEnable = config.Bind("Offsets", "Enable", true,
            "Apply the saved per-prefab adjustments (PrefabAdjustments) to where the snap points sit. Off uses the measured chest box as it is.");

        Adjustments = config.Bind("Offsets", "PrefabAdjustments", "",
            "Per-prefab tweaks to where the snap points sit. Usually changed with the console commands " +
            "chestsnap_shift / chestsnap_grow / chestsnap_point / chestsnap_reset (use chestsnap_show on to see the points), " +
            "but you can also edit it by hand. " +
            "Format: entries separated by ';', each 'prefab: shift=x,y,z grow=x,y,z p5=x,y,z'. " +
            "Distances are in meters along the chest's own axes (+X right, +Y up, +Z front), '.' is always the decimal separator. " +
            "shift moves all points together. grow is added to the half-size of the snap box per axis " +
            "(0.05 = 10 cm bigger on that axis, negative = smaller). " +
            "pN adds an offset to a single point, N = 1-14 in Q/E cycling order. " +
            "Example: MyMod_Crate: shift=0,0.05,0 grow=-0.02,0,-0.02 p5=0,0.1,0");

        DebugLog = config.Bind("DebugLog", "Enable", false,
            "Writes detailed lines to the BepInEx log (for bug reports). Leave off for normal play.");
    }
}
