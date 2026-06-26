# Agent Handoff

## Current Goal

Implement a reusable Unity 2D spreading prototype: drag to spread rice-like brush particles over a surface and track shared coverage completion.

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
- No `AddComponent(...)` calls exist under `Assets/Scripts` or `Assets/Scenes`.

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
