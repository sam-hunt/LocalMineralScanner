using UnityEngine;
using Verse;

namespace LocalMineralScanner;

// Mod entry point: wires up settings and nothing else (no Harmony, see CLAUDE.md). The
// constructor runs while mod assemblies load, before any def exists, so settings that
// override def fields are applied later: once from ModInit (after defs load) and again
// whenever the settings window closes (WriteSettings, called from
// Dialog_ModSettings.PreClose on every close path).
public class LocalMineralScannerMod : Mod
{
    public static LocalMineralScannerSettings Settings { get; private set; }

    public LocalMineralScannerMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LocalMineralScannerSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoWindowContents(inRect);
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        Settings.Apply();
    }

    public override string SettingsCategory() => "LocalMineralScanner_SettingsCategory".Translate();
}
