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

        // Patch RvDashboard.Preview ctor in v0.9.x to also translate the unwrapped CJK overview hint.
        Type previewType = rvTextType.Assembly.GetType("ZedMobileBase.RvDashboard+Preview", throwOnError: false);
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

        MethodInfo getUiFont = typeof(DrivingPanel).GetMethod(
            "GetUIFont",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getUiFont != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "ApocalypseRV:DrivingPanel.GetUIFont", getUiFont,
                postfix: typeof(ApocalypseRvModule).GetMethod(nameof(GetUIFontPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        // Translate static caches that may have initialized before RussianModPackPlugin.Load().
        if (okT && RussianModPackPlugin.ShouldUseRussian())
            TranslateStaticCaches(rvTextType.Assembly, log);

        return okT && okF;
    }

    private static void TranslateStaticCaches(Assembly rvAsm, ManualLogSource log)
    {
        int updatedSpecs = 0;
        int updatedGroups = 0;

        try
        {
            Type upgradesType = rvAsm.GetType("ZedMobileBase.RvUpgrades", throwOnError: false);
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
            log?.LogWarning($"[ApocalypseRV] Failed to refresh RvUpgrades.Catalog: {ex.Message}");
        }

        try
        {
            Type menuType = rvAsm.GetType("ZedMobileBase.RvUpgradeMenu", throwOnError: false);
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
            log?.LogWarning($"[ApocalypseRV] Failed to refresh RvUpgradeMenu.Groups: {ex.Message}");
        }

        if (updatedSpecs > 0 || updatedGroups > 0)
            log?.LogInfo($"[ApocalypseRV] Refreshed static caches: specs={updatedSpecs}, groups={updatedGroups}.");
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
