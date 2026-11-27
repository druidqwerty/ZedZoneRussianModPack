# MODLOG — ZED ZONE: Russian Translation Pack

## v1.3.0 (2026-11-27)
- Added full Russian localization for **Spawn Menu (`3839284592`, `girthy_mods.zedzone.spawnmenu` v1.4.3)** via `SoftDependency` (`EnableSpawnMenu` toggle in config):
  - Translates all UI headers, search prompt, amount label/buttons (`Кол-во`, `Стак`), all 9 category tabs (`Все`, `Моды`, `Оружие`, `Патроны`, `Ближний`, `Снаряж.`, `Еда`, `Ресурсы`, `Прочее`) without breaking `Menu.InTab` string switching, empty-list placeholders, item list tags (`стак`, `[мод]`), all 16 `ItemType` category labels (`Catalog.TypeLabel` + `Catalog.NameOf` fallback), footer hints, right-panel buttons, all 5 default kit names, and all status-bar messages (item spawn, magazine fill, dump, reload, kit results, missing items, Helicopter/Tank/rocket errors).
  - Fixes Cyrillic font selection in `SpawnMenu.Fonts.Sample` (original `c >= '\u0530'` skipped Unicode Cyrillic `\u0400–\u04FF`) so `MenuFont = auto` automatically selects a Cyrillic-capable game font (`ZedZoneSans-Regular`) and preloads Cyrillic glyphs across UI sizes.
  - Adds Russian/English layout-aware keyboard input for the search box (`Keyboard.current` + Win32 `GetKeyboardLayout`/`ToUnicodeEx`, supporting all 33 Cyrillic letters including `ё`, `х`, `ъ`, `ж`, `э`, `б`, `ю` as well as `.` and Latin switching, restoring original `In.TypeKeys` whenever `LanguageMode = off`).
  - Added Russian Mod Menu descriptions in `ConfigDescriptionsModule` for all **7** `Spawn Menu` settings plus `EnableSpawnMenu` (**58** total translated Mod Menu descriptions).
- Verified in real graphical `ZEDZONE.exe` (Patch `1.0.23`, isolated save lab, `25/25` checks PASS, `applied=17, failed=0`, visual inspection of 4 PNG screenshots: main RU, Cyrillic search, kit status bar, and `LanguageMode=off` revert), plus headless `SoftDependency` check without `Spawn Menu` (`applied=10, failed=0`) and `Save-Guard` (`8 386/8 386` save files byte-identical).

## v1.2.0 (2026-11-25)
- Added full support for **Apocalypse RV v0.9.4+ (`ZedMobileBase.RvLocale`)** alongside backward compatibility with `v0.7.5–v0.9.2` (`ZedMobileBase.RvText`): hooks `RvLocale.TFor` and `RvLocale.Pick`, covers all `399/399` `RvLocale.Entries` keys (plus legacy keys = `534` entries total), and refreshes static caches lazily when Russian becomes active (even on non-Russian Windows).
- Improved `ConfigDescriptionsModule` to rewrite `<Description>k__BackingField` in-place across loaded BepInEx plugins in addition to patching `ConfigDescription.get_Description`, preventing JIT inlining from bypassing Mod Menu translations.

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
