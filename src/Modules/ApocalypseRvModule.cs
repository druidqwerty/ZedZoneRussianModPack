using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ZedZone.RussianModPack.Modules;

internal static class ApocalypseRvModule
{
    private const string CyrillicGlyphSet =
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        "0123456789.,:;!?()[]{}«»—−-+×/\\<>…%#*_·°→≤≥ ";

    private static readonly HashSet<IntPtr> PreloadedFonts = new();
    private static ManualLogSource _log;

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        Type rvTextType = FindType("ZedMobileBase.RvText");
        if (rvTextType == null)
            return false;

        MethodInfo methodT = rvTextType.GetMethod(
            "T",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
        MethodInfo methodF = rvTextType.GetMethod(
            "F",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(object[]) },
            null);

        bool okT = SafeHarmonyPatcher.TryPatch(
            harmony, log, "ApocalypseRV:RvText.T", methodT,
            prefix: typeof(ApocalypseRvModule).GetMethod(nameof(TranslateTPrefix), BindingFlags.Static | BindingFlags.NonPublic));
        bool okF = SafeHarmonyPatcher.TryPatch(
            harmony, log, "ApocalypseRV:RvText.F", methodF,
            prefix: typeof(ApocalypseRvModule).GetMethod(nameof(TranslateFPrefix), BindingFlags.Static | BindingFlags.NonPublic));

        MethodInfo getUiFont = typeof(DrivingPanel).GetMethod(
            "GetUIFont",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getUiFont != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:DrivingPanel.GetUIFont", getUiFont,
                postfix: typeof(ApocalypseRvModule).GetMethod(nameof(GetUIFontPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        return okT && okF;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type t = asm.GetType(fullName, throwOnError: false);
                if (t != null)
                    return t;
            }
            catch { }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TranslateTPrefix(string zh, ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(zh) || !RussianModPackPlugin.ShouldUseRussian())
                return true;

            if (RvRussianDictionary.TryGet(zh, out string ru))
            {
                __result = ru;
                return false;
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ApocalypseRV] T() error: {ex.Message}");
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TranslateFPrefix(string zhFormat, object[] args, ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(zhFormat) || !RussianModPackPlugin.ShouldUseRussian())
                return true;

            if (RvRussianDictionary.TryGet(zhFormat, out string ruFormat))
            {
                __result = args != null ? string.Format(ruFormat, args) : ruFormat;
                return false;
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ApocalypseRV] F() error: {ex.Message}");
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GetUIFontPostfix(Font __result)
    {
        try
        {
            if (__result == null || !RussianModPackPlugin.ShouldUseRussian())
                return;

            IntPtr ptr = __result.Pointer;
            if (ptr == IntPtr.Zero || !PreloadedFonts.Add(ptr))
                return;

            if (__result.dynamic)
            {
                __result.RequestCharactersInTexture(CyrillicGlyphSet, 12, FontStyle.Normal);
                __result.RequestCharactersInTexture(CyrillicGlyphSet, 14, FontStyle.Normal);
                __result.RequestCharactersInTexture(CyrillicGlyphSet, 16, FontStyle.Normal);
                __result.RequestCharactersInTexture(CyrillicGlyphSet, 18, FontStyle.Normal);
                __result.RequestCharactersInTexture(CyrillicGlyphSet, 24, FontStyle.Bold);
            }
        }
        catch { }
    }
}
