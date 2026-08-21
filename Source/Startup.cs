using HarmonyLib;
using Verse;

namespace AutoHuntNumbersPatch;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        new Harmony("cruesoe.autohuntnumberspatch").PatchAll();
    }
}
