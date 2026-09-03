using UnityEngine;
using Verse;

namespace NumbersCompatibilityPatch;

public class NumbersCompatibilityPatchSettings : ModSettings
{
    public bool ForceAutoHuntBottom;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ForceAutoHuntBottom, "forceAutoHuntBottom", false);
    }
}

public class NumbersCompatibilityPatchMod : Mod
{
    public static NumbersCompatibilityPatchSettings Settings = null!;

    public NumbersCompatibilityPatchMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<NumbersCompatibilityPatchSettings>();
    }

    public override string SettingsCategory()
    {
        return "NumbersCompatibilityPatch_Title".Translate();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled(
            "NumbersCompatibilityPatch_ForceBottomSetting".Translate(),
            ref Settings.ForceAutoHuntBottom,
            "NumbersCompatibilityPatch_ForceBottomSettingTip".Translate());
        listing.End();
    }
}
