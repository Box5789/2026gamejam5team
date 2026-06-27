# Agent Handoff

## Current Goal

Keep the kitchen scene data-driven and scene-wired: ingredient sources and rice brush tuning come from the local ingredient CSV + prefabs, ingredient source rows center themselves from CSV counts, the hand/arm cursor is prefab-backed, and `KitchenTableNavigator` moves according to `MovingTablesRoot` child table transforms instead of duplicated spacing/count numbers.

## 2026-06-27 KitchenHandCursor Root Child Arm Rig

- Refactored `KitchenHandCursor` to the user-confirmed root-child rig:
  - `KitchenHandCursor`
  - `ArmPivot`: blue reference point, camera viewport anchor + `armPivotWorldOffset`.
  - `HandPivot`: red reference point, follows mouse/touch pointer world position.
  - `Arm`: direct child of `KitchenHandCursor`, owns shared hand/arm visual pose.
  - `Arm/HandVisual`: actual hand sprite renderer.
  - `Arm/ArmVisual`: actual arm sprite renderer.
- Runtime contract:
  - `HandPivot.position` is the pointer world position.
  - `ArmPivot.position` is the viewport anchor world position plus offset.
  - `Arm.rotation` looks from `ArmPivot` toward `HandPivot`, plus `armAngleOffset`.
  - `Arm.position` is `HandPivot.position + rotated armOffsetFromHandPivot`.
  - `Arm.localScale`, `HandVisual` local transform, and `ArmVisual` local transform are all Inspector-tunable.
- Kept existing behavior:
  - Mouse / first active touch tracking.
  - Pressed hand sprite switching, with default-sprite fallback.
  - Edit Mode preview via `OnValidate`.
  - Outside-camera hide/restore behavior.
- Updated `Assets/Prefabs/Kitchen/KitchenHandCursor.prefab`.
  - Preserved root and component fileIDs so the scene `targetCamera` prefab override stays connected.
  - Activated the scene instance in `Assets/Scenes/kitchen.unity`; it was previously serialized with `m_IsActive: 0`.
- Updated tests:
  - `KitchenHandCursorTests` now covers `HandPivot`, `ArmPivot`, `Arm` rotation/offset, visual local tuning, edit preview, pressed-sprite fallback, and outside-camera hide/restore.
  - `KitchenSceneWiringTests` now checks `armPivot`, `handPivot`, `armRoot`, `handVisualRoot`, `armVisualRoot`, and `handRenderer`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Prefab structure grep confirmed `ArmPivot`, `HandPivot`, `Arm`, `HandVisual`, and `ArmVisual` are present and wired.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.
- User tuning note:
  - The implementation exposes the rig values; final natural-looking position/rotation/scale values are intentionally left for the user to tune in Inspector.

## 2026-06-27 KitchenHandCursor Edit Mode Preview

- Added edit-mode preview controls to `KitchenHandCursor`.
  - `previewInEditMode`
  - `editModePreviewViewportPosition`
  - `editModePreviewPressed`
- `OnValidate` now applies a preview pose while not in Play Mode.
  - Uses the serialized `targetCamera`; if no camera is wired, it updates only the hand sprite and leaves transforms alone.
  - Preview pose applies immediately without follow smoothing so Inspector changes are visible in Scene View.
  - Runtime mouse/touch tracking still uses the existing Play Mode `LateUpdate` path.
- Updated `KitchenHandCursor.prefab`.
  - `previewInEditMode = true`
  - `editModePreviewViewportPosition = (0.5, 0.5)`
  - `editModePreviewPressed = false`
- Tests:
  - Extended `KitchenHandCursorTests` for preview viewport placement, arm rotation recalculation after serialized value changes, pressed preview sprite, and pressed-sprite fallback.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Kitchen Hand/Arm Cursor Visual

