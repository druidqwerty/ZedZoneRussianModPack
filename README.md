# ZED ZONE: Russian Translation Pack (`v1.3.0`)

**Specially made by request for user: `6VaN4!K96`**

[![Support development](docs/banner.png)](https://boosty.to/lrgpp)

Free and open source. The code is written by an AI; the direction, the testing and the time are mine. If the mod made your game better: [support on Boosty](https://boosty.to/lrgpp).

All-in-one Russian translation and localization fix pack for five popular community mods in **ZED ZONE** (BepInEx 6 IL2CPP). Automatically detects which supported mods are installed and activates only the needed modules — works with any combination of 1 to 5 mods.

## Included Mods

1. **Apocalypse RV | Mobile Survival Base** (`3842730326`, `v0.9.4+` & `v0.7.5–v0.9.2`) — complete Russian translation of all `399/399` `RvLocale` keys + legacy `RvText` keys (`534` entries total: RV-01 terminal, drive modes, Portal Summoner, upgrades, utilities, power/water/climate, roof turret, prompts & errors), Cyrillic font glyph preloading, and Mod Menu setting descriptions.
2. **Supply Drop** (`3840679964`, `v1.3.0`) — Russian translation of all on-screen helicopter & crate notifications, all 8 compass directions, distances, and all 37 Mod Menu setting descriptions.
3. **ZedCars: Apocalypse Garage** (`3845083252`, `v0.5.0`) — fixes mixed EN/RU localization: vehicle names that fell back to `GameLanguage.English`, `ZedLoc` language detection on startup / fan translations, and full Russian item names/descriptions on kit items.
4. **ZedQuests: Living World** (`3845083095`, `v0.5.0`) — fixes mixed EN/RU localization: synchronizes `ZedLoc` and `WorldLoc` to Russian, preserves native Russian names/descriptions on quest items, and clears `DirectorItems` name cache on language changes.
5. **Spawn Menu — item spawner and kits for testing** (`3839284592`, `v1.4.3`) — full Russian translation of the entire UI (header, search, amounts, all 9 tabs, all 16 item category labels, all 5 default kits, action buttons, footer hints, and all status messages), Cyrillic font selection & glyph preloading (`ZedZoneSans-Regular`), layout-aware Russian/English search typing (all 33 Cyrillic letters + Latin), and all 7 Mod Menu setting descriptions.

## Requirements & Installation

- Requires third-party **ZED ZONE Code Mod Loader** (Workshop `3834614258`, not a mod by druidqwerty).
- Subscribe via Steam Workshop or extract `RussianModPack/` into `ZED ZONE/Mods/`.
- Works with any subset of the 5 supported mods (all target dependencies are `SoftDependency`).

## Configuration

`BepInEx/config/com.druid.zedzone.russianmodpack.cfg`:
- `LanguageMode` (`auto` / `ru` / `off`, default `auto`): automatically activates Russian when the game or OS language is Russian (does not affect Chinese).
- `EnableApocalypseRV`, `EnableSupplyDrop`, `EnableZedCarsFix`, `EnableZedQuestsFix`, `EnableSpawnMenu`, `EnableModMenuDescriptions` (`true` / `false`): toggle individual modules independently.

## Verification

- Built for `net6.0` (`0` warnings, `0` errors), `Assembly-CSharp` reference normalized to `0.0.0.0`.
- Verified in real graphical `ZEDZONE.exe` (Patch `1.0.23`) with Spawn Menu + base mods (`25/25` checks PASS, `Harmony applied=17, failed=0`, visual inspection of RU UI, Cyrillic search, kit status bar, and `LanguageMode=off` revert), headless `SoftDependency` check without Spawn Menu (`applied=10, failed=0`), and `0` save file modifications (`8 386/8 386` files byte-identical).

---

© 2026 druidqwerty. MIT License. Free and open source; code written by AI, direction and testing by druid.
