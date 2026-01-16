*The list of user stories from highest priority to lowest priority*

## High Priority

**US 3.1: Board Display**

- **Story:** As a player, I want to view my city as a static 10x10 grid so that I can see the whole board at once.
    
- **Acceptance Criteria:**
    
    - The entire 10x10 grid fits within the "Safe Area" of a standard mobile screen in Portrait (or Landscape) .
        
    - Grid lines are clearly visible.


**US 3.2: Placement**

- **Story:** As a player, I want to tap anywhere on the city grid to tentatively place a "ghost" of my selected building shape, even if the spot is currently invalid.
    
- **Acceptance Criteria:**
    
    - **Prerequisite:** At least one Shape die and one Zone die must be selected before placing.
        
    - **Action:** Tapping any cell on the grid spawns the selected shape at that location.
        
    - **Visual Feedback:** The shape appears as a semi-transparent (lower opacity) "ghost" overlay.
        
    - **Illegal State:** If the tapped location creates an illegal move (e.g., overlapping or not adjacent), the shape appears **Red**.
        
    - **Legal State:** If the tapped location is valid, the shape appears **Green** (or natural color).


**US 2.3: Building Selection**

- **Story:** As a player, I want to tap one "Shape" die and one "Building/Zone" die to select the building block I will place this turn.
    
- **Acceptance Criteria:**
    
    - Tapping a die highlights it as "Selected."
        
    - The player _must_ select exactly one Shape and one Building/Zone type to proceed.
        
    - If shape is already placed, but not confirmed, changing selection updates the preview on the board immediately.


**US 3.3: Manipulation**

- **Story:** As a player, I want to manually adjust the position and orientation of the "ghost" shape so that I can fit it into my desired spot.
    
- **Acceptance Criteria:**
    
    - **Drag:** The player can drag the ghost shape across the grid.
        
    - **Rotate:** Tapping a "Rotate" button rotates the ghost 90 degrees clockwise.
        
    - **Flip/Mirror:** Tapping a "Flip" button mirrors the ghost shape horizontally.
        
    - **Live Validation:** As the player drags or rotates the shape, the color must update dynamically (Red/Green) based on the new position's validity.


**US 3.4: Rule Validation Logic**

- **Story:** As a player, I want the system to enforce the game rules so that I cannot accidentally finalize an illegal move.
    
- **Acceptance Criteria:**
    
    - **First Turn:** Placement must overlap the player's chosen Starting Number.
        
    - **Subsequent Turns:** Placement must be orthogonally adjacent to at least one existing building.
        
    - **River Exception:** If the "Water" die is used, they can place any zone/building type, and it must be placed anywhere along the river bank.
        
    - **Invalid Move:** The "Confirm" button is disabled and the shape turns red if the position is invalid, such as overlapping an existing building.


**US 3.5: Confirm Turn**

- **Story:** As a player, I want to confirm my placement with a specific button (eg. a Green Tick above the unconfirmed shape) to finalize my turn.
    
- **Acceptance Criteria:**
    
    - **Button State:** The "Confirm" button is **disabled** (greyed out) if the ghost shape is Red.
        
    - **Action:** The "Confirm" button becomes **enabled** only when the ghost shape is Green.
        
    - **Locking:** Tapping "Confirm" locks the piece to the grid, removes the ghost effect, and changes the player's status to "Ready/Waiting" for other players.


**US 1.4: Select Starting Location**

- **Story:** As a player, I want to select a starting number (1-8) so that I have a unique starting position on the map.
    
- **Acceptance Criteria:**
    
    - The UI displays 8 selectable numbered zones on the map at the start of the game.
        
    - Selecting a number locks that choice for the player and removes the other selectable numbers.
        
    - The system ensures no two players can select the same starting number.


**US 2.4: Wildcards**

- **Story:** As a player, I want to use a "Wildcard" button to change a die face to a value of my choice, accepting the future penalty to my score.
    
- **Acceptance Criteria:**
    
    - A "Use Wildcard" toggle is available in the UI.
        
    - Activating it allows the user to manually select _any_ building type or shape, ignoring the rolled results.
        
    - **Penalty Logic:** The system deducts points at the end of the game: -1 for the first use, -2 for the second, -3 for the third.


**US 6.3: Earning Stars**

- **Story:** As a player, I want to earn a "Star" automatically when I place a building that matches the "in-demand" (double roll) type.
    
- **Acceptance Criteria:**
    
    - **Logic:** If the dice roll contains doubles (2x same shape or 2x same zone), that type is "In Demand" .
        
    - If the player places a building matching the "In Demand" type, a Star icon appears on that building when placement is confirmed.

    - **Double Stars:** Two stars are awarded if a building is confirmed utilizing dice rolls of 2x same shape **and** 2x same building type
        
    - **River Logic:** Rolling double Rivers also awards a star if the player utilizes the river placement.