- Added `KitchenHandCursor`.
  - Tracks the current pointer from mouse or the first active touch.
  - Moves `handRoot` to the pointer world position plus serialized `handOffset`.
  - Places `armRoot` at serialized camera viewport anchor `armAnchorViewportPosition` and rotates it toward the hand.
  - Swaps to `pressedHandSprite` while mouse/touch is active; if no pressed sprite is provided, it keeps the default hand sprite.
  - `hideWhenOutsideCamera` hides both hand and arm when the pointer leaves the camera pixel rect.
- Added `Assets/Prefabs/Kitchen/KitchenHandCursor.prefab`.
  - Root has `KitchenHandCursor`.
  - Child `ArmRoot` and `HandRoot` use `SpriteRenderer` + `KitchenPlaceholderSprite` with skin-tone placeholders.
  - No colliders are present, so the cursor stays visual-only and does not block existing click/drag/rice-paint input.
  - `ArmRoot` sorting order is 299 and `HandRoot` sorting order is 300, above the drag preview.
- Updated `Assets/Scenes/kitchen.unity`.
  - Added a connected `KitchenHandCursor.prefab` instance.
  - Wired its `targetCamera` to `Main Camera`.
- Tests:
  - Added `KitchenHandCursorTests` for screen-to-world hand placement, viewport arm anchor placement, arm rotation, pressed sprite swap, pressed-sprite fallback, and camera-outside hide/restore.
  - Extended `KitchenSceneWiringTests` to verify the hand cursor prefab instance and its serialized references.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.
- User validation:
  - The user will verify in Play Mode whether hand/arm position, rotation, offset, and sprite timing look natural.
  - Replace the placeholder arm/hand sprites in the prefab when art is ready; keep the arm sprite pivot near the shoulder/start point, and tune `armAngleOffset` if the sprite's forward direction is not +X.

## 2026-06-27 Kitchen Ingredient Source Row Centering

- Changed `KitchenIngredientTablePopulator` source placement from left-start layout to row-centered layout.
  - Serialized `sourceLocalStart` became `sourceRowCenter`.
  - Each category is grouped before spawning so placement receives both `index` and category `totalCount`.
  - Each row calculates its own `rowItemCount`, so short rows and final partial rows center around `sourceRowCenter.x`.
- Updated `Assets/Scenes/kitchen.unity`.
  - `sourceRowCenter` is `{x: 0, y: 3.28, z: -0.2}`.
  - Existing `sourceSpacing`, `itemsPerRow`, and `sourceLocalScale` values are unchanged.
- Updated `OnDrawGizmos`.
  - Scene View source guides now reuse the same placement function as runtime spawning.
  - Edit-mode gizmos read the local ingredient CSV when possible, so table guides reflect current category counts.
  - Camera frame gizmos still draw from the serialized target camera.
- Tests:
  - `KitchenIngredientTablePopulatorTests` now covers 1, 2, 10, 11, and 12 seaweed rows so full rows and partial second rows stay centered.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "sourceLocalStart|new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Rice Brush Image Pattern Settings in Ingredient Sheet Rows

- Chose the designer-friendly data contract requested by the user: no separate brush sheet. Brush settings live on the same `김` tab / `ingredients.csv` row as each rice ingredient.
- Current local ingredient CSV and Google Sheet header:
  - `Index`, `이름`, `분류`, `가격`, `이미지`, `필수여부`, `브러시ID`, `브러시이미지`, `브러시반경`, `입자수`, `흩뿌림반경`, `최소크기`, `최대크기`, `스탬프간격`
- Brush image paths are `Resources` relative paths without extensions, for example `Kitchen/Brushes/white-rice`.
- Added example texture asset:
  - `Assets/Resources/Kitchen/Brushes/white-rice.png`
