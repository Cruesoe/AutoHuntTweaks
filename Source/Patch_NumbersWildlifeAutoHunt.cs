using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoHuntNumbersPatch;

[HarmonyPatch]
public static class Patch_NumbersWildlifeAutoHunt
{
    private const string AutoHuntPackageId = "Snues.AutoHunt";
    private const string NumbersPackageId = "Mehni.Numbers";
    private const string NumbersWindowTypeName = "Numbers.MainTabWindow_Numbers";
    private const string AutoHuntModTypeName = "AutoHunt.AutoHuntMod";
    private const string WildAnimalsTableDefName = "Numbers_WildAnimals";

    private const float IconSize = 28f;
    private const float CheckboxHeight = 30f;
    private const float Pad = 6f;
    private const float Gap = 6f;

    private static readonly Texture2D SettingsIcon = ContentFinder<Texture2D>.Get("UI/AutoHuntNumbersPatch/Settings");

    private static Type? numbersWindowType;
    private static FieldInfo? pawnTableDefField;
    private static FieldInfo? autoHuntSettingsField;
    private static FieldInfo? autoHuntEnabledField;
    private static Mod? autoHuntMod;

    public static bool Prepare()
    {
        if (ModLister.GetActiveModWithIdentifier(AutoHuntPackageId, true) == null
            || ModLister.GetActiveModWithIdentifier(NumbersPackageId, true) == null)
            return false;

        var numbersWindow = AccessTools.TypeByName(NumbersWindowTypeName);
        var autoHuntModType = AccessTools.TypeByName(AutoHuntModTypeName);
        var autoHuntSettings = AccessTools.TypeByName("AutoHunt.Settings");
        if (numbersWindow == null || autoHuntModType == null || autoHuntSettings == null)
        {
            Log.Warning("[Auto Hunt Numbers Patch] Auto Hunt and Numbers are both active, but their expected types were not found.");
            return false;
        }

        numbersWindowType = numbersWindow;
        pawnTableDefField = AccessTools.Field(numbersWindow, "pawnTableDef");
        autoHuntSettingsField = AccessTools.Field(autoHuntModType, "settings");
        autoHuntEnabledField = AccessTools.Field(autoHuntSettings, "enabled");
        if (pawnTableDefField == null || autoHuntSettingsField == null || autoHuntEnabledField == null)
        {
            Log.Warning("[Auto Hunt Numbers Patch] Auto Hunt and Numbers are both active, but their expected fields were not found.");
            return false;
        }

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

        if (numbersWindowType == null || !numbersWindowType.IsInstanceOfType(__instance))
            return;

        var tableDef = pawnTableDefField?.GetValue(__instance) as Def;
        if (tableDef == null || tableDef.defName != WildAnimalsTableDefName)
            return;

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
        var rowHeight = Mathf.Max(IconSize, CheckboxHeight);
        var y = rect.yMax - rowHeight - Pad;
        var cogX = rect.xMax - IconSize - Pad;
        var checkX = cogX - Gap - checkboxWidth;

        Widgets.CheckboxLabeled(
            new Rect(checkX, y + (rowHeight - CheckboxHeight) / 2f, checkboxWidth, CheckboxHeight),
            label,
            ref enabled,
            false,
            null,
            null,
            true,
            false);

        Text.Anchor = anchor;
        Text.Font = font;

        if (SettingsIcon != null)
        {
            var settingsRect = new Rect(cogX, y + (rowHeight - IconSize) / 2f, IconSize, IconSize);
            if (Widgets.ButtonImage(settingsRect, SettingsIcon))
            {
                var mod = GetAutoHuntMod();
                if (mod != null)
                    Find.WindowStack.Add(new Dialog_ModSettings(mod));
            }

            TooltipHandler.TipRegion(settingsRect, "AutoHuntNumbersPatch_SettingsTip".Translate());
        }

        if (enabled == before)
            return;

        autoHuntEnabledField.SetValue(settings, enabled);
        if (settings is ModSettings modSettings)
            modSettings.Write();
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
