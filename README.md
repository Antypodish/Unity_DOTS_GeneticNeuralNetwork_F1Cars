# Unity DOTS Genetic Neural Network

This project is inspired by Genetic Neural Network https://github.com/iambackit/COPS_AI. 
However, it has been completely reworked for DOTS. Some common features may be recognised.

![Watch the video](https://forum.unity.com/attachments/upload_2021-2-21_17-34-10-png.800219/)

![Watch the video](https://www.youtube.com/watch?v=vFFk4b2Xm0o)


## Motivation

Well, I love AI stuff, that should be plenty of the reason :)
But what I wanted to achieve, is to stray away a bit from Unity ML utilities, which is only working on dev machines and requires python, to do training. 
I want to be able to train AI at runtime as well, maybe even by players during game play.
Depending on the desired project. 
I have been playing with different NNs, but all have been written, using classical OOP paradigm. 
But I really wanted have NN in DOTS for long time already.
So hence here is my motivation.


## What to expect

After opening the project scene, multiple maps will be visible.
Additional maps can be added, or removed.
Each map is based on a prefab. It contains barriers, checkpoints, and car spawner points.

* Barriers basically limit where cars can go.
* Checkpoints allow each car to gain a score, which contributes to learning.
* Car spawner points spawn cars at its position, with some additional random position and rotation. Rotation of the checkpoint, will spawn cars facing the local X axis. More points can be added, or removed as needed per each map.

Maps can have different barriers configuration and have variation position and orientations of checkpoints.


## Training 

Project comes with a partially pretrained set of brains, based on 320 cars. 
The training took around 45 min.
After running further training, the car should arrive at the end of the map, within a few generations.
Data sets can be marked in a manager Game Object, to be saved and loaded.


## Cars

Each car has LIDAR, which casts rays in front of the car, 180 degrees.
Results are passed into NN.
Steering and throttle, is determined by the car controller system. 
Values can be further tweaked. Cars behaviour is rather rough, with the main goal, to prove the concept.

![Watch the video](https://forum.unity.com/attachments/upload_2021-2-21_3-48-28-png.799946/)


## Game Object Manager with conversion to entity

GO Manager contains a range of settings, allowing to define, how training will be proceeded.
It allows to define, how various new generations will be, based on parents.
Manager is converted to entity at runtime and can be viewed via Entity Debugger.
Changing settings, may require at current state, stop and run game again.
Manager is just an example and can be further modified.
It is responsible for managing two sets of populations, current and previous (parents).
Hence, if the target population is 320, the total visible population will be 620, but active and trained will be 320.
The duration of training is based on the preset time and alive brains. 
If time runs out (30 sec (starting time is lower)), or no alive population is present, generation will be finalised and a new generation will start.
If a car hits a barrier, it becomes inhibited, until the next generation. Score is evaluated and then is spawned again at spawn point.

![Watch the video](https://forum.unity.com/attachments/upload_2021-2-21_17-35-46-png.800222/)


## Genetic Neural Network

Each generation spawns a set of entities, in this case cars.
Each entity holds its own brain, with its own net settings. See Entity Debugger for details.
This NN uses 3 layers, input, hidden and output.

* Input layer is taking 9 LIDAR raycasts as input, speed and skidding factors. Total 11 neurons.
* Hidden layer has the same amount of neurons as input. But can be changed in an example manager.
* Output layer returns throttle and steering values, ranging 0.0 to 1.0. 
* Weights count are multiplication of previous and next layer.

When a new generation is initialized, it first crossovers with the previous generation. Then mutation is applied, based on settings of an example manager.


## What definatelly is still missing

* ~~Cars should be marked, or indicated, that they have finished training. For example, when they hit a wall, they should change color. I.e. to semi transparent.~~
* Manger input for saving / loading file path with brains. For now is hard coded.
* Support for multiple independet managers. (Not tested, partially implemented).


## Things to know

First generation is always random, regardless if it is read from file, or not.
The Second generation is the first one, which contributes into training.


## Requirements
Unity 2020.1.3 or later.


## Known issues

* Sometimes at initial runtime, cars seem to ignore Unity physics collisions. Don't know the reason at this point. Usually a few generations later, all gets fine.
* Viewing scene and game at the same time, while training is running, may slow down the simulation.
* Spawning too many cars at the same spawner, may introduce lag and significant slowdown, due to Unity Physics collision system. Best way to prevent it, is to add more spawn points in different places, or spawn more maps with points. Number of cars will be distributed across all points. Keep in mind, to keep the size of the population, as a multiplier of the number of spawners. Otherwise error may throw out. Desired number of cars per spawns, is around 300 or below.
* Example Manager System for handling new generations is far from being optimal. It may drop FPS for few framse. But otherwise, I obsererve quite decent performance as of this rather rough work.
