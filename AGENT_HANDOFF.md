# Agent Handoff

## Current Goal

Keep the kitchen scene data-driven and scene-wired: ingredient sources and rice brush tuning come from the local ingredient CSV + prefabs, ingredient source rows center themselves from CSV counts, the hand/arm cursor is prefab-backed, `KitchenTableNavigator` moves according to `MovingTablesRoot` child table transforms, and rolling is a small mouse/touch drag progress script without roll-guide runtime visuals.

## 2026-06-28 Order Root Person Sprite Target

- Changed order customer image targeting to the root `Person` world object.
  - `Assets/Scenes/order.unity` now wires `OrderSceneController.personSpriteRenderer` to the root `Person` `SpriteRenderer` (`fileID: 605640439`).
  - `OrderSceneController.personImage` remains unassigned, so the Canvas child `Person` `Image` no longer receives customer sprites in the default scene flow.
- Code behavior:
  - `SetPersonImage()` now applies loaded customer sprites to `personSpriteRenderer`.
  - Reaction particles prefer the world `SpriteRenderer` path when `personSpriteRenderer` is present.
  - The old `GameObject.Find("Person")` person fallback was removed because the scene has both root and Canvas `Person` objects.
  - If `personSpriteRenderer` is missing, fallback lookup searches active `SpriteRenderer` objects named `Person`, preferring scene-root objects.

## 2026-06-28 Mouse Cursor Visibility Helper

- Added a small reusable `MouseCursorVisibility` component under `Assets/Scripts/Gameplay/`.
  - Intended use: attach it to a scene object such as a cursor manager or `KitchenHandCursor` object when the OS cursor should be hidden.
  - On enable, it stores the current global `Cursor.visible` and `Cursor.lockState`, then hides the cursor.
  - Default lock mode is `CursorLockMode.None`, so existing mouse position input and custom hand cursor tracking continue to work.
  - On disable, it restores the saved cursor state by default.

## 2026-06-28 Rigidbody2D Mouse Throw Helper

- Added a small reusable `MouseThrow2D` component under `Assets/Scripts/Gameplay/`.
  - Intended use: attach it to any object that already has `Rigidbody2D` and `Collider2D`.
  - Uses the project’s existing old Input style: `OnMouseDown`, `Input.mousePosition`, and `Input.GetMouseButtonUp(0)`.
  - While dragging, the clicked local point is pulled toward the mouse with `Rigidbody2D.AddForceAtPosition()`.
  - On release, the component stops applying force and leaves the body's current linear/angular velocity untouched, so the object flies from the simulated physics state.
  - `SpringJoint2D` is intentionally not used here because this helper represents a free throwable body, not a fixed-anchor spring object.
- Tuning:
  - `dragForce` controls how strongly the grabbed point follows the cursor.
  - `dragDamping` damps the grabbed point's current velocity.
  - `maxForce` clamps extreme cursor pulls.
- Scope:
  - Mouse-only helper for quick 2D physics interactions.
  - Touch support, new Input System actions, fixed anchors, and scene wiring are intentionally out of scope.

## 2026-06-28 Rigidbody2D Mouse Spring Return Helper

- Added a small reusable `MouseSpringReturn2D` component under `Assets/Scripts/Gameplay/`.
  - Intended use: attach it to any object that already has `Rigidbody2D` and `Collider2D`.
  - Uses one local fixed point, `anchorLocalPoint`, as the red point shown in the sketch.
  - At Play start, the component stores the world position of `anchorLocalPoint` and configures a `HingeJoint2D` to pin that local point to the fixed world pivot.
  - During mouse drag, a `TargetJoint2D` pulls the clicked local point toward the cursor.
  - On release, only the target joint turns off; the hinge joint motor rotates the body back toward its start angle around the pinned local anchor.
  - No extra anchor GameObject is required.
- Tuning:
  - `returnMotorSpeed`, `returnMotorDamping`, and `returnMaxMotorTorque` control the angle return around the anchor.
  - `dragFrequency`, `dragDampingRatio`, and `dragMaxForce` control how the mouse pull feels.
