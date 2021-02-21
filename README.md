# Unity DOTS Genetic Neural Network

This project is inspired by Genetic Neural Network https://github.com/iambackit/COPS_AI. 
However, it has been completely reworked for DOTS. Some common feature may be recognised.


## What to expect.

After opening the project scene, multiple maps will be visible.
Additional maps can be added, or removed.
Each map is a based on a prefab. It contains barriers, checkpoints, and car spawner point.

* Barriers basically limits, where car can go.
* Checkpoints allow each car to gain score, which contributes for learning.
* Car spawner points spawn cars at its position, with some additional random position and rotation. Rotation of checkpoint, will spawn cars facing local X axis. More points can be added, or removed as needed per each map.

Maps can have different barriers configuration and have variation position and orientations of checkpoints.


## Training 

Project comes with partially pretrained set of brains, based on 320 cars. 
The training took arround 45 min.
After running further training, car should arrive to the end of map, within few generations.
Data sets can be marked in a manager Game Object, to be saved and loaded.


## Cars

Each car has LIDAR, which casts rays in front of car, 180 degrees.
Results are passed into NN.
Steering and throthle, is determined by car controller system. 
Values can be further tweeked. Cars behaviour is rather rough, with main goal, to prove of the concept.


## Game Object Manager with conversion to entity

GO Manager contains range of settings, allowing to define, how training will be proceeded.
It allows to define, how various new generaions will be, based on parents.
Manager is converted to entity at runtime and can be viewed via Entity Debugger.
Changing settings, may require at current state, stop and run game again.
Manager is just example and can be further modified.
It is responsible for managing two sets of populations, current and previous (parents).
Hence, if target population is 320, total visible population will be 620, but active and trained will be 320.
The duration of training is based on the preset time and alive brains. 
If time runs out (30 sec (starting time is lower)), or no alive population is present, generation will be finalised and new generation will start.
If car hits a barrier, it become inhibitted, until next generation. Score is evaluated and then is spawned again at spawn point.


## Genetic Neural Network

Each generation spawns set of entities, in this case cars.
Each entity holds own brain, with own net settings. See Entity Debugger for details.
This NN uses 3 layers, input, hidden and output.

* Input layer is taking 9 LIDAR raycasts as input, speed and skidding factors. Total 11 neurons.
* Hidden layer has same amount of neurons as input. But can be changed in a example manager.
* Output layer returns throtle and steering values, ranging 0.0 to 1.0. 
* Weights count are multiplication of previous and next layer.

When new generation is initialized, it first crossover with previous generation. Then mutation is applied, based on settings of a example manager.


## Things to know

First generation is always random, regardles if is read from file, or not.
Second generation is first one, which contributes into training.


## Requirements
Unity 2020.1.3 or later.


## Known issues

Sometimes at initial runtime, cars seems to ignore Unity physics collisions. Don't know the reason at this point.
Usually few generations later, all gets fine.
Viewing scene and game at the same time, while training is running, may slowdown the simulation.
Spawning too many cars at the same spawner, may intoroduce lag and significant slowdown, due to Unity Physics collision system.
Best way to prevent it, is to add more spawn points in different places, or spawn more maps with points. Number of cars will be distributed accross all points.
Keep in mind, to keep size of population, as multipler of number of spawners. Otherwise error may throw out. Desired number of cars per spawns, is around 300 or below.