**US 6.1: Automated Scoring**

- **Story:** As a player, I want the game to calculate my score automatically at the end of the game so I don't have to do the math myself.
    
- **Acceptance Criteria:**
    
    - **End of Game:** A scoring table should display listing the score for each scoring category and the combined score for each player.

    - **Zone Scoring:** Points for number of unique shapes in a contiguous zone (each individual zone type should be displayed in score table).
        
    - **Park Scoring:** +2 Points for each distinct zone adjacent to the park.
        
    - **School Scoring:** +2 Points for each building contained in the largest Residential Zone orthogonally adjacent.
        
    - **Star Scoring:** +1 Point per star collected.
        
    - **Wildcard Penalty:** Subtracts points based on usage (-1, -2, -3).
        
    - **Empty Space:** -1 point per empty grid square.
        

## Medium Priority

**US 1.0: Main Menu & Navigation**

- **Story:** As a player, I want to access a main menu when I launch the app so that I can choose to start a new game, join an existing one, adjust settings, or learn how to play.
    
- **Acceptance Criteria:**
    
    - **Launch State:** This is the first screen visible upon opening the application.
        
    - **Branding:** The "Pocket Planner" game logo is clearly displayed at the top.
        
    - **Navigation:** Four distinct buttons are available: "Create Game," "Join Game," "Settings," and "Rules".
        
    - **Rules:** Tapping "Rules" opens a scrollable modal or separate scene displaying the rulebook content/images.
        
    - **Settings:** Tapping "Settings" opens a toggle menu for Audio/Music (as defined in US 5.3) as well as additional accessibility settings.


**US 6.4: Edge Case Auto-Reroll**

- **Story:** As a player, I want the system to automatically re-roll any set of dice that lands on three of the same type so that the game avoids "impossible" or unbalanced turns.
    
- **Acceptance Criteria:**
    
    - **Trigger:** The system detects if all 3 Shape Dice show the same shape OR all 3 Zone Dice show the same zone immediately after a roll.
        
    - **Action:** The system automatically triggers a re-roll animation specifically for the set of dice that failed (the triplet).
        
    - **Loop:** This check must happen recursively; if the re-roll results in another triplet, it must re-roll again until a valid combination is found.
        
    - **Feedback:** The UI must briefly indicate "Re-rolling duplicates..." so the user understands why the dice are rolling again.


**US 6.5: Game End Logic & Wildcard Check**

- **Story:** As a player with no legal moves remaining, I want the system to check if I have Wildcards available and let me choose to continue or end the game, so that I have a final chance to save my game before ending it for everyone.
    
- **Acceptance Criteria:**
    
    - **Validation:** The system runs a check after the dice roll to see if the player has _any_ valid placement options on their grid.
        
    - **Wildcard Prompt:** If **Valid Moves = 0** but **Wildcards > 0**, the system must display a modal: "No legal moves. Use a Wildcard to create a valid move, or End Game?"
        
    - **Forced End:** If **Valid Moves = 0** and **Wildcards = 0** (or if the player chooses "End Game" in the modal), the system triggers the "Game Over" state.
        
    - **Global Trigger:** Upon the "Game Over" state being triggered by _any_ single player, the server immediately transitions **ALL** players from the Gameplay Phase to the final Scoring Phase.


**US 2.1: 3D Dice Animation**

- **Story:** As a player, I want to see six 3D dice roll on the screen with physics animations so that the digital game feels like the physical board game.
    
- **Acceptance Criteria:**
    
    - The animation triggers immediately at the start of a round.
        
    - **3 Dice** must display Shape icons/textures (L-shape, Square, Line, Z-shape, T-shape and Single) .
        
    - **3 Dice** must display Zone/Building icons/textures (Residential, Commercial, Industrial, Park, School, Water) .
        
    - Total animation duration is under 3 seconds to keep gameplay fast.


**US 2.2: Dice UI Management**

- **Story:** As a player, I want the dice to automatically move aside or vanish to a UI panel after rolling so that I have a clear view of my city grid.
    
- **Acceptance Criteria:**
    
    - Upon animation completion, the 3D dice transition to static 2D icons in a "Dice Tray" UI panel (top or side of screen).
        
    - The main board area becomes fully visible and interactable.


**US 1.1: Create Lobby**

- **Story:** As a host player, I want to create a private game lobby and generate a unique "Lobby Code" so that I can invite specific friends to play.
    
- **Acceptance Criteria:**
    
    - Generating a lobby creates a unique, alphanumeric code (4-6 characters) visible on the screen.
        
    - The host is automatically assigned the "Player 1" slot.
        
    - The lobby displays a list of joined players (up to 8) in real-time.


