# ChestSnapping - test coverage

**Automated** = covered by a test that ran green; **Manual** = needs a person; **Gap** = not covered. Never mark an untested behaviour as covered.

| Feature / requirement | Unit | Integration | TestLab | Manual | Status |
|---|---|---|---|---|---|
| Loads with exact KydlCore, no errors | - | ManifestTests, ArchitectureRulesTests | selftest | - | Automated |
| Prefab-name lists (parse / add / remove) | PrefabNamesTests | - | - | - | Automated |
| Feature rule EnableAll AND own switch | FunctionsTests | - | feature-toggle | - | Automated |
| KY hub Settings tab, open / close | - | - | hub-page | hub-look | Automated + Manual |
| Switches live (EnableAll, Prefabs, Offsets) | - | - | feature-toggle | - | Automated |
| 14 snap points on the built-in chests (ghost) | - | - | snap-points | snap-feel | Automated + Manual |
| ExcludedPrefabs live | - | - | excluded-prefab | - | Automated |
| Saved shift moves the points; Offsets off ignores it | - | - | adjustment-applies | - | Automated |
| Config layout, patch target, suite rules | - | SettingsTests, PatchTargetsTests, SuiteRulesTests | - | - | Automated |
| Q / E cycling, flush snapping to another chest | | | | snap-feel | Gap |
| `grow` and single-point offsets, `chestsnap_info` output | | | | in-game | Gap |
| `chestsnap_add / remove / list` commands | | | | in-game | Gap |
| `chestsnap_show` markers | | | | snap-feel | Gap |
| ExtraPrefabs / AutoDetectContainers with a modded chest | | | | in-game | Gap |
| Placed (not ghost) chests in a loaded world | | | | in-game | Gap |