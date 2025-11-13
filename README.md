# Procedural Grid Generation
In this project, you can generate a level by four generating algorithms, everyone configurable.<br>
- SimpleRoomPlacement
- BSP
- Cellular Automata
- FastNoiseLite

## How to start

### Step 1
Load a scene, I recommend you to use the GenerationScene in Assets/Scene<br>
![Scenes folder](https://example.com/path/to/image.png)

### Step 2
In the GenerationScene, you have a ProceduralGridGenerator object,<br>
this is the main of this project, inside you have grid parameters and generation method settings.<br>
Let's try with SimpleRoom Placement algorithm, set the generation method to Simple Room placement and play the game.<br>
![PGG_SRP_SEL](https://example.com/path/to/image.png)

<i>With the seed is "1234" and Room parameters set to "4", here is the result:</i><br>
![PGG_SRP_RESULT](https://example.com/path/to/image.png)

## Warning
<i>
• BSP methods have problems; the only one that is 100% functional is BSP_Correction.<br>
• If you want to move the grid, you must also change “Start Position" in the grid parameters<br>
and You cannot rotate the grid, as all algorithms use WorldSpace coordinates and are dependent on rotation.
</i>

## Examples
### BSP
