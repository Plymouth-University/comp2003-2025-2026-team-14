# Pocket Planner Mobile App - Complete Project Specifications

## Project Overview
**Pocket Planner** is a mobile app adaptation of a physical board game: a "Sim City" inspired roll-and-write city zoning game. The app serves as a digital demo and playtesting tool ahead of the physical launch in late 2026.

This is a demo/prototype app intended for playtesting and publisher pitching ahead of the physical game launch in late 2026.

- **Target Platform:** Android (Optimized for Phones & Tablets)
- **Engine:** Unity 6.3 LTS
- **Language:** C#
- **Primary Goal:** Playable prototype for single-player feedback, followed by multiplayer integration.

## Core Requirements Summary
- **Platform:** Android (Unity 6.3 LTS)
- **Target Devices:** Phones & tablets (optimized for both)
- **Multiplayer:** Peer-to-peer for up to 8 players (lobby code system)
- **Single Player:** Solo play against own high score
- **Monetization:** Free, no ads, no in-app purchases
- **Feedback System:** End-game popup for player comments
- **Scoreboard:** Local high scores, potential global leaderboard (stretch)
- **Assets:** Placeholder graphics provided; final art later
- **Sound:** Music & sound effects (Sim City cozy vibe)

## Development Environment & Tech Stack
- **Engine:** Unity 6.3 LTS (2D Core / 3D Mixed for dice)
- **Backend (Multiplayer):** Firebase Realtime Database (Game State Sync) + Google Play Games Services (Authentication)
- **Input:** Touch (Tap to place, Drag/Buttons to manipulate)
- **Assets:**
  - **Graphics:** SVGs for UI/Grid (preferred for scaling), 2D Sprites for Buildings, 3D Dice models
  - **Audio:** Dice roll SFX, Placement sounds, Background music (Sim City style)

## Game Rules (Source of Truth)
*See detailed ruleset in `ruleset.md`. Key rules summarized:*

### Board Setup
- **Grid:** 10×10 fixed grid.
- **River:** 11 fixed river tiles forming winding path down columns 4 and 5. Cannot be built upon.
- **Starting Positions:** 8 predefined locations (players choose different ones).
- **First Turn:** Must overlap chosen starting position.
- **Subsequent Turns:** Must be orthogonally adjacent to existing building.
- **Water Die Exception:** Must be adjacent to river tile; allows any building type (bypasses adjacency rules).

### Dice Mechanics
- **Dice Pool:** 6 total (3 Shape + 3 Building).
- **Shape Dice Faces:** T, Z, Square, L, Line, Single.
- **Building Dice Faces:** Industrial (I), Residential (R), Commercial (C), School (S), Park (P), Water (W).
  - **Water Clarification:** Water is unique and is not a building type even though it's part of the dice pool. Its purpose is to allow placement of any building type along the river bank, if it's selected.
- **Auto-Reroll:** If 3 identical faces in either pool, reroll those dice recursively.
- **Double Rolls:** Face appearing twice → star opportunity if selected.
- **Wildcards:** 3 max per game; escalating cost (-1, -2, -3 points). Allows manual selection of any die face.
- **Water Die:** Special die allowing riverbank placement; player chooses building type.

### Turn Structure (Simultaneous Play)
1. **Roll Phase:** All 6 dice rolled.
2. **Auto-Reroll Check:** Apply triple rerolls if needed.
3. **Selection Phase:** Player selects 1 Shape + 1 Building die.
4. **Placement Viability Check:** If no valid placement exists → game ends.
5. **Shape Creation:** Instantiate shape based on selection.
6. **Manipulation Phase:** Move, rotate, flip shape.
7. **Placement Validation:** Check boundaries, river, adjacency rules.
8. **Star Awarding:** Award stars for matching double rolls.
9. **Turn Increment:** Advance turn counter.

### Scoring System
- **Zones (Ind/Res/Com):** A contiguous block of the same building type.
  - *Score:* Points based on the count of **unique shapes** within that zone.
  - 1 unique: 1 pt, 2: 2 pts, 3: 4 pts, 4: 7 pts, 5: 11 pts, 6: 16 pts.
