#pragma once

// Grid dimensions
#define GRIDHEIGHT 10
#define GRIDWIDTH 10

// Building types (matching existing defines in main.cpp)
#define INDUSTRIAL 1
#define RESIDENTIAL 2
#define COMMERCIAL 3
#define SCHOOL 4
#define PARK 5
#define WATER 6

// Shape types for dice and shape generation
enum ShapeType {
    SHAPE_T = 0,
    SHAPE_Z,
    SHAPE_SQUARE,
    SHAPE_L,
    SHAPE_LINE,
    SHAPE_SINGLE,
    NUM_SHAPE_TYPES
};

// Dice constants
#define NUM_SHAPE_DICE 3
#define NUM_BUILDING_DICE 3
#define TOTAL_DICE (NUM_SHAPE_DICE + NUM_BUILDING_DICE)

// Scoring constants (from CLAUDE.md)
const int ZONE_SCORE_TABLE[] = {0, 1, 2, 4, 7, 11, 16}; // index = unique shapes
const int PARK_SCORE_PER_ZONE = 2;
const int SCHOOL_SCORE_PER_RESIDENTIAL = 2;
const int STAR_SCORE = 1;
const int PENALTY_PER_EMPTY_CELL = -1;

// Wildcard costs (from CLAUDE.md)
const int WILDCARD_COST_FIRST = -1;
const int WILDCARD_COST_SECOND = -2;
const int WILDCARD_COST_THIRD = -3;

// Color pair IDs for ncurses
const int COLOR_PAIR_DEFAULT = 1;
const int COLOR_PAIR_RIVER = 2;
const int COLOR_PAIR_STARTING_SPACE = 3;
const int COLOR_PAIR_EMPTY = 4;
const int COLOR_PAIR_INDUSTRIAL = 5;
const int COLOR_PAIR_RESIDENTIAL = 6;
const int COLOR_PAIR_COMMERCIAL = 7;
const int COLOR_PAIR_SCHOOL = 8;
const int COLOR_PAIR_PARK = 9;
const int COLOR_PAIR_SHAPE_DICE = 10;
const int COLOR_PAIR_BUILDING_DICE = 11;