- Local rice row `r16` uses `white-rice`, `Kitchen/Brushes/white-rice`, `0.18`, `4`, `0.22`, `0.75`, `1.25`, `0.04`.
- Other local rice rows keep their ids and numeric tuning but leave `브러시이미지` blank until matching texture assets exist.
- Code changes:
  - `KitchenIngredientCatalog` now parses `브러시이미지` as optional data with Korean/English aliases.
  - `KitchenIngredientCatalogItem` resolves rice brush image paths to `Texture2D` through `KitchenRiceBrushTextureLoader` and converts rice rows into full `SpreadBrushDefinition` instances.
  - `KitchenIngredientDefinition` can carry the parsed `SpreadBrushDefinition` alongside existing compatibility `RiceBrushId`.
  - `KitchenRicePaintBridge` now applies the full parsed brush definition when a rice source is selected.
  - `KitchenRiceBrushTextureLoader` loads `Texture2D` assets from `Resources`, normalizes `Assets/Resources/...png` to relative keys, warns on missing paths, and creates readable copies when needed.
  - `SpreadBrushDefinition` exposes default constants and a full constructor for sheet-driven values.
  - `SpreadableSurface` gained `SelectOrCreateBrush(SpreadBrushDefinition)` while preserving the old id/display/tint overload.
- Tests:
  - `KitchenIngredientCatalogTests` now verifies image path parsing, resolver-based texture wiring, path normalization, missing-path fallback, and blank-column defaults.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Static grep confirmed the old color-column names and tint parser symbols no longer remain in code/data.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.
- Follow-up:
  - Google Sheet `DataSet` / `김` tab was updated after this code pass:
    - `김!G1:N1` now contains `브러시ID`, `브러시이미지`, `브러시반경`, `입자수`, `흩뿌림반경`, `최소크기`, `최대크기`, `스탬프간격`.
    - First rice row `김!G17:N17` (`r16`, `흰쌀밥`) now contains the example `white-rice`, `Kitchen/Brushes/white-rice`, `0.18`, `4`, `0.22`, `0.75`, `1.25`, `0.04`.
    - Connector verification re-read `김!G1:N1` and `김!A17:N17` successfully.
  - Only the first rice row has a Google Sheet brush image example right now. Fill the other rice rows with matching `Resources` paths after adding texture assets.

## 2026-06-27 KitchenTableNavigator MovingTablesRoot-Based Movement

- Changed `KitchenTableNavigator` so table pages are derived from `movingTablesRoot` direct children.
  - `TableCount` now reads `movingTablesRoot.childCount`.
  - Slide targets are calculated from the selected child local position relative to the first child local position.
  - Target z stays fixed at the original `MovingTablesRoot` z.
- Kept the old `Configure(Transform, Button, float, int)` overload as compatibility glue, but it ignores spacing/count and delegates to the new root-based `Configure(Transform, Button)`.
- Added `ricePaintingTableRoot` to replace hard dependence on `ricePaintingTableIndex`.
  - Current scene wires it to `Rice Table`.
  - If the reference is missing, the old index fallback remains.
- Updated `Assets/Scenes/kitchen.unity`.
  - `KitchenTableNavigator` now serializes `ricePaintingTableRoot: Rice Table`.
  - Removed serialized `tableSpacing` and `tableCount` values from the navigator component.
- Expanded tests.
  - `KitchenTableNavigatorTests` now covers uniform child spacing, irregular child spacing, first-child offset, child-count clamping, and rice-painting root selection.
  - `KitchenSceneWiringTests` now verifies `ricePaintingTableRoot` is wired and is a direct child of `MovingTablesRoot`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Scene grep confirmed `movingTablesRoot` and `ricePaintingTableRoot` references are present and old serialized `tableSpacing/tableCount` entries are absent.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Local Ingredient CSV + Sheet Download

- Added local ingredient master data at `Assets/StreamingAssets/Kitchen/ingredients.csv`.
  - Header contract now includes `Index`, `이름`, `분류`, `가격`, `이미지`, `필수여부`, plus optional rice brush columns documented in the newer rice brush section above.
  - Seeded from spreadsheet `13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI`, tab `김` / `gid=0`.
  - Current local file has 115 data rows plus the header. Price/image/required columns are preserved but currently empty because the sheet values are empty.
