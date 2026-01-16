# Current Functionality - Pocket Planner

*Last Updated: 2026-01-14*

## Project Overview

Pocket Planner is a mobile app adaptation of a physical board game - a "Sim City" inspired roll-and-write city zoning game. The app serves as a digital demo and playtesting tool ahead of the physical launch in late 2026.

**Current Development Phase:** Early prototype (MVP foundation)

**Target Platform:** Android (Unity 6.3 LTS)
**Current Branch:** `create-scoring-system`

## Current State Summary

The project has made significant progress with core game systems now fully implemented. The current implementation includes board generation, shape management, dice system with UI, wildcard system, water die handling, basic movement, zone detection, scoring system, end game logic, starting position selection, and a complete end game screen. The prototype is now playable with core mechanics functional.

### Working Features
-  10×10 game board with correct river tile placement (11 fixed positions)
-  8 starting positions (numbered 1-8)
-  Starting position selection via mouse click (highlighting, validation)
-  Shape generation (all 6 shape types)
-  Basic shape movement using WASD keys with bounds checking (cannot move outside grid)
-  Mouse-based shape placement (placeholder for touch input)
-  Camera auto-framing of game board
-  Core data structures and enums
-  ScriptableObject system for shape data
-  Shape placement validation (boundaries, river, overlap)
-  Adjacency rule validation (first turn, subsequent turns, water die exception)
-  Valid position tracking (isValidPosition updated on movement)
-  **Zone detection algorithms implemented**: flood-fill adjacency detection, zone merging, unique shape tracking (Industrial/Residential/Commercial only)
-  **Dice system fully integrated**: random rolls, auto‑rerolls, double detection, UI selection, shape generation integration, star awarding, turn progression, water die special handling (building type choice, river adjacency validation), wildcard system (UI panels, cost tracking, face override)
-  **Scoring system implemented**: ScoreComponents struct, ScoreManager singleton, zone scoring (Industrial/Residential/Commercial based on unique shapes), park scoring (+2 per distinct zone adjacent), school scoring (+2 per residential building in largest adjacent zone), star scoring, empty cell penalty (-1 per empty non-river cell), wildcard cost integration, GameManager integration
-  **End game logic implemented**: GameManager end game detection placeholder, TriggerGameEnd() method, ESC keybind for manual game end, score breakdown UI panel (implemented, requires Unity Editor assignment), restart and main menu buttons

### Partially Implemented
- = GameManager (singleton pattern, full game loop implemented: dice roll → selection → shape placement → star awarding → turn advancement, wildcard tracking, water die usage flag)
- = Input system (actions defined, mouse placement implemented, dice UI interaction working, rotation/flip input not connected)
- = Shape rotation/flip functionality (methods implemented, input not connected)
- = Complete game loop (roll → select → place → score) - implemented but game end detection is placeholder

### Not Yet Implemented
- L UI/UX (menus, HUD not implemented; end game screen implemented but requires Unity Editor assignment)
- L Multiplayer systems
- L Audio system

## Detailed Implementation Status

### 1. Core Systems

#### **Board Generation** (`TilemapManager.cs`)
- Creates 10�10 grid of `GridTile` GameObjects
- Places 11 river tiles at fixed positions (columns 4-5, winding path)
- Marks 8 starting positions with TextMeshPro labels (1-8)
- Automatically positions camera to fit entire board
- Uses materials: `OutlinedTile` (regular), `RiverTile` (cyan), `WhiteTile` (starting)

#### **Starting Position Selection** (`TilemapManager.cs`, `GameManager.cs`, `ShapeManager.cs`)
- Mouse click selection of starting positions (1-8)
- Tile highlighting with yellow color when selected
- Validation to prevent multiple selections
- Integration with GameManager to track selected starting position
- ShapeManager skips placement during selection phase

