# MODLOG — ZED ZONE: Russian Translation Pack

## v1.1.0 (2026-11-25)
- Updated **Apocalypse RV** translation dictionary for **v0.9.2** (`423/423` current keys + legacy v0.7.5 keys = `505` entries total): full coverage of the new RV-01 terminal (`RvDashboard`), drive modes (`EV` / `Hybrid` / `Petrol`), `Portal Summoner` sidearm, and all workstation upgrade pages.
- Added static-cache refresh (`RvUpgrades.Catalog` & `RvUpgradeMenu.Groups`) in `ApocalypseRvModule.TryInstall` so upgrade names and categories are translated to Russian even if `ZedMobileBase` initializes before `RussianModPack`.
- Added `RvDashboard.Preview` constructor patch to translate the unwrapped Chinese overview hint in `RvUpgradeMenu.RenderOverview`.
- Added `ConfigDescriptionsModule` (`BepInEx.Configuration.ConfigDescription.get_Description` postfix) translating **49** Mod Menu setting descriptions into Russian (`Supply Drop`: 37, `Apocalypse RV`: 7, `Russian Translation Pack`: 5).

## v1.0.0 (2026-11-25)
- Specially made by request for `6VaN4!K96`.
- Unified all-in-one BepInEx 6 IL2CPP Harmony translation & localization fix pack (`com.druid.zedzone.russianmodpack`) using `SoftDependency` on all four target mods (automatically activates only for installed mods):
  1. **Apocalypse RV | Mobile Survival Base (`3842730326`)** — full Russian translation of all `286/286` `RvText.T/F` strings + Cyrillic glyph preloading via `DrivingPanel.GetUIFont()`.
  2. **Supply Drop (`3840679964`)** — Russian translation of all 5 `Drops.Notify` on-screen notifications, 8 compass directions, and distances.
  3. **ZedCars: Apocalypse Garage (`3845083252`)** — fixes mixed EN/RU vehicle names caused by `GameLanguage.English` lookup in `CarDefinition.DisplayName`, synchronizes `ZedCars.ZedLoc.L.ReadGameCode()` to `ru_RU`, and stamps `itemName`/`itemDescription` in `CarKitItems.Stamp`.
  4. **ZedQuests: Living World (`3845083095`)** — synchronizes `ZedQuests.ZedLoc.L.ReadGameCode()` and `WorldLoc` to `ru_RU`, preserves native Russian `itemName`/`itemDescription` in `ZedQuestsItemIds.RestoreText`, and clears `DirectorItems.Names` cache on `L.Changed`.
- Added per-module toggles and `LanguageMode = auto / ru / off` in `BepInEx/config/com.druid.zedzone.russianmodpack.cfg`.
- Normalized `Assembly-CSharp` reference to `0.0.0.0`.
- Verified in headless `ZEDZONE.exe` with all 4 base mods loaded (`applied=9, failed=0`, `0` save file modifications in `Save-Guard`).
