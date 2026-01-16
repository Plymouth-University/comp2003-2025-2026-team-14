#include <iostream>
#include <string>
#include <cmath>
#include <random>
#include <vector>
#include <set>
#include <ctime>
#include <ncurses.h>

#include "constants.h"
#include "geometry.h"


#include "shape_types.h"
#include "game_state.h"

#include "grid.h"

bool colorsEnabled = false;

void initialiseGrid(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]);
void updateDisplay(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], char displayGrid[GRIDHEIGHT][GRIDWIDTH]);
void moveShape(char input, Shape& movingShape);
void drawMovingShape(char displayGrid[GRIDHEIGHT][GRIDWIDTH], const Shape& movingShape);
void printGrid(char displayGrid[GRIDHEIGHT][GRIDWIDTH]);
bool confirmShapePlacement(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], Shape& shape, const GameState& gameState);
bool canShapeBePlaced(const Shape& shape, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], const GameState& gameState);
ShapePtr generateRandomShape();
ShapePtr createShapeFromDice(int shapeType, int buildingType);
void displayDice(const DicePool& dicePool);
void displayScoreBreakdown(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], GameState& gameState, int starsEarned);
void displayFinalScoreboard(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], GameState& gameState);
bool handleDiceSelection(DicePool& dicePool, GameState& gameState, int& selectedShapeType, int& selectedBuildingType, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], char displayGrid[GRIDHEIGHT][GRIDWIDTH]);
void selectStartingPosition(GameState& gameState, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]);


// Compile using : g++ -std=c++17 -lncurses main.cpp -o game

