using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using ZedZone.RussianModPack.Modules;

namespace ZedZone.RussianModPack;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("zedzone.mobilebase.rv", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("girthy_mods.zedzone.supplydrop", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("zedcars.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("zedquests.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("girthy_mods.zedzone.spawnmenu", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class RussianModPackPlugin : BasePlugin
{
    public const string PluginGuid = "com.druid.zedzone.russianmodpack";
    public const string PluginName = "ZED ZONE: Russian Translation Pack";
    public const string PluginVersion = "1.3.0";

    internal static ManualLogSource LogSource;
    internal static Harmony HarmonyInstance;

    internal static ConfigEntry<string> LanguageMode;
    internal static ConfigEntry<bool> EnableApocalypseRV;
    internal static ConfigEntry<bool> EnableSupplyDrop;
    internal static ConfigEntry<bool> EnableZedCarsFix;
    internal static ConfigEntry<bool> EnableZedQuestsFix;
    internal static ConfigEntry<bool> EnableSpawnMenu;
    internal static ConfigEntry<bool> EnableModMenuDescriptions;

    private static int _langCacheFrame = -1;
    private static bool _langCacheRu = true;

    public override void Load()
    {
        LogSource = Log;

        LanguageMode = Config.Bind(
            "General",
            "LanguageMode",
            "auto",
            "auto = enable Russian when game or OS language is Russian (does not affect Chinese); ru = always Russian; off = disable all translations.");
        EnableApocalypseRV = Config.Bind(
            "Modules",
            "EnableApocalypseRV",
            true,
            "Translate Apocalypse RV | Mobile Survival Base (Workshop 3842730326) if installed.");
        EnableSupplyDrop = Config.Bind(
            "Modules",
            "EnableSupplyDrop",
            true,
            "Translate Supply Drop (Workshop 3840679964) if installed.");
        EnableZedCarsFix = Config.Bind(
            "Modules",
            "EnableZedCarsFix",
            true,
            "Fix mixed EN/RU vehicle names, ZedLoc language detection, and kit item tooltips in ZedCars (Workshop 3845083252) if installed.");
        EnableZedQuestsFix = Config.Bind(
            "Modules",
            "EnableZedQuestsFix",
            true,
            "Fix mixed EN/RU ZedLoc/WorldLoc sync, quest item names/descriptions, and Director item cache in ZedQuests (Workshop 3845083095) if installed.");
        EnableSpawnMenu = Config.Bind(
            "Modules",
            "EnableSpawnMenu",
            true,
            "Translate Spawn Menu (Workshop 3839284592) UI, categories, kit names, status messages, and Cyrillic search if installed.");
        EnableModMenuDescriptions = Config.Bind(
            "Modules",
            "EnableModMenuDescriptions",
            true,
            "Translate Mod Menu setting descriptions for supported mods into Russian.");

        SafeHarmonyPatcher.ResetCounters();
        HarmonyInstance = new Harmony(PluginGuid);

        var activeModules = new List<string>();

        if (EnableApocalypseRV.Value && ApocalypseRvModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add($"ApocalypseRV({RvRussianDictionary.Count})");

        if (EnableSupplyDrop.Value && SupplyDropModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add("SupplyDrop");

        if (EnableZedCarsFix.Value && ZedCarsFixModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add("ZedCarsFix");

        if (EnableZedQuestsFix.Value && ZedQuestsFixModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add("ZedQuestsFix");

        if (EnableSpawnMenu.Value && SpawnMenuModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add("SpawnMenu");

        if (EnableModMenuDescriptions.Value && ConfigDescriptionsModule.TryInstall(HarmonyInstance, LogSource))
            activeModules.Add($"ModMenuDesc({ConfigDescriptionsModule.Count})");

        string list = activeModules.Count > 0 ? string.Join(", ", activeModules) : "none (no supported target mods detected)";
        LogSource.LogInfo(
            $"{PluginName} v{PluginVersion} loaded: mode={LanguageMode.Value}, active=[{list}], " +
            $"Harmony applied={SafeHarmonyPatcher.AppliedCount}, failed={SafeHarmonyPatcher.FailedCount}.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool ShouldUseRussian()
    {
        int frame = Time.frameCount;
        if (frame == _langCacheFrame)
            return _langCacheRu;

        _langCacheFrame = frame;
        _langCacheRu = DetectRussian();
        return _langCacheRu;
    }

    private static bool DetectRussian()
    {
        string mode = (LanguageMode?.Value ?? "auto").Trim().ToLowerInvariant();
        if (mode == "off" || mode == "disabled")
            return false;
        if (mode == "ru" || mode == "russian" || mode == "force")
            return true;

        try
        {
            var gc = GameController.instance;
            string code = gc?.gameSettings?.languageCode;
            if (!string.IsNullOrWhiteSpace(code))
            {
                string low = code.Trim().ToLowerInvariant();
                if (low.StartsWith("ru") || low.Contains("russian"))
                    return true;
                if (low.StartsWith("zh") || low == "cn" || low.Contains("chinese"))
                    return false;
            }
        }
        catch { }

        try
        {
            if (Application.systemLanguage == SystemLanguage.Russian)
                return true;
        }
        catch { }

        return false;
    }
}
