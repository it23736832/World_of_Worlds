---
name: unity-csharp
description: Use when writing or editing C# scripts for Unity. Covers MonoBehaviour
lifecycle, physics, NavMesh, serialization, coroutines, and Unity-specific patterns.
---

# Unity C# Patterns for This Project

## MonoBehaviour Lifecycle Order
Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate → OnDisable → OnDestroy

## Physics Interactions (Systems Engineer)
- Use Rigidbody + Colliders for interactive objects (barricades, doors)
- FixedUpdate for physics force application
- OnCollisionEnter/OnTriggerEnter for detection
- Layer-based collision matrix for performance

## NavMesh (World Builder + Graph Formulation)
- NavMesh.SamplePosition() to snap points to walkable surface
- NavMeshTriangulation for extracting mesh data into graph
- NavMeshObstacle for dynamic obstacles (barricades)

## Agent Movement (Agent Controller)
- Use NavMeshAgent OR manual transform movement following path arrays
- Quaternion.Slerp for smooth rotation toward next waypoint
- Animator component with blend trees for walk/run/idle transitions