int main() {
    // Initialize ncurses
    initscr();
    cbreak();
    noecho();
    keypad(stdscr, TRUE);

    // Initialize colors if available
    if (has_colors()) {
        start_color();
        // Define color pairs (foreground, background)
        init_pair(COLOR_PAIR_DEFAULT, COLOR_WHITE, COLOR_BLACK);
        init_pair(COLOR_PAIR_RIVER, COLOR_CYAN, COLOR_BLACK);
        init_pair(COLOR_PAIR_STARTING_SPACE, COLOR_MAGENTA, COLOR_BLACK);
        init_pair(COLOR_PAIR_EMPTY, COLOR_WHITE, COLOR_BLACK);
        init_pair(COLOR_PAIR_INDUSTRIAL, COLOR_YELLOW, COLOR_BLACK);
        init_pair(COLOR_PAIR_RESIDENTIAL, COLOR_GREEN, COLOR_BLACK);
        init_pair(COLOR_PAIR_COMMERCIAL, COLOR_BLUE, COLOR_BLACK);
        init_pair(COLOR_PAIR_SCHOOL, COLOR_WHITE, COLOR_BLACK);
        init_pair(COLOR_PAIR_PARK, COLOR_BLACK, COLOR_WHITE); // inverted for visibility
        init_pair(COLOR_PAIR_SHAPE_DICE, COLOR_CYAN, COLOR_BLACK);
        init_pair(COLOR_PAIR_BUILDING_DICE, COLOR_MAGENTA, COLOR_BLACK);
        colorsEnabled = true;
    }

    GridTile gridArr[GRIDHEIGHT][GRIDWIDTH];
    char displayGrid[GRIDHEIGHT][GRIDWIDTH];

    GameState gameState;
    gameState.initialiseGrid(gridArr);

    srand(time(0));

    // Player selects starting position
    selectStartingPosition(gameState, gridArr);

    ShapePtr currentShape = nullptr;
    bool gameRunning = true;
    bool shapePlaced = true; // Start with true to trigger new turn

    while (gameRunning) {
        // Start of a new turn
        if (shapePlaced) {
            shapePlaced = false;

            // Reset per-turn flags
            gameState.resetTurnFlags();

            // Dice phase
            DicePool& dicePool = gameState.getDicePool();
            dicePool.rollAll();
            dicePool.performAutoRerolls();
            gameState.detectDoubleRolls(dicePool);

            // Dice selection (for now auto-select)
            int selectedShapeType, selectedBuildingType;
            if (!handleDiceSelection(dicePool, gameState, selectedShapeType, selectedBuildingType, gridArr, displayGrid)) {
                // Error handling
                break;
            }

            // Create shape from selected dice
            currentShape = createShapeFromDice(selectedShapeType, selectedBuildingType);

            // Check if shape can be placed anywhere
            if (!canShapeBePlaced(*currentShape, gridArr, gameState)) {
                // No possible placement - game over
                int penalty = gameState.calculateEmptyCellPenalty(gridArr);
                gameState.addScore(penalty);
                displayFinalScoreboard(gridArr, gameState);
                mvprintw(GRIDHEIGHT + 6, 0, "No possible placement for selected shape. Game over!");
                mvprintw(GRIDHEIGHT + 7, 0, "Press any key to exit...");
                refresh();
                getch();
                gameRunning = false;
                continue;
            }

        }

        // Movement and placement phase
        updateDisplay(gridArr, displayGrid);
        if (currentShape != nullptr) {
            drawMovingShape(displayGrid, *currentShape);
        }
        printGrid(displayGrid);

        // Display game status
        mvprintw(GRIDHEIGHT + 4, 0, "Turn: %d  Score: %d  Stars: %d",
                gameState.getTurnNumber(), gameState.getScore(), gameState.getStars());
        mvprintw(GRIDHEIGHT + 5, 0, "Move: WASD, Rotate: r, Flip: f, Confirm: c, End game: e, Quit: q");
        refresh();

        int ch = getch();
        if (ch == ERR) { napms(10); continue; }
        char input = ch;

        if (input == 'q') {
            int penalty = gameState.calculateEmptyCellPenalty(gridArr);
            gameState.addScore(penalty);
            gameRunning = false;
            continue;
        }
        if (input == 'e') {
            int penalty = gameState.calculateEmptyCellPenalty(gridArr);
            gameState.addScore(penalty);
            displayFinalScoreboard(gridArr, gameState);
            // Wait for any key press
            mvprintw(GRIDHEIGHT + 6, 0, "Press any key to exit...");
            refresh();
            getch();
            gameRunning = false;
            continue;
        }

        if (currentShape != nullptr) {
            moveShape(input, *currentShape);
        }

        if (input == 'c' && currentShape != nullptr) {
            if (confirmShapePlacement(gridArr, *currentShape, gameState)) {
                // Shape successfully placed
                shapePlaced = true;
                gameState.incrementTurn();
                gameState.setFirstTurnCompleted(true);

                // Transfer ownership to GameState
                Shape* rawShape = currentShape.get();
                gameState.addPlacedShape(std::move(currentShape));
                // currentShape is now empty

                // Calculate score
                int scoreDelta = gameState.calculateScoreForPlacement(*rawShape, gridArr);
                gameState.addScore(scoreDelta);
                // Award stars for double rolls
                int starsEarned = gameState.awardStarsForPlacement(*rawShape);
                gameState.addStars(starsEarned);
                gameState.addScore(starsEarned * STAR_SCORE);
                // Display score breakdown
                displayScoreBreakdown(gridArr, gameState, starsEarned);

                // Reset currentShape - new one will be created next turn
                currentShape = nullptr;
            }
        }
    }

    endwin();
    return 0;
}


void initialiseGrid(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) {
    for (int i = 0; i < GRIDHEIGHT; i++) {
        for (int j = 0; j < GRIDWIDTH; j++) {
            GridPosition p = {i, j};
            GridTile newTile = GridTile(p);
            gridArr[i][j] = newTile;
        }
    }

    //Temporary handling of fixed positions
    GridPosition startingPos[8] = {{1, 1}, {3, 3}, {6, 0}, {8, 2}, {7, 8}, {5, 6}, {2, 9}, {0, 7}};
    for (int i = 0; i < 8; i++) {
        gridArr[startingPos[i].y][startingPos[i].x].setStartingSpace(true);
        gridArr[startingPos[i].y][startingPos[i].x].setStartingPositionNumber(i + 1);
    }

    GridPosition riverPos[11] = {{0,4},{1,4},{2,4},{3,4},{4,4},{4,5},{5,5},{6,5},{7,5},{8,5},{9,5}};
    for (int i = 0; i < 11; i++) {
        gridArr[riverPos[i].y][riverPos[i].x].setRiver(true);
    }
    
}

