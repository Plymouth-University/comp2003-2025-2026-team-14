# Developer Instructions for Pocket Planner

## Connecting Shape Rotation/Flip Inputs [X]

Shape rotation and flip functionality has been implemented in `ShapeController.cs` with the following methods:

- `public void OnShapeRotate()` - Rotates shape 90� clockwise
- `public void OnShapeFlip()` - Flips shape horizontally (mirror over y-axis)

These methods are ready to be connected to input actions. Currently they are not bound to any input.

### Steps to Connect Inputs in Unity Editor:

1. **Open Input Actions Asset**:
   - Navigate to `Assets/Inputs/PlayerInputs.inputactions`
   - Double-click to open the Input Actions Editor

2. **Add New Actions**:
   - In the "Gameplay" action map, add two new actions:
     - **ShapeRotate** (Type: Button)
     - **ShapeFlip** (Type: Button)

3. **Assign Keyboard Bindings** (temporary for testing):
   - For ShapeRotate: Bind to `R` key
   - For ShapeFlip: Bind to `F` key
   - (Later these can be replaced with UI button interactions or touch gestures)

4. **Update ShapeController Input Handling**:
   - The existing `ShapeController` uses the `OnShapeMovement` and `OnShapeConfirm` methods triggered by input actions.
   - Add two new methods with the `[SerializeField]` attribute or use the `PlayerInput` component's event references.
   - Alternatively, modify the `PlayerInput` component on the shape prefab or manager to send messages to `OnRotate` and `OnFlip`.

5. **Recommended Approach**:
   - Add a `PlayerInput` component to the shape GameObject (or ensure one exists).
   - In the `PlayerInput` component, set the "Behavior" to "Send Messages".
   - Ensure the action names match exactly: "ShapeRotate" and "ShapeFlip".
   - The `ShapeController` will automatically receive `OnShapeRotate` and `OnShapeFlip` messages (Unity adds "On" prefix).
   - Rename the methods to `OnShapeRotate` and `OnShapeFlip` if you prefer automatic mapping, or manually wire events.