- Added shared CSV parsing under `Assets/Scripts/Data/CsvTableParser.cs` and reused it from `GoogleSheetOrderLoader` so the existing order CSV path no longer owns a private parser.
- Added shared ingredient name mapping under `Assets/Scripts/Data/IngredientTypeMapper.cs`.
  - `IngredientType.GenericFilling` was added as the compatibility fallback for unknown filling rows.
  - Known names still map to existing enum values where possible.
- Added kitchen ingredient catalog model:
  - `KitchenIngredientCatalog`
  - `KitchenIngredientCatalogItem`
  - `KitchenIngredientCategory.Sauce`
  - `KitchenIngredientDefinition` can now carry a `dragPrefab` from generated definitions.
- Added `KitchenIngredientTablePopulator`.
  - Reads `Kitchen/ingredients.csv` from `StreamingAssets`.
  - Instantiates `KitchenIngredientSource.prefab` under the serialized seaweed/rice/filling table roots.
  - Skips `소스` rows for source spawning while keeping them in the catalog.
  - Rice catalog definitions can now carry per-row `SpreadBrushDefinition` values from the ingredient sheet.
- Updated `Assets/Scenes/kitchen.unity`.
  - Removed the three edit-time ingredient source prefab instances from the table roots.
  - Added `KitchenIngredientTablePopulator` to `MovingTablesRoot` with serialized references to source prefab, drag preview prefab, controller, drop zone, camera, and table roots.
  - UI objects/buttons/Canvas were left as scene objects per user request.
- Added Editor-only menu `Tools/Kimbap/Download Ingredient Sheet`.
  - Downloads `https://docs.google.com/spreadsheets/d/13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI/export?format=csv&gid=0`.
  - Writes to `Assets/StreamingAssets/Kitchen/ingredients.csv` and calls `AssetDatabase.ImportAsset` only on success; failures log a warning and preserve the existing local CSV.
- Added/updated EditMode tests:
  - `CsvTableParserTests`
  - `KitchenIngredientCatalogTests`
  - `KitchenIngredientTablePopulatorTests`
  - `KitchenSceneWiringTests` now checks populator wiring instead of expecting edit-time source instances.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 pre-existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Scene grep confirmed old source instance names/fileIDs (`Plain Seaweed Source`, `White Rice Source`, `Ham Source`, `151101`, `152101`, `153101`) are gone from `kitchen.unity`.
  - Unity Editor processes were open, so batchmode EditMode tests were not run. Compile/static validation was used per the agreed fallback.
- Notes:
  - `Assembly-CSharp.csproj` is ignored/generated by Unity. It was locally updated only so `dotnet build` could compile the newly added files before Unity regenerates project files.
  - Menu download was implemented but not manually clicked in Unity during this pass.

## 2026-06-27 Kitchen Runtime Construction Removal

- Removed production `Assets/Scripts/Kitchen/KitchenSceneBootstrap.cs` and its meta file.
- Rebuilt `Assets/Scenes/kitchen.unity` as a serialized, minimally playable kitchen scene with:
  - `KitchenController`, `KitchenDropZone`, `KitchenRicePaintBridge`, `KitchenTableNavigator`, `KitchenRollAnimator`, Canvas, EventSystem, table roots, buttons, and ingredient sources wired in the scene.
  - Three serialized ingredient sources: seaweed, white rice, and ham.
  - Roll/complete/submit buttons on the complete table, with submit wired to `KitchenReturnNavigator`.
- Added prefab-backed kitchen visuals under `Assets/Prefabs/Kitchen/`:
  - `KitchenDragPreview.prefab`
  - `KitchenRiceSurface.prefab`
  - `KitchenRollGuide.prefab`
  - `KitchenCompletedKimbap.prefab`
  - `KitchenCompletedFillingCap.prefab`
  - `KitchenIngredientSource.prefab`
