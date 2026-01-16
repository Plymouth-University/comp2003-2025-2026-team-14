# Pocket Planner CLI - Complete Ruleset

## Overview
Pocket Planner will be a mobile app adaptation of a board game, a prototype was implemented in C++17 with a CLI interface using ncurses. This document describes the complete ruleset, covering all gameplay mechanics, scoring systems, and user interactions.

## Table of Contents
1. [Board Setup](#board-setup)
2. [Dice Mechanics](#dice-mechanics)
3. [Turn Structure](#turn-structure)
4. [Placement Rules](#placement-rules)
5. [Shape System](#shape-system)
6. [Scoring System](#scoring-system)
7. [Game End Conditions](#game-end-conditions)
8. [User Interface & Controls](#user-interface--controls)
9. [Zone Detection & Composition](#zone-detection--composition)

---

## Board Setup

### Grid
- **Dimensions**: 10×10 fixed grid (100 cells total)
- **Coordinates**: (0,0) top-left to (9,9) bottom-right

### River
- **Positions**: 11 fixed river tiles forming a winding path:
  - Column 4: (0,4), (1,4), (2,4), (3,4), (4,4)
  - Column 5: (4,5), (5,5), (6,5), (7,5), (8,5), (9,5)
- **Properties**:
  - Cannot be occupied by buildings
  - Provides adjacency for Water die placements

### Starting Positions
- **8 predefined locations**:
  - (1,1), (3,3), (6,0), (8,2), (7,8), (5,6), (2,9), (0,7)
- **Display**: Numbers '1'–'8' shown
- **First Turn Rule**: First building must overlap the selected starting position; after placement, numbers disappear.

---

## Dice Mechanics

### Dice Pool
- **Total Dice**: 6 (3 Shape Dice + 3 Building Dice)
- **Rolling**: All dice rolled at start of each turn

### Shape Dice 
**Faces**:
- T-Shape (T)
- Z-Shape (Z)
- Square (Sq)
- L-Shape (L)
- Line (Ln)
- Single (Sg)

### Building Dice 
**Faces**:
- Industrial (I)
- Residential (R)
- Commercial (C)
- School (S)
- Park (P)
- Water (W) – Special die

### Auto-Reroll Rules
- **Trigger**: 3 Shape dice match OR 3 Building dice match
- **Action**: Recursively re-roll the matching dice
- **Continues**: Until no more triples exist
- **Purpose**: Prevent uninteresting turns with all-same dice

### Dice Selection
- **Requirement**: Exactly 1 Shape die + 1 Building die
- **Visual**: Selected dice highlighted

### Wildcards
- **Maximum**: 3 per game
- **Cost**: -1 point (1st), -2 points (2nd), -3 points (3rd)
- **Effect**: Player may choose any shape or building type to use, regardless of what was rolled.

### Water Die Special Rules
- **When Selected**: Player chooses any building type (Industrial, Residential, Commercial, School, or Park)
- **Placement Exception**: Bypasses normal adjacency rules
- **Riverbank Requirement**: Must be orthogonally adjacent to river tile

### Double Roll Detection
- **Definition**: Any face appearing 2 times in dice pool
- **Purpose**: Star awarded if player picks an 'in demand' type
- **Double Star Case**: If player selects both a shape and building type that appears twice in the pool, they get two stars.

---

## Turn Structure

Each turn follows this exact sequence:

1. **Roll Phase**
   - All 6 dice rolled
   - Auto-rerolls performed if triples exist
   - Double rolls detected for star opportunities

2. **Dice Selection Phase**
   - Player selects 1 Shape die + 1 Building die
   - Wildcards available for use
   - Water die allows building type choice if selected

3. **Placement Viability Check**
   - If no valid placement exists for selected shape/building combination, game ends automatically
   - Checks all orientations and positions

4. **Shape Creation**
   - Shape instantiated from selected dice faces

5. **Movement & Manipulation Phase**
   - Player moves shape 
   - Rotates and flips shape
   - Attempts placement via confirmation 

6. **Placement Validation**
   - Basic validation (bounds, river, overlap)
   - Adjacency rule checking
   - Clear feedback for violations (eg. Shape appears red)

7. **Star Awarding**
   - Stars awarded for matching double rolls
   - Up to 2 stars possible per turn
   - Visible stars appear on the placed shape

8. **Turn Increment**
   - Turn counter increases

---

## Placement Rules

### Basic Validation
1. **Boundaries**: Entire shape must fit within 10×10 grid
2. **River**: No part of shape may occupy river tile
3. **Overlap**: Shape cannot overlap existing buildings

### Adjacency Rules

#### First Turn
- **Requirement**: Must overlap a starting position (exact tile)

#### Subsequent Turns (Turns 2+)
- **Requirement**: Must be orthogonally adjacent to ≥1 existing building
- **Adjacency**: Shares edge with existing building (not diagonal)

#### Water Die Exception
- **Applies**: When Water die selected this turn
- **Requirement**: Must be orthogonally adjacent to river tile
- **Overrides**: Bypasses both first-turn and subsequent-turn adjacency rules

### Shape Manipulation
- **Movement**: Dragging the shape (on Mobile)
- **Rotation**: 90° clockwise
- **Flip**: Horizontal mirror
- **No Diagonal Movement**

---

## Shape System

### Shape Types
Six distinct shapes, each occupying specific tiles relative to center:

1. **T-Shape**: 4 tiles forming T pattern
2. **Z-Shape**: 4 tiles forming Z pattern
3. **Square**: 2×2 block (4 tiles)
4. **L-Shape**: 4 tiles forming L pattern
5. **Line**: 4 tiles in straight line
6. **Single**: 1 tile

### Shape Properties
- **Building Type**: Determined by selected Building die (or player choice for Water die)
- **Shape Type**: Determined by selected Shape die
- **Occupied Tiles**: ArrayList/Vector of positions tracking exact placement
- **Center Point**: Reference for movement/rotation

---

## Scoring System

### Zone Scoring (Industrial, Residential, Commercial)

#### Zone Composition
- **Definition**: Contiguous block of same building type (orthogonally connected), including non-unique shape types.
- **Types**: Industrial (I), Residential (R), Commercial (C) are zone types
- **Excluded**: Schools (S) and Parks (P) do not form zones

#### Scoring Formula
Score based on **unique shapes** within zone (not total shapes):

| Unique Shapes | Points |
|---------------|--------|
| 0            | 0      |
| 1            | 1      |
| 2            | 2      |
| 3            | 4      |
| 4            | 7      |
| 5            | 11     |
| 6            | 16     |

**Important**: Shape uniqueness refers to shape type (T, Z, Square, L, Line, Single), not building type.

### Park Scoring
- **+2 points** per **distinct contiguous zone** orthogonally adjacent to park (**NOT** dependant on zone type)
- **Multiple Parks**: Can score from same zone
- **Zone Type**: Any zone type (Industrial, Residential, Commercial), can score from multiple of the same type as long as they are distinct zones.
- **Adjacency**: Orthogonal only (not diagonal)

### School Scoring
- **+2 points** per **residential building** (count of shapes) in the biggest residential zone orthogonally adjacent to school
- **One School Per Zone and One Zone Per School**: Only one school scores from one adjacent residential zone
- **Zone Selection**: Largest adjacent residential zone by shape count which is not used by another school
- **Adjacency**: Orthogonal only

### Star Awarding
- **Earned**: When placed building matches a "Double Roll"
- **Double Roll**: Face appearing 2 times in dice pool
- **Maximum**: 2 stars per turn (1 for matching shape double, 1 for matching building double)
- **Value**: +1 point per star
- **Awarded**: After placement, based on shape type and building type. Displays a star on placed shape.

### Penalties
- **-1 point** per empty grid cell at game end
- **Excludes**: River cells
- **Applied**: When game ends

### Score Calculation Flow
1. **Zone Detection**: Identify all contiguous zones
2. **Zone Scoring**: Calculate each zone's score based on unique shapes
3. **Park Scoring**: Add +2 per distinct zone adjacent to each park
4. **School Scoring**: Add +2 per residential building in largest adjacent zone per school
5. **Star Addition**: Add +1 per star earned
6. **Total**: Add all scores together
7. **Penalty**: Applied at game end (including wildcards)
8. **End**: Display score at game end

---

## Game End Conditions
(in multiplayer, game ends for all players)
### Automatic End 
- **Trigger**: Player's selected shape cannot be placed anywhere on grid 
- **Check**: Tests all orientations and positions
- **Result**: Game ends, penalty applied, final score shown

### End-of-Game Sequence
1. **Penalty Calculation**: -1 per empty non-river cell
2. **Final Score**: Total score minus penalty
3. **Display**: Score breakdown and statistics

---

## User Interface & Controls

### Color Coding 
| Element | Color |
|---------|-------|
| Residential | Green |
| Commercial | Blue |
| Industrial | Yellow |
| School | White |
| Park | Grey/Black (inverted) |
| River | Cyan |
| Starting Positions | Magenta |
| Shape Dice | Cyan text |
| Building Dice | Magenta text |

### Display Elements
- **Main Grid**: 10×10 with color-coded buildings (placed shapes)
- **Dice Display**: Two rows (Shape/Building) with selection highlighting
- **Game Status**: Turn, Stars, Player statuses (Waiting/Ready in Multiplayer)
- **Validation**: Feedback for invalid placements (red shape or error message)
- **Final Scoreboard**: Comprehensive statistics at game end

### Placement Validation
- **Out of bounds**: Placement out of grid bounds
- **River overlap**: Cannot place on river
- **Building overlap**: Overlaps existing building
- **First turn adjacency**: First building must overlap a starting position tile
- **Subsequent turn adjacency**: Must be adjacent to existing building
- **Water die placement**: Water die buildings must be adjacent to river

---

## Zone Detection & Composition

### Algorithm
- **Method**: Flood-fill BFS (breadth-first search)
- **Trigger**: After each placement
- **Scope**: Industrial, Residential, Commercial buildings only

### Zone Properties
- **Building Type**: Homogeneous within zone
- **Shape Collection**: All shapes within zone boundary
- **Unique Shape Tracking**: Counts distinct shape types for scoring
- **Tile References**: Zone reference stored in each grid tile for adjacency checks

### Important Distinctions
1. **Zone Composition ≠ Scoring**:
   - Zones include all adjacent same-type buildings regardless of shape uniqueness
   - Shape uniqueness only affects scoring multiplier

2. **Park Scoring Accuracy**:
   - Parks score per distinct zone (not per unique building type)
   - Requires zone references in grid tiles for accurate counting

3. **School Zone Assignment**:
   - Schools are assigned to the largest adjacent residential zone which is not already assigned to a school
   - Prevents multiple schools scoring from same zone

### Implementation Classes
- **`Zone`**: Manages zone data, shape collection, score calculation
- **`GridTile`**: Stores zone reference, building type, shape reference
- **`GameState`**: Coordinates zone detection and scoring

---

## Glossary

| Term | Definition |
|------|-----------|
| Zone | Contiguous block of same building type (I/R/C) |
| Unique Shape | Distinct shape type (T, Z, Sq, L, Ln, Sg) within zone |
| Orthogonal Adjacency | Sharing an edge (up/down/left/right) |
| Double Roll | Dice face appearing 2 times in pool |
| Riverbank | Cells orthogonally adjacent to river tiles |
| Starting Position | One of 8 predefined locations for first turn |
| Wildcard | Optional re-roll with escalating point cost |
| Water Die | Special die allowing riverbank placement |