- Implementation note:
  - The first force-based version used `AddForceAtPosition()` at both the anchor and drag point. With the current tall test object in `order.unity`, that created a large lever arm and caused immediate spinning.
  - A later single-`SpringJoint2D` version returned one point but could not guarantee rotation recovery.
  - A `RelativeJoint2D` version recovered body pose but did not use `anchorLocalPoint` as the rotation pivot. The current version uses Unity's built-in `HingeJoint2D` for the local-anchor pivot and `TargetJoint2D` for mouse dragging.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing `OrderSceneController` deprecation warnings.
  - Static grep confirmed `MouseSpringReturn2D.cs` no longer contains `RelativeJoint2D`, `SpringJoint2D`, `MovePosition`, `MoveRotation`, or direct velocity assignment.

## 2026-06-28 CompleteButton Immediate Order Return

- Changed the completed-roll flow back to one button press.
  - `KitchenRollAnimator` now owns a serialized `KitchenReturnNavigator returnNavigator` reference.
  - `CompleteButton` click now runs `FinalizeRoll()`, calls `KitchenController.CompleteAndSave()`, then calls `returnNavigator.ReturnToOrderScene()`.
  - The old `submitButton.gameObject.SetActive(true)` flow is no longer used.
- Scene wiring:
  - `Assets/Scenes/kitchen.unity` connects `KitchenRollAnimator.returnNavigator` to the existing `KitchenReturnNavigator` component on the inactive legacy `SubmitButton`.
  - `SubmitButton` remains in the scene for later UI cleanup, but it is no longer required for the player to return to `order`.
- Test notes:
  - `KitchenReturnNavigator.ReturnToOrderScene()` is now virtual so EditMode tests can override it without loading scenes.
  - `KitchenRollAnimatorTests` covers ready complete-click save/return and below-threshold no-save/no-return.
  - `KitchenSceneWiringTests` now requires `returnNavigator` instead of `submitButton`.

## 2026-06-28 Filling Image Search Root Fix

- Fixed source/drag-preview visual sprite lookup for filling rows.
  - Root cause: `Assets/StreamingAssets/Kitchen/ingredients.csv` uses filename-only values such as `햄_line.png`, while the actual assets live under `Assets/Resources/Kitchen/fillings_image/`.
  - `KitchenIngredientSpriteLoader` now searches `Kitchen/fillings_image` in addition to `Kitchen` and `Kitchen/재료`.
  - CSV can keep filename-only values like `햄_line.png`; direct paths such as `Kitchen/fillings_image/햄_line.png` also work.
- Scope:
  - Current source and drag preview visuals use the `*_line` image referenced by the `이미지` column.
  - `*_box` images are present as assets but are not used by this source/preview path yet.
- Updated tests:
  - `KitchenIngredientCatalogTests` covers `햄_line.png`, `단무지_line.png`, and direct `Kitchen/fillings_image/햄_line.png` lookup.
  - `KitchenIngredientTablePopulatorTests` verifies the filling source renderer and drag preview renderer receive `햄_line_0`.

## 2026-06-28 Rice Brush ID Fallback Fix

- Fixed rice brush IDs defaulting to `white-rice`.
  - `KitchenIngredientCatalogItem` now uses explicit sheet/CSV `브러시ID` first.
  - If `브러시ID` is blank, it falls back to row `Index` such as `r18`.
  - If both are blank, it falls back to the display name, then `"rice"` as the last resort.
- Current local `Assets/StreamingAssets/Kitchen/ingredients.csv` already has distinct rice brush IDs:
  - `흰쌀밥 -> b16`
  - `현미밥 -> b17`
  - `흑미밥 -> b18`
  - and so on through `b25`.
