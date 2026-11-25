using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.RussianModPack.Modules;

internal static class ZedQuestsFixModule
{
    private static FieldInfo _directorNamesField;

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        Assembly zqAsm = FindZedQuestsAssembly();
        if (zqAsm == null)
            return false;

        Type zedLocType = zqAsm.GetType("ZedLoc.L", throwOnError: false);
        MethodInfo readGameCode = zedLocType?.GetMethod(
            "ReadGameCode",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        SafeHarmonyPatcher.TryPatch(
            harmony, log, "ZedQuests:ZedLoc.L.ReadGameCode", readGameCode,
            postfix: typeof(ZedQuestsFixModule).GetMethod(nameof(ReadGameCodePostfix), BindingFlags.Static | BindingFlags.NonPublic));

        Type itemIdsType = zqAsm.GetType("ZedQuests.ZedQuestsItemIds", throwOnError: false);
        MethodInfo restoreText = itemIdsType?.GetMethod(
            "RestoreText",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        SafeHarmonyPatcher.TryPatch(
            harmony, log, "ZedQuests:ZedQuestsItemIds.RestoreText", restoreText,
            postfix: typeof(ZedQuestsFixModule).GetMethod(nameof(RestoreTextPostfix), BindingFlags.Static | BindingFlags.NonPublic));

        Type dirItemsType = zqAsm.GetType("ZedQuests.DirectorItems", throwOnError: false);
        _directorNamesField = dirItemsType?.GetField("Names", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        try
        {
            EventInfo changedEvt = zedLocType?.GetEvent("Changed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            changedEvt?.AddEventHandler(null, new Action(OnZedLocChanged));
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ZedQuestsFix] Subscribing to L.Changed failed: {ex.Message}");
        }

        try
        {
            if (RussianModPackPlugin.ShouldUseRussian() && zedLocType != null)
            {
                MethodInfo applyLang = zedLocType.GetMethod(
                    "ApplyLanguage",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), typeof(string), typeof(bool) },
                    null);
                applyLang?.Invoke(null, new object[] { "ru", "RussianModPack", false });
            }
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ZedQuestsFix] Initial ApplyLanguage failed: {ex.Message}");
        }

        return true;
    }

    private static Assembly FindZedQuestsAssembly()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.GetType("ZedQuests.ZedQuestsItemIds", throwOnError: false) != null)
                    return asm;
            }
            catch { }
        }
        return null;
    }

    private static void OnZedLocChanged()
    {
        try
        {
            if (_directorNamesField?.GetValue(null) is IDictionary dict)
                dict.Clear();
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReadGameCodePostfix(ref string __result)
    {
        try
        {
            if (!RussianModPackPlugin.ShouldUseRussian())
                return;

            string low = (__result ?? "").Trim().ToLowerInvariant();
            if (!low.StartsWith("ru") && !low.StartsWith("zh"))
                __result = "ru_RU";
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RestoreTextPostfix(ItemAttr __0)
    {
        try
        {
            if (__0 == null || !RussianModPackPlugin.ShouldUseRussian())
                return;

            string ruName = __0.itemName;
            string ruDesc = __0.itemDescription;
            if (!string.IsNullOrWhiteSpace(ruName) && ContainsCyrillic(ruName) && !ContainsCyrillic(__0.itemName_Runtime))
            {
                __0.itemName_Runtime = ruName;
                __0.modLocaleName = ruName;
            }
            if (!string.IsNullOrWhiteSpace(ruDesc) && ContainsCyrillic(ruDesc) && !ContainsCyrillic(__0.itemDescription_WithLanguage))
            {
                __0.itemDescription_WithLanguage = ruDesc;
                __0.modLocaleDescription = ruDesc;
                __0.runtimeItemDescription = null;
                __0.cachedDescLang = null;
                __0.InvalidateDescriptionCache();
            }
        }
        catch { }
    }

    private static bool ContainsCyrillic(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= '\u0400' && c <= '\u04FF') || c == 'Ё' || c == 'ё')
                return true;
        }
        return false;
    }
}
