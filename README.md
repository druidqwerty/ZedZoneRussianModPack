# ZED ZONE: Russian Translation Pack (`v1.0.0`)

**Specially made by request for user: `6VaN4!K96`**

[![Support development](docs/banner.png)](https://boosty.to/lrgpp)

All-in-one Russian translation and localization fix pack for popular community mods in **ZED ZONE** (BepInEx 6 IL2CPP). Automatically detects which supported mods are installed and activates only the needed modules — works with any combination of 1 to 4 mods.

## Included Mods

1. **Apocalypse RV | Mobile Survival Base** (`3842730326`, `v0.7.5`) — complete Russian translation of all **286 / 286** interface strings (dashboard, upgrades, utilities, power/water/climate, roof turret, prompts and error notices) + Cyrillic font glyph preloading.
2. **Supply Drop** (`3840679964`, `v1.3.0`) — Russian translation of all on-screen helicopter & crate notifications, all 8 compass directions, and distances.
3. **ZedCars: Apocalypse Garage** (`3845083252`, `v0.5.0`) — fixes mixed EN/RU localization: vehicle names that fell back to `GameLanguage.English`, `ZedLoc` language detection on startup / fan translations, and full Russian item names/descriptions on kit items.
4. **ZedQuests: Living World** (`3845083095`, `v0.5.0`) — fixes mixed EN/RU localization: synchronizes `ZedLoc` and `WorldLoc` to Russian, preserves native Russian names/descriptions on quest items, and clears `DirectorItems` name cache on language changes.

## Requirements & Installation

- Requires third-party **ZED ZONE Code Mod Loader** (Workshop `3834614258`, not a mod by druidqwerty).
- Subscribe via Steam Workshop or extract `RussianModPack/` into `ZED ZONE/Mods/`.
- Works with any subset of the 4 supported mods (all target dependencies are `SoftDependency`).

## Configuration

`BepInEx/config/com.druid.zedzone.russianmodpack.cfg`:
- `LanguageMode` (`auto` / `ru` / `off`, default `auto`): automatically activates Russian when the game or OS language is Russian (does not affect Chinese).
- `EnableApocalypseRV`, `EnableSupplyDrop`, `EnableZedCarsFix`, `EnableZedQuestsFix` (`true` / `false`): toggle individual modules independently.

## Verification

- Built for `net6.0` (`0` warnings, `0` errors), `Assembly-CSharp` reference normalized to `0.0.0.0`.
- Verified in headless `ZEDZONE.exe` with all 4 base mods loaded (`active=[ApocalypseRV(286), SupplyDrop, ZedCarsFix, ZedQuestsFix]`, `Harmony applied=9, failed=0`, `0` save file modifications).

---

© 2026 druidqwerty. MIT License. Free and open source; AI-assisted code, direction and testing by druid.