void updateDisplay(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], char displayGrid[GRIDHEIGHT][GRIDWIDTH]) {
    for (int i = 0; i < GRIDHEIGHT; i++) {
        for (int j = 0; j < GRIDWIDTH; j++) {
            displayGrid[i][j] = gridArr[i][j].getTileSymbol();
        }
    }
}

int getColorPairForSymbol(char symbol) {
    switch (symbol) {
        case '#': return COLOR_PAIR_RIVER;
        case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': return COLOR_PAIR_STARTING_SPACE;
        case '*': return COLOR_PAIR_EMPTY;
        case 'I': return COLOR_PAIR_INDUSTRIAL;
        case 'R': return COLOR_PAIR_RESIDENTIAL;
        case 'C': return COLOR_PAIR_COMMERCIAL;
        case 'S': return COLOR_PAIR_SCHOOL;
        case 'P': return COLOR_PAIR_PARK;
        default: return COLOR_PAIR_DEFAULT;
    }
}

void printGrid(char displayGrid[GRIDHEIGHT][GRIDWIDTH]) {
    clear(); // Clear the screen
    for (int i = 0; i < GRIDHEIGHT; i++) {
        for (int j = 0; j < GRIDWIDTH; j++) {
            if (colorsEnabled) {
                int colorPair = getColorPairForSymbol(displayGrid[i][j]);
                attron(COLOR_PAIR(colorPair));
            }
            mvaddch(i, j, displayGrid[i][j]); // Print at row i, column j
            if (colorsEnabled) {
                attroff(COLOR_PAIR(getColorPairForSymbol(displayGrid[i][j])));
            }
        }
    }
    refresh(); // Refresh the screen
}

void moveShape(char input, Shape& movingShape) {
    if (input == 'w') movingShape.moveUp();
    if (input == 's') movingShape.moveDown();
    if (input == 'a') movingShape.moveLeft();
    if (input == 'd') movingShape.moveRight();
    if (input == 'r') movingShape.rotate();
    if (input == 'f') movingShape.flip();
}

void drawMovingShape(char displayGrid[GRIDHEIGHT][GRIDWIDTH], const Shape& movingShape) { //Draw the shape on top of the display grid while it's being moved
    std::vector<GridPosition> vec = movingShape.getOccupiedTiles();

    for (std::vector<GridPosition>::iterator it = vec.begin(); it != vec.end(); it++) {
        if (withinGridBoundaries(*it)) {
            displayGrid[it->y][it->x] = movingShape.getTileSymbol();
        }
    }
}

bool confirmShapePlacement(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], Shape& shape, const GameState& gameState) {
    std::vector<GridPosition> vec = shape.getOccupiedTiles();

    for (std::vector<GridPosition>::iterator it = vec.begin(); it != vec.end(); it++) {
        if (!gameState.withinGridBoundaries(*it)) { //checking grid boundaries
            mvprintw(GRIDHEIGHT + 6, 0, "Error: Shape would extend outside grid!");
            refresh();
            napms(1500);
            return false;
        }
        else if (gridArr[it->y][it->x].isRiver()) {
            mvprintw(GRIDHEIGHT + 6, 0, "Error: Cannot place on river!");
            refresh();
            napms(1500);
            return false;
        }
        else if (gridArr[it->y][it->x].hasShape()) {
            mvprintw(GRIDHEIGHT + 6, 0, "Error: Cannot overlap existing building!");
            refresh();
            napms(1500);
            return false;
        }
    }

    if (!gameState.isValidPlacement(shape, gridArr)) {
        // Determine more specific error
        if (gameState.isWaterDieUsedThisTurn()) {
            mvprintw(GRIDHEIGHT + 6, 0, "Error: Water die placement must be adjacent to river!");
        } else if (!gameState.isFirstTurnCompleted()) {
            mvprintw(GRIDHEIGHT + 6, 0, "Error: First turn must overlap selected starting position!");
        } else {
            mvprintw(GRIDHEIGHT + 6, 0, "Error: Must be adjacent to existing building!");
        }
        refresh();
        napms(1500);
        return false;
    }

    for (std::vector<GridPosition>::iterator it = vec.begin(); it != vec.end(); it++) {
        gridArr[it->y][it->x].setShape(&shape);
    }
    return true;
}