- **Park Scoring:** +2 pts per **distinct zone** orthogonally adjacent.
- **School Scoring:** +2 pts per **residential building** (count of individual shapes) in the largest adjacent residential zone (one school per zone).
- **Stars:** +1 pt per star earned (max 2 per turn). Get 2 stars if placing a building that matches two "Double Rolls" for both the shape dice and building dice.
- **Penalties:** -1 pt per empty non-river cell at game end.
- **Wildcard Costs:** -1, -2, -3 pts for 1st, 2nd, 3rd use.

### Game End Conditions
- **Automatic End:** When player cannot place any valid building.
- **Multiplayer:** Game ends for all players when one player cannot continue.
- **End Sequence:** Calculate penalties, final score, display breakdown.

## Unity Architecture & Components

### Project Structure
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── TilemapManager.cs
│   │   ├── DiceManager.cs
│   │   ├── ShapeManager.cs
│   │   ├── ZoneManager.cs
│   │   └── ScoreManager.cs
│   ├── UI/
│   │   ├── UIManager.cs
│   │   ├── MenuManager.cs
│   │   ├── LobbyManager.cs
│   │   └── FeedbackManager.cs
│   ├── Multiplayer/
│   │   ├── MultiplayerManager.cs
│   │   ├── FirebaseManager.cs
│   │   └── SyncManager.cs
│   ├── Data/
│   │   ├── GameData.cs
│   │   ├── PlayerData.cs
│   │   └── ScoreData.cs
│   └── Utilities/
│       ├── Constants.cs
│       ├── Extensions.cs
│       └── Helpers.cs
├── Prefabs/
│   ├── Grid/
│   │   ├── Tile.prefab
│   │   └── RiverTile.prefab
│   ├── Shapes/
│   │   ├── TShape.prefab
│   │   ├── ZShape.prefab
│   │   ├── SquareShape.prefab
│   │   ├── LShape.prefab
│   │   ├── LineShape.prefab
│   │   └── SingleShape.prefab
│   ├── UI/
│   │   ├── DiceUI.prefab
│   │   ├── PlayerStatus.prefab
│   │   └── ScoreBreakdown.prefab
│   └── Dice/
│       └── Dice3D.prefab
├── ScriptableObjects/
│   ├── ShapeData.asset
│   ├── BuildingTypeData.asset
│   ├── ScoreTable.asset
│   └── GameSettings.asset
├── Scenes/
│   ├── Splash.unity
│   ├── MainMenu.unity
│   ├── Lobby.unity
│   ├── Game.unity
│   └── EndGame.unity
├── Art/
│   ├── Placeholders/ (provided by client)
│   │   ├── Logos/
│   │   ├── Shapes/ (SVG)
│   │   ├── Grid/ (SVG)
│   │   └── DiceFaces/
│   └── UI/
│       ├── Sprites/
│       └── Fonts/
├── Audio/
│   ├── Music/
│   └── SFX/
└── Resources/
    └── GameConfig.json
```

### Core Classes (C# Port from C++ Prototype)

*See detailed class diagram in `cpp-prototype.md`. Key structures defined in C#:*

#### 1. Data Structures
```csharp
[System.Serializable]
public struct GridPosition {
    public int x;
    public int y;
    // Equality operators, hashcode, etc.
}

[System.Serializable]
public struct ScoreComponents {
    public int industrialZoneScore;
    public int residentialZoneScore;
    public int commercialZoneScore;
    public int parkScore;
    public int schoolScore;
    public int starScore;
    public int penaltyScore;
}
```

#### 2. Enums
```csharp
public enum ShapeType {
    T, Z, Square, L, Line, Single
}

public enum BuildingType {
    Industrial,    // Yellow
    Residential,   // Green
    Commercial,    // Blue
    School,        // White
    Park,          // Grey/Black
    Water          // Special (not a building type)
}

public enum DiceType {
    Shape,
    Building
}
```

#### 3. ShapeController Class
```csharp
public abstract class ShapeController : MonoBehaviour {
    private BuildingType buildingType;
    private ShapeData shapeData;
    private GridPosition center;
    private int rotationState;
    private bool flipped;

    public void UpdateShapePosition();
    public void Rotate() { /* 90° clockwise */ }
    public void Flip() { /* horizontal mirror */ }
    public bool IsValidPlacement(Board board) { /* validation logic */ }
    // ... other methods from cpp-prototype.md
}

