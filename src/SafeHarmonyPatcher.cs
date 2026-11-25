using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace ZedZone.RussianModPack;

internal static class SafeHarmonyPatcher
{
    internal static int AppliedCount { get; private set; }
    internal static int FailedCount { get; private set; }

    internal static void ResetCounters()
    {
        AppliedCount = 0;
        FailedCount = 0;
    }

    internal static bool TryPatch(
        Harmony harmony,
        ManualLogSource log,
        string label,
        MethodBase target,
        MethodInfo prefix = null,
        MethodInfo postfix = null)
    {
        if (target == null)
        {
            FailedCount++;
            log?.LogWarning($"[Harmony] Missing target for {label}");
            return false;
        }

        try
        {
            harmony.Patch(
                target,
                prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
            AppliedCount++;
            return true;
        }
        catch (Exception ex)
        {
            FailedCount++;
            log?.LogWarning($"[Harmony] Failed to patch {label}: {ex.Message}");
            return false;
        }
    }
}
