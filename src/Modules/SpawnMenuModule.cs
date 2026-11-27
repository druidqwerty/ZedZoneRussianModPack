using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZedZone.RussianModPack.Modules;

internal static class SpawnMenuModule
{
    private const string CyrillicGlyphSet =
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        "0123456789.,:;!?()[]{}«»—–−-+×/\\<>…%#*_·°| ";

    private const string CyrillicSampleProbe = "МенюСпавнаЖЩЪЫЭЮЯё";

    private static ManualLogSource _log;

    private static FieldInfo _menuOpenField;
    private static FieldInfo _menuSearchField;
    private static FieldInfo _menuDirtyField;
    private static MethodInfo _menuRebuildMethod;
    private static FieldInfo _fontsCurrentField;
    private static FieldInfo _fontsCheckedForField;

    private static IList _inTypeKeysList;
    private static List<object> _origTypeKeys;
    private static bool _typeKeysDisabled;

    private static bool _wasOpen;
    private static int _openedFrame = -1;
    private static readonly HashSet<IntPtr> PreloadedFonts = new();

    private static readonly byte[] EmptyKeyState = new byte[256];

    private readonly struct LayoutKeyDef
    {
        public readonly Key Key;
        public readonly uint Vk;
        public readonly char RuFallback;
        public readonly char EnFallback;
        public readonly bool FixedChar;

        public LayoutKeyDef(Key key, uint vk, char ruFallback, char enFallback, bool fixedChar = false)
        {
            Key = key;
            Vk = vk;
            RuFallback = ruFallback;
            EnFallback = enFallback;
            FixedChar = fixedChar;
        }
    }

