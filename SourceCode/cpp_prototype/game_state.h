#pragma once

#include "dice.h"
#include "constants.h"
#include "geometry.h"
#include "grid.h"
#include <vector>
#include <set>

struct ScoreComponents {
    int industrialZoneScore;
    int residentialZoneScore;
    int commercialZoneScore;
    int parkScore;
    int schoolScore;
    ScoreComponents() : industrialZoneScore(0), residentialZoneScore(0), commercialZoneScore(0), parkScore(0), schoolScore(0) {}
};

// Forward declarations
class Shape;

class GameState {
private:
    int score;
    int turnNumber;
    int stars;
    int previousBoardScore;
    std::vector<int> shapeDoubleFaces;
    std::vector<int> buildingDoubleFaces;
    int wildcardsUsed; // 0-3
    DicePool dicePool;
    bool waterDieUsedThisTurn;
    int selectedStartingPosition; // 1-8, 0 if not selected yet
    bool firstTurnCompleted;
    std::vector<ShapePtr> placedShapes;


    int computeSchoolScore(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH], const std::vector<ZonePtr>& zones) const {
        int total = 0;
        (void)zones; // suppress unused parameter warning
        // Track assigned zones
        std::set<Zone*> assignedZones;

        // Iterate over all grid positions to find schools
        for (int y = 0; y < GRIDHEIGHT; ++y) {
            for (int x = 0; x < GRIDWIDTH; ++x) {
                Shape* shape = gridArr[y][x].getShape();
                if (!shape || shape->getBuildingType() != SCHOOL) continue;

                // Find adjacent residential zones
                std::set<Zone*> adjacentZones;
                GridPosition neighbors[4] = {{y-1,x}, {y+1,x}, {y,x-1}, {y,x+1}};
                for (const GridPosition& nb : neighbors) {
                    if (nb.y < 0 || nb.y >= GRIDHEIGHT || nb.x < 0 || nb.x >= GRIDWIDTH) continue;
                    Zone* nbZone = gridArr[nb.y][nb.x].getZone();
                    if (nbZone && nbZone->getBuildingType() == RESIDENTIAL) {
                        adjacentZones.insert(nbZone);
                    }
                }

                // Find biggest adjacent zone not already assigned
                Zone* bestZone = nullptr;
                size_t bestSize = 0;
                for (Zone* zone : adjacentZones) {
                    if (assignedZones.count(zone)) continue;
                    size_t zoneSize = zone->getShapes().size();
                    if (zoneSize > bestSize) {
                        bestSize = zoneSize;
                        bestZone = zone;
                    }
                }

                if (bestZone) {
                    assignedZones.insert(bestZone);
                    total += bestSize * SCHOOL_SCORE_PER_RESIDENTIAL;
                }
            }
        }
        return total;
    }