#### **Shape System** (`ShapeManager.cs`, `ShapeController.cs`)
- Generates shapes combining `ShapeType` and `BuildingType`
- All 6 shape types implemented: T, Z, Square, L, Line, Single
- Shape prefabs for each type in `/Assets/Prefabs/Shapes/`
- WASD movement controls for active shape
- Mouse-based shape placement (clicks snap to grid tiles)
- Rotation and flip methods implemented (OnShapeRotate/OnShapeFlip)
- Shape placement confirmation with full validation (boundaries, river, overlap, adjacency rules)
- Shape data stored as ScriptableObjects with relative tile positions

#### **Dice System** (`Dice.cs`, `DicePool.cs`, `DiceManager.cs`, `DiceUIManager.cs`, `WildcardSelectionPanel.cs`)
- Core dice classes with random face generation, selection state, wildcard override capability
- Dice pool manages 3 shape dice and 3 building dice
- Auto‑reroll for triples (recursive)
- Double face detection for star opportunities (original faces for star eligibility, current faces for display)
- Integration with GameManager turn flow
- UI manager (`DiceUIManager`) fully implemented with dice buttons, selection highlighting, double face highlighting
- Water die special handling: building type choice, river adjacency validation, UI panel for type selection
- Wildcard system: UI panels (`WildcardSelectionPanel`) with Water button (index 5) and Cancel button, cost tracking, face override, integration with dice selection

#### **Data Structures**
- `GridPosition`: Vector-like struct with coordinate operators. **Important:** GridPosition always deals with logical representation of tiles (0-9 coordinates). Use TilemapManager conversion methods (`LogicalToTilemapCell`, `TilemapCellToLogical`, `LogicalToWorld`, `WorldToLogical`) to convert between logical coordinates and Tilemap cell coordinates/world positions.
- `GridTile`: Represents individual board tiles (position, river flag, starting position, zone reference)
- `Zone`: Class for grouping shapes of same building type (Industrial/Residential/Commercial) with unique shape counting and merging capabilities
- `Enums.cs`: `ShapeType`, `BuildingType`, `DiceType` enums
- `ShapeData.cs`: ScriptableObject for shape configuration

#### **Zone System** (`Zone.cs`, `ZoneManager.cs`)
- `ZoneManager`: Singleton manager that handles zone detection after each shape placement
- Flood-fill adjacency detection: automatically groups orthogonally adjacent buildings of same type
- Zone merging: when a shape connects multiple zones, they merge into one
- Unique shape tracking: counts distinct shape types within each zone for scoring
- Industrial, Residential, and Commercial buildings form zones; Schools and Parks do not
- Integration: automatically called from `GameManager.OnShapePlacementConfirmed()`

#### **Scoring System** (`ScoreComponents.cs`, `ScoreManager.cs`)
- `ScoreComponents` struct: breakdown of all score components (zone scores, park/school scores, star score, penalties)
- `ScoreManager`: Singleton manager for scoring calculations
- Zone scoring: Industrial/Residential/Commercial zones scored based on unique shapes (1=1, 2=2, 3=4, 4=7, 5=11, 6=16)
- Park scoring: +2 points per distinct contiguous zone orthogonally adjacent to park
- School scoring: +2 points per residential building in largest adjacent residential zone (one school per zone)
- Star scoring: +1 point per star earned (max 2 per turn)
- Penalties: -1 point per empty non-river cell at game end, wildcard costs (-1, -2, -3)
- Integration: `GameManager.CalculateFinalScore()` method, automatic penalty calculation

#### **End Game System** (`GameManager.cs`)
- `CheckGameEndCondition()` placeholder for automatic game end detection (no valid placements)
- `TriggerGameEnd()` method calculates final score and displays end game UI
- `OnGameEndInput()` public method bound to Input System's "GameEndInput" action
- End game UI panel with score breakdown display (implemented, requires Unity Editor assignment)
- Restart and main menu buttons (placeholder: restart reloads scene, main menu placeholder)
- Integration: `InitializeEndGameUI()` called in `GameManager.Start()` to set up button listeners
- Score breakdown formatting using `ScoreComponents` data

