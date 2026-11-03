using System.IO;
using System.Reflection;
using BepInEx;
using ChestSnapping.Bootstrap;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using KydlCore.Common;

namespace ChestSnapping
{
    /// <summary>
    /// Adds vanilla-style "snappoint" children to chests/containers so they take part in Valheim's
    /// built-in snap system. Q/E (TabLeft/TabRight) cycling and the "Snapping: ..." message are
    /// 100% vanilla (Player.UpdatePlacementGhost) - this mod only supplies the snap points.
    /// Order in Awake matters: log first, then the config, then everything that uses them.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(KydlCore.Bootstrap.Plugin.ModGuid, "0.1.0")]     // the KY hub (manifest: KydlDev-KydlCore-0.1.0, same version)
    // Purely client-side: placed pieces are synced by vanilla, snap points are local helpers. A player without the mod can join;
    // when both sides have it the version must match.
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    public class Plugin : BaseUnityPlugin
    {
        // ModGuid never changes (it is the config file name: BepInEx/config/com.kydldev.ChestSnapping.cfg).
        // ModVersion = manifest.json version_number = newest CHANGELOG heading (tests check it).
        public const string ModGuid = "com.kydldev.ChestSnapping";
        public const string ModName = "ChestSnapping";
        public const string ModVersion = "2.0.0";

        private Harmony _harmony;
        private ConfigReloader _cfgWatcher;
        private ModInstaller _installer;

        // Config events can fire off the main thread; Unity API must only be touched on the main thread.
        private volatile bool _refreshQueued;

        private void Awake()
        {
            ModLog.Init(Logger, ModName, () => ModSettings.DebugLog != null && ModSettings.DebugLog.Value);
            ModLog.Info(ModName + " " + ModVersion + " loaded (dll built " + File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToString("yyyy-MM-dd HH:mm:ss") + ")");

            ModSettings.Bind(Config);
            _cfgWatcher = new ConfigReloader(Config);
            PrefabSets.Rebuild();
            SnapLocalization.Register();

            _installer = new ModInstaller(Config);
            bool installed = _installer.Install();
            Config.SettingChanged += (s, e) => _refreshQueued = true;
            ModLog.Verbose("Plugin: ModInstaller " + (installed ? "installed" : "reported a problem"));

            CommandManager.Instance.AddConsoleCommand(new ListContainersCommand());
            CommandManager.Instance.AddConsoleCommand(new AddPrefabCommand());
            CommandManager.Instance.AddConsoleCommand(new RemovePrefabCommand());
            CommandManager.Instance.AddConsoleCommand(new ShiftCommand());
            CommandManager.Instance.AddConsoleCommand(new GrowCommand());
            CommandManager.Instance.AddConsoleCommand(new PointCommand());
            CommandManager.Instance.AddConsoleCommand(new ResetCommand());
            CommandManager.Instance.AddConsoleCommand(new InfoCommand());
            CommandManager.Instance.AddConsoleCommand(new ShowCommand());

            _harmony = new Harmony(ModGuid);
            bool patched = PatchInstaller.Apply(_harmony, Assembly.GetExecutingAssembly());
            ModLog.Info(ModName + " ready" + (patched ? "" : " (patching failed, see the log)"));
        }

        private void Update()
        {
            if (_cfgWatcher != null) _cfgWatcher.Tick();
            if (!_refreshQueued) return;
            _refreshQueued = false;

            if (_installer != null) _installer.Refresh();
            PrefabSets.Rebuild();
            ChestSnapPoints.RefreshAll();
        }

        private void OnDestroy()
        {
            if (_cfgWatcher != null) { _cfgWatcher.Dispose(); _cfgWatcher = null; }
            if (_installer != null) { _installer.Uninstall(); _installer = null; }
            if (_harmony != null) _harmony.UnpatchSelf();
            ModLog.Verbose("Plugin: destroyed (patches removed)");
        }
    }
}
