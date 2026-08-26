using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NumbersCompatibilityPatch;

internal static class NumbersWildlifeTable
{
    internal const string PackageId = "Mehni.Numbers";
    internal const string WindowTypeName = "Numbers.MainTabWindow_Numbers";
    internal const string TableDefName = "Numbers_WildAnimals";
    internal const string LogPrefix = "[Numbers Compatibility Patch]";

    internal static Type? WindowType;
    internal static FieldInfo? PawnTableDefField;

    internal static bool TryInit()
    {
        if (WindowType != null && PawnTableDefField != null)
            return true;

        if (ModLister.GetActiveModWithIdentifier(PackageId, true) == null)
            return false;

        var numbersWindow = AccessTools.TypeByName(WindowTypeName);
        if (numbersWindow == null)
        {
            Log.Warning($"{LogPrefix} Numbers is active, but {WindowTypeName} was not found.");
            return false;
        }

        var pawnTableDefField = AccessTools.Field(numbersWindow, "pawnTableDef");
        if (pawnTableDefField == null)
        {
            Log.Warning($"{LogPrefix} Numbers is active, but pawnTableDef was not found.");
            return false;
        }

        WindowType = numbersWindow;
        PawnTableDefField = pawnTableDefField;
        return true;
    }

    internal static bool IsOpen(MainTabWindow_PawnTable instance)
    {
        if (WindowType == null || PawnTableDefField == null)
            return false;

        if (!WindowType.IsInstanceOfType(instance))
            return false;

        var tableDef = PawnTableDefField.GetValue(instance) as Def;
        return tableDef != null && tableDef.defName == TableDefName;
    }
}