#### **Game State Management** (`GameManager.cs`)
- Singleton pattern established
- Starting position selection tracking (selectedStartingPosition field)
- Turn tracking, star awarding, water die usage flag
- Wildcard tracking (max 3 per game, escalating cost -1, -2, -3)
- Dice system integration, shape placement confirmation handling
- Star awarding logic based on original double faces (wildcards don't affect star eligibility)
- References to other managers (partially connected)

### 2. Assets & Prefabs

#### **Shape Prefabs** (`/Assets/Prefabs/Shapes/`)
- `TShape.prefab`, `ZShape.prefab`, `SquareShape.prefab`
- `LShape.prefab`, `LineShape.prefab`, `SingleShape.prefab`

#### **Grid Prefab**
- `GridTile.prefab` (base tile GameObject)

#### **ScriptableObjects** (`/Assets/ScriptableObjects/`)
- Shape data assets for all 6 shape types
- Each defines relative tile positions (e.g., T-shape: (0,0), (0,-1), (1,0), (-1,0))

#### **Materials**
- `OutlinedTile.asset` (regular tiles)
- `RiverTile.asset` (cyan river tiles)
- `WhiteTile.asset` (starting position tiles)

#### **Input System**
- `PlayerInputs.inputactions` - Unity Input System configuration

### 3. Scenes
- **SampleScene.unity**: Only scene, contains basic game board and camera
- **Recent modification**: GameObject position adjustment (0,0,0 from previous offset)

## Architecture Compliance

The current structure partially follows the architecture outlined in `CLAUDE.md`:

** Implemented as Specified:**
- ScriptableObject system for shape data
- Core data structures (`GridPosition`, enums)
- Basic shape controller pattern
- Dice system (random rolls, auto‑rerolls, double detection, selection, water die handling, wildcard system)
- Scoring system (ScoreManager, ScoreComponents, zone scoring, park scoring, school scoring, penalties)
- Zone detection algorithms (ZoneManager, Zone, flood-fill adjacency, merging)
- End game screen with score display (ESC keybind, UI panel)

**= Partially Implemented:**
- Script organization (scripts exist but not in subdirectories)
- Prefab structure (prefabs exist but not fully organized)

**L Not Yet Started:**
- Multiplayer systems
- UI managers (DiceUIManager fully implemented, main menu and HUD not implemented, end game screen implemented but requires Unity Editor assignment)
- Audio system

## Recent Development History

### Git Commits (Recent)
1. `259ac6b` - "Added starting position selection and tile highlighting"
2. `e99e697` - "Tweak to order layer so end screen renders on top"
3. `5fe9015` - "Added ESC keybind to end game and built end game screen with score display"
4. `877a5a7` - "Implemented the complete scoring system logic for Pocket Planner"
5. `d6563e9` - "Implement zone detection algorithms [#20]"
6. `60715a1` - "Added a functional random dice system with basic UI elements for die selection [#19]"
7. `050d581` - "Cleanup develop: merge main into develop"
8. `1af976c` - "Add shape placement demo with rule validation, bound checking and random shape generation [#18]"
9. `0dde895` - "Added minutes for third client meeting [#17]"
10. `a533631` - "Merge pull request #16 from Plymouth-University/DimitarKostadinov84-patch-1"

### Current Git Status
- **Branch:** `create-scoring-system`
- **Modified:** `Assets/Scripts/Core/Dice.cs`, `Assets/Scripts/Core/DiceManager.cs`, `Assets/Scripts/Core/DicePool.cs`, `Assets/Scripts/GameManager.cs`, `Assets/Scripts/UI/DiceUIManager.cs`
- **New file:** `Assets/Scripts/UI/WildcardSelectionPanel.cs`

## Known Issues & Limitations

1. **Validation implemented**: Shape placement checks board boundaries, river tiles, overlap, and adjacency rules (first turn, subsequent turns, water die exception - requires river adjacency, does NOT bypass first turn rule). Input validation for movement prevents moving outside grid.
2. **Rotation/Flip methods implemented**: ShapeController has rotation and flip methods, but UI input for them is not yet connected.
3. **Game Loop implemented**: Dice rolling, selection, turn progression, star awarding, wildcard system all functional
4. **Basic UI implemented**: Dice selection UI with feedback, shape updates when dice selection changes, water die panel for building type selection (fixed: panel appears when water die is clicked in dice panel regardless of selection state; otherwise hidden when building type already chosen; persists across shape changes), wildcard selection panels for shape and building dice (including cancel button), end game UI panel implemented (requires Unity Editor assignment)
5. **Basic Input**: WASD movement for shapes, mouse click for placement, UI interaction for dice selection, water die panel, and wildcard buttons (including cancel)
6. **Zone System**: Detection and merging algorithms implemented (Industrial/Residential/Commercial only)
7. **Wildcard button first-click issue**: The first click on each wildcard button may not register/show the panel. Debug logging and a coroutine have been added to ensure GameManager is ready and update UI. CanvasGroup support added to ensure panel visibility.
8. **End game panel persistence issue (FIXED)**: The endGamePanel would reappear after scene reload due to DontDestroyOnLoad. Fixed by adding SceneManager.sceneLoaded event handler to find new UI references and hide panel after scene load.

## Next Priority Tasks

Based on the current state and project specifications, the next critical tasks are:

### Phase 1A: Core Game Mechanics (Highest Priority)
1. **Placement validation** (boundaries, river, adjacency rules) - **IMPLEMENTED**
2. **Create basic dice system** (random generation, UI integration, wildcard system, water die handling) - **FULLY IMPLEMENTED**
3. **Implement zone detection algorithms** - **IMPLEMENTED**
4. **Build scoring system** (industrial/residential/commercial zones, parks, schools) - **IMPLEMENTED**

### Phase 1B: User Interface
1. **Create main menu scene**
2. **Implement in-game HUD** (turn counter, dice display, score)
3. **Add shape manipulation UI** (rotate/flip buttons)
4. **Create end-game screen** with score breakdown - **IMPLEMENTED (requires Unity Editor assignment)**

### Phase 1C: Input & Polish
1. **Integrate Unity Input System** with game actions
2. **Add touch input** for shape movement/placement
3. **Implement visual feedback** (valid/invalid placement indicators)
4. **Add basic sound effects**

## Technical Debt & Refactoring Needs

1. **Script Organization**: Core and UI directories created; move existing scripts into appropriate subdirectories
2. **Manager References**: DicePool and DiceManager added to GameManager; other managers need integration
3. **Error Handling**: Add validation and error checking throughout
4. **Code Comments**: Add documentation for complex algorithms

## Testing Status

**No automated tests implemented yet.** Manual testing confirms:
- Board generates correctly with river and starting positions
- Shapes generate without errors
- WASD movement works for active shape
- Camera frames board appropriately
- Dice selection UI functions correctly (selection, highlighting, double face detection)
- Dice auto‑reroll for triples works recursively
- Water die UI panel allows building type selection and persists across shape changes
- Wildcard system: UI panels show correctly, face overrides apply, cost tracking works
- Star awarding based on original double faces (wildcards don't affect stars)
- Zone detection algorithms correctly group adjacent buildings of same type
- Scoring system calculates zone scores, park scores, school scores, penalties
- End game screen displays score breakdown with ESC keybind
- Starting position selection with tile highlighting works

## Dependencies & Configuration

**Unity Version:** 6.3 LTS (assumed, not verified)
**Required Packages:**
- Input System
- TextMeshPro
- (Future) Firebase for multiplayer

---

*This document should be updated after each significant feature implementation to track progress toward MVP.*
*`./developer-instructions.md` should be updated after each significant feature detailing the required actions in the Unity Editor* 