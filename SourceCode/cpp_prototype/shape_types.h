#pragma once

#include "geometry.h"
#include "constants.h"
#include <vector>
#include <memory>

class Shape;
using ShapePtr = std::unique_ptr<Shape>;

class Shape  { // Shape is only concerned with the tiles it occupies not what zone it belongs to.
protected:
    int buildingType;
    int shapeType;
    GridPosition centerOfShape; // You build the shape off of the center when its placed
    int rotationState; // 0,1,2,3 for 0°, 90°, 180°, 270°
    bool flipped; // true if shape is mirrored horizontally
    std::vector<GridPosition> occupiedTiles;
public:
    Shape() : shapeType(-1), centerOfShape({GRIDHEIGHT / 2, GRIDWIDTH / 2}), rotationState(0), flipped(false) {}

    // Always validate with this method before accessing the Zone
    bool isOfZoneType() const {
        if (buildingType == INDUSTRIAL ||
            buildingType == COMMERCIAL ||
            buildingType == RESIDENTIAL) {
                return true;
            }
        return false;
    }
    int getBuildingType() const {
        return buildingType;
    }
    void setBuildingType(int type) {
        buildingType = type;
    }
    int getShapeType() const {
        return shapeType;
    }
    void setShapeType(int type) {
        shapeType = type;
    }
    std::vector<GridPosition> getOccupiedTiles() const {
        return occupiedTiles;
    }
    void setCenter(GridPosition pos) {
        centerOfShape = pos;
        updateShapePosition();
    }
    virtual void updateShapePosition() = 0; //Uses centerOfShape to fill in the occupiedTiles vector with GridPositions
    bool moveUp() {
        if (centerOfShape.y - 1 < 0) {
            return false;
        }
        centerOfShape.y -= 1;
        updateShapePosition();
        return true;
    }
    bool moveLeft() {
        if (centerOfShape.x - 1 < 0) {
            return false;
        }
        centerOfShape.x -= 1;
        updateShapePosition();
        return true;
    }
    bool moveRight() {
        if (centerOfShape.x + 1 >= GRIDWIDTH) {
            return false;
        }
        centerOfShape.x += 1;
        updateShapePosition();
        return true;
    }
    bool moveDown() {
        if (centerOfShape.y + 1 >= GRIDHEIGHT) {
            return false;
        }
        centerOfShape.y += 1;
        updateShapePosition();
        return true;
    }
    char getTileSymbol() const {
        if (buildingType == INDUSTRIAL) return 'I';
        if (buildingType == COMMERCIAL) return 'C';
        if (buildingType == RESIDENTIAL) return 'R';
        if (buildingType == SCHOOL) return 'S';
        if (buildingType == PARK) return 'P';

        return 'E';
    }

    void rotate() { rotationState = (rotationState + 1) % 4; updateShapePosition(); }
    void flip() { flipped = !flipped; updateShapePosition(); }

protected:
    // Transform offset (dx, dy) based on rotationState and flipped
    GridPosition transformOffset(int dx, int dy) const {
        int tx = dx, ty = dy;
        // Apply flip (mirror horizontally)
        if (flipped) {
            tx = -tx;
        }
        // Apply rotation (90° clockwise steps)
        for (int i = 0; i < rotationState; ++i) {
            int newTx = -ty;
            int newTy = tx;
            tx = newTx;
            ty = newTy;
        }
        return {centerOfShape.y + ty, centerOfShape.x + tx};
    }
};

class TShape : public Shape {
/*
T Shape Center(C):
    *C*
     *
*/
public: 
    TShape(int type) {
        buildingType = type;
        shapeType = SHAPE_T;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        // Base offsets for T shape relative to center: left, right, down
        int offsets[][2] = {{-1, 0}, {1, 0}, {0, 1}};
        occupiedTiles.push_back(centerOfShape); // center always included
        for (auto& off : offsets) {
            occupiedTiles.push_back(transformOffset(off[0], off[1]));
        }
    }
};
// Mirroring and Rotation are just going to be offset attributes for updating the shape position
class ZShape : public Shape {
/*
Z Shape Center(C):
   **
    C*
*/
public:
    ZShape(int type) {
        buildingType = type;
        shapeType = SHAPE_Z;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        // Base offsets for Z shape relative to center: up, up-left, right
        int offsets[][2] = {{0, -1}, {-1, -1}, {1, 0}};
        occupiedTiles.push_back(centerOfShape);
        for (auto& off : offsets) {
            occupiedTiles.push_back(transformOffset(off[0], off[1]));
        }
    }
};

class Square : public Shape {
/*
Square Center(C):
    **
    C*
*/
public: 
    Square(int type) {
        buildingType = type;
        shapeType = SHAPE_SQUARE;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        // Base offsets for Square shape relative to center: up, up-right, right
        int offsets[][2] = {{0, -1}, {1, -1}, {1, 0}};
        occupiedTiles.push_back(centerOfShape);
        for (auto& off : offsets) {
            occupiedTiles.push_back(transformOffset(off[0], off[1]));
        }
    }
};

class LShape : public Shape {
/*
L Shape Center(C):
    *
    *
    C*
*/
public:
    LShape(int type) {
        buildingType = type;
        shapeType = SHAPE_L;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        // Base offsets for L shape relative to center: up, up-up, right
        int offsets[][2] = {{0, -1}, {0, -2}, {1, 0}};
        occupiedTiles.push_back(centerOfShape);
        for (auto& off : offsets) {
            occupiedTiles.push_back(transformOffset(off[0], off[1]));
        }
    }
};

class Line : public Shape {
/*
Line Center(C):
    *
    *
    C
    *
*/
public:
    Line(int type) {
        buildingType = type;
        shapeType = SHAPE_LINE;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        // Base offsets for Line shape relative to center: up, up-up, down
        int offsets[][2] = {{0, -1}, {0, -2}, {0, 1}};
        occupiedTiles.push_back(centerOfShape);
        for (auto& off : offsets) {
            occupiedTiles.push_back(transformOffset(off[0], off[1]));
        }
    }
};

class Single : public Shape {
/*
Single Center(C):
    C
*/
public:
    Single(int type) {
        buildingType = type;
        shapeType = SHAPE_SINGLE;
    }
    void updateShapePosition() override {
        occupiedTiles.clear();
        occupiedTiles.push_back(centerOfShape);
    }
};