// ShapeData class creates scriptable objects to inject data to a Shape and instantiate it at runtime.
[CreateAssetMenu(fileName = "ShapeData", menuName = "Scriptable Objects/ShapeData")]
public class ShapeData : ScriptableObject
{
    public string shapeName;
    public Sprite icon;
    public Color color;
    // Define the shape layout relative to center (0,0)
    public List<GridPosition> relativeTilePositions;
}

// A Scriptable Object can be created from this class for each shape type (eg. ZShape, TShape, Square...) 
```

#### 4. Dice System
```csharp
public class Dice {
    public DiceType type;
    public int currentFace;
    public bool selected;

    public void Roll() { /* random face */ }
}

public class DicePool {
    private List<Dice> shapeDice = new List<Dice>(3);
    private List<Dice> buildingDice = new List<Dice>(3);

    public void RollAll() { /* roll all 6 dice */ }
    public void PerformAutoRerolls() { /* check for triples */ }
    public List<int> GetDoubleFaces(DiceType type) { /* detect doubles */ }
}
```

#### 5. Grid & Zone System
```csharp
public class GridTile : MonoBehaviour {
    public GridPosition position;
    public bool isRiver;
    public bool isStartingSpace;
    public int startingPositionNumber;

    public ShapeController placedShape;
    public Zone zone;
}

public class Zone {
    public BuildingType buildingType;
    public List<ShapeController> shapes = new List<ShapeController>();

    public int GetUniqueShapeCount() { /* count distinct shape types */ }
    public int GetScore() { /* calculate zone score */ }
}
```

#### 6. Game State Manager
```csharp
public class GameManager : MonoBehaviour {
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game state
    private int currentTurn;
    private int stars;
    private int wildcardsUsed;
    private DicePool dicePool;
    private bool waterDieUsedThisTurn;
    private int selectedStartingPosition;
    private bool firstTurnCompleted;

    // References
    private BoardManager boardManager;
    private ShapeManager shapeManager;
    private ZoneManager zoneManager;
    private UIManager uiManager;

    void Awake() { /* singleton setup */ }

