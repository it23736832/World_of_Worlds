#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Tools > Abandoned Asylum > Setup All Doors
/// Finds every GameObject whose name contains "door" (case-insensitive),
/// adds an Animator with the shared DoorController, and adds a trigger
/// zone child with DoorTrigger — skipping any that are already set up.
/// </summary>
public static class DoorSetupTool
{
    private const string ControllerPath = "Assets/Abandoned_Asylum/Animations/DoorAnimator.controller";
    private const string TriggerChildName = "DoorTriggerZone";

    [MenuItem("Tools/Abandoned Asylum/Setup All Doors")]
    public static void SetupAllDoors()
    {
        // Make sure the animator controller exists first.
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            CreateDoorAnimatorController.Create();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        if (controller == null)
        {
            Debug.LogError("DoorSetupTool: Could not find or create DoorAnimator.controller.");
            return;
        }

        // Find all GameObjects in the scene whose name contains "door".
        Transform[] allTransforms = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        List<GameObject> doors = new List<GameObject>();

        foreach (Transform t in allTransforms)
        {
            if (t.name.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                doors.Add(t.gameObject);
        }

        if (doors.Count == 0)
        {
            Debug.LogWarning("DoorSetupTool: No GameObjects with 'door' in their name found in the scene.");
            return;
        }

        int setupCount = 0;

        foreach (GameObject door in doors)
        {
            // Skip if trigger zone already exists on this door.
            if (door.transform.Find(TriggerChildName) != null) continue;

            Undo.RecordObject(door, "Setup Door");

            // 1. Add Animator to the door if missing.
            Animator anim = door.GetComponent<Animator>();
            if (anim == null)
                anim = Undo.AddComponent<Animator>(door);

            if (anim.runtimeAnimatorController == null)
                anim.runtimeAnimatorController = controller;

            // 2. Create trigger zone child.
            GameObject triggerZone = new GameObject(TriggerChildName);
            Undo.RegisterCreatedObjectUndo(triggerZone, "Create Door Trigger Zone");
            triggerZone.transform.SetParent(door.transform, false);
            triggerZone.transform.localPosition = new Vector3(0f, 1f, 0.8f);

            // 3. Add trigger collider sized for interaction reach.
            BoxCollider col = triggerZone.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(2f, 2f, 1.5f);

            // 4. Add DoorTrigger — Awake() will auto-find the Animator from parent.
            triggerZone.AddComponent<DoorTrigger>();

            setupCount++;
        }

        Debug.Log($"DoorSetupTool: Set up {setupCount} door(s). {doors.Count - setupCount} already had triggers.");
    }
}
#endif
