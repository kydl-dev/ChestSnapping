# Changelog

## 2.0.0
- Tested in game with the full TestLab suite (6 tests): 3 green runs in a row on 2026-11-05, plus the ecosystem run with all 12 KydlDev mods loaded together.
- Breaking: ChestSnapping now needs the KydlCore mod (hard dependency). Install KydlCore as well.
- Ported to the KydlDev layout (feature switches, KY hub settings tab, TestLab suites). The snap points and console commands work as before. Checked in game on 2026-11-04 (clean start and quit with all mods loaded, no errors from this mod).
- New config switches: `[General] EnableAll`, `[Prefabs] Enable` (snap points on chests) and `[Offsets] Enable` (apply the saved `PrefabAdjustments`). Switching them changes the chests that are already built, no restart needed.
- The settings are a tab in the KY hub window of KydlCore (F6).
- `[DebugLog] Enable` writes detailed lines to the BepInEx log.
- Still client-side: the network check is `VersionCheckOnly` with exact version match when both sides have the mod (a player without it can still join).

## 1.0.0

First release.

- Chests get 14 snap points that work with Valheim's built-in snap system: 4 bottom corners, 4 top corners and 6 face centers. Press **Q / E** while placing to cycle through them; the vanilla "Snapping: ..." message shows the active one.
- Built-in support for the wood chest, reinforced chest, private chest and blackmetal chest.
- Snap points are placed on the chest's real collider bounds, so chests snap flush against each other.
- **Modded chests:** add any prefab name with the `ExtraPrefabs` config entry or the `chestsnap_add` console command (`chestsnap_remove` takes it out again, `chestsnap_list` shows candidates). `ExcludedPrefabs` opts prefabs out, and `AutoDetectContainers` can give every buildable container snap points automatically.
- **Adjustable snap points:** per-prefab `shift`, `grow` and single-point offsets, saved in the `PrefabAdjustments` config entry. Console commands: `chestsnap_shift`, `chestsnap_grow`, `chestsnap_point`, `chestsnap_reset`, `chestsnap_info`.
- `chestsnap_show` draws a sphere on every snap point so you can see what an adjustment does.
- Changes to the config or the console commands apply to chests that are already built right away, no restart needed.
- Client-side only: other players and servers do not need the mod.
- Config file: `BepInEx/config/com.kydldev.ChestSnapping.cfg`.