ShapePtr generateRandomShape() {
    int buildingType;
    switch (rand() % 5) {
    case 0:
        buildingType = INDUSTRIAL;
        break;
    case 1:
        buildingType = COMMERCIAL;
        break;
    case 2:
        buildingType = RESIDENTIAL;
        break;
    case 3:
        buildingType = SCHOOL;
        break;
    case 4:
        buildingType = PARK;
        break;
    default:
        break;
    }

    ShapePtr myShape;
    switch (rand() % 6)
    {
    case 0:
        myShape = std::make_unique<TShape>(buildingType);
        break;
    case 1:
        myShape = std::make_unique<ZShape>(buildingType);
        break;
    case 2:
        myShape = std::make_unique<Square>(buildingType);
        break;
    case 3:
        myShape = std::make_unique<LShape>(buildingType);
        break;
    case 4:
        myShape = std::make_unique<Line>(buildingType);
        break;
    case 5:
        myShape = std::make_unique<Single>(buildingType);
        break;
    default:
        break;
    }

    myShape->updateShapePosition();
    return myShape;
}

ShapePtr createShapeFromDice(int shapeType, int buildingType) {
    ShapePtr myShape;
    switch (shapeType) {
    case SHAPE_T:
        myShape = std::make_unique<TShape>(buildingType);
        break;
    case SHAPE_Z:
        myShape = std::make_unique<ZShape>(buildingType);
        break;
    case SHAPE_SQUARE:
        myShape = std::make_unique<Square>(buildingType);
        break;
    case SHAPE_L:
        myShape = std::make_unique<LShape>(buildingType);
        break;
    case SHAPE_LINE:
        myShape = std::make_unique<Line>(buildingType);
        break;
    case SHAPE_SINGLE:
        myShape = std::make_unique<Single>(buildingType);
        break;
    default:
        // Fallback to single shape
        myShape = std::make_unique<Single>(buildingType);
        break;
    }

    myShape->updateShapePosition();
    return myShape;
}


void selectStartingPosition(GameState& gameState, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) {
    char displayGrid[GRIDHEIGHT][GRIDWIDTH];
    bool positionSelected = false;
    int selectedPos = 0;

    while (!positionSelected) {
        clear();
        updateDisplay(gridArr, displayGrid);
        printGrid(displayGrid);

        // Display instructions
        mvprintw(GRIDHEIGHT + 2, 0, "Select starting position (1-8) where numbers 1-8 mark starting spaces:");
        mvprintw(GRIDHEIGHT + 3, 0, "Enter number 1-8: ");
        refresh();

        int ch = getch();
        if (ch == ERR) { napms(10); continue; }
        char input = ch;

        if (input >= '1' && input <= '8') {
            selectedPos = input - '0';
            // Validate position exists (should always be true)
            GridPosition pos = gameState.getStartingPositionCoordinates(selectedPos);
            if (pos.x >= 0 && pos.y >= 0) {
                gameState.setSelectedStartingPosition(selectedPos);
                // Clear other starting position numbers
                for (int i = 0; i < GRIDHEIGHT; i++) {
                    for (int j = 0; j < GRIDWIDTH; j++) {
                        if (gridArr[i][j].isStartingSpace() && gridArr[i][j].getStartingPositionNumber() != selectedPos) {
                            gridArr[i][j].setStartingSpace(false);
                            gridArr[i][j].setStartingPositionNumber(0);
                        }
                    }
                }
                positionSelected = true;
            }
        } else if (input == 'q') {
            // Quit game
            endwin();
            exit(0);
        }
    }

    // Show confirmation
    clear();
    updateDisplay(gridArr, displayGrid);
    printGrid(displayGrid);
    mvprintw(GRIDHEIGHT + 2, 0, "Starting position %d selected. Press any key to start game.", selectedPos);
    refresh();
    getch();
}