- Important runtime note:
  - `KitchenIngredientTablePopulator.Start()` loads `StreamingAssets/Kitchen/ingredients.csv` once and spawns source instances.
  - If the CSV or Google Sheet is updated while Play Mode is already running, existing spawned source definitions are not automatically refreshed. Restart Play Mode or add an explicit repopulate/debug refresh path in a later task.
  - `흑미밥(브러시)` is not a Resources texture path, so `Brush Texture` remaining `None` is expected until a real texture exists under `Assets/Resources/...` and the CSV uses a path such as `Kitchen/Brushes/black-rice`.
- Updated tests:
  - `KitchenIngredientCatalogTests` now expects blank rice `브러시ID` to fall back to row `Index` instead of `white-rice`.
  - Added coverage that explicit `브러시ID = b18` for `흑미밥` overrides the fallback and remains `b18` even if its `브러시이미지` is not loadable.

## 2026-06-28 Rice Brush Resources-Wide Image Lookup

- Expanded `KitchenRiceBrushTextureLoader` so `브러시이미지` is not limited to a direct texture path.
  - Search order is now direct `Texture2D` path, direct `Sprite` path, Resources-wide sprite/sub-sprite name lookup, then Resources-wide `Texture2D` name lookup.
  - Resources-wide sprite and texture indexes are cached after first use.
  - Duplicate names warn once and keep the first match.
- Sprite sheet brush behavior:
  - `흑미밥(브러시)` is found as a sub-sprite under `Assets/Resources/Kitchen/재료/제목 없음 (7).png`.
  - The loader crops the sub-sprite `textureRect` into a readable `Texture2D` copy instead of using the whole sheet.
  - Non-readable source textures still go through the existing RenderTexture copy path before cropping.
- Updated tests:
  - `KitchenRiceBrushTextureLoader.Load("흑미밥(브러시)")` returns a non-null texture with the sub-sprite dimensions, not the whole sheet dimensions.
  - `KitchenRiceBrushTextureLoader.Load("Kitchen/Brushes/white-rice")` still loads the direct texture path.
  - Catalog conversion for `r18 / 흑미밥 / b18 / 흑미밥(브러시)` now verifies `BrushTexture != null`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing `OrderSceneController` deprecation warnings.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-28 Non-Readable Rice Brush Sprite Fix

- Fixed the Unity Console error from `Texture2D.GetPixels32` on `제목 없음 (7)`.
  - Root cause: `KitchenRiceBrushTextureLoader.IsReadable()` only caught `UnityException`, but Unity threw `ArgumentException` for the non-readable sprite sheet texture.
  - The readable probe now treats any exception as "not readable" so the RenderTexture copy fallback can run.
  - If the RenderTexture copy also fails, `EnsureReadable()` returns `null` instead of handing the same non-readable texture to the crop path.
- Hardened sprite sheet cropping:
  - `CreateReadableTextureFromSprite()` now exits quietly when a readable source cannot be produced.
  - Sprite `textureRect` crop coordinates are clamped to the readable texture bounds before `GetPixels`.
  - `흑미밥(브러시)` remains a sub-sprite crop, not the full `제목 없음 (7)` sheet.
- Updated tests:
  - Catalog conversion and direct loader tests for `흑미밥(브러시)` now assert the load path does not throw.
  - Existing width/height checks still verify that the returned texture matches the sub-sprite dimensions.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing `OrderSceneController` deprecation warnings.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-28 Ingredient Source Renderer-Based Pickup

- Changed table source pickup to follow the actual ingredient `SpriteRenderer`.
  - `KitchenIngredientSource` now realigns the root `BoxCollider2D` after `Awake()` / `Configure()` using `ingredientRenderer.sprite.bounds` converted into source-root local space.
  - If `ingredientRenderer`, sprite, or root `BoxCollider2D` is missing, the existing collider is left unchanged and no exception is thrown.
  - Drag preview initial x/y position now starts from `ingredientRenderer.bounds.center`; the existing preview depth behavior is preserved with `source.transform.position.z - 1f`.
- Preserved existing behavior:
  - Rice source click still routes to `KitchenController.SelectRice()` and does not spawn a drag preview.
  - Dragging, drop zone checks, CSV/image parsing, source sorting, and dropped ingredient sorting were not changed.
