# CLAUDE.md

## Project Overview
Asylum-themed 3D simulation for SLIIT BSc CS Year 3.
Joint project: SE3032 (Graphics & Visualization) + SE3062 (Intelligent Systems).
AI agents pathfind through an abandoned asylum; player can place barricades
and interact with doors to force agents to recalculate routes.
Engine: Unity [YOUR VERSION]
Render Pipeline: [URP / Built-in]

## Team
- [Name] — World Builder (GV) / Graph Formulation (IS)
- [Name] — Systems Engineer (GV) / Dynamic Adaptation (IS)
- [Name] — Core Developer (GV) / A* Search (IS)
- [Name] — Agent Controller (GV) / Secondary Search & Debugger (IS)

## What Already Exists
Main environment: Abandoned_Asylum/ package (114 models, 95+ prefabs, 150 materials)
Primary scene: Scenes/asylum.unity
Player movement: Scripts/ThirdPersonMovement.cs + ThirdPersonCamera.cs
Alt FPS mode: Abandoned_Asylum/scripts/FirstPersonMovement.cs + cameraControll.cs
Door system: Abandoned_Asylum/scripts/DoorTrigger.cs (E-key open/close with animation)
Portals: PortalTeleporter.cs and ScenePortal.cs
Editor tools: door animator creators, door setup tool, player animation fixer
NPC models: npc_casual_set_00/ (ready for agent characters)
Animator controllers: DoorAnimator.controller, PlayerAnimator.controller

## What Needs to Be Built
ALL AI/IS components are missing:
- Assets/Scripts/Graph/ — NavMesh triangulation → adjacency list graph
- Assets/Scripts/AI/ — A* with min-heap, BFS or UCS as secondary search
- Assets/Scripts/Agent/ — path-following, smooth rotation, walk animation
- Assets/Scripts/Debug/ — F1-toggle debug overlay (graph edges, paths, frontiers)
- Barricade system — player places obstacles, graph edges get severed, agents recalc

## Architecture
- The NavMesh is baked by the World Builder and consumed by Graph Formulation scripts
- Graph is stored as adjacency list: Dictionary<int, List<Edge>>
- When player places a barricade, Dynamic Adaptation severs graph edges and triggers recalc
- A* uses Euclidean distance heuristic (admissible for 3D space)
- Debug mode (F1 toggle) draws paths using Unity Gizmos or GL lines
- Custom models come from Blender as .fbx with embedded textures
- NPC agents use characters from npc_casual_set_00/ with Animator + path-following

## Code Conventions
- C# Unity style: PascalCase public, _camelCase private
- [SerializeField] for inspector fields
- Existing scripts do NOT use namespaces — keep consistent, don't add namespaces
- Keep .meta files paired with every asset move

## Critical Rules
- NEVER move files inside Abandoned_Asylum/, Flooded_Grounds/, or other
  third-party pack folders — they have internal references that will break
- Don't reorganize existing working scripts — only organize NEW code
- DoorTrigger.cs handles door interaction — barricade system should follow
  similar pattern but also notify graph system to sever edges
- NavMesh bake: Window > AI > Navigation (rebake after geometry changes)
- After pulling teammates' changes, run the project in Unity before 
  editing anything to make sure it compiles and nothing is broken
## UI Elements
- Torch: toggle with T key or mouse click, has limited fuel/battery (UI bar)
- Interaction prompts: context-sensitive ("Press E to open", "Press G to grab")
- Objective tracker: top of screen, updates per level
- Game over screen: when villain reaches player
- Level transition: brief loading/story screen between portals
- Villain proximity: screen vignette or heartbeat when villain is close

## Git
- Prefix: feat:, fix:, refactor:, asset:
- Never commit Library/ or Temp/