bool canShapeBePlaced(const Shape& shape, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], const GameState& gameState) {
    int shapeType = shape.getShapeType();
    int buildingType = shape.getBuildingType();

    // Iterate over all orientations
    for (int rotation = 0; rotation < 4; ++rotation) {
        for (int flip = 0; flip < 2; ++flip) {
            // Create temporary shape
            ShapePtr tempShape = createShapeFromDice(shapeType, buildingType);
            if (!tempShape) continue;

            // Apply rotations
            for (int r = 0; r < rotation; ++r) {
                tempShape->rotate();
            }
            // Apply flip if needed
            if (flip == 1) {
                tempShape->flip();
            }

            // Iterate over all grid positions
            for (int y = 0; y < GRIDHEIGHT; ++y) {
                for (int x = 0; x < GRIDWIDTH; ++x) {
                    // Set center position
                    tempShape->setCenter({y, x});

                    // Check boundaries, river, overlap, adjacency
                    std::vector<GridPosition> occupied = tempShape->getOccupiedTiles();
                    bool valid = true;

                    // Check boundaries and collisions
                    for (const GridPosition& pos : occupied) {
                        if (!gameState.withinGridBoundaries(pos)) {
                            valid = false;
                            break;
                        }
                        if (gridArr[pos.y][pos.x].isRiver()) {
                            valid = false;
                            break;
                        }
                        if (gridArr[pos.y][pos.x].hasShape()) {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) continue;

                    // Check adjacency rules
                    if (!gameState.isValidPlacement(*tempShape, gridArr)) {
                        continue;
                    }

                    // Found a valid placement
                    return true;
                }
            }
        }
    }

    // No valid placement found
    return false;
}

void displayDice(const DicePool& dicePool) {
    int startRow = GRIDHEIGHT + 2;
    const int START_COL = 15; // column after labels (15 avoids overwriting "Building Dice:")
    const int SLOT_WIDTH = 17; // width sufficient for longest building dice name

    mvprintw(startRow, 0, "Shape Dice:");
    for (int i = 0; i < NUM_SHAPE_DICE; i++) {
        const Dice& d = dicePool.getDice(i);
        int col = START_COL + i * SLOT_WIDTH;
        if (colorsEnabled) {
            attron(COLOR_PAIR(COLOR_PAIR_SHAPE_DICE));
            if (d.isSelected()) attron(A_REVERSE);
        }
        mvprintw(startRow, col, "[%d: %s%s]", i + 1, d.getFaceString().c_str(), d.isSelected() ? "*" : " ");
        if (colorsEnabled) {
            if (d.isSelected()) attroff(A_REVERSE);
            attroff(COLOR_PAIR(COLOR_PAIR_SHAPE_DICE));
        }
    }

    mvprintw(startRow + 1, 0, "Building Dice:");
    for (int i = 0; i < NUM_BUILDING_DICE; i++) {
        const Dice& d = dicePool.getDice(NUM_SHAPE_DICE + i);
        int col = START_COL + i * SLOT_WIDTH;
        if (colorsEnabled) {
            attron(COLOR_PAIR(COLOR_PAIR_BUILDING_DICE));
            if (d.isSelected()) attron(A_REVERSE);
        }
        mvprintw(startRow + 1, col, "[%d: %s%s]", NUM_SHAPE_DICE + i + 1, d.getFaceString().c_str(), d.isSelected() ? "*" : " ");
        if (colorsEnabled) {
            if (d.isSelected()) attroff(A_REVERSE);
            attroff(COLOR_PAIR(COLOR_PAIR_BUILDING_DICE));
        }
    }

    refresh();
}

void displayScoreBreakdown(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], GameState& gameState, int starsEarned) {
    ScoreComponents comp = gameState.computeScoreComponents(gridArr);
    int startRow = GRIDHEIGHT + 8;
    mvprintw(startRow, 0, "Score breakdown:");
    mvprintw(startRow + 1, 2, "Industrial zone score: %d", comp.industrialZoneScore);
    mvprintw(startRow + 2, 2, "Residential zone score: %d", comp.residentialZoneScore);
    mvprintw(startRow + 3, 2, "Commercial zone score: %d", comp.commercialZoneScore);
    mvprintw(startRow + 4, 2, "Park adjacency: %d", comp.parkScore);
    mvprintw(startRow + 5, 2, "School adjacency: %d", comp.schoolScore);
    mvprintw(startRow + 6, 2, "Stars earned this turn: %d", starsEarned);
    refresh();
    napms(3000); // Show for 3 seconds
    // Clear the breakdown area
    for (int i = 0; i < 7; i++) {
        mvprintw(startRow + i, 0, "                                                                  ");
    }
}

