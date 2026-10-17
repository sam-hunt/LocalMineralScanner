# TODOs

- Check whether the Vanilla Quests Expanded series defines its own cryptosleep casket defs for
  its special characters. Caskets on the vanilla `Building_AncientCryptosleepCasket` class are
  already picked up by the ancient-casket target; any on a custom class would need adding to
  `MapComponent_FoggedMinerals.IsAncientCasket` (and the same wake-path check as
  `Docs/design-research.md`, "Ancient cryptosleep caskets").