- Updated tests:
  - `KitchenIngredientTablePopulatorTests` covers collider alignment to a child renderer with offset/scale, visual-sprite re-alignment, drag preview spawn at renderer center, and rice no-preview click behavior.
  - `KitchenSceneWiringTests` now checks the three table-specific source prefabs have root `BoxCollider2D` and wired `ingredientRenderer`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing `OrderSceneController` deprecation warnings.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Static prefab grep confirmed `KitchenSeaweedIngredientSource.prefab`, `KitchenRiceIngredientSource.prefab`, and `KitchenFillingIngredientSource.prefab` contain root `BoxCollider2D` and `ingredientRenderer` references.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-28 Table Source Spawn Sorting

- Added table source sorting controls to `KitchenIngredientTablePopulator.TableSourceSettings`.
  - `sourceSortingOrderBase` sets the first spawned source's base renderer order.
  - `sourceSortingOrderStep` sets how much each later spawned source moves upward in sorting.
  - Default scene values are base `4`, step `10` for seaweed, rice, and filling tables.
- Runtime behavior:
  - `KitchenIngredientTablePopulator` applies sorting after `KitchenIngredientSource.Configure()`.
  - Same-table source index now drives sorting: index `0` starts at base, index `1` starts at `base + step`, etc.
  - Internal prefab renderer offsets are preserved, so a prefab with renderers at `4, 5` becomes `14, 15` for index `1`.
  - Dropped ingredient sorting still belongs to `KitchenController.RegisterDroppedObject()` and was not changed.
- Updated tests:
  - `KitchenIngredientTablePopulatorTests` covers later spawned sources rendering above earlier sources, preserved internal renderer offsets, and table-specific base/step values.
  - `KitchenSceneWiringTests` checks each table settings group has a positive `sourceSortingOrderStep`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 warnings and 0 errors.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Table-Specific Ingredient Source Settings

- Split table source prefabs by table.
  - Added `KitchenSeaweedIngredientSource.prefab`, `KitchenRiceIngredientSource.prefab`, and `KitchenFillingIngredientSource.prefab`.
  - The original `KitchenIngredientSource.prefab` remains as the common reference/template asset.
- Refactored `KitchenIngredientTablePopulator`.
  - Added serialized `TableSourceSettings` groups for seaweed, rice, and filling tables.
  - Each group controls `tableRoot`, `sourcePrefab`, `rowCenter`, `spacing`, `itemsPerRow`, `localScale`, `dragPreviewSize`, and `placeholderColor`.
  - Runtime spawning and Scene View gizmos now use the same settings object, so table-specific layout and gizmos stay aligned.
  - Legacy top-level serialized fields are hidden and kept for fallback; if an old scene has empty settings, the old fields seed the new settings at runtime/editor gizmo time.
- Updated `kitchen.unity`.
  - The populator now explicitly references the three new table-specific source prefabs.
  - Existing scene layout values were preserved in the new settings: center `(0, 2, -0.2)`, spacing `(1.1, -0.8)`, scale `(1, 1, 1)`, and row counts `11 / 7 / 11`.
- Updated tests.
  - `KitchenIngredientTablePopulatorTests` covers table-specific prefabs, independent scale/drag/color settings, centered rows, sauce exclusion, visual sprite application, drag preview sprite application, and legacy field fallback.
  - `KitchenSceneWiringTests` verifies each settings group points at its table root and the expected table-specific prefab path.
- Validation status:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 warnings and 0 errors.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Rice Surface Seaweed Renderer Bounds Alignment

- Changed the top seaweed rice paint surface to align from the dropped seaweed `SpriteRenderer` bounds instead of a hardcoded local rectangle.
  - `KitchenController` now finds `TopSeaweedObject.GetComponent<SpriteRenderer>()`, then falls back to `GetComponentInChildren<SpriteRenderer>()`.
  - The renderer sprite bounds are converted into the top seaweed local space, so root sprites, child sprites, non-centered pivots, and sprite-sheet slices can all drive the rice surface position.
  - New tuning field: `riceSurfaceSeaweedSizeRatio`, default `(0.92, 0.78)`, keeps the paintable area slightly inset from the seaweed image.