**US 1.2: Join Lobby**

- **Story:** As a joining player, I want to enter a "Lobby Code" so that I can enter the correct game room with my group.
    
- **Acceptance Criteria:**
    
    - Input field accepts the 4-6 character code.
        
    - If the code is valid and the room is not full, the player enters the lobby.
        
    - If the code is invalid or the room is full (8/8), an error message is displayed.


**US 4.1: Simultaneous Turns**

- **Story:** As a player, I want to play my turn simultaneously with other players so that the game moves quickly without waiting for individual turns.
    
- **Acceptance Criteria:**
    
    - The "Next Round" / "New Roll" only triggers when _all_ active players have confirmed their move.
        
    - Dice results are synced: All players see the exact same roll result for the round.


**US 4.2: Player Status Widget**

- **Story:** As a player, I want to see a status indicator for opponents so I know who the group is waiting for.
    
- **Acceptance Criteria:**
    
    - A UI list shows all player names in a game.
        
    - Indicators show: "Thinking" (e.g., Dot or Spinning icon) vs "Ready" (e.g., Green Light).


## Low Priority

**US 1.3: Player Identification**

- **Story:** As a player, I want to enter a username/screen name before joining so that other players can identify me in the lobby and on the status board.
    
- **Acceptance Criteria:**
    
    - Usernames are text-only and limited to 12 characters for UI spacing.
        
    - **Constraint:** Usernames do not require a password or email registration (Guest mode).


**US 5.1: Building Visualization**

- **Story:** As a player, I want placed buildings to display specific icons or textures (e.g., factories for industrial, houses for residential) so the map looks like a real city.
    
- **Acceptance Criteria:**
    
    - **Residential:** Displays Houses.
        
    - **Park:** Displays a Park and Trees.
        
    - **School:** Displays School.
        
    - **Industrial:** Displays Factory.
        
    - **Commercial:** Displays Shopping center.


**US 5.2: Zone Merging**

- **Story:** As a player, I want the zones to have colored borders that visually merge when I place matching types next to each other.
    
- **Acceptance Criteria:**
    
    - Adjacent shape blocks of the same type (e.g., Residential next to Residential) share a continuous colored border, visually forming a "District" .
        
        
**US 4.4: Spectate Opponent Boards**

- **Story:** As a player, I want to swipe between the boards of other players using a bottom UI bar so that I can view their cities and monitor their progress during the game.
    
- **Acceptance Criteria:**
    
    - **UI Bar:** A dedicated "Spectator Bar" at the bottom of the screen displays the Username of the player whose board is currently being viewed.
        
    - **Interaction:** Swiping **Left or Right** on the bar cycles through the active players in the lobby.
        
    - **Visual Clarity:** When viewing an opponent's board, the UI must clearly indicate "Viewing: [Player Name]" to distinguish it from the user's own active board.
        
    - **Round Updates:** The opponent's board must update after every round.


**US 5.3: Audio Feedback**

- **Story:** As a player, I want to hear sound effects for dice rolling and building placement to increase immersion.
    
- **Acceptance Criteria:**
    
    - Audio plays on: Dice Roll, Building Placed (e.g., construction thud), Star Earned, and Game Over .
        
    - Option to mute sound in Settings.


**US 6.2: Submit Feedback**

- **Story:** As a playtester, I want to open a simple text box at the end of the game to submit feedback or report bugs directly to the developers.
    
- **Acceptance Criteria:**
    
    - Button "Give Feedback" appears on the Game Over / Scoreboard screen.
        
    - Opens a modal with a text field.
        
    - Submitting sends the text to the developer without requiring an email client .


**US 4.3: Rejoin & Catch-up**

- **Story:** As a player who disconnected, I want the game to let me replay the specific dice rolls I missed when I reconnect so that I can catch up to the group fairly.
    
- **Acceptance Criteria:**
    
    - System detects player reconnection and restores their game state.
        
    - Game enters "Catch Up Mode" for that player only: They are presented with the historical dice rolls for the rounds they missed (Round X, X+1...) .
        
    - The live game waits (or continues, pending specific design) until the player catches up to the current round.


**US 4.5: Global Leaderboard**

- **Story:** As a player I want my highest score to be recorded and uploaded to a global leaderboard so that I can compare it to my friends and other player's scores worldwide. 

- **Acceptance Criteria:**

    - Global leaderboard is accessible from the main menu.

    - Has a global tab which displays the top player's scores from around the world

    - Highest score is detected and uploaded to the global leaderboard automatically.

    - Toggle to turn off uploading scores to leaderboards in settings menu. 
        
        

        
        