public:
    GameState()
        : score(0), turnNumber(1), stars(0), previousBoardScore(0), wildcardsUsed(0),
          waterDieUsedThisTurn(false), selectedStartingPosition(1),
          firstTurnCompleted(false) {}

    // Getters
    int getScore() const { return score; }
    int getTurnNumber() const { return turnNumber; }
    int getStars() const { return stars; }
    int getWildcardsUsed() const { return wildcardsUsed; }
    DicePool& getDicePool() { return dicePool; }
    const DicePool& getDicePool() const { return dicePool; }
    bool isWaterDieUsedThisTurn() const { return waterDieUsedThisTurn; }
    int getSelectedStartingPosition() const { return selectedStartingPosition; }
    bool isFirstTurnCompleted() const { return firstTurnCompleted; }

    GridPosition getStartingPositionCoordinates(int startingPositionNumber) const {
        static GridPosition startingPositions[8] = {
            {1, 1}, {3, 3}, {6, 0}, {8, 2}, {7, 8}, {5, 6}, {2, 9}, {0, 7}
        };
        if (startingPositionNumber >= 1 && startingPositionNumber <= 8) {
            return startingPositions[startingPositionNumber - 1];
        }
        return {-1, -1};
    }

    bool withinGridBoundaries(GridPosition p) const {
        return p.y >= 0 && p.y < GRIDHEIGHT && p.x >= 0 && p.x < GRIDWIDTH;
    }

    void initialiseGrid(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]);

    // Setters
    void setScore(int s) { score = s; }
    void addScore(int delta) { score += delta; }
    void setTurnNumber(int t) { turnNumber = t; }
    void incrementTurn() { turnNumber++; }
    void setStars(int s) { stars = s; }
    void addStar() { stars++; }
    void addStars(int count) { stars += count; }
    void setWildcardsUsed(int w) { wildcardsUsed = w; }
    void useWildcard() {
        if (wildcardsUsed < 3) wildcardsUsed++;
    }
    void setWaterDieUsedThisTurn(bool used) { waterDieUsedThisTurn = used; }
    void setSelectedStartingPosition(int pos) {
        if (pos >= 1 && pos <= 8) selectedStartingPosition = pos;
    }
    void setFirstTurnCompleted(bool completed) { firstTurnCompleted = completed; }

    // Double roll detection
    void detectDoubleRolls(const DicePool& dicePool);
    void resetDoubleFaces();
    int awardStarsForPlacement(const Shape& shape) const;

    // Calculate wildcard cost for next use
    int getWildcardCost() const {
        switch (wildcardsUsed) {
            case 0: return WILDCARD_COST_FIRST;
            case 1: return WILDCARD_COST_SECOND;
            case 2: return WILDCARD_COST_THIRD;
            default: return 0; // No more wildcards allowed
        }
    }

    // Check if wildcard can be used
    bool canUseWildcard() const {
        return wildcardsUsed < 3;
    }

    // Use a wildcard and apply cost
    bool useWildcardWithCost() {
        if (!canUseWildcard()) return false;
        int cost = getWildcardCost();
        score += cost; // cost is negative, so this subtracts
        wildcardsUsed++;
        return true;
    }

    // Reset per-turn flags
    void resetTurnFlags() {
        waterDieUsedThisTurn = false;
        resetDoubleFaces();
    }

    // Check if placement is valid according to adjacency rules
    bool isValidPlacement(const Shape& shape, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) const {
        // Water die exception: if water die used this turn, placement can be anywhere along riverbank
        if (waterDieUsedThisTurn) {
            // Check if any tile of the shape is adjacent to river
            std::vector<GridPosition> occupied = shape.getOccupiedTiles();
            for (const GridPosition& pos : occupied) {
                if (!withinGridBoundaries(pos)) continue;

                // Check four orthogonal directions for river adjacency
                GridPosition neighbors[4] = {
                    {pos.y - 1, pos.x},
                    {pos.y + 1, pos.x},
                    {pos.y, pos.x - 1},
                    {pos.y, pos.x + 1}
                };
                for (const GridPosition& neighbor : neighbors) {
                    if (withinGridBoundaries(neighbor) && gridArr[neighbor.y][neighbor.x].isRiver()) {
                        return true; // At least one tile adjacent to river
                    }
                }
            }
            // No tile adjacent to river
            return false;
        }

        // First turn: must overlap selected starting position
        if (!firstTurnCompleted) {
            int selectedPos = selectedStartingPosition;
            if (selectedPos == 0) {
                // Starting position not selected yet - invalid placement
                return false;
            }

            GridPosition startPos = getStartingPositionCoordinates(selectedPos);
            std::vector<GridPosition> occupied = shape.getOccupiedTiles();

            // Check if any tile overlaps starting position
            for (const GridPosition& pos : occupied) {
                if (pos.y == startPos.y && pos.x == startPos.x) {
                    return true;
                }
            }
            return false;
        }

        // Subsequent turns: must be orthogonally adjacent to at least one existing building
        std::vector<GridPosition> occupied = shape.getOccupiedTiles();
        for (const GridPosition& pos : occupied) {
            if (!withinGridBoundaries(pos)) continue;

            // Check four orthogonal directions
            GridPosition neighbors[4] = {
                {pos.y - 1, pos.x},
                {pos.y + 1, pos.x},
                {pos.y, pos.x - 1},
                {pos.y, pos.x + 1}
            };

            for (const GridPosition& neighbor : neighbors) {
                if (withinGridBoundaries(neighbor)) {
                    if (gridArr[neighbor.y][neighbor.x].hasShape()) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // Compute score components (zone, park, school)
    ScoreComponents computeScoreComponents(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) const;

    // Compute total score based on current board state
    int computeTotalScore(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) const {
        int total = 0;
        // Detect zones and compute zone scores
        auto zones = detectZones(gridArr);
        for (const auto& zone : zones) {
            total += zone->getScore();
        }
        // Park scores (use zone pointers stored in tiles)
        for (int y = 0; y < GRIDHEIGHT; ++y) {
            for (int x = 0; x < GRIDWIDTH; ++x) {
                Shape* shape = gridArr[y][x].getShape();
                if (!shape || shape->getBuildingType() != PARK) continue;
                // Count distinct zones orthogonally adjacent
                std::set<Zone*> adjacentZones;
                GridPosition neighbors[4] = {{y-1,x}, {y+1,x}, {y,x-1}, {y,x+1}};
                for (const GridPosition& nb : neighbors) {
                    if (nb.y < 0 || nb.y >= GRIDHEIGHT || nb.x < 0 || nb.x >= GRIDWIDTH) continue;
                    Zone* nbZone = gridArr[nb.y][nb.x].getZone();
                    if (nbZone) {
                        adjacentZones.insert(nbZone);
                    }
                }
                total += adjacentZones.size() * PARK_SCORE_PER_ZONE;
            }
        }
        // School scores
        total += computeSchoolScore(gridArr, zones);
        return total;
    }

    // Calculate score for a placed shape
    int calculateScoreForPlacement(const Shape& shape, GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) {
        int newTotal = computeTotalScore(gridArr);
        int delta = newTotal - previousBoardScore;
        previousBoardScore = newTotal;
        return delta;
    }

    // Calculate penalty for empty cells at game end
    int calculateEmptyCellPenalty(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) const {
        int emptyCount = 0;
        for (int i = 0; i < GRIDHEIGHT; i++) {
            for (int j = 0; j < GRIDWIDTH; j++) {
                if (!gridArr[i][j].hasShape() && !gridArr[i][j].isRiver()) {
                    emptyCount++;
                }
            }
        }
        return emptyCount * PENALTY_PER_EMPTY_CELL;
    }

    // Display game status
    void displayStatus() const {
        // To be implemented with ncurses
    }

    // Add a placed shape to ownership vector
    void addPlacedShape(ShapePtr shape) {
        placedShapes.push_back(std::move(shape));
    }
};

// Implementation of score components method
ScoreComponents GameState::computeScoreComponents(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) const {
    ScoreComponents comp;
    // Detect zones and compute zone scores
    auto zones = detectZones(gridArr);
    for (const auto& zone : zones) {
        int score = zone->getScore();
        switch (zone->getBuildingType()) {
            case INDUSTRIAL:
                comp.industrialZoneScore += score;
                break;
            case RESIDENTIAL:
                comp.residentialZoneScore += score;
                break;
            case COMMERCIAL:
                comp.commercialZoneScore += score;
                break;
            default:
                // Should not happen for zone types
                break;
        }
    }
    // Park scores (use zone pointers stored in tiles)
    for (int y = 0; y < GRIDHEIGHT; ++y) {
        for (int x = 0; x < GRIDWIDTH; ++x) {
            Shape* shape = gridArr[y][x].getShape();
            if (!shape || shape->getBuildingType() != PARK) continue;
            // Count distinct zones orthogonally adjacent
            std::set<Zone*> adjacentZones;
            GridPosition neighbors[4] = {{y-1,x}, {y+1,x}, {y,x-1}, {y,x+1}};
            for (const GridPosition& nb : neighbors) {
                if (nb.y < 0 || nb.y >= GRIDHEIGHT || nb.x < 0 || nb.x >= GRIDWIDTH) continue;
                Zone* nbZone = gridArr[nb.y][nb.x].getZone();
                if (nbZone) {
                    adjacentZones.insert(nbZone);
                }
            }
            comp.parkScore += adjacentZones.size() * PARK_SCORE_PER_ZONE;
        }
    }
    // School scores
    comp.schoolScore = computeSchoolScore(gridArr, zones);
    return comp;
}

// Implementation of double roll detection methods
void GameState::detectDoubleRolls(const DicePool& dicePool) {
    shapeDoubleFaces = dicePool.getDoubleFaces(Dice::SHAPE);
    buildingDoubleFaces = dicePool.getDoubleFaces(Dice::BUILDING);
}

void GameState::resetDoubleFaces() {
    shapeDoubleFaces.clear();
    buildingDoubleFaces.clear();
}

int GameState::awardStarsForPlacement(const Shape& shape) const {
    int stars = 0;
    // Check shape type
    for (int face : shapeDoubleFaces) {
        if (face == shape.getShapeType()) {
            stars++;
            break;
        }
    }
    // Check building type
    for (int face : buildingDoubleFaces) {
        if (face == shape.getBuildingType()) {
            stars++;
            break;
        }
    }
    return stars;
}

void GameState::initialiseGrid(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) {
    for (int i = 0; i < GRIDHEIGHT; i++) {
        for (int j = 0; j < GRIDWIDTH; j++) {
            GridPosition p = {i, j};
            gridArr[i][j] = GridTile(p);
            gridArr[i][j].setRiver(false);
            gridArr[i][j].setStartingSpace(false);
            gridArr[i][j].setStartingPositionNumber(0);
        }
    }

    GridPosition riverPositions[11] = {
        {0,4},{1,4},{2,4},{3,4},{4,4},
        {4,5},{5,5},{6,5},{7,5},{8,5},{9,5}
    };
    // River (columns 4 and 5)
    for (int i = 0; i < 11; i++) {
        GridPosition pos = riverPositions[i];
        gridArr[pos.y][pos.x].setRiver(true);
    }
    // Starting spaces
    GridPosition startingPositions[8] = {
        {1, 1}, {3, 3}, {6, 0}, {8, 2},
        {7, 8}, {5, 6}, {2, 9}, {0, 7}
    };
    for (int i = 0; i < 8; i++) {
        GridPosition pos = startingPositions[i];
        gridArr[pos.y][pos.x].setStartingSpace(true);
        gridArr[pos.y][pos.x].setStartingPositionNumber(i + 1);
    }
}