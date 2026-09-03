using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NumbersCompatibilityPatch;

[HarmonyPatch]
public static class Patch_NumbersWildlifeAutoHunt
{
    private const string AutoHuntPackageId = "Snues.AutoHunt";
    private const string AutoHuntModTypeName = "AutoHunt.AutoHuntMod";

    // Auto Hunt replaces the vanilla Wildlife tab with this class and draws its own toggle at
    // this fixed Rect (WildlifeTabWithToggle.DoWindowContents). We add a settings cog next to it,
    // or (if the user opts in) hide it and redraw the toggle at the bottom instead.
    internal const string AutoHuntWildlifeTabTypeName = "AutoHunt.WildlifeTabWithToggle";
    internal const float NativeCheckboxX = 5f;
    internal const float NativeCheckboxY = 5f;
    internal const float NativeCheckboxWidth = 140f;

    private const float IconSize = 28f;
    internal const float CheckboxHeight = 30f;
    private const float Pad = 6f;
    private const float Gap = 6f;

    // Match Auto Tame's own Wildlife-tab button row so both sit on the same line when Numbers is
    // open (or when Auto Tame draws its own button on the vanilla tab): left side for Auto Tame,
    // right side (this row) for Auto Hunt.
    private const float SharedRowOffsetY = 40f;
    private const float SharedRowHeight = 32f;

    private static readonly Texture2D SettingsIcon = ContentFinder<Texture2D>.Get("UI/NumbersCompatibilityPatch/Settings");

    private static FieldInfo? autoHuntSettingsField;
    private static FieldInfo? autoHuntEnabledField;
    private static Mod? autoHuntMod;
    private static Type? autoHuntWildlifeTabType;

    public static bool Prepare()
    {
        if (ModLister.GetActiveModWithIdentifier(AutoHuntPackageId, true) == null)
            return false;

        var autoHuntModType = AccessTools.TypeByName(AutoHuntModTypeName);
        var autoHuntSettings = AccessTools.TypeByName("AutoHunt.Settings");
        autoHuntWildlifeTabType = AccessTools.TypeByName(AutoHuntWildlifeTabTypeName);
        if (autoHuntModType == null || autoHuntSettings == null || autoHuntWildlifeTabType == null)
        {
            Log.Warning($"{NumbersWildlifeTable.LogPrefix} Auto Hunt is active, but its expected types were not found.");
            return false;
        }

        autoHuntSettingsField = AccessTools.Field(autoHuntModType, "settings");
        autoHuntEnabledField = AccessTools.Field(autoHuntSettings, "enabled");
        if (autoHuntSettingsField == null || autoHuntEnabledField == null)
        {
            Log.Warning($"{NumbersWildlifeTable.LogPrefix} Auto Hunt is active, but its expected fields were not found.");
            return false;
        }

        // Optional: only used to redraw on Numbers' wild animals table when Numbers is also active.
        NumbersWildlifeTable.TryInit();
        return true;
    }

