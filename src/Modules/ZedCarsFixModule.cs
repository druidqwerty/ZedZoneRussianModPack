using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.RussianModPack.Modules;

internal static class ZedCarsFixModule
{
    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        Assembly zcAsm = FindZedCarsAssembly();
        if (zcAsm == null)
            return false;

        Type carDefType = zcAsm.GetType("ZedCars.CarDefinition", throwOnError: false);
        MethodInfo displayName = carDefType?.GetMethod(
            "DisplayName",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
        SafeHarmonyPatcher.TryPatch(
            harmony, log, "ZedCars:CarDefinition.DisplayName", displayName,
            prefix: typeof(ZedCarsFixModule).GetMethod(nameof(DisplayNamePrefix), BindingFlags.Static | BindingFlags.NonPublic));

        Type zedLocType = zcAsm.GetType("ZedLoc.L", throwOnError: false);
        MethodInfo readGameCode = zedLocType?.GetMethod(
            "ReadGameCode",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        SafeHarmonyPatcher.TryPatch(
            harmony, log, "ZedCars:ZedLoc.L.ReadGameCode", readGameCode,
            postfix: typeof(ZedCarsFixModule).GetMethod(nameof(ReadGameCodePostfix), BindingFlags.Static | BindingFlags.NonPublic));

        Type kitItemsType = zcAsm.GetType("ZedCars.CarKitItems", throwOnError: false);
        MethodInfo stampMethod = kitItemsType?.GetMethod(
            "Stamp",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        SafeHarmonyPatcher.TryPatch(
            harmony, log, "ZedCars:CarKitItems.Stamp", stampMethod,
            postfix: typeof(ZedCarsFixModule).GetMethod(nameof(StampPostfix), BindingFlags.Static | BindingFlags.NonPublic));

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
            log?.LogWarning($"[ZedCarsFix] Initial ApplyLanguage failed: {ex.Message}");
        }

        return true;
    }

    private static Assembly FindZedCarsAssembly()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.GetType("ZedCars.CarDefinition", throwOnError: false) != null)
                    return asm;
            }
            catch { }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DisplayNamePrefix(ref string languageCode)
    {
        try
        {
            if (RussianModPackPlugin.ShouldUseRussian())
                languageCode = "ru";
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
    private static void StampPostfix(ItemAttr __0, string __2, string __3)
    {
        try
        {
            if (__0 == null || !RussianModPackPlugin.ShouldUseRussian())
                return;

            if (!string.IsNullOrEmpty(__2))
            {
                __0.itemName = __2;
                __0.itemName_Runtime = __2;
                __0.modLocaleName = __2;
            }
            if (!string.IsNullOrEmpty(__3))
            {
                __0.itemDescription = __3;
                __0.itemDescription_WithLanguage = __3;
                __0.modLocaleDescription = __3;
                __0.InvalidateDescriptionCache();
            }
        }
        catch { }
    }
}
