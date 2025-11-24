[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/xGnTrW1S)
[![Open in Codespaces](https://classroom.github.com/assets/launch-codespace-2972f46106e565e64193e422d61a12cf1da4916b45550586e14ef0a7c637dd04.svg)](https://classroom.github.com/open-in-codespaces?assignment_repo_id=20914604)
# COMP2003-2025 : Pocket Planner
Pocket Planner is a mobile video game that combines SimCity elements with a complex scoring system to create an entertaining multiplayer experience.
The gameplay involves dice rolls to determine the types and shapes of the buildings you can build on the 10x10 during each round and making the decision which scores you points. At the end of the game,
everyone's scores are tallied up and the one with the most points is declared the winner. 
The game features:
- a matchmaking system that allows you to pair up with your friends across the world via lobby codes
- a simple yet elegant artstyle
- pleasant music that will make you feel good

The detailed gameplay loop is as follows:
Rounds start with a roll of 6 dice, all of which have different faces with specific effects on the types of buildings you can construct.
  -  Residential, Commercial and Industrial buildings are generic structures which offer no points on their own but can be connected to earn a point per connected structure.
  -  Parks and schools are special structures that award 2 points for each connected structure.
  -  The tetriminos determine the shapes of the structures you can place on your board. First piece must touch the river.
  -  The river face allows for the placement of a structure next to the river and the selection of any type of structure for that tetrimino.

Players choose the type and shape of structure they want to build and place it in a legal position.
Stars are awarded if the same structure type or shape is rolled twice and used in that turn and adds a point to the player's tally.
This cycle is then repeated until one player cannot place any more structures, which marks the end of the game. The scores are then tallied up to reveal the winner.