    // Patching Numbers' own DoWindowContents would run its static constructor, which reads
    // Find.World and throws at startup. Its override always calls this base method, so hook
    // that instead and filter on the instance.
    public static MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(MainTabWindow_PawnTable), nameof(MainTabWindow_PawnTable.DoWindowContents));
    }

    public static void Postfix(MainTabWindow_PawnTable __instance, Rect rect)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (NumbersWildlifeTable.IsOpen(__instance))
        {
            DrawBottomRightRow(rect);
        }
        else if (autoHuntWildlifeTabType != null && autoHuntWildlifeTabType.IsInstanceOfType(__instance))
        {
            // When forcing the bottom position, Patch_AutoHuntWildlifeTabForceBottom hides the
            // native toggle and redraws the whole row after it, so skip the cog here to avoid
            // drawing it twice.
            if (!NumbersCompatibilityPatchMod.Settings.ForceAutoHuntBottom)
                DrawSettingsCogNextToNativeToggle();
        }
    }

    // Draws Auto Hunt's toggle+cog right-justified on the shared bottom row (matching Auto
    // Tame's own left-justified button). Used both on Numbers' wild animals table and, when the
    // user opts in, on Auto Hunt's own vanilla Wildlife tab.
    internal static void DrawBottomRightRow(Rect rect)
    {
        var settings = autoHuntSettingsField?.GetValue(null);
        if (settings == null || autoHuntEnabledField == null)
            return;

        var enabled = (bool)autoHuntEnabledField.GetValue(settings);
        var before = enabled;

        var font = Text.Font;
        var anchor = Text.Anchor;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        var label = "AutoHunt_Toggle".Translate().Resolve();
        var checkboxWidth = Text.CalcSize(label).x + 28f;
        var rowTop = rect.yMax - SharedRowOffsetY;
        var cogX = rect.xMax - IconSize - Pad;
        var checkX = cogX - Gap - checkboxWidth;

        Widgets.CheckboxLabeled(
            new Rect(checkX, rowTop + (SharedRowHeight - CheckboxHeight) / 2f, checkboxWidth, CheckboxHeight),
            label,
            ref enabled,
            false,
            null,
            null,
            true,
            false);

        Text.Anchor = anchor;
        Text.Font = font;

        DrawSettingsButton(new Rect(cogX, rowTop + (SharedRowHeight - IconSize) / 2f, IconSize, IconSize));

        if (enabled == before)
            return;

        autoHuntEnabledField.SetValue(settings, enabled);
        if (settings is ModSettings modSettings)
            modSettings.Write();
    }

    private static void DrawSettingsCogNextToNativeToggle()
    {
        var cogX = NativeCheckboxX + NativeCheckboxWidth + Gap;
        var cogY = NativeCheckboxY + (CheckboxHeight - IconSize) / 2f;
        DrawSettingsButton(new Rect(cogX, cogY, IconSize, IconSize));
    }

    private static void DrawSettingsButton(Rect settingsRect)
    {
        if (SettingsIcon == null)
            return;

        if (Widgets.ButtonImage(settingsRect, SettingsIcon))
        {
            var mod = GetAutoHuntMod();
            if (mod != null)
                Find.WindowStack.Add(new Dialog_ModSettings(mod));
        }

        TooltipHandler.TipRegion(settingsRect, "NumbersCompatibilityPatch_SettingsTip".Translate());
    }

    private static Mod? GetAutoHuntMod()
    {
        if (autoHuntMod != null)
            return autoHuntMod;

        foreach (var mod in LoadedModManager.ModHandles)
        {
            if (mod?.Content == null)
                continue;

            if (string.Equals(mod.Content.PackageId, AutoHuntPackageId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mod.Content.PackageIdPlayerFacing, AutoHuntPackageId, StringComparison.OrdinalIgnoreCase))
            {
                autoHuntMod = mod;
                return mod;
            }
        }

        return null;
    }
}

// Optional (Mod Settings > Auto Hunt Tweaks > "Always show Auto Hunt controls at the bottom"):
// hides Auto Hunt's native top-left toggle on its own vanilla Wildlife tab and redraws it on the
// shared bottom row instead, so it lands in the same place it would if Numbers were installed.
// Patched directly, unlike MainTabWindow_PawnTable.DoWindowContents above — this is Auto Hunt's
// own compiled override, so there's no static-constructor hazard from patching it.
[HarmonyPatch]
public static class Patch_AutoHuntWildlifeTabForceBottom
{
    public static bool Prepare()
    {
        return ModLister.GetActiveModWithIdentifier("Snues.AutoHunt", true) != null
            && AccessTools.TypeByName(Patch_NumbersWildlifeAutoHunt.AutoHuntWildlifeTabTypeName) != null;
    }

    public static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName(Patch_NumbersWildlifeAutoHunt.AutoHuntWildlifeTabTypeName);
        return AccessTools.DeclaredMethod(type, "DoWindowContents");
    }

    public static void Postfix(Rect rect)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (!NumbersCompatibilityPatchMod.Settings.ForceAutoHuntBottom)
            return;

        Widgets.DrawBoxSolid(
            new Rect(
                Patch_NumbersWildlifeAutoHunt.NativeCheckboxX,
                Patch_NumbersWildlifeAutoHunt.NativeCheckboxY,
                Patch_NumbersWildlifeAutoHunt.NativeCheckboxWidth,
                Patch_NumbersWildlifeAutoHunt.CheckboxHeight),
            Widgets.WindowBGFillColor);

        Patch_NumbersWildlifeAutoHunt.DrawBottomRightRow(rect);
    }
}
