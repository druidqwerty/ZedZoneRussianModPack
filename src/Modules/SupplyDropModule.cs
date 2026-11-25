using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.RussianModPack.Modules;

internal static class SupplyDropModule
{
    private static ManualLogSource _log;

    private static readonly Dictionary<string, string> ExactMap = new(StringComparer.Ordinal)
    {
        ["A supply drop is already on its way."] = "Груз снабжения уже в пути.",
        ["Supply drop: nobody is outside right now."] = "Сброс груза: сейчас никого нет на поверхности.",
        ["The supply helicopter couldn't drop its cargo."] = "Вертолёт снабжения не смог сбросить груз."
    };

    private static readonly Dictionary<string, string> DirectionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["east"] = "на востоке",
        ["north-east"] = "на северо-востоке",
        ["north"] = "на севере",
        ["north-west"] = "на северо-западе",
        ["west"] = "на западе",
        ["south-west"] = "на юго-западе",
        ["south"] = "на юге",
        ["south-east"] = "на юго-востоке"
    };

    private static readonly Regex RxInbound = new(
        @"^Supply helicopter inbound! Crate drops (.+) in (\d+) s\. It's marked on your map\. Its beacon will draw zombies\.$",
        RegexOptions.Compiled);

    private static readonly Regex RxLanded = new(
        @"^Supply crate down (.+)! Grab it before the horde does\.$",
        RegexOptions.Compiled);

    private static readonly Regex RxWhereDir = new(
        @"^to the ([a-z-]+) \((\d+) m\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        MethodInfo notifyMethod = FindTargetNotifyMethod();
        if (notifyMethod == null)
            return false;

        return SafeHarmonyPatcher.TryPatch(
            harmony, log, "SupplyDrop:Drops.Notify", notifyMethod,
            prefix: typeof(SupplyDropModule).GetMethod(nameof(NotifyPrefix), BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static MethodInfo FindTargetNotifyMethod()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type dropsType = asm.GetType("SupplyDrop.Drops", throwOnError: false);
                if (dropsType == null)
                    continue;

                MethodInfo m = dropsType.GetMethod(
                    "Notify",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);
                if (m != null)
                    return m;
            }
            catch { }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NotifyPrefix(ref string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text) || !RussianModPackPlugin.ShouldUseRussian())
                return;

            text = Translate(text);
        }
        catch (Exception ex)
        {
            _log?.LogWarning($"[SupplyDrop] Translation error: {ex.Message}");
        }
    }

    internal static string Translate(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (ExactMap.TryGetValue(text, out string exact))
            return exact;

        Match m = RxInbound.Match(text);
        if (m.Success)
        {
            string where = TranslateWhere(m.Groups[1].Value);
            string sec = m.Groups[2].Value;
            return $"Приближается вертолёт снабжения! Ящик упадёт {where} через {sec} с. Точка отмечена на карте — маяк привлечёт зомби.";
        }

        m = RxLanded.Match(text);
        if (m.Success)
        {
            string where = TranslateWhere(m.Groups[1].Value);
            return $"Ящик снабжения приземлился {where}! Заберите его раньше орды.";
        }

        return text;
    }

    private static string TranslateWhere(string where)
    {
        if (string.IsNullOrEmpty(where) || string.Equals(where, "nearby", StringComparison.OrdinalIgnoreCase))
            return "поблизости";

        if (string.Equals(where, "right next to you", StringComparison.OrdinalIgnoreCase))
            return "прямо рядом с вами";

        Match m = RxWhereDir.Match(where);
        if (m.Success && DirectionMap.TryGetValue(m.Groups[1].Value, out string ruDir))
            return $"{ruDir} ({m.Groups[2].Value} м)";

        return where;
    }
}
