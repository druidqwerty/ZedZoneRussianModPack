using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.RussianModPack.Modules;

internal static class ConfigDescriptionsModule
{
    private static readonly Dictionary<string, string> EnToRu = new(StringComparer.Ordinal)
    {
        // --- ZED ZONE: Russian Translation Pack ---
        ["auto = enable Russian when game or OS language is Russian (does not affect Chinese); ru = always Russian; off = disable all translations."] =
            "auto = включать русский, если язык игры или ОС русский (не затрагивает китайский); ru = всегда русский; off = отключить все переводы.",
        ["Translate Apocalypse RV | Mobile Survival Base (Workshop 3842730326) if installed."] =
            "Переводить Apocalypse RV | Mobile Survival Base (Workshop 3842730326), если установлен.",
        ["Translate Supply Drop (Workshop 3840679964) if installed."] =
            "Переводить Supply Drop (Workshop 3840679964), если установлен.",
        ["Fix mixed EN/RU vehicle names, ZedLoc language detection, and kit item tooltips in ZedCars (Workshop 3845083252) if installed."] =
            "Исправлять смешанные EN/RU названия машин, определение языка ZedLoc и описания комплектов в ZedCars (Workshop 3845083252).",
        ["Fix mixed EN/RU ZedLoc/WorldLoc sync, quest item names/descriptions, and Director item cache in ZedQuests (Workshop 3845083095) if installed."] =
            "Исправлять синхронизацию ZedLoc/WorldLoc, названия/описания квестовых предметов и кэш Директора в ZedQuests (Workshop 3845083095).",
        ["Translate Mod Menu setting descriptions for supported mods into Russian."] =
            "Переводить описания настроек поддерживаемых модов в меню модов (Mod Menu) на русский.",

        // --- Apocalypse RV (zedzone.mobilebase.rv) ---
        ["Enter the motorhome from its interaction list."] =
            "Клавиша входа в автодом из списка взаимодействий.",
        ["Alternate cabin entry and emergency exit. None disables the key."] =
            "Альтернативный вход в салон и аварийный выход. None — отключить клавишу.",
        ["Provisions one motorhome in the active solo world. None disables the key."] =
            "Выдать один автодом в текущем одиночном мире. None — отключить клавишу.",
        ["Open the upgrade menu at the parked motorhome. None disables the key."] =
            "Открыть меню тюнинга у припаркованного автодома. None — отключить клавишу.",
        ["Open the power, water and climate console at the parked motorhome. None disables the key."] =
            "Открыть консоль питания, воды и климата у припаркованного автодома. None — отключить клавишу.",
        ["Optional world UUID for isolated testing. Empty allows manual provisioning in any solo world."] =
            "Необязательный UUID мира для изолированного теста. Пусто — выдача разрешена в любом одиночном мире.",
        ["Interface language: auto follows the game language; zh or en force one."] =
            "Язык интерфейса: auto следует языку игры; zh или en задают принудительно.",

        // --- Supply Drop (girthy_mods.zedzone.supplydrop) ---
        ["Turn supply drops on or off. The host (or single player) runs the drops; guests with the mod see the helicopter, the message and the guide arrow."] =
            "Включить или выключить сброс грузов. Сбросами управляет хост (или одиночная игра); гости с модом видят вертолёт, сообщение и стрелку.",
        ["Log extra detail in BepInEx\\SupplyDrop.log."] =
            "Записывать подробный журнал в BepInEx\\SupplyDrop.log.",
        ["Used by the mod to update old default values. Leave it alone."] =
            "Служебный параметр мода для обновления старых значений по умолчанию. Не менять.",
        ["Host: send the drop to guests who have the mod (message, helicopter, arrow)."] =
            "Хост: отправлять событие сброса гостям с установленным модом (сообщение, вертолёт, стрелка).",
        ["Show the helicopter flying over and play its rotor sound."] =
            "Показывать пролёт вертолёта и воспроизводить звук винтов.",
        ["Draw the UH-1Y helicopter (built into this mod; no other mod needed). Off = the simple code-drawn helicopter."] =
            "Отрисовывать вертолёт UH-1Y (встроен в мод). Выкл = простая модель вертолёта.",
        ["Rotor sound volume."] =
            "Громкость звука винтов вертолёта.",
        ["Flying speed in world units per second."] =
            "Скорость полёта вертолёта в единицах мира в секунду.",
        ["Size of the helicopter sprite."] =
            "Размер спрайта вертолёта.",
        ["Host: let CallDropKey call a drop right now (handy for testing)."] =
            "Хост: разрешить мгновенный вызов груза по клавише CallDropKey (удобно для проверки).",
        ["Host: key that calls a supply drop right now. Default End: F5 is the game's quick save, F7/F8 are DogShare's give/take-back dog keys (F9/F10 its kennel test build), Insert is Spawn Menu."] =
            "Хост: клавиша мгновенного вызова груза (по умолчанию End).",
        ["The crate lands at a random spot at least this far (world units) from a random player. It's marked on the map."] =
            "Минимальная дистанция сброса ящика от случайного игрока (в единицах мира). Точка отмечается на карте.",
        ["...and at most this far. Very far drops can land in areas nobody has loaded yet."] =
            "Максимальная дистанция сброса. Слишком дальние грузы могут упасть в незагруженные области.",
        ["Random: each crate gets random items from every item that can spawn in the world (a gun with ammo, plus a mix of ammo, gear, food, materials...). Tables: the game's own supply-crate tables (elite/armoury/personal boxes)."] =
            "Random: случайные предметы из всех возможных в мире (оружие с патронами, снаряжение, еда, материалы). Tables: ванильные таблицы ящиков снабжения.",
        ["Random mode: fewest items in a crate."] =
            "Режим Random: минимальное число предметов в ящике.",
        ["Random mode: most items in a crate."] =
            "Режим Random: максимальное число предметов в ящике.",
        ["How good the loot is (1-15). Uses the game's own supply-crate loot tables; higher means better weapons and more of them."] =
            "Качество лута (1–15). Использует таблицы игры: чем выше значение, тем лучше и больше оружия.",
        ["How strong random weapon/armour bonuses on crate loot are (0 = none, 1 = strongest)."] =
            "Сила случайных бонусов на оружии и броне в ящике (0 = без бонусов, 1 = максимум).",
        ["Random mode: which kinds of gun a crate can hold, comma separated. Choices: Pistol, Revolver, SMG, Shotgun, Rifle, Bow, Crossbow, Energy, Liquid, Flare. Leave empty for no guns. The log lists which gun counts as what."] =
            "Режим Random: типы оружия в ящике через запятую (Pistol, Revolver, SMG, Shotgun, Rifle, Bow, Crossbow, Energy, Liquid, Flare). Пусто — без оружия.",
        ["Random mode: allow guns and melee weapons added by other mods (off = only the game's own weapons)."] =
            "Режим Random: разрешить огнестрельное и холодное оружие из других модов (выкл = только ванильное).",
        ["Random mode: allow other non-weapon items added by other mods (ammo, gear, food...)."] =
            "Режим Random: разрешить прочие предметы из других модов (патроны, снаряжение, еда…).",
        ["Let the supply pack itself (green satchel) be disassembled like a backpack once it's empty."] =
            "Разрешить разбирать пустой мешок снабжения (зелёную сумку) как обычный рюкзак.",
        ["Drop one crate per player in the session (so nobody has to fight over it)."] =
            "Сбрасывать по одному ящику на каждого игрока в сессии (чтобы не делить один).",
        ["Most crates in one drop."] =
            "Максимальное число ящиков за один сброс.",
        ["Minutes of play after loading into the world before the first drop."] =
            "Через сколько минут игры после загрузки мира произойдёт первый сброс.",
        ["Average minutes of play between drops (default 90 = about one drop every 1-2 hours with the random spread below)."] =
            "Средний интервал между сбросами в минутах (90 = примерно раз в 1–2 часа с учётом разброса).",
        ["Each wait is IntervalMinutes plus or minus up to this many minutes."] =
            "Случайный разброс ожидания (± указанное число минут от IntervalMinutes).",
        ["Seconds between the 'helicopter inbound' message and the crate dropping."] =
            "Задержка в секундах между предупреждением о вертолёте и падением ящика.",
        ["Show an arrow next to your character that points at the newest crate."] =
            "Показывать рядом с персонажем стрелку к последнему ящику.",
        ["How long the arrow stays (it also goes away when you reach the crate)."] =
            "Сколько минут отображается стрелка (также исчезает при приближении к ящику).",
        ["Put the drop on your map as a waypoint (map pin, minimap, edge-of-screen arrow with distance; teammates see it too). Your old waypoint comes back after you reach the crate."] =
            "Отмечать сброс меткой на карте и миникарте (видна союзникам). Прежняя метка вернётся, когда доберётесь до ящика.",
        ["How long the map marker stays if nobody reaches the crate."] =
            "Сколько минут держится метка на карте, если никто не дошёл до ящика.",
        ["The crate's beacon draws a horde: zombies come out around the drop zone after it lands."] =
            "Маяк ящика привлекает орду: после приземления вокруг зоны сброса появляются зомби.",
        ["How many zombies the beacon draws."] =
            "Сколько зомби привлекает маяк.",
        ["Grow the horde with the in-game day count the same way the game's alarms do."] =
            "Увеличивать размер орды с числом прожитых дней, как у игровых сигнализаций.",
        ["Seconds after the crate lands before the horde starts coming."] =
            "Через сколько секунд после падения ящика начнёт подходить орда.",
        ["How long the beacon keeps calling zombies."] =
            "Сколько секунд маяк продолжает призывать зомби."
    };

    internal static int Count => EnToRu.Count;

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        MethodInfo getter = typeof(ConfigDescription).GetProperty(
            nameof(ConfigDescription.Description),
            BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod();

        return SafeHarmonyPatcher.TryPatch(
            harmony, log, "ModMenu:ConfigDescription.get_Description", getter,
            postfix: typeof(ConfigDescriptionsModule).GetMethod(nameof(GetDescriptionPostfix), BindingFlags.Static | BindingFlags.NonPublic));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GetDescriptionPostfix(ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(__result) || !RussianModPackPlugin.ShouldUseRussian())
                return;

            if (EnToRu.TryGetValue(__result, out string ru))
                __result = ru;
        }
        catch { }
    }
}
