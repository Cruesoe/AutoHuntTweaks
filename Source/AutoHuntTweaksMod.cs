using UnityEngine;
using Verse;

namespace AutoHuntTweaks;

public class AutoHuntTweaksSettings : ModSettings
{
    public bool ForceAutoHuntBottom;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ForceAutoHuntBottom, "forceAutoHuntBottom", false);
    }
}

public class AutoHuntTweaksMod : Mod
{
    public static AutoHuntTweaksSettings Settings = null!;

    public AutoHuntTweaksMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<AutoHuntTweaksSettings>();
    }

    public override string SettingsCategory()
    {
        return "AutoHuntTweaks_Title".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled(
            "AutoHuntTweaks_ForceBottomSetting".Translate(),
            ref Settings.ForceAutoHuntBottom,
            "AutoHuntTweaks_ForceBottomSettingTip".Translate());
        listing.End();
    }
}