- Fixed the visible dark rectangle issue.
  - `TopSeaweedRiceSurface` is still created, active, renderer-enabled, and wired to `SpreadableSurface` / `SpreadInputController`.
  - Its initial `SurfaceColor` is now `Color.clear`, so rice only appears after brush painting writes pixels.
- Preserved existing gameplay flow.
  - `KitchenRicePaintBridge.SelectRice()` still applies the selected rice brush to the active surface.
  - The rice surface remains a child of the dropped seaweed, so rolling movement/compression follows the seaweed and completion still hides the surface.
- Updated `KitchenControllerTests`.
  - Covers active transparent surface creation, renderer-bounds alignment, non-centered sprite pivots, painting on a transparent surface, second seaweed replacement, and rice brush application through `KitchenRicePaintBridge`.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and the 2 existing `OrderSceneController` deprecation warnings.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Kitchen Ingredient Resource Sprite Auto-Apply

- Added `KitchenIngredientSpriteLoader` for visible ingredient sprites.
  - Explicit `이미지` values are tried first.
  - Blank `이미지` values fall back to the row `이름`, then `Index`.
  - Resources search supports direct paths such as `Kitchen/단무지`, exact sprite-sheet sub-sprite names such as `sheet_0`, and root lookup under `Kitchen` and `Kitchen/재료`.
  - Explicit missing image values log a warning and fall back to the existing placeholder color path.
- Extended `KitchenIngredientDefinition` with `VisualSprite`.
  - `KitchenIngredientCatalogItem.ToDefinition()` now resolves `ImageName` / `DisplayName` / `Id` into `VisualSprite`.
  - Existing constructors remain compatible for tests and scene/prefab serialized definitions.
- Updated `KitchenIngredientSource`.
  - Source `ingredientRenderer` uses `VisualSprite` with `Color.white` when available.
  - Drag preview renderers use the same `VisualSprite`, so dropped seaweed/fillings get the sheet image.
  - `KitchenRollSeaweedCover` already copies the source seaweed renderer, so the cover inherits the seaweed image automatically.
- Updated tests.
  - `KitchenIngredientCatalogTests` covers image-column conversion, direct Resources path lookup, blank display-name fallback, sprite-sheet sub-sprite lookup, and missing explicit image fallback.
  - `KitchenIngredientTablePopulatorTests` covers source renderer sprite application and drag preview sprite application.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and the 2 existing `OrderSceneController` deprecation warnings.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.
- Usage note:
  - Ingredient images must live under `Assets/Resources/...` and be imported as `Sprite (2D and UI)`.
  - For a direct image, put a Resources-relative path without extension in the sheet, e.g. `Kitchen/단무지`.
  - For a sprite sheet sub-sprite, put the exact sub-sprite name, e.g. `sheet_0`; if `이미지` is blank, the loader tries the ingredient `이름`.

## 2026-06-27 KitchenRollAnimator Child Seaweed Cover

- Implemented the user-selected "extra seaweed object as child of original seaweed" cover approach.
  - Added `KitchenRollSeaweedCover` helper on the drag preview root.
  - Added inactive child `RollSeaweedCover` with a `SpriteRenderer` to `Assets/Prefabs/Kitchen/KitchenDragPreview.prefab`.
  - The helper copies the source seaweed renderer sprite/color/flips/material/sorting layer, sets `localScale.y` from `0..1`, and offsets local y so the cover grows from the source sprite bottom.
- Updated `KitchenRollAnimator`.
  - Caches `KitchenRollSeaweedCover` from the current top seaweed snapshot.
  - Shows the cover only when `rollProgress > 0`.
  - Hides the cover when progress returns to `0`.
  - Keeps the cover active at full height after `FinalizeRoll()`.
  - Calculates cover sorting above current seaweed, dropped fillings, and rice surface without including the cover itself, avoiding per-frame sorting drift.