void displayFinalScoreboard(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], GameState& gameState) {
    // Clear screen and display final scoreboard
    clear();

    // Compute score components
    ScoreComponents comp = gameState.computeScoreComponents(gridArr);
    int totalStars = gameState.getStars();
    int starScore = totalStars * STAR_SCORE;
    int penalty = gameState.calculateEmptyCellPenalty(gridArr);
    int totalScore = gameState.getScore(); // Already includes penalty if applied

    int startRow = 2;

    // Title
    mvprintw(startRow, 0, "=== FINAL SCOREBOARD ===");
    startRow += 2;

    // Score breakdown
    mvprintw(startRow++, 2, "Industrial Zone Score: %d", comp.industrialZoneScore);
    mvprintw(startRow++, 2, "Residential Zone Score: %d", comp.residentialZoneScore);
    mvprintw(startRow++, 2, "Commercial Zone Score: %d", comp.commercialZoneScore);
    mvprintw(startRow++, 2, "Park Adjacency Score: %d", comp.parkScore);
    mvprintw(startRow++, 2, "School Adjacency Score: %d", comp.schoolScore);
    mvprintw(startRow++, 2, "Stars Collected: %d (x %d each = %d)", totalStars, STAR_SCORE, starScore);
    mvprintw(startRow++, 2, "Empty Cell Penalty: %d", penalty);
    startRow++;
    mvprintw(startRow++, 2, "TOTAL SCORE: %d", totalScore);
    startRow += 2;

    // Additional stats
    mvprintw(startRow++, 2, "Game Statistics:");
    mvprintw(startRow++, 4, "Turns played: %d", gameState.getTurnNumber() - 1); // Turn number is next turn
    mvprintw(startRow++, 4, "Wildcards used: %d", gameState.getWildcardsUsed());
    mvprintw(startRow++, 4, "Stars earned: %d", totalStars);

    // Wait for user to read
    mvprintw(startRow + 2, 0, "Press any key to exit...");
    refresh();
}


