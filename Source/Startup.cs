using HarmonyLib;
using Verse;

namespace NumbersCompatibilityPatch;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        new Harmony("cruesoe.autohuntnumberspatch").PatchAll();
    }
}