6. **Testing**:
   - Enter Play Mode and press R/F to verify shape rotates/flips.
   - Ensure rotation/flip respects `isPlacementConfirmed` flag (won't rotate after placement).

### Notes:
- Rotation state is stored as `rotationState` (0-3) and flip state as `isFlipped`.
- The visual transformation is applied via `UpdateVisual()` which sets `transform.localRotation` and `transform.localScale`.
- Occupied grid positions are computed correctly via `GetOccupiedPositions()` which accounts for rotation/flip.
- The shape cannot be rotated/flipped after placement (`isPlacementConfirmed == true`).

### Next Steps:
- After input connection, test shape placement validation with rotated/flipped shapes.
- Consider adding UI buttons for mobile touch input.
- Integrate with the Input System's touch controls for drag/rotate gestures.

[*Methods are now connected to the Input Actions*]

## Mouse-Based Shape Placement [X]

Shape placement via mouse click has been implemented as a placeholder for future touch input. Shapes now align to grid tiles when placed.

### Implementation Details:

- **ShapeManager.cs**:
  - Added `OnPlaceShapeInput(Vector2 screenPosition)` method for Input System binding
  - Added `PlaceShapeAtGridPosition(GridPosition gridPos)` method for programmatic placement
  - Added `GetGridPositionFromScreen(Vector2 screenPosition)` helper for screen-to-grid conversion
  - Added `GetWorldPositionFromGridPosition(GridPosition gridPos)` helper for grid-to-world conversion
  - Modified `generateRandomShape(GridPosition? gridPos = null)` to accept optional grid position
  - Removed automatic shape generation in Update; shapes now generate on first click
  - Added temporary mouse click handling in Update (old input system) for testing

- **ShapeController.cs**:
  - Added `SetGridPosition(GridPosition newPosition)` method to move shape and update visual position
  - Added `Tilemap` reference for accurate grid-to-world conversion
  - Shape now snaps to tilemap cell centers when placed

### Steps to Set Up in Unity Editor:

1. **Assign Tilemap Reference**:
   - Select the `ShapeManager` GameObject in the scene
   - In the Inspector, find the "References" section
   - Assign the `Tilemap` component (from the board) to the "Board Tilemap" field
   - If left empty, the system will try to find a Tilemap automatically at runtime

2. **Input System Binding (for future touch)**:
   - Open `Assets/Inputs/PlayerInputs.inputactions`
   - In the "Gameplay" action map, add a new action:
     - **PlaceShape** (Type: Value, Control Type: Vector2)
   - Bind to mouse position (for testing) or touch position
   - In the `ShapeManager` component, wire the action to call `OnPlaceShapeInput` with the screen position value

3. **Testing**:
   - Enter Play Mode and click anywhere on the board
   - A random shape should appear at the clicked grid cell (aligned to tile centers)
   - Subsequent clicks move the active shape to new positions
   - Confirm placement with the existing confirm input (currently unused)
   - Use WASD keys to move shape (existing functionality)

### Notes:
- The system uses `Tilemap.CellToWorld` and `Tilemap.WorldToCell` for precise grid alignment
- Shapes fit within grid tiles with no overlapping borders
- The temporary mouse input (`Input.GetMouseButtonDown`) will be replaced by the Input System binding
- Shape generation now occurs on first click instead of automatically at (0.5, 0.5)

[*Mouse placement implemented; Input System integrated*]

## Basic Dice System UI Setup [X]

A basic dice system has been implemented with core classes (`Dice`, `DicePool`, `DiceManager`). To visualize dice and allow player selection, a UI must be created.

### Dice System Components Created:

1. **Core Classes** (in `Assets/Scripts/Core/`):
   - `Dice.cs` - Represents a single die with face, type, selection state.
   - `DicePool.cs` - Manages 3 shape dice and 3 building dice, handles rolling, auto-rerolls, double detection.
   - `DiceManager.cs` - MonoBehaviour wrapper for DicePool, provides Unity integration.

2. **UI Manager** (in `Assets/Scripts/UI/`):
   - `DiceUIManager.cs` - Manages UI display of dice and handles click selection.

### Steps to Set Up Dice UI in Unity Editor:

1. **Create Canvas and Dice Panel**:
   - In the Scene, create a new Canvas (GameObject → UI → Canvas).
   - Create a Panel as child of Canvas (GameObject → UI → Panel). Name it "DicePanel".
   - Adjust Panel's RectTransform to position at bottom of screen (e.g., anchor to bottom, stretch horizontally, set height to 20-30% of screen).

2. **Create Dice Buttons**:
   - Inside DicePanel, create 6 Buttons (GameObject → UI → Button). Name them "ShapeDice0", "ShapeDice1", "ShapeDice2", "BuildingDice0", etc.
   - Arrange them in two rows (top row for shape dice, bottom row for building dice) using Horizontal Layout Group or manual positioning.
   - For each button, add a TextMeshPro - Text (UI) child (or use existing Text child). Set text placeholder to "Shape" or "Building".
   - Optionally add an Image component for background color (or use Button's Image).

3. **Assign UI Elements to DiceUIManager**:
   - Create an empty GameObject (e.g., "UIManagers") and attach the `DiceUIManager` script.
   - Assign the DiceManager reference (if DiceManager is in scene, drag it; otherwise DiceUIManager will find it at runtime).
   - In the Inspector, expand Shape Dice UI Elements and Building Dice UI Elements.
   - Drag each button into the respective `Button` list (order matters: index 0,1,2).
   - Drag each button's TextMeshPro component into the `Texts` list.
   - Drag each button's Image component into the `Backgrounds` list.
   - Adjust colors if desired (default, selected, invalid).

4. **Connect DiceManager to GameManager**:
   - Ensure a DiceManager component exists in the scene (attach to a GameObject, e.g., "Managers").
   - In GameManager Inspector, assign the DiceManager reference.

5. **Testing**:
   - Enter Play Mode.
   - In the Console, you can call `GameManager.Instance.startNewTurn()` to roll dice (or attach a temporary UI button).
   - Dice buttons should update with random faces (TShape, SquareShape, etc., and Industrial, Residential, etc.).
   - Clicking a shape die selects it (turns green), clicking another shape die switches selection.
   - Same for building dice.
   - Check Console logs for dice roll output.
   - **Quick Testing Script**: A `DiceTester` script is available in `Assets/Scripts/Test/`. Attach it to a GameObject and use the ContextMenu items (right-click component) to test dice rolls, selection, and double detection.

### Integration with Game Flow:
- The dice system is integrated into `GameManager.startNewTurn()` which calls `DiceManager.RollAllDice()` and clears selection.
- Selection state can be queried via `DiceManager.GetSelectedShapeType()` and `GetSelectedBuildingType()`.
- Water die special case: `DiceManager.IsSelectedBuildingWater()`.

### Next Steps:
- **✓ Dice selection connected to shape generation**: When player selects shape and building dice, shape is created on grid click.
- **✓ Star awarding for double rolls implemented**: Dice with double faces highlighted yellow; stars awarded on placement.
- **✓ Dice reroll and UI update on placement confirmation**: After shape placement, dice reroll, selection cleared, UI updated.
- **✓ Shape updates when dice selection changes**: Unconfirmed shapes automatically update to match selected dice (preserves position, rotation, flip).
- Replace text with icons/sprites for each shape and building type.
- Add 3D dice models and animations later.
- **✓ Water die special handling implemented**: Player can choose building type when water die selected; panel appears/hides appropriately; validation rules updated.
- **✓ Wildcard system implemented**: Two wildcard buttons for each set of dice; cancel button reverts wildcard usage and hides wildcard panel. 

[*UI elements are now added and connected to the DiceUIManager; DiceManager is connected to GameManager*]

## Water Die Special Handling [X]

Water die special handling has been implemented in code. When a player selects a water die (building die face "Water"), they must choose a building type (Industrial, Residential, Commercial, School, Park) for the shape. The code now supports this with the following changes:

1. **DiceManager**: Added `waterDieChosenBuildingType` field and methods to get/set chosen building type.
2. **ShapeManager**: Updated to use `GetBuildingTypeForShape()` which returns chosen building type for water die.
3. **GameManager**: Updated star awarding to use selected dice faces (supports water die double rolls).
4. **DiceUIManager**: Added UI fields for water die panel and building type buttons.

### Steps to Set Up Water Die UI in Unity Editor:

1. **Create Water Die Panel**:
   - In the Canvas, create a new Panel child (GameObject → UI → Panel). Name it "WaterDiePanel".
   - Position it appropriately (e.g., centered over the dice area).
   - Add a Vertical Layout Group to arrange buttons neatly.
   - Add a Text header (optional) "Choose Building Type".

2. **Create Building Type Buttons**:
   - Inside WaterDiePanel, create 5 Buttons (GameObject → UI → Button).
   - Name them: "IndustrialButton", "ResidentialButton", "CommercialButton", "SchoolButton", "ParkButton".
   - For each button, set the button text to the building type name (or use icons later).
   - Style as desired.

3. **Assign UI Elements to DiceUIManager**:
   - Select the GameObject with `DiceUIManager` script.
   - In the Inspector, find the "Water Die UI" section.
   - Drag the WaterDiePanel GameObject to the "Water Die Panel" field.
   - Drag each button to the corresponding field (Industrial Button, etc.).

4. **Testing**:
   - Enter Play Mode.
   - Roll dice until a water die appears (face "Water").
   - Click the water die (building dice row). The water die panel should appear.
   - Click one of the building type buttons. The panel should hide after selection.
   - Create a shape (click on grid). The shape should have the chosen building type color.
   - Place the shape adjacent to a river tile (water die exception). Validation should pass.
   - If water die is a double face (appears twice) and selected, star should be awarded on placement.

### Validation Rules:
- Water die selected → `waterDieUsedThisTurn` flag set true.
- Shape must be adjacent to river tile (orthogonal adjacency).
- **Important**: Water die does NOT bypass first-turn rule. On first turn, shape must still overlap starting position AND be adjacent to river.
- Water die bypasses subsequent-turn adjacency rule (does not need to be adjacent to existing buildings).
- Player must choose a building type before shape can be created (otherwise shape creation fails with log message).

### Notes:
- The water die panel automatically shows when water die is selected (even if already selected).
- **Panel hides after building type selection**. To change building type, player must reselect the water die in dice panel (panel will reappear).
- Chosen building type persists while water die remains selected.
- When dice are rolled (new turn), chosen building type is cleared.
- Star awarding now uses selected dice faces (not shape building type), so water die double rolls correctly award stars.

[*Water die panel fully implemented with intended behavior*]

## Wildcard System Implementation [Phase 1: Button-Based] [X]

The wildcard system allows players to override dice faces up to 3 times per game with escalating costs (-1, -2, -3 points). Phase 1 implements two dedicated wildcard buttons (shape and building) with selection panels.

### Code Changes Made:

1. **GameManager.cs** - Added wildcard tracking:
   - Constants: `MAX_WILDCARDS = 3`, `WILDCARD_COSTS = [-1, -2, -3]`
   - Methods: `CanUseWildcard()`, `GetNextWildcardCost()`, `UseWildcard()`, `GetWildcardCostTotal()`
   - `wildcardsUsed` field initialized to 0 in `Start()`

2. **WildcardSelectionPanel.cs** - New generic panel controller:
   - Manages a panel with multiple buttons (for shape or building selection)
   - Fires `onSelectionMade` event with button index (0-5)
   - Provides `Show()`, `Hide()`, `SetButtonText()` methods
   - Attach this script to wildcard panel prefabs

### Steps to Set Up Wildcard UI in Unity Editor:

#### 1. Create Wildcard Buttons
- In the Canvas (same as dice UI), create two new Buttons:
  - **Shape Wildcard Button**: Name "ShapeWildcardButton", set text "Wildcard Shape"
  - **Building Wildcard Button**: Name "BuildingWildcardButton", set text "Wildcard Building"
- Position near respective dice pools (shape button near shape dice, building button near building dice)
- Style consistently with existing dice buttons

#### 2. Create Wildcard Selection Panels
**Shape Wildcard Panel**:
- Create a new Panel child of Canvas, name "ShapeWildcardPanel"
- Add `WildcardSelectionPanel` component to it
- Inside panel, create 6 Buttons (for T, Z, Square, L, Line, Single shapes)
- Arrange in grid (2×3 or 3×2) using Layout Group
- Set button texts to shape names (or use icons later)
- In Inspector, assign the Panel GameObject to "Panel" field (or leave null to use self)
- Drag all 6 buttons into "Selection Buttons" list (order matters: 0=T, 1=Z, 2=Square, 3=L, 4=Line, 5=Single)
- Add a Cancel button (GameObject → UI → Button) as child of panel, position appropriately (e.g., below selection buttons)
- Assign the Cancel button to the "Cancel Button" field in WildcardSelectionPanel component
- Hide panel initially (uncheck GameObject active)

**Building Wildcard Panel**:
- Create another Panel, name "BuildingWildcardPanel"
- Add `WildcardSelectionPanel` component
- Create 6 Buttons (Industrial, Residential, Commercial, School, Park, Water)
- Set button texts to building type names (include "Water" for the sixth button)
- Drag buttons into "Selection Buttons" list (order: 0=Industrial, 1=Residential, 2=Commercial, 3=School, 4=Park, 5=Water)
- Add a Cancel button (GameObject → UI → Button) as child of panel, position appropriately (e.g., below selection buttons)
- Assign the Cancel button to the "Cancel Button" field in WildcardSelectionPanel component
- Hide panel initially (uncheck GameObject active)

#### 3. Update DiceUIManager References
- Select the GameObject with `DiceUIManager` script
- In Inspector, add the following new serialized fields (code already updated):
  - **Shape Wildcard Button** (Button) - assign shape wildcard button
  - **Building Wildcard Button** (Button) - assign building wildcard button
  - **Shape Wildcard Panel** (WildcardSelectionPanel) - assign shape panel component
  - **Building Wildcard Panel** (WildcardSelectionPanel) - assign building panel component
  - **Wildcard Count Text** (TextMeshProUGUI) - create text element showing "3/3", "2/3", etc.
  - **Wildcard Cost Text** (TextMeshProUGUI) - create text showing "Cost: -1", etc.
- Position text elements near wildcard buttons

#### 4. Set Up Button Wiring (Automated)
The updated `DiceUIManager.Start()` will:
- Wire wildcard button click events to `OnShapeWildcardButtonClicked()` and `OnBuildingWildcardButtonClicked()`
- Subscribe to panel selection events
- Update UI state based on wildcard count

#### 5. Update DiceManager for Wildcard Overrides
- `DiceManager` now has `ApplyWildcardOverride(DiceType diceType, int dieIndex, int overrideFace)` method
- Called when wildcard selection is made
- Overrides die face for current turn (does not affect original roll for star awarding)

### Testing Wildcard Functionality:

1. **Enter Play Mode**
2. **Check wildcard buttons are visible and enabled** (should show "3/3" and "Cost: -1")
3. **Click Shape Wildcard Button**:
   - Shape wildcard panel should appear with 6 shape buttons
   - Cancel button is available to close panel without using wildcard
   - Click a shape (e.g., "T")
   - Panel should hide
   - Selected shape die (or first shape die if none selected) should change to chosen shape
   - Wildcard count should update to "2/3", cost to "Cost: -2"
4. **Click Building Wildcard Button**:
   - Building wildcard panel appears with 6 building type buttons (including Water)
   - Cancel button is available to close panel without using wildcard
   - Selection updates building die face
   - Count updates to "1/3", cost to "Cost: -3"
5. **Test max limit**:
   - After 3 uses, both wildcard buttons should be disabled
   - Panels should not appear when clicked
6. **Test with dice selection**:
   - Select a specific shape die (turns green)
   - Click shape wildcard button → should override selected die
   - Select a specific building die → wildcard building should override that die
7. **Test star awarding with wildcards**:
   - Wildcard overrides should NOT affect star eligibility
   - Stars awarded based on original dice faces, not wildcard choices

### Notes:
- Wildcard costs are applied at game end (via `GetWildcardCostTotal()`)
- Wildcard usage persists across turns within a game
- Reset on new game (GameManager.Start() resets `wildcardsUsed`)
- Building wildcard panel includes Water button (index 5) allowing selection of Water building type
- Both wildcard panels now have Cancel buttons to close panel without using a wildcard
- Phase 2 will add long-press on dice faces as alternative activation method

### Potential Next Steps (Phase 2):
- Implement `LongPressHandler.cs` utility for hold gestures
- Add long-press detection to dice buttons
- Add visual feedback (timer ring, button scaling)
- Add sound effects and haptic feedback

[*Wildcard buttons and panels fully implemented with intended behavior.*]

## Zone Detection System [X]

Zone detection algorithms have been implemented to automatically group Industrial, Residential, and Commercial buildings into contiguous zones for scoring. The system detects adjacency, merges zones when shapes connect them, and tracks unique shape types within each zone.

### Code Changes Made:

1. **Zone.cs** - Enhanced zone class:
   - Fixed `getUniqueShapeCount()` to correctly count distinct shape types
   - Added `AddShape()`, `MergeZone()`, and `ContainsShape()` methods
   - Stores list of shapes and building type

2. **ZoneManager.cs** - New singleton manager (in `Assets/Scripts/Core/`):
   - Maintains list of all zones on the board
   - `AddShape(ShapeController shape)` - main entry point called after shape placement
   - Automatically detects adjacent zones of same building type
   - Creates new zones or merges existing ones as needed
   - Updates `GridTile.zone` references for all occupied tiles
   - Logs debug information for testing

3. **GameManager.cs** - Integration:
   - Added `ZoneManager` reference and `ZoneManager` property
   - Automatically creates `ZoneManager` GameObject if none exists in scene
   - Calls `zoneManager.AddShape(shape)` in `OnShapePlacementConfirmed()`

### How It Works:

- After a shape placement is confirmed, `GameManager.OnShapePlacementConfirmed()` calls `ZoneManager.AddShape()`.
- ZoneManager checks the shape's building type:
  - Industrial, Residential, Commercial: form zones
  - School, Park, Water: do not form zones (tile zone references set to null)
- For zone types, the manager finds all adjacent zones of the same building type:
  - No adjacent zones → creates new zone
  - One adjacent zone → adds shape to that zone
  - Multiple adjacent zones → merges all into one zone, adds shape
- Zone references are updated on each occupied `GridTile` for future adjacency checks.

### Testing Zone Detection:

1. **Enter Play Mode** and place shapes via mouse click (select dice first).
2. **Check Console logs** for ZoneManager messages:
   - "ZoneManager: Created new Industrial zone."
   - "ZoneManager: Added shape to existing Residential zone."
   - "ZoneManager: Merging X zones for Commercial."
3. **Verify zone merging** by placing shapes that connect separate zones of same type.
4. **Verify non-zone buildings** (School, Park) do not create zones.

### Notes:

- Zones are only for Industrial, Residential, Commercial buildings (as per rules).
- Schools and Parks do not form zones but will later affect scoring (adjacency to zones).
- Unique shape counting is based on `ShapeType` (T, Z, Square, L, Line, Single).
- Zone detection runs after each placement; there is no full board scan (not needed).
- The system assumes shapes are never removed (no removal logic).

### Next Steps:

- ~~Implement scoring system that uses zone data (zone scores, park bonuses, school bonuses).~~ **IMPLEMENTED**
- Add visual feedback for zones (border coloring, zone highlighting).

## Scoring System [X]

The scoring system has been fully implemented according to the ruleset specifications. It calculates all score components including zone scores, park/school bonuses, star points, and penalties.

### Code Changes Made:

1. **ScoreComponents.cs** - New struct in `Assets/Scripts/`:
   - Contains all score components (industrialZoneScore, residentialZoneScore, commercialZoneScore, parkScore, schoolScore, starScore, emptyCellPenalty, wildcardCostTotal, totalScore)
   - `CalculateTotal()` method sums all components
   - `ToString()` method for formatted score breakdown

2. **ScoreManager.cs** - New singleton manager (in `Assets/Scripts/Core/`):
   - `CalculateScore()` - main method that computes complete score breakdown
   - `CalculateZoneScore()` - computes zone scores based on unique shapes (scoring table: 1=1, 2=2, 3=4, 4=7, 5=11, 6=16)
   - `CalculateParkScore()` - +2 points per distinct contiguous zone orthogonally adjacent to each park
   - `CalculateSchoolScore()` - +2 points per residential building in largest adjacent residential zone (one school per zone)
   - `CalculateEmptyCellPenalty()` - -1 point per empty non-river cell
   - Integration with existing GameManager star tracking and wildcard cost tracking

3. **ZoneManager.cs** - Enhanced with adjacency methods:
   - `GetAdjacentZones(ShapeController shape)` - returns all zones orthogonally adjacent to a shape
   - `GetZonesAdjacentToPosition(GridPosition position)` - returns zones adjacent to specific position

4. **GameManager.cs** - Integration:
   - Added `ScoreManager` reference and `ScoreManager` property
   - Automatically creates `ScoreManager` GameObject if none exists in scene
   - Added `CalculateFinalScore()` public method to get score breakdown
   - ScoreManager automatically initialized in `Start()` method

5. **ScoreTester.cs** - New test script (in `Assets/Scripts/Test/`):
   - Context menu tests for individual score components and complete breakdown
   - Attach to GameObject and use right-click component menu to test

### How Scoring Works:

1. **Zone Scoring** (Industrial, Residential, Commercial):
   - Each contiguous zone of same building type scores based on unique shape types within it
   - Scoring table: 1 unique shape = 1pt, 2 = 2pt, 3 = 4pt, 4 = 7pt, 5 = 11pt, 6 = 16pt

2. **Park Scoring**:
   - Each park scores +2 points per distinct contiguous zone orthogonally adjacent
   - Multiple parks can score from same zone
   - Any zone type (Industrial, Residential, Commercial) counts

3. **School Scoring**:
   - Each school scores +2 points per residential building (shape) in the largest adjacent residential zone
   - One school per zone and one zone per school (prevents double-counting)
   - Schools assigned to largest unassigned residential zone adjacent to them

4. **Star Scoring**:
   - Already implemented in GameManager (stars tracked per turn)
   - +1 point per star earned (max 2 per turn)

5. **Penalties**:
   - Empty cell penalty: -1 point per empty non-river cell at game end
   - Wildcard costs: -1, -2, -3 points for 1st, 2nd, 3rd wildcard use

### Testing the Scoring System:

1. **Attach ScoreTester** to a GameObject in the scene
2. **Enter Play Mode** and place some shapes to create zones, parks, schools
3. **Right-click ScoreTester component** and select test options:
   - "Test Score Calculation" - calculates and logs complete score
   - "Test Zone Scoring" - tests industrial/residential/commercial zone scores
   - "Test Park Scoring" - tests park adjacency scoring
   - "Test School Scoring" - tests school adjacency scoring
   - "Test Penalty Calculation" - tests empty cell penalty
   - "Test Complete Score Breakdown" - shows formatted score breakdown

4. **Verify calculations** match expected scores based on placed shapes

### Notes:

- Scoring is calculated on-demand via `GameManager.CalculateFinalScore()`
- Park and school scoring requires zone detection to be working correctly
- Empty cell penalty counts all empty non-river cells (including starting positions)
- Wildcard costs are automatically included from GameManager tracking
- Score breakdown can be displayed in end-game UI (future implementation)

### Next Steps:

- Create end-game screen UI to display score breakdown
- Add visual feedback for zones during scoring (highlighting, score popups)
- Implement score animation/count-up effect for game end

## End Game Logic and UI [X]

End game logic has been implemented in `GameManager.cs` with the following features:
- `CheckGameEndCondition()` placeholder for automatic game end detection
- `TriggerGameEnd()` method that calculates final score and displays end game UI
- `OnGameEndInput()` public method bound to Input System's "GameEndInput" action
- End game UI panel with score breakdown display
- Restart and main menu buttons (placeholder)

### Code Changes Made:

1. **GameManager.cs** - Added end game functionality:
   - Added `gameEnded` state field and `GameEnded` property
   - Added UI references: `endGamePanel`, `scoreBreakdownText`, `restartButton`, `mainMenuButton`
   - Added `InitializeEndGameUI()` called in `Start()` to set up button listeners
   - Added `ShowEndGameScreen()` and `HideEndGameScreen()` methods
   - Added `FormatScoreBreakdown()` to format score display
   - Added `RestartGame()` (reloads current scene) and `ReturnToMainMenu()` (placeholder)
   - Added `OnGameEndInput()` method for input system binding

### Steps to Set Up End Game UI in Unity Editor:

#### 1. Create End Game Panel
- In the Canvas (same as dice UI), create a new Panel (GameObject → UI → Panel). Name it "EndGamePanel".
- Set its RectTransform to stretch across entire screen (anchors min 0,0 max 1,1).
- Add a background Image with semi-transparent color (e.g., black with 70% alpha).
- Add a Vertical Layout Group component to arrange child elements neatly.

#### 2. Add Score Display Text
- Inside EndGamePanel, create a TextMeshPro - Text (UI) GameObject. Name it "ScoreBreakdownText".
- Set its RectTransform: center anchors, width 80% of panel, height 60%.
- Set font size large enough for readability (e.g., 24).
- Set alignment to upper left.
- Set text placeholder: "FINAL SCORE: 0\n\nIndustrial Zones: 0\n...".
- Enable Word Wrap.

#### 3. Add Restart Button
- Create a Button (GameObject → UI → Button) inside EndGamePanel. Name it "RestartButton".
- Set its child Text to "Play Again".
- Position appropriately (e.g., below score text).
- Style consistently with existing UI buttons.

#### 4. Add Main Menu Button
- Create another Button named "MainMenuButton".
- Set its child Text to "Main Menu".
- Position below restart button.
- Style consistently.

#### 5. Assign UI Elements to GameManager
- Select the GameManager GameObject (should be in scene).
- In the Inspector, find the "End Game UI" section.
- Drag the EndGamePanel GameObject to the "End Game Panel" field.
- Drag the ScoreBreakdownText TextMeshPro component to the "Score Breakdown Text" field.
- Drag the RestartButton Button component to the "Restart Button" field.
- Drag the MainMenuButton Button component to the "Main Menu Button" field.

#### 6. Test End Game Functionality
- Enter Play Mode.
- Press the key/button bound to "GameEndInput" (check Input Actions asset for binding).
- The end game panel should appear with score breakdown.
- Click "Play Again" button to reload the scene (should reset game state).
- Click "Main Menu" button (currently reloads scene as placeholder).
- Verify score calculation matches placed shapes.

### Integration with Game Flow:
- The `OnGameEndInput()` method is already bound to the "GameEndInput" action in PlayerInputs.inputactions.
- Automatic game end detection (`CheckGameEndCondition`) is not yet implemented; placeholder returns false.
- To test end game manually, press the bound input key (check binding in Input Actions).
- Score breakdown uses existing `ScoreManager.CalculateScore()` method.

### Next Steps:
- Implement `CheckGameEndCondition()` to detect when no valid placements exist.
- Add automatic game end trigger after dice selection if no valid placements.
- Create main menu scene and implement scene transition.
- Polish UI with animations and better visual design.

---

*Last Updated: 2026-01-13*