- Updated tests.
  - `KitchenRollAnimatorTests` now covers cover activation at progress, `localScale.y`, bottom anchoring, sorting above fillings, hiding after unroll, and full-height active cover after finalize.
  - `KitchenSceneWiringTests` now verifies `KitchenDragPreview.prefab` has a root `KitchenRollSeaweedCover`, wired `coverRenderer`, and inactive `RollSeaweedCover` child.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 warnings and 0 errors.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 KitchenRollAnimator Filling Snapshot Refresh Fix

- Confirmed the exact runtime cause from Unity `Editor.log`.
  - `KitchenRollAnimator` captured a roll snapshot right after seaweed registration with `DroppedFillingObjects count=0` and `snapshotFillingCount=0`.
  - Later filling registrations increased `DroppedFillingObjects` to 3, but the roller did not log a refreshed snapshot because `CaptureSnapshot()` returned early for the same top seaweed.
- Changed `KitchenRollAnimator.CaptureSnapshot()`.
  - Seaweed pose/bounds/sorting snapshot is captured only when the top seaweed changes.
  - Filling snapshot is refreshed from current `controller.DroppedFillingObjects` even when the same seaweed remains active.
  - To avoid per-frame drift while rolling, existing filling snapshots are reused when the non-null dropped filling transform sequence has not changed.
- Added regression coverage in `KitchenRollAnimatorTests`.
  - The new test captures an initial seaweed-only snapshot, registers a filling afterward, then verifies `SetRollProgressForTests(0.5f)` compresses that late-registered filling's y position.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 warnings and 0 errors.
  - `Assembly-CSharp.csproj` includes `Assets\Tests\EditMode\Kitchen\KitchenRollAnimatorTests.cs`, so the new test source compiled during the build.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - `git diff --check` passed; only Git line-ending conversion warnings were printed.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Kitchen Filling Registration Debugging

- Added opt-in registration tracing on `KitchenController`.
  - Inspector toggle: `logIngredientRegistrationDebug`.
  - Inspector counters: `debugDroppedFillingCount`, `debugTopSeaweedName`, `debugLastRegisteredObjectName`.
  - Counters update from `ResetPreparation()` and `RegisterDroppedObject()` only.
- Added debug logs along the drop/register/roll snapshot path.
  - `KitchenDraggableItem.Drop()` logs drop rejection, `TryAddIngredient` failure, and successful `RegisterDroppedObject` calls through `KitchenController.LogRegistrationDebug`.
  - `KitchenController.RegisterDroppedObject()` logs seaweed top updates, filling list registration, and non-filling categories that are not tracked for rolling.
  - `KitchenRollAnimator.CaptureSnapshot()` logs current top seaweed, `DroppedFillingObjects` count, snapshot filling count, and skipped null fillings.
- Updated tests:
  - `KitchenControllerTests` now verifies filling debug count/name values and reset clearing.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Static grep confirmed the new registration logs route through `logIngredientRegistrationDebug` / `LogRegistrationDebug`.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Kitchen Drag-Based Rolling

- Replaced `KitchenRollAnimator`'s RollButton/coroutine flow with pointer-driven `rollProgress`, then minimized it by removing the roll-guide visual path.
  - Drag starts only from the lower area of the current top seaweed bounds.
  - Dragging upward increases `rollProgress`; releasing below the completion threshold decreases progress by serialized `unrollSpeed`.
  - Reaching the serialized completion threshold holds the ready state so `CompleteButton` can be pressed.
- Removed roll-guide runtime visuals from `KitchenRollAnimator`.
  - `rollGuidePrefab`, roll guide instantiation/destruction, roll guide renderer state, and roll guide test accessors are gone.
  - `kitchen.unity` no longer serializes a roll guide reference on `KitchenRollAnimator`.
  - `KitchenSceneWiringTests` now checks only `controller`, `targetCamera`, `completeButton`, and `submitButton` for the roller.
