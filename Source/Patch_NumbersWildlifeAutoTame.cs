using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AutoHuntNumbersPatch;

[HarmonyPatch]
public static class Patch_NumbersWildlifeAutoTame
{
    private const string AutoTamePackageId = "ceramic.autotame";
    private const string AutoTameDialogTypeName = "AutoTame.Dialog_AutoTameSettings";

    // Match Auto Tame's own Wildlife-tab button: left side, 200x32, 40px up from the bottom.
    private const float ButtonWidth = 200f;
    private const float ButtonHeight = 32f;
    private const float ButtonOffsetY = 40f;

    private static Type? autoTameDialogType;

    public static bool Prepare()
    {
        if (ModLister.GetActiveModWithIdentifier(AutoTamePackageId, true) == null
            || !NumbersWildlifeTable.TryInit())
            return false;

        autoTameDialogType = AccessTools.TypeByName(AutoTameDialogTypeName);
        if (autoTameDialogType == null)
        {
            Log.Warning($"{NumbersWildlifeTable.LogPrefix} Auto Tame is active, but {AutoTameDialogTypeName} was not found.");
            return false;
        }

        return true;
    }

    public static MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(MainTabWindow_PawnTable), nameof(MainTabWindow_PawnTable.DoWindowContents));
    }

    public static void Postfix(MainTabWindow_PawnTable __instance, Rect rect)
    {
        if (Event.current.type == EventType.Layout)
            return;

        if (!NumbersWildlifeTable.IsOpen(__instance) || autoTameDialogType == null)
            return;

        var buttonRect = new Rect(rect.x, rect.yMax - ButtonOffsetY, ButtonWidth, ButtonHeight);
        TooltipHandler.TipRegion(buttonRect, "NumbersCompatibilityPatch_AutoTameTip".Translate());
        if (!Widgets.ButtonText(buttonRect, "Auto-tame settings"))
            return;

        var dialog = Activator.CreateInstance(autoTameDialogType) as Window;
        if (dialog == null)
        {
            Log.Warning($"{NumbersWildlifeTable.LogPrefix} Auto Tame's settings dialog could not be opened.");
            return;
        }

        Find.WindowStack.Add(dialog);
    }
}
