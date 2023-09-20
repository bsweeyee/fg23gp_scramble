# Scrambler

## How to use
1. Attach ScamberHandler Component to a gameobject.
2. All child gameobjects will automatically be attached with ScramblerInstance
3. Adjust initial position by simply going to any ScramblerInstance and moving it in the editor
4. Call SetState method with input integer to toggle between 4 states
    - STOP_REVERT: 0
        - Set this state to stop after finishing REVERT
    - STOP_SCRAMBLE: 1
        - Set this state to stop after finishing SCRAMBLE
    - SCRAMBLE: 2
        - Set this state to scramble child objects 
    - REVERT: 3
        - Set this state to reverse child objects to original position
    
## Features
- Define random parameters: random zone, range of rotation, range of scale
- Define how fast objects move with total travel time
- Debug options to visualize movement, rotation and target points etc.