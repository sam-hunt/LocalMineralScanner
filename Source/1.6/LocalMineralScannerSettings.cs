using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LocalMineralScanner;

// Mod settings: the scanner's tunable def values, the window that edits them, and Apply(),
// which writes them onto the live def. Every setting is a def or comp-props field the game
// reads live (decompile-verified, 1.6), so Apply() runs at startup (ModInit) and on every
// settings-window close (LocalMineralScannerMod.WriteSettings) and nothing needs a restart:
//  - scanFindMtbDays / scanFindGuaranteedDays: read from Props on each CompScanner.Used call.
//  - Mass: SetStatBaseValue rewrites the StatModifier in place; Mass is a mutable stat
//    (it has parts), so StatWorker recomputes it on every GetStatValue.
//  - costList: Frame/Blueprint_Build read CostListAdjusted live, but CostListCalculator
//    caches per def, so the list is rebuilt and the cache Reset() after each write.
//  - minifiedDef: ThingDef.Minifiable is `minifiedDef != null`, read live by the Uninstall
//    gizmo and MinifyUtility. The install blueprint def is generated once at load from the
//    XML (minifiable), so it exists whichever way the toggle goes and minified scanners
//    already in a save stay installable with the toggle off.
//  - canBeUsedUnderRoof only feeds vanilla's Alert_CannotBeUsedRoofed, whose def list is
//    built lazily once per play session; a mid-game flip reaches it on the next game load.
//    The rule itself lives in PlaceWorker_NotUnderRoofIfRequired and the comp's CanUseNow,
//    both of which read the setting directly.
// The XML carries the same values as the *Default consts below (they are what a player who
// never opens the settings gets, and what Restore defaults returns to); the XML copies are
// documentation, since Apply() overwrites them at startup. Keep the two in step.
//
// Every settings-window string is localized through .Translate() against Keyed/
// LocalMineralScanner.xml, except where vanilla already has the exact string (its Keyed key
// or a def label is reused rather than shipping a copy translators would do twice).
//
// To add a setting: declare the field with its default as the initializer and a const for
// the default, Scribe_Values.Look it in ExposeData with the same default, restore it in
// ResetToDefaults, write it onto the def in Apply, add its Keyed strings, draw it in the
// matching Draw*Section. The field / ExposeData / ResetToDefaults defaults must agree.
public class LocalMineralScannerSettings : ModSettings
{
    public const bool RequireUnroofedDefault = true;
    public const bool MinifiableDefault = true;
    public const float FindMtbDaysDefault = 4f;
    public const float FindGuaranteedDaysDefault = 8f;
    public const int SteelCostDefault = 120;
    public const int ComponentCostDefault = 4;
    public const int AdvancedComponentCostDefault = 1;
    public const float MassDefault = 40f;

    public bool requireUnroofed = RequireUnroofedDefault;
    public bool minifiable = MinifiableDefault;
    public float findMtbDays = FindMtbDaysDefault;
    public float findGuaranteedDays = FindGuaranteedDaysDefault;
    public int steelCost = SteelCostDefault;
    public int componentCost = ComponentCostDefault;
    public int advancedComponentCost = AdvancedComponentCostDefault;
    public float mass = MassDefault;

    // Trailing space each section leaves below itself.
    private const float SectionGap = 18f;

