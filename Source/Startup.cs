using HarmonyLib;
using Verse;

namespace AutoHuntTweaks;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        new Harmony("cruesoe.autohuntnumberspatch").PatchAll();
    }
}