- Removed production completed-preview construction.
  - `completedKimbapPrefab`, `completedFillingCapPrefab`, `CreateCompletedKimbap`, and `CreateRectChild` are gone from production kitchen code/scene.
  - `kitchen.unity` no longer has a `RollButton` scene object or `rollButton` serialized reference.
  - Completion now reuses existing objects: the top seaweed becomes the final visible body, rice surface is hidden, and dropped fillings keep x/scale/rotation while only y is compressed.
- New serialized tuning on `KitchenRollAnimator`:
  - `targetCamera`, `dragStartBottomRatio`, `dragDistanceToFullRoll`, `unrollSpeed`, `completeProgressThreshold`, `finalSeaweedSizeRatio`, `finalFillingYScale`.
- Filling y compression contract:
  - `finalSeaweedSizeRatio.y` defaults to `0.5`.
  - `finalFillingYScale` defaults to `0.5`.
  - Each filling stores its original y and moves by `Lerp(originalY, finalSeaweedCenterY + (originalY - originalSeaweedCenterY) * finalFillingYScale, rollProgress)`.
  - Fillings are not captured into roll-guide slots and are not evenly redistributed; original x/scale/rotation and side protrusion are preserved during roll, unroll, and completion.
- Updated tests:
  - `KitchenRollAnimatorTests` now covers bottom-area drag start, rejected outside starts, unroll on release, progress-based filling y compression, filling y returning during unroll, final y-only compression, rice hiding, and no completed preview object creation.
  - `KitchenSceneWiringTests` now checks `targetCamera`, `completeButton`, and `submitButton`; old roll guide/completed prefab references are no longer expected.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Static grep confirmed `RollButton`, `rollButton`, `PlayRoll`, `RollRoutine`, old duration fields, completed-preview prefab fields, and completed-preview creation methods no longer remain in production kitchen scripts or `kitchen.unity`.
  - Static grep confirmed old filling slot/capture tuning symbols `CapturedFilling`, `CaptureOverlapping`, `MoveCaptured`, `ArrangeFinalFillings`, `finalFillingVerticalInset`, and `finalFillingHeightScale` no longer remain in kitchen scripts, scene, or EditMode tests.
  - Static grep confirmed `KitchenRollAnimator` no longer contains `Instantiate`, `Destroy`, `rollGuidePrefab`, completed-preview symbols, or roll-guide test accessors.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

## 2026-06-27 Kitchen Ingredient Category Column Limits

- Split `KitchenIngredientTablePopulator` source row limits by ingredient category.
  - Replaced common `itemsPerRow` with `seaweedItemsPerRow`, `riceItemsPerRow`, and `fillingItemsPerRow`.
  - Runtime spawning and `OnDrawGizmos` both use the category-specific column count.
  - Values are clamped to at least 1 column in code; Inspector fields also use `Min(1)`.
- Updated `Assets/Scenes/kitchen.unity`.
  - Removed serialized `itemsPerRow`.
  - Added `seaweedItemsPerRow`, `riceItemsPerRow`, and `fillingItemsPerRow`.
  - All three were initialized to the current scene value `7`; exact tuning is left to the user in Inspector.
- Updated tests.
  - `KitchenIngredientTablePopulatorTests` now checks category-specific column limits, final-row centering per category, and clamp-to-one behavior.
  - Updated older partial-row expectations to match the current horizontal + vertical centering formula.
  - `KitchenSceneWiringTests` now verifies the three serialized row-limit fields exist and are positive.
- Validation:
  - `dotnet build 2026gamejam5team.sln --no-restore -v:minimal` passed with 0 errors and 2 existing warnings in `OrderSceneController`.
  - Production grep `rg -n "new GameObject|AddComponent|GameObject\.Find|FindObjectOfType|Resources\.FindObjectsOfTypeAll|KitchenSceneBootstrap" Assets\Scripts\Kitchen Assets\Scenes\kitchen.unity` returned no matches.
  - Static grep confirmed `itemsPerRow` no longer remains in kitchen scripts, `kitchen.unity`, or kitchen EditMode tests.
  - Unity Editor processes were open, so batchmode EditMode tests were not run.

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