- Added `KitchenPlaceholderSprite` so placeholder sprite/color/sorting data can live on scene/prefab objects without runtime `AddComponent`.
- Changed `KitchenController` to require serialized `dropZone`, `ricePaintBridge`, and `riceSurfacePrefab`; rice painting surface creation now instantiates the prefab.
- Changed `KitchenIngredientSource` to require `definition.DragPrefab`; missing drag prefab now warns and aborts instead of generating a placeholder GameObject.
- Changed `KitchenRollAnimator` to instantiate serialized roll/completed/cap prefabs and use serialized button references instead of bootstrap-created listeners/visual objects.
- Changed `KitchenRicePaintBridge` and `KitchenController` to remove `FindObjectOfType` fallback wiring.
- Changed `KitchenTableNavigator` to own rice-painting bridge mode updates through serialized references.
- Replaced `KitchenSceneBootstrapTests` with `KitchenSceneWiringTests`, checking that `kitchen.unity` has no bootstrap and that key serialized references are present.
- Updated existing kitchen tests to provide prefab-backed test objects where needed. Test-only `new GameObject`/`AddComponent` remains allowed.
- Important validation:
  - `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `rg -n "KitchenPlaceholderFactory\.CreateSpriteObject|KitchenIngredientSource\.CreatePlaceholderPreview|KitchenSceneBootstrap" Assets\Tests Assets\Scripts Assets\Scenes\kitchen.unity` returned no matches.
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 warnings and 0 errors.
  - Static scene reference check for critical local fileIDs passed.
  - Prefab GUID references in `kitchen.unity` were checked against the actual `.meta` GUIDs after fixing an initial mismatch.
- Unity batchmode EditMode test command was attempted with Unity `6000.0.53f1`, but it did not produce test results because the project was already open in another Unity Editor instance (`HandleProjectAlreadyOpenInAnotherInstance`).
- Current caveat: generated `.csproj` files do not currently include `Assets/Tests/EditMode/Kitchen/*.cs`, so kitchen EditMode tests should be verified inside Unity Test Runner after the Editor reloads/regenerates project files.

## 2026-06-27 Kitchen Table Source Prefab Instance Follow-up

- UI was explicitly left out of scope: Canvas, `NextTableButton`, `RollButton`, `CompleteButton`, `SubmitButton`, and `CompleteTableCanvas` remain scene-owned objects.
- Converted `Plain Seaweed Source`, `White Rice Source`, and `Ham Source` in `Assets/Scenes/kitchen.unity` from scene-owned GameObject/component blocks into connected `PrefabInstance` entries of `Assets/Prefabs/Kitchen/KitchenIngredientSource.prefab`.
- Kept table roots scene-owned: `Seaweed Table`, `Rice Table`, `Filling Table`, and `Complete Table` remain fixed scene structure.
- Each source prefab instance now overrides only placement/name and ingredient wiring data such as `definition`, `controller`, `dropZone`, `targetCamera`, and `dragPreviewSize`.
- Strengthened `KitchenSceneWiringTests` so every `KitchenIngredientSource` must be a connected prefab instance whose source asset path starts with `Assets/Prefabs/Kitchen/`.
- Latest validation:
  - Production kitchen grep for `new GameObject`, `AddComponent`, `GameObject.Find`, `FindObjectOfType`, `Resources.FindObjectsOfTypeAll`, and `KitchenSceneBootstrap` returned no matches.
  - Static grep confirmed the old direct source component references like `m_GameObject: {fileID: 151100}` and old scene-owned `ingredientRenderer` fileIDs are gone.
  - Scene YAML now has three `PrefabInstance` entries using source prefab GUID `6d8cdd306e842104f989ac5a19598e3d`.
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed; it reported two existing `OrderSceneController` deprecation warnings unrelated to this kitchen source refactor.
  - A one-off Roslyn compile of `Assets/Tests/EditMode/Kitchen/KitchenSceneWiringTests.cs` into `.agent-work/KitchenSceneWiringTests.compile.dll` passed after adding Unity/NUnit references manually.
  - Unity batchmode EditMode tests were not rerun because Unity processes were still open.

## 2026-06-27 Data Parsing Investigation

- User-provided Google Sheet URL points to spreadsheet `DataSet`, `gid=0` / tab `김`.
- Live spreadsheet metadata also has tab `손님` with `sheetId=650686695`.
- Current project order parsing does not read the `김` tab. It reads the same spreadsheet through CSV export URL `https://docs.google.com/spreadsheets/d/13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI/export?format=csv&gid=650686695`.
- The hardcoded URL appears in:
  - `Assets/Scripts/Order/GoogleSheetOrderLoader.cs`
  - `Assets/Scripts/Order/OrderSceneController.cs`
  - serialized `Assets/Scenes/order.unity`
- `GoogleSheetOrderLoader.Parse` maps `손님` headers into `SheetOrderData`, then builds a flattened `List<IngredientType>` by repeating one mapped ingredient per category count.
- Supported parsed order columns include `Index`, `이름`, `주문-대사`, `주문-이미지`, `힌트-대사`, `힌트-이미지`, `성공-대사`, `성공-이미지`, `실패-대사`, `실패-이미지`, `김-이름`, `김-갯수`, `밥-이름`, `밥-갯수`, `속-이름`, `속-갯수`.
- Current parser limitation: comma-separated names/counts such as `단무지,우엉조림,당근` plus `1,1,1` are not split. `ParseCount` uses `int.TryParse` on the whole cell, so those counts become `0` and are not added to `SheetOrderData.ingredients`.
- Current `IngredientType` only supports `Seaweed`, `Rice`, `Ham`, `Egg`, `Carrot`, `Spinach`, `Tuna`, `CrabMeat`, `PickledRadish`; the `김` tab contains many more master ingredients/sauces that are currently ignored or collapsed by evaluator text heuristics.
- `KimbapEvaluator` uses the original `SheetOrderData` text fields as well as the flattened ingredients, so some requirements are inferred from dialogue/name text even when `ingredients` misses comma-separated items.

## Project Summary

- Unity project using Unity `6000.0.53f1`.
- Product name in `ProjectSettings/ProjectSettings.asset`: `cienjam2026_team5`.
- Template origin: Unity Universal 2D / URP 2D.
- Current tracked scene: `Assets/Scenes/SampleScene.unity`.
- Current MVP intent from remote `origin/main:pro.md`: a Good Pizza Great Pizza-style kimbap game where customers order kimbap, the player drags ingredients into a roll, and the result is checked against the order.

## Current Implementation State

- Spreading prototype scripts now exist under `Assets/Scripts/Gameplay/Spreading`.
- EditMode tests now exist under `Assets/Tests/EditMode/Gameplay/Spreading`.
- `Assets` contains the default-ish URP 2D setup, input actions, one sample scene, and render pipeline settings.
- `SampleScene.unity` contains at least:
  - `Triangle` sprite object.
  - `Main Camera`.
  - `Global Light 2D`.
- `Assets/Scenes/Drag.unity` is an untracked prototype scene containing `PizzaDough`, `Main Camera`, and spreading components.
- `PizzaDough` has two serialized brush definitions by default:
  - `white-rice`
  - `seasoned-rice`
- Input System asset exists at `Assets/InputSystem_Actions.inputactions` with default-style actions such as `Move`, `Look`, `Attack`, `Interact`, `Crouch`, `Jump`, `Previous`, `Next`, and `Sprint`.

## Key Existing / Planned Architecture

The remote planning document recommends this rough structure:

- `Assets/Scripts/Data`
  - `IngredientType.cs`
  - `OrderData.cs`
  - `PlayerKimbap.cs`
  - `CurrentOrder.cs`
- `Assets/Scripts/Managers`
  - `DataManager.cs`
  - `OrderManager.cs`
  - `GameManager.cs`
  - `UIManager.cs`
- `Assets/Scripts/Gameplay`
  - `DragItem.cs`
  - `Ingredient.cs`
  - `RecipeChecker.cs`
  - `Kimbap.cs`
- `Assets/Scripts/UI`
  - `OrderUI.cs`
  - `InventoryUI.cs`

Planned data flow:

`Orders.xlsx -> DataManager -> OrderData -> OrderManager -> CurrentOrder -> cooking scene -> PlayerKimbap -> RecipeChecker -> success/failure`

## Decisions / Observations

- Start with shared data contracts before gameplay/UI to reduce merge conflicts and rework.
- Use `IngredientType` enum instead of raw strings for recipe ingredients.
- Keep recipe checking as a separate domain/gameplay service rather than burying it in UI drag handlers.
- Local `main` is behind `origin/main` by one commit that adds `pro.md`.
- `.gitignore` no longer ignores all `*.meta` files because Unity asset GUIDs must be tracked.
- Spreading implementation is reusable rather than pizza-specific:
  - `SpreadMask` owns shared coverage logic and can be tested without Unity UI.
  - `SpreadBrushDefinition` owns inspector-configurable brush data: id, display name, texture, tint, radius, particle count, scatter, scale range, and stamp spacing.
  - `SpreadBrushSelector` owns index/id brush selection logic for later UI integration.
  - `SpreadableSurface` owns runtime texture/sprite updates, brush particle stamping, and shared coverage state.
  - `SpreadInputController` owns mouse/touch polling.
  - `SpreadDebugHud` owns selected brush display, coverage display, and reset key.
- Default prototype completion threshold is 70% coverage.
- Different rice brush types share one coverage mask; painting the same position with another brush does not increase coverage twice.
- Current kitchen refactor target is narrower: no scene-construction `AddComponent(...)` calls exist under production `Assets/Scripts/Kitchen` or `Assets/Scenes/kitchen.unity`; test-only object construction remains allowed.

## Commands / Evidence Used

- `rg --files`
- `find . -maxdepth ...`
- `git status --short --branch`
- `git branch -avv`
- `git diff --stat main..origin/main`
- `git show origin/main:pro.md`
- Read:
  - `Packages/manifest.json`
  - `Packages/packages-lock.json`
  - `ProjectSettings/ProjectVersion.txt`
  - `ProjectSettings/EditorBuildSettings.asset`
  - `ProjectSettings/ProjectSettings.asset`
  - `ProjectSettings/TagManager.asset`
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scenes/Drag.unity`
  - `Assets/InputSystem_Actions.inputactions`
- Validation commands attempted:
  - Unity batchmode EditMode test run using Unity `6000.0.53f1`; blocked because the project is already open in another Unity Editor.
  - Unity bundled Roslyn compile of all spreading runtime scripts with UnityEngine references; passed.
  - Unity bundled Roslyn compile of `SpreadMaskTests` and `SpreadBrushSelectorTests` against NUnit and the spreading runtime DLL; passed.
  - `rg "AddComponent" Assets/Scripts Assets/Scenes`; no matches.

## Validation Status

- Static/manual compile validation passed for the new runtime scripts and test code.
- Unity Test Runner did not run because the project was already open in another Unity Editor.
- User will perform play testing.

## Open Questions

- Should local `main` be updated from `origin/main` before implementation?
- What is the exact game loop target for the first playable MVP?
- Should order data really come from `.xlsx`, or should early MVP use JSON/ScriptableObjects/CSV for simpler Unity integration?
- Should the first implementation target one scene or separate order/cooking/result scenes?
- After the user play-tests spreading, should shared rice coverage feed into recipe/order scoring or stay as a separate preparation mechanic?
- Which future UI will call `SelectBrush(int)` / `SelectBrush(string)`?

## Recommended Next Steps

1. Open `Assets/Scenes/Drag.unity` in Unity.
2. Enter Play Mode and drag over `PizzaDough`; rice particles should appear.
3. Confirm debug HUD shows selected brush and coverage reaches `Complete` at 70% coverage.
4. Press `R` during Play Mode to reset spreading.
5. Close the Unity Editor and run EditMode tests from the Test Runner, or rerun batchmode tests.
6. Connect future UI brush buttons to `SelectBrush(int)` or `SelectBrush(string)`.
7. Decide whether to connect shared rice coverage completion to future order/recipe scoring.
