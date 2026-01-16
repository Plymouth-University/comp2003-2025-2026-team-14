#pragma once

#include "constants.h"
#include "geometry.h"
#include "shape_types.h"
#include <vector>
#include <set>
#include <queue>
#include <map>
#include <memory>

// Forward declarations
class Shape;
class Zone;
using ZonePtr = std::unique_ptr<Zone>;

class GridTile {
private:
    Shape* shape; //Pointer to the shape covering the tile
    Zone* zone; //Pointer to the zone containing the shape
    GridPosition pos;
    bool river;
    bool startingSpace;
    int startingPositionNumber;

public:
    GridTile() : shape(NULL), zone(NULL), pos({-1, -1}), river(false), startingSpace(false), startingPositionNumber(0) {}
    GridTile(GridPosition pos) : shape(NULL), zone(NULL), pos(pos), river(false), startingSpace(false), startingPositionNumber(0) {}
    GridPosition getPosition() {
        return pos;
    }
    bool isRiver() {
        return river;
    }
    void setRiver(bool isRiver) {
        river = isRiver;
    }
    bool isStartingSpace() {
        return startingSpace;
    }
    void setStartingSpace(bool isStartingSpace) {
        startingSpace = isStartingSpace;
    }
    int getStartingPositionNumber() const {
        return startingPositionNumber;
    }
    void setStartingPositionNumber(int number) {
        startingPositionNumber = number;
    }
    char getTileSymbol() {
        if (shape == NULL && !isRiver()) {
            if (startingSpace) {
                if (startingPositionNumber > 0) {
                    return '0' + startingPositionNumber;
                }
                return 'S';
            }
            return '*';
        }
        else if (isRiver()) {
            return '#';
        }
        // Here you return the symbol defined in the shape object
        return shape->getTileSymbol();
    }
    bool hasShape() {
        if (shape != NULL) {
            return true;
        }
        return false;
    }
    void setShape(Shape* placedShape) {
        shape = placedShape;
    }
    Shape* getShape() {
        return shape;
    }
    void setZone(Zone& z) {
        zone = &z;
    }
    Zone* getZone() {
        return zone;
    }
    void resetZone() {
        zone = NULL;
    }
};

class Zone {
private:
    int buildingType;
    std::vector<Shape*> shapes;

public:
    Zone(int type) : buildingType(type) {}

    int getBuildingType() const { return buildingType; }
    void addShape(Shape* shape) { shapes.push_back(shape); }
    const std::vector<Shape*>& getShapes() const { return shapes; }

    int getUniqueShapeCount() const {
        std::set<int> uniqueShapeTypes;
        for (Shape* shape : shapes) {
            uniqueShapeTypes.insert(shape->getShapeType());
        }
        return uniqueShapeTypes.size();
    }

    int getScore() const {
        int uniqueCount = getUniqueShapeCount();
        if (uniqueCount >= 0 && uniqueCount <= 6) {
            return ZONE_SCORE_TABLE[uniqueCount];
        }
        return 0;
    }
};

std::vector<ZonePtr> detectZones(GridTile gridArr[GRIDHEIGHT][GRIDWIDTH]) {
    // Reset zone pointers for all tiles
    for (int y = 0; y < GRIDHEIGHT; ++y) {
        for (int x = 0; x < GRIDWIDTH; ++x) {
            gridArr[y][x].resetZone();
        }
    }

    std::vector<ZonePtr> zones;
    bool visited[GRIDHEIGHT][GRIDWIDTH] = {false};
    std::set<Shape*> visitedShapes;
    std::map<Shape*, Zone*> shapeToZone;

    for (int y = 0; y < GRIDHEIGHT; ++y) {
        for (int x = 0; x < GRIDWIDTH; ++x) {
            if (visited[y][x]) continue;
            Shape* shape = gridArr[y][x].getShape();
            if (!shape) continue;
            if (!shape->isOfZoneType()) continue;
            if (visitedShapes.count(shape)) continue;

            // Start new zone
            auto zonePtr = std::make_unique<Zone>(shape->getBuildingType());
            Zone* zone = zonePtr.get();
            std::queue<GridPosition> q;
            q.push({y, x});
            visited[y][x] = true;
            visitedShapes.insert(shape);
            shapeToZone[shape] = zone;
            zone->addShape(shape);
            gridArr[y][x].setZone(*zone);

            while (!q.empty()) {
                GridPosition pos = q.front(); q.pop();
                // Check orthogonal neighbors
                GridPosition neighbors[4] = {{pos.y-1, pos.x}, {pos.y+1, pos.x}, {pos.y, pos.x-1}, {pos.y, pos.x+1}};
                for (const GridPosition& nb : neighbors) {
                    if (nb.y < 0 || nb.y >= GRIDHEIGHT || nb.x < 0 || nb.x >= GRIDWIDTH) continue;
                    if (visited[nb.y][nb.x]) continue;
                    Shape* nbShape = gridArr[nb.y][nb.x].getShape();
                    if (!nbShape) continue;
                    if (nbShape->getBuildingType() != shape->getBuildingType()) continue;
                    visited[nb.y][nb.x] = true;
                    q.push(nb);
                    if (!visitedShapes.count(nbShape)) {
                        visitedShapes.insert(nbShape);
                        shapeToZone[nbShape] = zone;
                        zone->addShape(nbShape);
                    }
                    // Set zone pointer for this tile
                    gridArr[nb.y][nb.x].setZone(*zone);
                }
            }
            zones.push_back(std::move(zonePtr));
        }
    }
    return zones;
}