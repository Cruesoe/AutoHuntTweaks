# Branding

This mod does not own its preview image.

The design, the rules about what may and may not appear on a preview, and the tool that
renders it all live in **`..\_Branding`**. Read
[`_Branding\README.md`](../_Branding/README.md) before changing anything visual here.

`About\Preview.png` is **generated output**. Do not hand-edit it and do not replace it
with a one-off image — the next render overwrites it.

To change this mod's preview, edit its entry in `_Branding\branding.json`, then:

```powershell
cd ..\_Branding
.\Tools\Build-Previews.ps1 -Only autohunt -Deploy
```

## This one is an overlay, not a family tile

Auto Hunt Tweaks patches someone else's mod, so its preview has to say *which* mod it
modifies. It is declared under `overlays` rather than `mods`: the parent mod's own
preview is kept whole and our branding goes on as a band across the bottom — accent
rule, then the word in the family face.

The base image is **the preview from Auto Hunt by Snues**, stored at
`_Branding\Bases\auto-hunt.png`. It is copied into `_Branding` on purpose, so a render
never depends on which mods are installed on the machine doing the rendering, and so a
parent-mod update cannot silently change our art. If Auto Hunt's preview changes and you
want to track it, replace that file deliberately and re-render.