bool handleDiceSelection(DicePool& dicePool, GameState& gameState, int& selectedShapeType, int& selectedBuildingType, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], char displayGrid[GRIDHEIGHT][GRIDWIDTH]) {
    dicePool.clearSelections();

    bool selectionComplete = false;
    bool waterDieSelected = false;

    while (!selectionComplete) {
        clear();
        updateDisplay(gridArr, displayGrid);
        printGrid(displayGrid);
        displayDice(dicePool);

        // Display instructions
        mvprintw(GRIDHEIGHT + 6, 0, "Select dice: 1-6 toggle, 'c' confirm, 'v' use wildcard (cost: %d)", gameState.getWildcardCost());
        mvprintw(GRIDHEIGHT + 7, 0, "Selected shape dice: %d/%d, building dice: %d/%d",
                 dicePool.countSelected(Dice::SHAPE), 1,
                 dicePool.countSelected(Dice::BUILDING), 1);
        refresh();

        int ch = getch();
        if (ch == ERR) { napms(10); continue; }
        char input = ch;

        if (input >= '1' && input <= '6') {
            int dieIndex = input - '1'; // Convert '1' -> 0, '2' -> 1, etc.
            if (dieIndex >= 0 && dieIndex < TOTAL_DICE) {
                dicePool.getDice(dieIndex).toggleSelected();

                // If water die is selected, set flag
                if (dicePool.getDice(dieIndex).getType() == Dice::BUILDING &&
                    dicePool.getDice(dieIndex).getFace() == WATER) {
                    waterDieSelected = dicePool.getDice(dieIndex).isSelected();
                }
            }
        } else if (input == 'c') {
            // Check if exactly one shape die and one building die are selected
            int shapeSelected = dicePool.countSelected(Dice::SHAPE);
            int buildingSelected = dicePool.countSelected(Dice::BUILDING);

            if (shapeSelected == 1 && buildingSelected == 1) {
                // Get selected dice indices
                std::vector<int> selectedIndices = dicePool.getSelectedIndices();
                int shapeDieIndex = -1, buildingDieIndex = -1;
                for (int idx : selectedIndices) {
                    if (dicePool.getDice(idx).getType() == Dice::SHAPE) {
                        shapeDieIndex = idx;
                    } else {
                        buildingDieIndex = idx;
                    }
                }

                selectedShapeType = dicePool.getDice(shapeDieIndex).getFace();
                selectedBuildingType = dicePool.getDice(buildingDieIndex).getFace();

                // Handle water die special case
                if (selectedBuildingType == WATER) {
                    gameState.setWaterDieUsedThisTurn(true);
                    // Player chooses building type for water die placement
                    bool buildingTypeSelected = false;
                    while (!buildingTypeSelected) {
                        clear();
                        updateDisplay(gridArr, displayGrid);
                        printGrid(displayGrid);
                        displayDice(dicePool);

                        // Display building type selection prompt
                        mvprintw(GRIDHEIGHT + 8, 0, "Water die selected! Choose building type:");
                        mvprintw(GRIDHEIGHT + 9, 0, "1: Industrial (I)  2: Residential (R)  3: Commercial (C)  4: School (S)  5: Park (P)");
                        refresh();

                        int ch = getch();
                        if (ch == ERR) { napms(10); continue; }
                        char choice = ch;

                        switch (choice) {
                            case '1':
                                selectedBuildingType = INDUSTRIAL;
                                buildingTypeSelected = true;
                                break;
                            case '2':
                                selectedBuildingType = RESIDENTIAL;
                                buildingTypeSelected = true;
                                break;
                            case '3':
                                selectedBuildingType = COMMERCIAL;
                                buildingTypeSelected = true;
                                break;
                            case '4':
                                selectedBuildingType = SCHOOL;
                                buildingTypeSelected = true;
                                break;
                            case '5':
                                selectedBuildingType = PARK;
                                buildingTypeSelected = true;
                                break;
                            case 'q':
                                // Quit game
                                return false;
                            default:
                                // Invalid choice - show error
                                mvprintw(GRIDHEIGHT + 10, 0, "Invalid choice! Please press 1-5.");
                                refresh();
                                napms(1500);
                                break;
                        }
                    }
                }

                selectionComplete = true;
            } else {
                // Invalid selection - show error message
                mvprintw(GRIDHEIGHT + 8, 0, "Error: Must select exactly 1 shape die and 1 building die!");
                refresh();
                napms(1500); // Show error for 1.5 seconds
            }
        } else if (input == 'v') {
            // Wildcard usage
            std::vector<int> selectedIndices = dicePool.getSelectedIndices();
            if (selectedIndices.empty()) {
                mvprintw(GRIDHEIGHT + 8, 0, "Error: Select at least one die to use wildcard!");
                refresh();
                napms(1500);
                continue;
            }
            if (!gameState.canUseWildcard()) {
                mvprintw(GRIDHEIGHT + 8, 0, "Error: No wildcards remaining (max 3).");
                refresh();
                napms(1500);
                continue;
            }
            // Get cost before using
            int cost = gameState.getWildcardCost(); // negative value
            // Apply wildcard cost and reroll selected dice
            if (gameState.useWildcardWithCost()) {
                for (int idx : selectedIndices) {
                    dicePool.getDice(idx).roll();
                }
                // Update water die flag based on selected dice after reroll
                waterDieSelected = false;
                for (int idx : selectedIndices) {
                    if (dicePool.getDice(idx).getType() == Dice::BUILDING &&
                        dicePool.getDice(idx).getFace() == WATER) {
                        waterDieSelected = true;
                        break;
                    }
                }
                mvprintw(GRIDHEIGHT + 8, 0, "Wildcard used! Cost: %d points.", cost);
                refresh();
                napms(1500);
            } else {
                mvprintw(GRIDHEIGHT + 8, 0, "Error: Cannot use wildcard.");
                refresh();
                napms(1500);
            }
        } else if (input == 'q') {
            // Quit game from dice selection
            return false;
        }
    }

    return true;
}