    private static readonly LayoutKeyDef[] SearchKeys =
    {
        // Row 1 (ЙЦУКЕНГШЩЗХЪ)
        new(Key.Q, 0x51, 'й', 'q'),
        new(Key.W, 0x57, 'ц', 'w'),
        new(Key.E, 0x45, 'у', 'e'),
        new(Key.R, 0x52, 'к', 'r'),
        new(Key.T, 0x54, 'е', 't'),
        new(Key.Y, 0x59, 'н', 'y'),
        new(Key.U, 0x55, 'г', 'u'),
        new(Key.I, 0x49, 'ш', 'i'),
        new(Key.O, 0x4F, 'щ', 'o'),
        new(Key.P, 0x50, 'з', 'p'),
        new(Key.LeftBracket, 0xDB, 'х', '\0'),
        new(Key.RightBracket, 0xDD, 'ъ', '\0'),

        // Row 2 (ФЫВАПРОЛДЖЭ)
        new(Key.A, 0x41, 'ф', 'a'),
        new(Key.S, 0x53, 'ы', 's'),
        new(Key.D, 0x44, 'в', 'd'),
        new(Key.F, 0x46, 'а', 'f'),
        new(Key.G, 0x47, 'п', 'g'),
        new(Key.H, 0x48, 'р', 'h'),
        new(Key.J, 0x4A, 'о', 'j'),
        new(Key.K, 0x4B, 'л', 'k'),
        new(Key.L, 0x4C, 'д', 'l'),
        new(Key.Semicolon, 0xBA, 'ж', '\0'),
        new(Key.Quote, 0xDE, 'э', '\0'),

        // Row 3 (ЯЧСМИТЬБЮ + Ё + ./ )
        new(Key.Z, 0x5A, 'я', 'z'),
        new(Key.X, 0x58, 'ч', 'x'),
        new(Key.C, 0x43, 'с', 'c'),
        new(Key.V, 0x56, 'м', 'v'),
        new(Key.B, 0x42, 'и', 'b'),
        new(Key.N, 0x4E, 'т', 'n'),
        new(Key.M, 0x4D, 'ь', 'm'),
        new(Key.Comma, 0xBC, 'б', '\0'),
        new(Key.Period, 0xBE, 'ю', '.'),
        new(Key.Slash, 0xBF, '.', '/'),
        new(Key.Backquote, 0xC0, 'ё', '\0'),

        // Digits, numpad & punctuation
        new(Key.Digit0, 0x30, '0', '0', true),
        new(Key.Digit1, 0x31, '1', '1', true),
        new(Key.Digit2, 0x32, '2', '2', true),
        new(Key.Digit3, 0x33, '3', '3', true),
        new(Key.Digit4, 0x34, '4', '4', true),
        new(Key.Digit5, 0x35, '5', '5', true),
        new(Key.Digit6, 0x36, '6', '6', true),
        new(Key.Digit7, 0x37, '7', '7', true),
        new(Key.Digit8, 0x38, '8', '8', true),
        new(Key.Digit9, 0x39, '9', '9', true),
        new(Key.Numpad0, 0x60, '0', '0', true),
        new(Key.Numpad1, 0x61, '1', '1', true),
        new(Key.Numpad2, 0x62, '2', '2', true),
        new(Key.Numpad3, 0x63, '3', '3', true),
        new(Key.Numpad4, 0x64, '4', '4', true),
        new(Key.Numpad5, 0x65, '5', '5', true),
        new(Key.Numpad6, 0x66, '6', '6', true),
        new(Key.Numpad7, 0x67, '7', '7', true),
        new(Key.Numpad8, 0x68, '8', '8', true),
        new(Key.Numpad9, 0x69, '9', '9', true),
        new(Key.NumpadPeriod, 0x6E, '.', '.', true),
        new(Key.NumpadMinus, 0x6D, '-', '-', true),
        new(Key.Space, 0x20, ' ', ' ', true),
        new(Key.Minus, 0xBD, '-', '-', true),
    };

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyExW(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    private static MethodInfo _guiLabelRectString;
    private static MethodInfo _drawTranslatedLabelMethod;

    private static readonly Dictionary<string, string> ButtonMap = new(StringComparer.Ordinal)
    {
        // Tabs
        ["All"] = "Все",
        ["Mods"] = "Моды",
        ["Guns"] = "Оружие",
        ["Ammo"] = "Патроны",
        ["Melee"] = "Ближний",
        ["Gear"] = "Снаряж.",
        ["Food"] = "Еда",
        ["Material"] = "Ресурсы",
        ["Other"] = "Прочее",

        // Amounts
        ["Stack"] = "Стак",

        // Right-panel action buttons
        ["Fill my magazines"] = "Зарядить магазины",
        ["Save item list"] = "Сохранить список предметов",
        ["Reload kits"] = "Обновить наборы",
        ["Close"] = "Закрыть",

        // Default kit names
        ["Boltgun kit"] = "Набор: Болтер",
        ["Laser Gun kit"] = "Набор: Лазерная винтовка",
        ["Helicopter kit"] = "Набор: Вертолёт",
        ["Tank kit"] = "Набор: Танк",
        ["Battle Tank kit"] = "Набор: Боевой танк",
    };

    private static readonly Dictionary<string, string> ExactLabelMap = new(StringComparer.Ordinal)
    {
        ["Amount"] = "Кол-во",
        ["No items loaded yet."] = "Предметы ещё не загружены.",
        ["Nothing matches."] = "Ничего не найдено.",
        ["<b>Kits</b>"] = "<b>Наборы</b>",
        ["No kits. Add some to\nBepInEx\\config\\SpawnMenu.kits.txt"] =
            "Нет наборов. Добавьте их в\nBepInEx\\config\\SpawnMenu.kits.txt",
        ["Couldn't save the list, see SpawnMenu.log"] =
            "Не удалось сохранить список, см. SpawnMenu.log",
    };

    private static readonly Dictionary<string, string> EnglishTypeLabelFallback = new(StringComparer.Ordinal)
    {
        ["Gun"] = "Оружие",
        ["Melee"] = "Ближний",
        ["Mask"] = "Маска",
        ["Gun part"] = "Обвес",
        ["Liquid"] = "Жидкость",
        ["Material"] = "Материал",
        ["Food"] = "Еда",
        ["Clothing"] = "Одежда",
        ["Ammo"] = "Патроны",
        ["Backpack"] = "Рюкзак",
        ["Throwable"] = "Метат.",
        ["Magazine"] = "Магазин",
        ["Arrow"] = "Стрелы",
        ["Deployable"] = "Постройка",
        ["Container"] = "Контейнер",
        ["Cloak"] = "Плащ",
        ["Liqiud"] = "Жидкость",
    };

    private static readonly Regex RxHeader = new(
        @"^<b>Spawn Menu</b>   (\d+) items   ·   type to search, click an item to spawn it, (.+) / Esc to close$",
        RegexOptions.Compiled);

    private static readonly Regex RxFooter = new(
        @"^<color=#aaaaaa>(\d+)-(\d+) of (\d+)   ·   mouse wheel / arrows / PgUp PgDn to scroll, Enter spawns the highlighted item, Tab switches tabs</color>$",
        RegexOptions.Compiled);

    private static readonly Regex RxItemRow = new(
        @"^<color=#aaaaaa>([^<]+)</color>(  .+)$",
        RegexOptions.Compiled);

    private static readonly Regex RxStackSuffix = new(
        @"  \(stack (\d+)\)",
        RegexOptions.Compiled);

    private static readonly Regex RxSpawned = new(
        @"^Spawned (.+) x(\d+)$",
        RegexOptions.Compiled);

    private static readonly Regex RxCouldntSpawn = new(
        @"^Couldn't spawn (.+), see BepInEx\\SpawnMenu\.log$",
        RegexOptions.Compiled);

    private static readonly Regex RxLoadedMags = new(
        @"^Loaded (\d+) of (\d+) magazines from the ammo you carry$",
        RegexOptions.Compiled);

    private static readonly Regex RxReloaded = new(
        @"^Reloaded: (\d+) kits, (\d+) items$",
        RegexOptions.Compiled);

    private static readonly Regex RxKitStatus = new(
        @"^(.+): spawned (\d+) of (\d+) items(.*)$",
        RegexOptions.Compiled);

    private static readonly Regex RxKitLoadedMags = new(
        @", loaded (\d+)/(\d+) magazines",
        RegexOptions.Compiled);

    private static readonly Regex RxModTooOld = new(
        @"(Helicopter|Tank) mod is too old for '([^']+)' \(update it\)",
        RegexOptions.Compiled);

    private static readonly Regex RxModOlderGame = new(
        @"No (helicopter|tank): the (Helicopter|Tank) mod was built for an older version of the game\. Update or rebuild it \(([^)]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex RxNoModNotInstalled = new(
        @"No (helicopter|tank): the (Helicopter|Tank) mod isn't installed",
        RegexOptions.Compiled);

    private static readonly Regex RxNoModError = new(
        @"No (helicopter|tank): ([^.]+)",
        RegexOptions.Compiled);

    private static readonly Regex RxNoRocketsMissingItem = new(
        @"No rockets: item (\d+) isn't in the item list",
        RegexOptions.Compiled);

    private static readonly Regex RxRocketsCount = new(
        @"\. (\d+) rockets(?=$|[.,])",
        RegexOptions.Compiled);

    private static readonly Regex RxFallbackItemName = new(
        @"^Item (\d+)$",
        RegexOptions.Compiled);

    internal static bool TryInstall(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        Assembly smAsm = FindSpawnMenuAssembly();
        if (smAsm == null)
            return false;

        Type menuType = smAsm.GetType("SpawnMenu.Menu", throwOnError: false);
        Type catalogType = smAsm.GetType("SpawnMenu.Catalog", throwOnError: false);
        Type fontsType = smAsm.GetType("SpawnMenu.Fonts", throwOnError: false);
        Type inType = smAsm.GetType("SpawnMenu.In", throwOnError: false);

        if (menuType == null)
            return false;

        _menuOpenField = menuType.GetField("_open", BindingFlags.Static | BindingFlags.NonPublic);
        _menuSearchField = menuType.GetField("_search", BindingFlags.Static | BindingFlags.NonPublic);
        _menuDirtyField = menuType.GetField("_dirty", BindingFlags.Static | BindingFlags.NonPublic);
        _menuRebuildMethod = menuType.GetMethod("Rebuild", BindingFlags.Static | BindingFlags.NonPublic);

        _fontsCurrentField = fontsType?.GetField("_font", BindingFlags.Static | BindingFlags.NonPublic);
        _fontsCheckedForField = fontsType?.GetField("_checkedFor", BindingFlags.Static | BindingFlags.NonPublic);

        FieldInfo typeKeysField = inType?.GetField("TypeKeys", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (typeKeysField?.GetValue(null) is IList tkList)
        {
            _inTypeKeysList = tkList;
            _origTypeKeys = new List<object>(tkList.Count);
            foreach (object item in tkList)
                _origTypeKeys.Add(item);
        }

        _guiLabelRectString = typeof(GUI).GetMethod(
            nameof(GUI.Label),
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(Rect), typeof(string) },
            null);
        _drawTranslatedLabelMethod = typeof(SpawnMenuModule).GetMethod(
            nameof(DrawTranslatedLabel),
            BindingFlags.Static | BindingFlags.NonPublic);

        bool anyOk = false;

        MethodInfo drawMethod = menuType.GetMethod("Draw", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Menu.Draw", drawMethod,
                prefix: typeof(SpawnMenuModule).GetMethod(nameof(DrawPrefix), BindingFlags.Static | BindingFlags.NonPublic),
                transpiler: _guiLabelRectString != null
                    ? typeof(SpawnMenuModule).GetMethod(nameof(DrawTranspiler), BindingFlags.Static | BindingFlags.NonPublic)
                    : null))
        {
            anyOk = true;
        }

        MethodInfo buttonMethod = menuType.GetMethod(
            "Button",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(Rect), typeof(string), typeof(bool), typeof(Vector2) },
            null);
        if (SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Menu.Button", buttonMethod,
                prefix: typeof(SpawnMenuModule).GetMethod(nameof(ButtonPrefix), BindingFlags.Static | BindingFlags.NonPublic)))
        {
            anyOk = true;
        }

        MethodInfo tickMethod = menuType.GetMethod("Tick", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (tickMethod != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Menu.Tick", tickMethod,
                prefix: typeof(SpawnMenuModule).GetMethod(nameof(TickPrefix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        MethodInfo typeLabelMethod = catalogType?.GetMethod(
            "TypeLabel",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(ItemType) },
            null);
        if (typeLabelMethod != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Catalog.TypeLabel", typeLabelMethod,
                postfix: typeof(SpawnMenuModule).GetMethod(nameof(TypeLabelPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        MethodInfo nameOfMethod = catalogType?.GetMethod(
            "NameOf",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(ItemAttr) },
            null);
        if (nameOfMethod != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Catalog.NameOf", nameOfMethod,
                postfix: typeof(SpawnMenuModule).GetMethod(nameof(NameOfPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        MethodInfo fontsSampleMethod = fontsType?.GetMethod(
            "Sample",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(IEnumerable<string>), typeof(int) },
            null);
        if (fontsSampleMethod != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Fonts.Sample", fontsSampleMethod,
                prefix: typeof(SpawnMenuModule).GetMethod(nameof(FontsSamplePrefix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        MethodInfo fontsChooseMethod = fontsType?.GetMethod(
            "Choose",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(IEnumerable<string>) },
            null);
        if (fontsChooseMethod != null)
        {
            SafeHarmonyPatcher.TryPatch(
                harmony, log, "SpawnMenu:Fonts.Choose", fontsChooseMethod,
                postfix: typeof(SpawnMenuModule).GetMethod(nameof(FontsChoosePostfix), BindingFlags.Static | BindingFlags.NonPublic));
        }

        return anyOk;
    }

    private static Assembly FindSpawnMenuAssembly()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.GetType("SpawnMenu.Menu", throwOnError: false) != null)
                    return asm;
            }
            catch { }
        }
        return null;
    }

    private static IEnumerable<CodeInstruction> DrawTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction ci in instructions)
        {
            if (ci.opcode == OpCodes.Call &&
                ci.operand is MethodInfo mi &&
                mi == _guiLabelRectString &&
                _drawTranslatedLabelMethod != null)
            {
                ci.operand = _drawTranslatedLabelMethod;
                replaced++;
            }
            yield return ci;
        }

        if (replaced == 0)
            _log?.LogWarning("[SpawnMenu] DrawTranspiler found 0 GUI.Label(Rect, string) call sites.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TickPrefix()
    {
        try
        {
            bool useRu = RussianModPackPlugin.ShouldUseRussian();
            bool isOpen = _menuOpenField?.GetValue(null) is bool b && b;

            if (isOpen && !_wasOpen)
                _openedFrame = Time.frameCount;
            _wasOpen = isOpen;

            if (_inTypeKeysList != null && _origTypeKeys != null)
            {
                if (useRu && !_typeKeysDisabled)
                {
                    _inTypeKeysList.Clear();
                    _typeKeysDisabled = true;
                }
                else if (!useRu && _typeKeysDisabled)
                {
                    _inTypeKeysList.Clear();
                    foreach (object item in _origTypeKeys)
                        _inTypeKeysList.Add(item);
                    _typeKeysDisabled = false;
                }
            }

            if (useRu && isOpen && Time.frameCount != _openedFrame)
                PollLayoutSearchInput();
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PollLayoutSearchInput()
    {
        if (_menuSearchField == null)
            return;

        Keyboard kb = Keyboard.current;
        if (kb == null || kb.Pointer == IntPtr.Zero)
            return;

        // Do not type characters when Ctrl or Alt is held (e.g. Ctrl+Backspace, Alt+Tab)
        if (IsKeyHeld(kb, Key.LeftCtrl) || IsKeyHeld(kb, Key.RightCtrl) ||
            IsKeyHeld(kb, Key.LeftAlt) || IsKeyHeld(kb, Key.RightAlt))
            return;

        string cur = _menuSearchField.GetValue(null) as string ?? "";
        if (cur.Length >= 40)
            return;

        IntPtr hkl = IntPtr.Zero;
        bool hklResolved = false;
        bool changed = false;

        for (int i = 0; i < SearchKeys.Length; i++)
        {
            LayoutKeyDef def = SearchKeys[i];
            if (!IsKeyPressedThisFrame(kb, def.Key))
                continue;

            if (!hklResolved)
            {
                hkl = GetKeyboardLayout(0);
                hklResolved = true;
            }

            char ch = ResolveCharForLayout(def, hkl);
            if (ch != '\0' && cur.Length < 40)
            {
                cur += ch;
                changed = true;
            }
        }

        if (changed)
        {
            _menuSearchField.SetValue(null, cur);
            _menuDirtyField?.SetValue(null, true);
            _menuRebuildMethod?.Invoke(null, null);
        }
    }

    private static bool IsKeyPressedThisFrame(Keyboard kb, Key key)
    {
        try { return kb[key]?.wasPressedThisFrame ?? false; }
        catch { return false; }
    }

    private static bool IsKeyHeld(Keyboard kb, Key key)
    {
        try { return kb[key]?.isPressed ?? false; }
        catch { return false; }
    }

    internal static char ResolveCharForLayout(Key key, IntPtr hkl)
    {
        for (int i = 0; i < SearchKeys.Length; i++)
        {
            if (SearchKeys[i].Key == key)
                return ResolveCharForLayout(SearchKeys[i], hkl);
        }
        return '\0';
    }

    private static char ResolveCharForLayout(LayoutKeyDef def, IntPtr hkl)
    {
        if (def.FixedChar)
            return def.EnFallback;

        if (hkl != IntPtr.Zero)
        {
            try
            {
                uint scan = MapVirtualKeyExW(def.Vk, 0, hkl);
                var sb = new StringBuilder(4);
                // wFlags = 4 (0x0004): do not modify keyboard state (Win10 1607+)
                int rc = ToUnicodeEx(def.Vk, scan, EmptyKeyState, sb, sb.Capacity, 4, hkl);
                if (rc > 0 && sb.Length > 0)
                {
                    char c = char.ToLowerInvariant(sb[0]);
                    if (IsAllowedSearchChar(c))
                        return c;
                }
            }
            catch { }

            // Fallback by primary language id (0x19 = Russian)
            int primaryLang = (int)(hkl.ToInt64() & 0x3FF);
            if (primaryLang == 0x19)
                return def.RuFallback;
        }

        return def.EnFallback;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DrawPrefix()
    {
        try
        {
            if (!RussianModPackPlugin.ShouldUseRussian())
                return;

            bool isOpen = _menuOpenField?.GetValue(null) is bool b && b;
            if (!isOpen)
            {
                _wasOpen = false;
                return;
            }

            if (!_wasOpen)
            {
                _wasOpen = true;
                _openedFrame = Time.frameCount;
            }

            EnsureCyrillicFont();
        }
        catch { }
    }

    private static bool IsAllowedSearchChar(char ch)
    {
        if (char.IsLetterOrDigit(ch))
            return true;
        return ch == ' ' || ch == '-' || ch == '.' || ch == '\'' || ch == '/' || ch == '_';
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureCyrillicFont()
    {
        if (_fontsCurrentField == null)
            return;

        Font cur = _fontsCurrentField.GetValue(null) as Font;
        if (cur != null && cur.Pointer != IntPtr.Zero)
        {
            PreloadCyrillicGlyphs(cur);
            return;
        }

        Font fallback = TryFindCyrillicFont();
        if (fallback != null && fallback.Pointer != IntPtr.Zero)
        {
            _fontsCurrentField.SetValue(null, fallback);
            PreloadCyrillicGlyphs(fallback);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Font TryFindCyrillicFont()
    {
        Font[] candidates =
        {
            SafeGetFont(() => UIFontPolicy.VectorFont),
            SafeGetFont(() => DrivingPanel.GetUIFont()),
            SafeGetFont(() => UIFontPolicy.PixelFont),
        };

        foreach (Font f in candidates)
        {
            if (f == null || f.Pointer == IntPtr.Zero)
                continue;
            try
            {
                if (f.dynamic)
                    f.RequestCharactersInTexture(CyrillicSampleProbe, 16, FontStyle.Normal);
                if (f.HasCharacter('Ж') && f.HasCharacter('я'))
                    return f;
            }
            catch { }
        }

        return null;
    }

    private static Font SafeGetFont(Func<Font> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PreloadCyrillicGlyphs(Font font)
    {
        if (font == null)
            return;

        IntPtr ptr = font.Pointer;
        if (ptr == IntPtr.Zero || !PreloadedFonts.Add(ptr))
            return;

        try
        {
            if (font.dynamic)
            {
                font.RequestCharactersInTexture(CyrillicGlyphSet, 12, FontStyle.Normal);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 14, FontStyle.Normal);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 14, FontStyle.Bold);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 16, FontStyle.Normal);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 16, FontStyle.Bold);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 18, FontStyle.Normal);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 20, FontStyle.Normal);
                font.RequestCharactersInTexture(CyrillicGlyphSet, 24, FontStyle.Bold);
            }
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FontsSamplePrefix(IEnumerable<string> texts, int max, ref string __result)
    {
        try
        {
            if (!RussianModPackPlugin.ShouldUseRussian())
                return true;

            var seen = new HashSet<char>();
            var sb = new StringBuilder(Math.Max(16, max));

            foreach (char c in CyrillicSampleProbe)
            {
                if (seen.Add(c))
                {
                    sb.Append(c);
                    if (sb.Length >= max)
                    {
                        __result = sb.ToString();
                        return false;
                    }
                }
            }

            if (texts != null)
            {
                foreach (string s in texts)
                {
                    if (s == null)
                        continue;
                    foreach (char c in s)
                    {
                        if (c >= '\u0400' &&
                            !char.IsWhiteSpace(c) &&
                            !char.IsSurrogate(c) &&
                            (c < '\u2000' || c > '\u206f') &&
                            seen.Add(c))
                        {
                            sb.Append(c);
                            if (sb.Length >= max)
                            {
                                __result = sb.ToString();
                                return false;
                            }
                        }
                    }
                }
            }

            __result = sb.ToString();
            return false;
        }
        catch
        {
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FontsChoosePostfix()
    {
        try
        {
            if (!RussianModPackPlugin.ShouldUseRussian())
                return;

            EnsureCyrillicFont();
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ButtonPrefix(ref string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text) || !RussianModPackPlugin.ShouldUseRussian())
                return;

            if (ButtonMap.TryGetValue(text, out string ru))
                text = ru;
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TypeLabelPostfix(ItemType t, ref string __result)
    {
        try
        {
            if (!RussianModPackPlugin.ShouldUseRussian())
                return;

            __result = TranslateItemType(t, __result);
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NameOfPostfix(ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(__result) || !RussianModPackPlugin.ShouldUseRussian())
                return;

            Match m = RxFallbackItemName.Match(__result);
            if (m.Success)
                __result = "Предмет " + m.Groups[1].Value;
        }
        catch { }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DrawTranslatedLabel(Rect rect, string text)
    {
        try
        {
            if (!string.IsNullOrEmpty(text) && RussianModPackPlugin.ShouldUseRussian())
                text = TranslateLabel(text);
        }
        catch { }

        GUI.Label(rect, text);
    }

    internal static string TranslateItemType(ItemType t, string fallback = null)
    {
        return t switch
        {
            ItemType.RangedWeapon => "Оружие",
            ItemType.MeleeWeapon => "Ближний",
            ItemType.FaceMask => "Маска",
            ItemType.GunPart => "Обвес",
            ItemType.Liqiud => "Жидкость",
            ItemType.Material => "Материал",
            ItemType.Food => "Еда",
            ItemType.Clothing => "Одежда",
            ItemType.Ammo => "Патроны",
            ItemType.Backpack => "Рюкзак",
            ItemType.Throwable => "Метат.",
            ItemType.Magazine => "Магазин",
            ItemType.Arrow => "Стрелы",
            ItemType.Deployable => "Постройка",
            ItemType.Container => "Контейнер",
            ItemType.Cloak => "Плащ",
            _ => fallback ?? t.ToString(),
        };
    }

    internal static string TranslateLabel(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Fast-path: right-aligned item IDs ("<color=#888888>#123</color>")
        if (text.StartsWith("<color=#888888>#", StringComparison.Ordinal))
            return text;

        if (ExactLabelMap.TryGetValue(text, out string exact))
            return exact;

        if (text.StartsWith("Search: ", StringComparison.Ordinal))
            return "Поиск: " + text.Substring("Search: ".Length);

        if (text.StartsWith("<b>Spawn Menu</b>", StringComparison.Ordinal))
        {
            Match hm = RxHeader.Match(text);
            if (hm.Success)
                return $"<b>Меню спавна</b>   {hm.Groups[1].Value} шт.   ·   введите для поиска, клик — выдать предмет, {hm.Groups[2].Value} / Esc — закрыть";
            return text.Replace("<b>Spawn Menu</b>", "<b>Меню спавна</b>", StringComparison.Ordinal);
        }

        if (text.StartsWith("<color=#aaaaaa>", StringComparison.Ordinal))
        {
            Match fm = RxFooter.Match(text);
            if (fm.Success)
                return $"<color=#aaaaaa>{fm.Groups[1].Value}–{fm.Groups[2].Value} из {fm.Groups[3].Value}   ·   колёсико / стрелки / PgUp PgDn — прокрутка, Enter — выдать, Tab — вкладки</color>";

            Match rm = RxItemRow.Match(text);
            if (rm.Success)
            {
                string rawType = rm.Groups[1].Value;
                string trimmedType = rawType.TrimEnd();
                string typeOut = EnglishTypeLabelFallback.TryGetValue(trimmedType, out string ruType)
                    ? ruType.PadRight(9)
                    : rawType;

                string rest = rm.Groups[2].Value;
                if (rest.Contains("  (stack ", StringComparison.Ordinal))
                    rest = RxStackSuffix.Replace(rest, "  (стак $1)");
                if (rest.Contains("<color=#7fd4ff>[mod]</color>", StringComparison.Ordinal))
                    rest = rest.Replace("<color=#7fd4ff>[mod]</color>", "<color=#7fd4ff>[мод]</color>", StringComparison.Ordinal);

                return $"<color=#aaaaaa>{typeOut}</color>{rest}";
            }
        }

        // Status bar messages
        return TranslateStatus(text);
    }

    internal static string TranslateStatus(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (ExactLabelMap.TryGetValue(text, out string exact))
            return exact;

        if (text.StartsWith("Saved to ", StringComparison.Ordinal))
            return "Сохранено в " + text.Substring("Saved to ".Length);

        Match m = RxSpawned.Match(text);
        if (m.Success)
            return $"Выдано: {m.Groups[1].Value} x{m.Groups[2].Value}";

        m = RxCouldntSpawn.Match(text);
        if (m.Success)
            return $"Не удалось выдать {m.Groups[1].Value}, см. BepInEx\\SpawnMenu.log";

        m = RxLoadedMags.Match(text);
        if (m.Success)
            return $"Заряжено магазинов: {m.Groups[1].Value} из {m.Groups[2].Value} (из имеющихся патронов)";

        m = RxReloaded.Match(text);
        if (m.Success)
            return $"Обновлено: наборов — {m.Groups[1].Value}, предметов — {m.Groups[2].Value}";

        m = RxKitStatus.Match(text);
        if (m.Success)
        {
            string kitName = m.Groups[1].Value;
            if (ButtonMap.TryGetValue(kitName, out string ruKit))
                kitName = ruKit;

            string tail = TranslateKitStatusTail(m.Groups[4].Value);
            string unit = tail.StartsWith(".", StringComparison.Ordinal) ? "предм" : "предм.";
            return $"{kitName}: выдано {m.Groups[2].Value} из {m.Groups[3].Value} {unit}{tail}";
        }

        return text;
    }

    private static string TranslateKitStatusTail(string tail)
    {
        if (string.IsNullOrEmpty(tail))
            return tail;

        string s = tail;

        s = RxKitLoadedMags.Replace(s, ", заряжено магазинов: $1/$2");
        s = s.Replace(". Missing: ", ". Не найдено: ", StringComparison.Ordinal);

        s = RxNoModNotInstalled.Replace(s, match =>
        {
            string mod = match.Groups[1].Value;
            return mod == "helicopter"
                ? "Без вертолёта: мод Helicopter не установлен"
                : "Без танка: мод Tank не установлен";
        });

        s = RxModTooOld.Replace(s, match =>
            $"Мод {match.Groups[1].Value} устарел для «{match.Groups[2].Value}» (обновите его)");

        s = RxModOlderGame.Replace(s, match =>
        {
            string mod = match.Groups[1].Value;
            string prefix = mod == "helicopter" ? "Без вертолёта" : "Без танка";
            return $"{prefix}: мод {match.Groups[2].Value} собран для старой версии игры ({match.Groups[3].Value})";
        });

        s = RxNoModError.Replace(s, match =>
        {
            string mod = match.Groups[1].Value;
            string prefix = mod == "helicopter" ? "Без вертолёта" : "Без танка";
            return $"{prefix}: {match.Groups[2].Value}";
        });

        s = s.Replace("No rockets: the Tank mod isn't installed",
            "Без ракет: мод Tank не установлен", StringComparison.Ordinal);
        s = s.Replace("No rockets: the Tank mod found no recoilless rockets in this game",
            "Без ракет: мод Tank не нашёл безоткатные ракеты в игре", StringComparison.Ordinal);
        s = RxNoRocketsMissingItem.Replace(s, "Без ракет: предмет $1 отсутствует в списке");
        s = s.Replace("No rockets: spawning failed",
            "Без ракет: не удалось выдать", StringComparison.Ordinal);
        s = RxRocketsCount.Replace(s, ". Ракет: $1");

        return s;
    }
}
