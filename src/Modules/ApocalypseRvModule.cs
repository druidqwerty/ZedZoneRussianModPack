using System;
using System.Collections;
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

    private const string UnwrappedOverviewTailZh =
        "<color=#7A8C9C>可用工位</color>\n外部防护 · 动力轮胎\n生活设备 · 车载武器\n\n<color=#7A8C9C>操作提示</color>\n点击下方或左侧工位查看外观预览与材料清单";

    private const string UnwrappedOverviewTailRu =
        "<color=#7A8C9C>Доступные категории</color>\nВнешняя защита · Двигатель и шины\nКоммуникации · Вооружение\n\n<color=#7A8C9C>Подсказка</color>\nВыберите категорию ниже или слева для просмотра вида и списка материалов";

    private static readonly HashSet<IntPtr> PreloadedFonts = new();
    private static ManualLogSource _log;
    private static Assembly _rvAsm;
    private static bool _staticCachesTranslated;

    // v0.9.4+ RvLocale.Entries (Dictionary<string, ValueTuple<string, string>>)
    private static Dictionary<string, ValueTuple<string, string>> _rvLocaleEntries;

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        _staticCachesTranslated = false;
        _rvLocaleEntries = null;

        // 1. Prefer v0.9.4+ ZedMobileBase.RvLocale
        Type rvLocaleType = FindType("ZedMobileBase.RvLocale");
        if (rvLocaleType != null)
        {
            _rvAsm = rvLocaleType.Assembly;

            FieldInfo entriesField = rvLocaleType.GetField("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _rvLocaleEntries = entriesField?.GetValue(null) as Dictionary<string, ValueTuple<string, string>>;

            MethodInfo methodTFor = rvLocaleType.GetMethod(
                "TFor",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(bool) },
                null);
            MethodInfo methodPick = rvLocaleType.GetMethod(
                "Pick",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(string) },
                null);

            bool okTFor = SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:RvLocale.TFor", methodTFor,
                prefix: typeof(ApocalypseRvModule).GetMethod(nameof(RvLocaleTForPrefix), BindingFlags.Static | BindingFlags.NonPublic));
            bool okPick = methodPick == null || SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:RvLocale.Pick", methodPick,
                prefix: typeof(ApocalypseRvModule).GetMethod(nameof(RvLocalePickPrefix), BindingFlags.Static | BindingFlags.NonPublic));

            PatchUiFont(harmony, log);

            if (okTFor && RussianModPackPlugin.ShouldUseRussian())
                EnsureStaticCachesTranslated();

            return okTFor && okPick;
        }

        // 2. Fallback to v0.7.5 - v0.9.2 ZedMobileBase.RvText
        Type rvTextType = FindType("ZedMobileBase.RvText");
        if (rvTextType == null)
            return false;

        _rvAsm = rvTextType.Assembly;

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

        Type previewType = _rvAsm.GetType("ZedMobileBase.RvDashboard+Preview", throwOnError: false);
        ConstructorInfo previewCtor = previewType?.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Texture), typeof(string), typeof(string) },
            null);
        if (previewCtor != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:RvDashboard.Preview.ctor", previewCtor,
                prefix: typeof(ApocalypseRvModule).GetMethod(nameof(PreviewCtorPrefix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        PatchUiFont(harmony, log);

        if (okT && RussianModPackPlugin.ShouldUseRussian())
            EnsureStaticCachesTranslated();

        return okT && okF;
    }

    private static void PatchUiFont(Harmony harmony, ManualLogSource log)
    {
        MethodInfo getUiFont = typeof(DrivingPanel).GetMethod(
            "GetUIFont",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getUiFont != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:DrivingPanel.GetUIFont", getUiFont,
                postfix: typeof(ApocalypseRvModule).GetMethod(nameof(GetUIFontPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }
    }

    private static void EnsureStaticCachesTranslated()
    {
        if (_staticCachesTranslated || _rvAsm == null)
            return;

        _staticCachesTranslated = true;
        int updatedSpecs = 0;
        int updatedGroups = 0;

        try
        {
            Type upgradesType = _rvAsm.GetType("ZedMobileBase.RvUpgrades", throwOnError: false);
            PropertyInfo catalogProp = upgradesType?.GetProperty("Catalog", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (catalogProp?.GetValue(null) is IEnumerable catalog)
            {
                string[] fieldNames =
                {
                    "<Name>k__BackingField",
                    "<Effect>k__BackingField",
                    "<Comparison>k__BackingField",
                    "<Appearance>k__BackingField"
                };

                foreach (object spec in catalog)
                {
                    if (spec == null)
                        continue;

                    Type specType = spec.GetType();
                    bool specChanged = false;
                    foreach (string fn in fieldNames)
                    {
                        FieldInfo f = specType.GetField(fn, BindingFlags.Instance | BindingFlags.NonPublic);
                        if (f?.GetValue(spec) is string cur && RvRussianDictionary.TryGetAny(cur, out string ru) && cur != ru)
                        {
                            f.SetValue(spec, ru);
                            specChanged = true;
                        }
                    }

                    if (specChanged)
                        updatedSpecs++;
                }
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ApocalypseRV] Failed to refresh RvUpgrades.Catalog: {ex.Message}");
        }

        try
        {
            Type menuType = _rvAsm.GetType("ZedMobileBase.RvUpgradeMenu", throwOnError: false);
            FieldInfo groupsField = menuType?.GetField("Groups", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (groupsField?.GetValue(null) is IEnumerable groups)
            {
                foreach (object grp in groups)
                {
                    if (grp == null)
                        continue;

                    FieldInfo nameField = grp.GetType().GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (nameField?.GetValue(grp) is string cur && RvRussianDictionary.TryGetAny(cur, out string ru) && cur != ru)
                    {
                        nameField.SetValue(grp, ru);
                        updatedGroups++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ApocalypseRV] Failed to refresh RvUpgradeMenu.Groups: {ex.Message}");
        }

        if (updatedSpecs > 0 || updatedGroups > 0)
            _log?.LogInfo($"[ApocalypseRV] Refreshed static caches: specs={updatedSpecs}, groups={updatedGroups}.");
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
    private static bool RvLocaleTForPrefix(string key, bool chinese, ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || !RussianModPackPlugin.ShouldUseRussian())
                return true;

            if (!_staticCachesTranslated)
                EnsureStaticCachesTranslated();

            if (_rvLocaleEntries != null && _rvLocaleEntries.TryGetValue(key, out ValueTuple<string, string> pair))
            {
                if (!string.IsNullOrEmpty(pair.Item1) && RvRussianDictionary.TryGet(pair.Item1, out string ru))
                {
                    __result = ru;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[ApocalypseRV] RvLocale.TFor() error: {ex.Message}");
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool RvLocalePickPrefix(string zh, string en, ref string __result)
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
            _log?.LogWarning($"[ApocalypseRV] RvLocale.Pick() error: {ex.Message}");
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TranslateTPrefix(string zh, ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(zh) || !RussianModPackPlugin.ShouldUseRussian())
                return true;

            if (!_staticCachesTranslated)
                EnsureStaticCachesTranslated();

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

            if (!_staticCachesTranslated)
                EnsureStaticCachesTranslated();

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
    private static void PreviewCtorPrefix(ref string __2)
    {
        try
        {
            if (string.IsNullOrEmpty(__2) || !RussianModPackPlugin.ShouldUseRussian())
                return;

            if (__2.Contains(UnwrappedOverviewTailZh, StringComparison.Ordinal))
                __2 = __2.Replace(UnwrappedOverviewTailZh, UnwrappedOverviewTailRu, StringComparison.Ordinal);
        }
        catch { }
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
