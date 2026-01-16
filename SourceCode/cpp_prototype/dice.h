#pragma once

#include "constants.h"
#include <vector>
#include <random>

class Dice {
public:
    enum Type { SHAPE, BUILDING };

private:
    Type type;
    int currentFace; // For SHAPE: 0-5 (ShapeType), For BUILDING: 1-6 (building type constants)
    bool selected; // For player selection

public:
    Dice(Type t) : type(t), currentFace(0), selected(false) {}

    // Roll the die to a random face
    void roll() {
        if (type == SHAPE) {
            currentFace = rand() % NUM_SHAPE_TYPES; // 0-5
        } else { // BUILDING
            // Roll 1-6 (Industrial=1, Residential=2, Commercial=3, School=4, Park=5, Water=6)
            currentFace = (rand() % 6) + 1;
        }
    }

    // Get current face value
    int getFace() const { return currentFace; }

    // Set face (for wildcards)
    void setFace(int face) { currentFace = face; }

    Type getType() const { return type; }

    bool isSelected() const { return selected; }
    void setSelected(bool sel) { selected = sel; }
    void toggleSelected() { selected = !selected; }

    // Get face as string for display
    std::string getFaceString() const {
        if (type == SHAPE) {
            switch (currentFace) {
                case SHAPE_T: return "T";
                case SHAPE_Z: return "Z";
                case SHAPE_SQUARE: return "Square";
                case SHAPE_L: return "L";
                case SHAPE_LINE: return "Line";
                case SHAPE_SINGLE: return "Single";
                default: return "?";
            }
        } else {
            switch (currentFace) {
                case INDUSTRIAL: return "Industrial";
                case RESIDENTIAL: return "Residential";
                case COMMERCIAL: return "Commercial";
                case SCHOOL: return "School";
                case PARK: return "Park";
                case WATER: return "Water";
                default: return "?";
            }
        }
    }

    // Check if this die matches another die's face
    bool matches(const Dice& other) const {
        return type == other.type && currentFace == other.currentFace;
    }
};

class DicePool {
private:
    std::vector<Dice> dice;

public:
    DicePool() {
        // Create 3 shape dice and 3 building dice
        for (int i = 0; i < NUM_SHAPE_DICE; i++) {
            dice.push_back(Dice(Dice::SHAPE));
        }
        for (int i = 0; i < NUM_BUILDING_DICE; i++) {
            dice.push_back(Dice(Dice::BUILDING));
        }
    }

    // Roll all dice
    void rollAll() {
        for (auto& d : dice) {
            d.roll();
        }
    }

    // Roll specific dice by index
    void rollDice(const std::vector<int>& indices) {
        for (int idx : indices) {
            if (idx >= 0 && idx < dice.size()) {
                dice[idx].roll();
            }
        }
    }

    // Get dice by index
    Dice& getDice(int index) { return dice[index]; }
    const Dice& getDice(int index) const { return dice[index]; }

    // Get all dice
    std::vector<Dice>& getAllDice() { return dice; }
    const std::vector<Dice>& getAllDice() const { return dice; }

    // Count matches among dice of same type
    int countMatches(Dice::Type type) const {
        std::vector<int> faceCount(7, 0); // Max faces: 6 for building, 6 for shape
        for (const auto& d : dice) {
            if (d.getType() == type) {
                faceCount[d.getFace()]++;
            }
        }
        int maxCount = 0;
        for (int count : faceCount) {
            if (count > maxCount) maxCount = count;
        }
        return maxCount;
    }

    // Find indices of dice that match a triple (or more)
    std::vector<int> getMatchingDiceIndices(Dice::Type type) const {
        std::vector<int> faceCount(7, 0);
        std::vector<std::vector<int>> indicesByFace(7);

        for (int i = 0; i < dice.size(); i++) {
            const Dice& d = dice[i];
            if (d.getType() == type) {
                int face = d.getFace();
                faceCount[face]++;
                indicesByFace[face].push_back(i);
            }
        }

        // Find face with max count >= 3
        int maxFace = -1;
        int maxCount = 0;
        for (int face = 0; face < 7; face++) {
            if (faceCount[face] > maxCount) {
                maxCount = faceCount[face];
                maxFace = face;
            }
        }

        if (maxCount >= 3) {
            return indicesByFace[maxFace];
        }
        return {}; // No triple match
    }

    // Auto-reroll logic: if 3 shape dice match OR 3 building dice match, re-roll those
    // Returns true if any reroll occurred
    bool autoReroll() {
        bool rerolled = false;

        // Check shape dice
        std::vector<int> shapeMatches = getMatchingDiceIndices(Dice::SHAPE);
        if (shapeMatches.size() >= 3) {
            rollDice(shapeMatches);
            rerolled = true;
        }

        // Check building dice
        std::vector<int> buildingMatches = getMatchingDiceIndices(Dice::BUILDING);
        if (buildingMatches.size() >= 3) {
            rollDice(buildingMatches);
            rerolled = true;
        }

        return rerolled;
    }

    // Recursive auto-reroll until no more triples
    void performAutoRerolls() {
        while (autoReroll()) {
            // Continue until no more triples
        }
    }

    // Get selected dice indices (for player selection)
    std::vector<int> getSelectedIndices() const {
        std::vector<int> selected;
        for (int i = 0; i < dice.size(); i++) {
            if (dice[i].isSelected()) {
                selected.push_back(i);
            }
        }
        return selected;
    }

    // Clear all selections
    void clearSelections() {
        for (auto& d : dice) {
            d.setSelected(false);
        }
    }

    // Count how many dice of each type are selected
    int countSelected(Dice::Type type) const {
        int count = 0;
        for (const auto& d : dice) {
            if (d.isSelected() && d.getType() == type) {
                count++;
            }
        }
        return count;
    }

    // Get faces that appear at least twice (doubles)
    std::vector<int> getDoubleFaces(Dice::Type type) const {
        std::vector<int> faces;
        std::vector<int> faceCount(7, 0); // max faces 6
        for (const auto& d : dice) {
            if (d.getType() == type) {
                faceCount[d.getFace()]++;
            }
        }
        for (int face = 0; face < 7; ++face) {
            if (faceCount[face] >= 2) {
                faces.push_back(face);
            }
        }
        return faces;
    }
};