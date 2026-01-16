#pragma once

#include "constants.h"

struct GridPosition {
public:
    int y;
    int x;

    GridPosition() : y(-1), x(-1) {}
    GridPosition(int y, int x) : y(y), x(x) {}

    bool operator==(const GridPosition& other) const {
        return y == other.y && x == other.x;
    }

    bool operator!=(const GridPosition& other) const {
        return !(*this == other);
    }

    bool operator<(const GridPosition& other) const {
        if (y != other.y) return y < other.y;
        return x < other.x;
    }
};

inline bool withinGridBoundaries(GridPosition p) {
    return p.y >= 0 && p.y < GRIDHEIGHT && p.x >= 0 && p.x < GRIDWIDTH;
}