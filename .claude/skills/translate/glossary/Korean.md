# Korean glossary — Local Mineral Scanner

Machine-assisted, 2026-09-09, pending native review. Family-wide Korean
mechanics (josa markers, register, dash policy) live in `l10n/languages/Korean.md`
— not repeated here.

| English | Korean | Grounding |
|---|---|---|
| local mineral scanner (title, building label) | 근거리 광물 탐색기 | 광물 탐색기 from `LongRangeMineralScanner.label`/`GroundPenetratingScanner.label`; 근거리 (short/close-range) is vanilla's own antonym of 장거리 (long-range), attested in `AccuracyShort.label` and the `Mech_Lancer`/`Mech_Pikeman` descriptions |
| tuned to (gizmo verb) | 조정 | reuses Core `CommandSelectMineralToScanFor` verbatim |
| twin consoles / two operators | 조작대 | coined (no vanilla "console" furniture term for this slot); loanword 콘솔 was rejected as less consistent with vanilla's native-word register |
| undiscovered deposit | 미발견 {0} 매장지 | 매장지 ("deposit") from `DeepDrilling.description`'s "자원 매장지"; 미발견 is a transparent negation of 발견 (find/discover), the verb vanilla uses throughout scanner/quest text. No vanilla string names "undiscovered" directly — flagged for review |
| in an undiscovered (fogged) area | 안개에 덮인 지역 | coined, ties directly to the fog-of-war mechanic rather than reusing 미발견 a second way in the same sentence |
| guaranteed find | 확실한 발견 | verbatim from Core `ScanningProgressToGuaranteedFind` ("확실한 발견으로 진행") |
| combined scanning speed | 합산 탐색 속도 (조작자 {0}명) | 탐색 속도 from Core `UserScanAbility`/`ResearchSpeed.description`; 합산 (summed) and 조작자 (operator) are coined |
| operate local mineral scanner (WorkGiver) / scan at / scanning at | 근거리 광물 탐색기 조작 / 탐색 / 에서 탐색 | mirrors Core `LongRangeScan.label`/`.verb`/`.gerund` exactly, substituting 근거리 for 장거리 |
| reportString: scanning at TargetA. | TargetA에서 자원 탐색 중 | verbatim match of Core `OperateScanner.reportString`'s Korean value — same mechanic, same phrasing |
| can be uninstalled / uninstalled (Minifiable) | 포장 가능 / 포장된 | 포장 from Core `DesignatorUninstall` |
| deconstructed | 해체 | Core `DesignatorDeconstruct` |
| caravan | 상단 | Core, per `l10n/languages/Korean.md`'s grounded table |
| resource labels ({0} in cost rows) | 강철/부품/고급 부품/금/은/플라스틸/우라늄/비취옥 | Core `Steel`/`ComponentIndustrial`/`ComponentSpacer`/`Gold`/`Silver`/`Plasteel`/`Uranium`/`Jade` labels, verbatim |

## Pending native review

- 조작대 for "console" (twin-console mechanic) — no vanilla precedent found for this exact
  furniture-panel sense; a native speaker may prefer a loanword or a different compound.
- 미발견 vs. 미확인 for "undiscovered" — chose 미발견 (transparent negation of 발견) over
  미확인 (lit. "unconfirmed/unidentified") since neither is vanilla-attested and 미발견 pairs
  more directly with vanilla's own 발견/감지 vocabulary; flag if a native pass disagrees.
- The ThingDef description's "lateral sensor unit" (측면 탐지 장치) and "twin consoles" (두 개의
  조작대) are both mod-coined phrasing with no close vanilla analogue; check for naturalness.