    // Presentation state for the scroll view, deliberately not scribed.
    private Vector2 scrollPosition;
    private float contentHeight;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref requireUnroofed, "requireUnroofed", RequireUnroofedDefault);
        Scribe_Values.Look(ref minifiable, "minifiable", MinifiableDefault);
        Scribe_Values.Look(ref findMtbDays, "findMtbDays", FindMtbDaysDefault);
        Scribe_Values.Look(ref findGuaranteedDays, "findGuaranteedDays", FindGuaranteedDaysDefault);
        Scribe_Values.Look(ref steelCost, "steelCost", SteelCostDefault);
        Scribe_Values.Look(ref componentCost, "componentCost", ComponentCostDefault);
        Scribe_Values.Look(ref advancedComponentCost, "advancedComponentCost", AdvancedComponentCostDefault);
        Scribe_Values.Look(ref mass, "mass", MassDefault);
    }

    public void ResetToDefaults()
    {
        requireUnroofed = RequireUnroofedDefault;
        minifiable = MinifiableDefault;
        findMtbDays = FindMtbDaysDefault;
        findGuaranteedDays = FindGuaranteedDaysDefault;
        steelCost = SteelCostDefault;
        componentCost = ComponentCostDefault;
        advancedComponentCost = AdvancedComponentCostDefault;
        mass = MassDefault;
    }

    // Writes the settings onto the live scanner def and its comp properties (see the header
    // for why each write is enough). Idempotent: it runs at startup and on every
    // settings-window close.
    public void Apply()
    {
        ThingDef def = LocalMineralScannerDefOf.LocalMineralScanner;
        CompProperties_LocalMineralScanner props = def.GetCompProperties<CompProperties_LocalMineralScanner>();

        def.canBeUsedUnderRoof = !requireUnroofed;
        def.minifiedDef = minifiable ? ThingDefOf.MinifiedThing : null;
        props.scanFindMtbDays = findMtbDays;
        props.scanFindGuaranteedDays = findGuaranteedDays;
        def.SetStatBaseValue(StatDefOf.Mass, mass);

        // Zero-count entries are dropped rather than written: construction treats a 0 count as
        // already delivered, but the build menu's cost readout and the info card would still
        // list "0 steel". An empty list is fine (a no-cost blueprint goes straight to a frame).
        def.costList = new List<ThingDefCountClass>();
        AddCost(def.costList, ThingDefOf.Steel, steelCost);
        AddCost(def.costList, ThingDefOf.ComponentIndustrial, componentCost);
        AddCost(def.costList, ThingDefOf.ComponentSpacer, advancedComponentCost);
        CostListCalculator.Reset();
    }

    private static void AddCost(List<ThingDefCountClass> costList, ThingDef resource, int count)
    {
        if (count > 0)
        {
            costList.Add(new ThingDefCountClass(resource, count));
        }
    }

    public void DoWindowContents(Rect inRect)
    {
        const float buttonHeight = 30f;
        const float buttonGap = 10f;
        const float buttonWidth = 200f;
        const float scrollBarWidth = 16f;

        // Reserve the bottom strip for the pinned reset button; the scroll view gets everything
        // above it. Content is the view minus the scrollbar gutter wide, and the content or the
        // view tall, whichever is larger, so the scrollbar appears only once the rows overflow.
        // contentHeight is 0 on the first frame and measured off the listing for every frame after.
        Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - buttonHeight - buttonGap);
        Rect buttonRect = new Rect(inRect.x, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
        float innerWidth = viewRect.width - scrollBarWidth;
        Rect innerRect = new Rect(0f, 0f, innerWidth, Mathf.Max(contentHeight, viewRect.height));

        Widgets.BeginScrollView(viewRect, ref scrollPosition, innerRect);

        Listing_Standard listing = new Listing_Standard();
        // Tall scratch rect so the listing never clamps its own height; the real one comes back
        // below via CurHeight.
        listing.Begin(new Rect(0f, 0f, innerWidth - 8f, 99999f));
        GameFont prevFont = Text.Font;

        listing.Gap();

        DrawPlacementSection(listing);
        DrawScanningSection(listing);
        DrawCostSection(listing);

        Text.Font = prevFont;
        contentHeight = listing.CurHeight;
        listing.End();
        Widgets.EndScrollView();

        if (Widgets.ButtonText(buttonRect, "RestoreToDefaultSettings".Translate()))
        {
            ResetToDefaults();
        }
    }

    // Roof rule, uninstalling and mass: everything about where the scanner can go.
    private void DrawPlacementSection(Listing_Standard listing)
    {
        SectionHeader(listing, "LocalMineralScanner_SettingsPlacement".Translate());

        listing.CheckboxLabeled(
            "LocalMineralScanner_RequireUnroofed".Translate(),
            ref requireUnroofed,
            "LocalMineralScanner_RequireUnroofedDesc".Translate());
        listing.CheckboxLabeled(
            "LocalMineralScanner_Minifiable".Translate(),
            ref minifiable,
            "LocalMineralScanner_MinifiableDesc".Translate());

        // Rendered as the info card renders the stat ("Mass: 40 kg"), from the stat's own
        // label and format string.
        mass = SliderRow(listing,
            StatDefOf.Mass.LabelCap + ": " + StatDefOf.Mass.ValueToString(mass),
            "LocalMineralScanner_MassDesc".Translate(),
            mass, MassDefault, min: 5f, max: 200f, step: 5f);

        listing.Gap(SectionGap);
    }

    // The two CompScanner timings, labelled with the same vanilla and mod Keyed strings the
    // inspect pane uses for them.
    private void DrawScanningSection(Listing_Standard listing)
    {
        SectionHeader(listing, "LocalMineralScanner_SettingsScanning".Translate());

        findMtbDays = SliderRow(listing,
            "ScanAverageInterval".Translate() + ": " + "PeriodDays".Translate(findMtbDays.ToString("0.#")),
            "LocalMineralScanner_FindMtbDaysDesc".Translate(),
            findMtbDays, FindMtbDaysDefault, min: 0.5f, max: 30f, step: 0.5f);
        findGuaranteedDays = SliderRow(listing,
            "LocalMineralScanner_GuaranteedFindWithin".Translate() + ": " + "PeriodDays".Translate(findGuaranteedDays.ToString("0.#")),
            "LocalMineralScanner_FindGuaranteedDaysDesc".Translate(),
            findGuaranteedDays, FindGuaranteedDaysDefault, min: 0.5f, max: 60f, step: 0.5f);

        listing.Gap(SectionGap);
    }

    // One row per costList resource, labelled from the resource's own def label.
    private void DrawCostSection(Listing_Standard listing)
    {
        SectionHeader(listing, "Cost".Translate().CapitalizeFirst());

        steelCost = CostRow(listing, ThingDefOf.Steel, steelCost, SteelCostDefault, max: 400, step: 10);
        componentCost = CostRow(listing, ThingDefOf.ComponentIndustrial, componentCost, ComponentCostDefault, max: 20, step: 1);
        advancedComponentCost = CostRow(listing, ThingDefOf.ComponentSpacer, advancedComponentCost, AdvancedComponentCostDefault, max: 10, step: 1);

        listing.Gap(SectionGap);
    }

    private static int CostRow(Listing_Standard listing, ThingDef resource, int value, int defaultValue, int max, int step)
    {
        float result = SliderRow(listing,
            "LocalMineralScanner_ResourceCost".Translate(resource.label, value).CapitalizeFirst(),
            "LocalMineralScanner_ResourceCostDesc".Translate(resource.label),
            value, defaultValue, min: 0f, max: max, step: step);
        return Mathf.RoundToInt(result);
    }

    // Top-level section heading (medium font).
    private static void SectionHeader(Listing_Standard listing, string label)
    {
        Text.Font = GameFont.Medium;
        listing.Label(label);
        Text.Font = GameFont.Small;
        listing.Gap(6f);
    }

    // One labelled slider row: the label (already carrying the current value) with the
    // description as a hover tooltip only, a " (default)" suffix when the value is the default,
    // and the returned value snapped to `step` measured from `min`. Mathf.Approximately rather
    // than ==, because snapping off a non-zero `min` does not reproduce the default's exact
    // float and an exact compare would silently never show the suffix on those rows.
    private static float SliderRow(Listing_Standard listing, string label, string tooltip,
        float value, float defaultValue, float min, float max, float step)
    {
        if (Mathf.Approximately(value, defaultValue))
        {
            label += "LocalMineralScanner_DefaultSuffix".Translate();
        }
        listing.Label(label, tooltip: tooltip);
        return Mathf.Round((listing.Slider(value, min, max) - min) / step) * step + min;
    }
}
