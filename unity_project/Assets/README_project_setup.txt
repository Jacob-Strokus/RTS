Unity project placeholder.
Create a new Unity URP project here named FrontierAges.
Then add Assembly Definitions:
 - Assets/Scripts/Simulation/Gameplay.Simulation.asmdef (no UnityEngine refs if possible)
 - Assets/Scripts/Presentation/Gameplay.Presentation.asmdef (reference Simulation)
 - Assets/Scripts/Editor/Gameplay.Editor.asmdef (Editor only, reference Simulation & Presentation)
Attach SimBootstrap to an empty GameObject in a new scene and run – it will tick the simulation.
Add an empty parent object named MainCameraRig with RTSCameraController, child a Camera angled ~60 deg downward, position rig at (0,40,-40) looking toward origin.
Create a simple Plane (10x10) as ground, add collider for raycasts. Assign a simple UnitPrefab (capsule + collider) to SimBootstrap.