    public void StartNewTurn() { /* roll dice, etc */ }
    public bool IsValidPlacement(Shape shape) { /* validation */ }
    public ScoreComponents CalculateScore() { /* compute all scores */ }
}
```

### Architecture Guidelines (C# / Unity)
- **Separation of Concerns:**
  - `TilemapManager`: Handles Grid logic, valid move checking, and coordinate translation.
  - `DiceManager`: Handles 3D rolling, RNG, and result data.
  - `GameManager`: Manages game state (Roll -> Select -> Place -> Wait).
  - `ScoreCalculator`: Pure logic class for calculating points based on board state.
- **Data Structures:**
  - Use a 2D Array (`Node[,]`) for logical board state (ZoneType, Shape reference).
  - Use `Tilemap` for visual representation (zone merging - colored borders merging for adjacent same-type zones).
- **Managers:** Singleton pattern for `GameManager` and `AudioManager`.

## UI/UX Specifications

### Screen Flow

1. **Main Menu**
   - Game title/logo
   - Buttons:
     - Solo Play (direct to game setup)
     - Multiplayer (opens submenu)
     - How to Play / Rules
     - Settings
     - Exit (optional)
   - Multiplayer submenu:
     - Create Lobby (generates code)
     - Join Lobby (enter code)
     - Matchmaking (stretch goal)

2. **Lobby Screen**
   - Room code displayed prominently
   - Player list with ready status
   - Crown icon for lobby host
   - Host can kick players (tap player → kick option)
   - "Start Game" button (host only)
   - "Leave Lobby" button

3. **Game Screen (Primary Focus)**
   - **Top Section (20%):**
     - Turn counter
     - Player status indicators (dots/checks for waiting/ready)
     - Wildcard counter & cost
     - Menu button (settings/rules/scoring table)

   - **Middle Section (60%):**
     - 10×10 game board (centered)
     - River tiles visually distinct (cyan)
     - Color-coded buildings (Green=R, Blue=C, Yellow=I, White=S, Grey=P)
     - Starting positions (numbers, disappear after first turn)
     - Current shape as "ghost" with outline during positioning

   - **Bottom Section (20%):**
     - Dice display area (after roll) - dice move to a UI tray for selection
     - Shape dice row (3 dice)
     - Building dice row (3 dice)
     - Selected dice highlighted
     - Shape manipulation buttons (Rotate, Flip, Reset)
     - Confirm Placement button
     - "Switch View" button (shows other players' boards) / "Spectator Bar": Swipe to view other players' boards

4. **End Game Screen**
   - Final rankings (1st, 2nd, etc.)
   - Score breakdown (expandable sections):
     - Zone scores (I/R/C)
     - Park scores
     - School scores
     - Stars
     - Penalties
     - Wildcard costs
     - TOTAL
   - Button to view other players' breakdowns
   - "Play Again" button
   - "Main Menu" button
   - Feedback popup (optional) - simple text box modal at Game Over screen for user feedback

### Input & Interaction
- **Shape Placement:**
  - Place: Tap any space on grid to place 'ghost' shape (low opacity)
  - Manipulation: Drag ghost shape around board
  - Visual feedback: Red outline if invalid, green if valid
  - Rotation: Tap rotate button (90° clockwise)
  - Flip: Tap flip button (horizontal mirror)
- **Dice Selection:** Tap dice to select/deselect
- **Board Viewing:** "Switch View" opens player selection modal
- **Quick Reference:** Tap menu/settings button to view scoring table and rules mid-game
- **Multiplayer Turns:** Async; see who's finished (green check) vs waiting (red dot)

### Visual Design
- **Color Scheme:**
  - Residential: Green (#4CAF50)
  - Commercial: Blue (#2196F3)
  - Industrial: Yellow (#FFEB3B)
  - School: White (#FFFFFF)
  - Park: Grey (#9E9E9E) with dark border
  - River: Cyan (#00BCD4)
  - Starting Positions: Magenta (#E91E63)
  - Valid placement: Green outline
  - Invalid placement: Red outline
- **Typography:** Clear, legible sans-serif (Roboto recommended)
- **Icons:** Standard Material Design icons for buttons
- **Animations:**
  - Dice rolling (pre-baked 3D animations)
  - Shape placement (subtle "drop" effect)
  - Score tallying (counting up)
  - Star appearance (sparkle effect)

## Multiplayer Architecture

### Technology Stack
- **Backend:** Firebase Realtime Database (free tier)
- **Authentication:** Google Play Services (optional, for player names)
- **Netcode:** Client-authoritative with server validation
- **Synchronization:** Game state JSON serialization

### Lobby System
1. **Creation:** Host creates lobby → generates 6-character code
2. **Joining:** Players enter code → joins lobby
3. **State Sync:** All players see same player list, ready status
4. **Start Game:** Host triggers → all clients load game scene
5. **Disconnection:** Player can rejoin with same code to catch up
6. **Vote Kick:** Stretch goal (host or majority vote)

### Game State Sync
- **Turn-based sync:** After each player confirms placement
- **Minimal data:** Send shape type, building type, position, rotation
- **Validation:** Client-side validation + server sanity check
- **Catch-up:** Reconnecting players receive missed turns

## Audio Specifications

### Sound Effects
- **Dice rolling:** Physical dice sounds
- **Dice selection:** Click/confirm
- **Shape movement:** Subtle slide
- **Shape rotation/flip:** Mechanical click
- **Placement valid:** Satisfying "thud" or "click"
- **Placement invalid:** Error buzz
- **Star awarded:** Sparkle/chime
- **Zone merge:** Gentle "pop"
- **Button clicks:** Standard UI sounds
- **Game end:** Fanfare

### Music
- **Main Menu:** Calm, Sim City-style ambient
- **Gameplay:** Light background music (loopable)
- **End Game:** Triumphant version of main theme
- **Volume Controls:** Separate sliders for music/SFX

## Performance & Optimization

### Target Specifications
- **FPS:** Stable 60fps on mid-range Android devices
- **Memory:** < 200MB RAM usage
- **Load Times:** < 5 seconds scene transitions
- **Battery:** Efficient use of GPU/CPU

### Optimization Strategies
- **3D Dice:** Pre-baked animations instead of real physics
- **Object Pooling:** Reuse shape/tile GameObjects
- **Texture Atlas:** Combine UI sprites
- **LOD:** Not needed for 2D game
- **Code:** Avoid Update() loops; use event-driven patterns
- **GC:** Minimize allocations; reuse collections

## Development Phases

### Phase 1: Core Game Logic (MVP)
- Single player only
- Basic UI (no polish)
- Core rules implementation
- Scoring system
- Local high scores

### Phase 2: Multiplayer
- Firebase integration
- Lobby system
- Peer-to-peer sync
- Player status indicators

### Phase 3: Polish & UX
- 3D dice animations
- Sound effects & music
- UI polish (animations, transitions)
- Feedback system
- Rulebook integration

### Phase 4: Advanced Features
- Matchmaking
- Global leaderboards
- Preset chat commands (no open chat, preset messages like "Good Game" only)
- Player profiles
- Statistics tracking

## Testing Plan

### Unit Tests
- Shape placement validation
- Scoring calculations
- Zone detection algorithms
- Dice roll probabilities

### Integration Tests
- Multiplayer synchronization
- Firebase connectivity
- UI flow and navigation
- Save/load functionality

### User Testing
- Playtest with target audience (11+)
- Collect feedback via in-app system
- Observe UX pain points
- Performance on various devices

## Deployment Checklist

### Google Play Store
- App signing key
- Store listing (description, screenshots)
- Age rating (11+)
- Privacy policy
- Content rating questionnaire
- APK/AAB build

### Firebase Setup
- Project creation
- Realtime Database rules
- Authentication setup (optional)
- Security rules configuration

### Analytics (Optional)
- Google Analytics for Firebase
- Crashlytics for error reporting
- Player behavior tracking (anonymous)

## Success Metrics
- **Game Stability:** < 1% crash rate
- **Player Retention:** > 30% return after first play
- **Session Length:** 10-15 minutes average
- **Feedback Quality:** Actionable comments from players
- **Performance:** Runs smoothly on 3-year-old Android devices

## Non-Functional Requirements
- **GDPR/LSEP:**
  - No PII collection (No email/names stored).
  - Safe for ages 11+.
  - No open chat (Preset messages only).
- **Accessibility:** Colorblind support (Icons + Colors used redundantly).
- **Performance:** Dice physics must be optimized for mobile (potentially baked).

## Asset Placeholder List
1. **Logos:** Game Logo, Company Logo.
2. **Sprites:** Tetromino Shapes (Black & White), Icon Pack (SVG).
3. **Grid:** 10x10 Grid SVG with River markings.
4. **Dice:** 3D Models / Faces.

## Notes & Decisions

### Key Design Decisions
1. **Simultaneous Turns:** All players act concurrently (async multiplayer)
2. **Hidden Scoring:** Scores revealed only at game end (as per board game)
3. **Pre-baked Dice Animations:** Better performance than physics simulation
4. **Firebase over Photon:** Chosen for simplicity and Google Play integration
5. **No Login System:** Use Google Play handle if available, else "Player X"
6. **Lobby Codes over Matchmaking:** Simpler implementation for MVP
7. **Portrait Orientation First:** Better for phone gameplay, although support should be added for landscape. 
8. **Input:** Use unity's newest Input System utilizing actions. *Add unity editor instructions to `developer-instructions.md`.*  

### Risks & Mitigations
- **Firebase Costs:** Free tier sufficient for playtesting; monitor usage
- **Performance on Old Devices:** Test early on low-end hardware
- **Multiplayer Sync Bugs:** Implement extensive logging and reconnection
- **Asset Delays:** Use placeholder colors until final assets arrive
- **Scope Creep:** Stick to MVP; advanced features as stretch goals

## Additional Context Files
- These files are in the project's working directory. Read or write to them for additional context. 
- **./current-functionality.md:** Documentation on the current features, 'what has been implemented so far' in the project and 'what is next'. Always update this file after adding a feature.
- **./user-stories.md:** Contains a list of user stories in order of priority with acceptance criteria. Critical functionality to be implemented first.
- **./ruleset.md:** Provides more detailed rule clarifications, including specifications for the fixed river tile and starting positions. Read only. 
- **./developer-instructions.md:** Write to this file specifically to instruct the human developer to perform any actions that are required in the Unity Editor.  
- **./cpp-prototype.md:** A class diagram written in Mermaid JS representing the architecture of the c++ CLI prototype of Pocket Planner. Read only.

### Final note
- The terms 'Zone' and 'Building' are sometimes used interchangeably when describing requirements. In the rules however, the distinction is important, only Industrial, Commercial and Residential buildings are Zone types. 

---

*This document serves as the single source of truth for the Pocket Planner Unity mobile app development.*