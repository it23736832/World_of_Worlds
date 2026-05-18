#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public static class DoorSetupTool
{
    private const string ControllerPath    = "Assets/Abandoned_Asylum/Animations/DoorAnimator.controller";
    private const string TriggerChildName  = "DoorTriggerZone";

    // ── Reset & Clean ─────────────────────────────────────────────────────────

    /// <summary>
    /// Removes every DoorTriggerZone child and every DoorTrigger component from
    /// the scene, then places exactly ONE DoorTrigger on each door root.
    /// A door root is any GameObject whose direct children include a panel named
    /// with "right" or "left" (e.g. DoorD_V2_Right).
    /// doorTransform is explicitly wired to that right/left child so Awake()'s
    /// search is never needed.
    /// </summary>
    [MenuItem("Tools/Abandoned Asylum/Reset and Clean Door Setup")]
    public static void ResetAndCleanDoorSetup()
    {
        // ── Step 1: destroy all DoorTriggerZone children ──────────────────────
        Transform[] allTransforms = GameObject.FindObjectsByType<Transform>();
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform t in allTransforms)
        {
            if (t.name == TriggerChildName)
                toDestroy.Add(t.gameObject);
        }

        foreach (GameObject go in toDestroy)
            Undo.DestroyObjectImmediate(go);

        // ── Step 2: remove any stray DoorTrigger components ───────────────────
        DoorTrigger[] stray = GameObject.FindObjectsByType<DoorTrigger>();
        foreach (DoorTrigger dt in stray)
            Undo.DestroyObjectImmediate(dt);

        // ── Step 3: find door roots ───────────────────────────────────────────
        // A door root has at least one direct child whose name contains "right" or "left".
        allTransforms = GameObject.FindObjectsByType<Transform>();
        List<Transform> doorRoots = new List<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (HasPanelChild(t))
                doorRoots.Add(t);
        }

        if (doorRoots.Count == 0)
        {
            Debug.LogWarning("DoorSetupTool: No door roots found (objects with a child named *right* or *left*).");
            return;
        }

        // ── Step 4: add one DoorTrigger per root ──────────────────────────────
        int added = 0;

        foreach (Transform root in doorRoots)
        {
            Transform panel = FindPanelChild(root);   // _Right preferred, _Left fallback

            // Create trigger zone child centered at the door.
            GameObject zone = new GameObject(TriggerChildName);
            Undo.RegisterCreatedObjectUndo(zone, "Create Door Trigger Zone");
            zone.transform.SetParent(root, false);
            zone.transform.localPosition = Vector3.zero;

            DoorTrigger dt = zone.AddComponent<DoorTrigger>();

            // Wire doorTransform explicitly — bypasses Awake() search entirely.
            SerializedObject so = new SerializedObject(dt);
            so.FindProperty("doorTransform").objectReferenceValue = panel;

            // Wire animator if present on the root.
            Animator anim = root.GetComponent<Animator>();
            if (anim != null)
                so.FindProperty("doorAnimator").objectReferenceValue = anim;

            // useTransformFallback = true (default), so rotation coroutine is used.
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dt);

            added++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"DoorSetupTool: Cleaned up and placed {added} DoorTrigger(s) on door roots. Save the scene.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool HasPanelChild(Transform t)
    {
        foreach (Transform child in t)
        {
            string lower = child.name.ToLowerInvariant();
            if (lower.Contains("right") || lower.Contains("left"))
                return true;
        }
        return false;
    }

    // Returns the _Right child first, then _Left, then first child.
    private static Transform FindPanelChild(Transform root)
    {
        Transform fallback = null;
        foreach (Transform child in root)
        {
            string lower = child.name.ToLowerInvariant();
            if (lower.Contains("right")) return child;
            if (lower.Contains("left"))  fallback = child;
        }
        return fallback ?? root.GetChild(0);
    }

    // ── Setup All Doors (original — kept for reference) ───────────────────────

    [MenuItem("Tools/Abandoned Asylum/Setup All Doors")]
    public static void SetupAllDoors()
    {
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

        Transform[] allTransforms = GameObject.FindObjectsByType<Transform>();
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
            if (door.transform.Find(TriggerChildName) != null) continue;

            Undo.RecordObject(door, "Setup Door");

            Animator anim = door.GetComponent<Animator>();
            if (anim == null)
                anim = Undo.AddComponent<Animator>(door);

            if (anim.runtimeAnimatorController == null)
                anim.runtimeAnimatorController = controller;

            GameObject triggerZone = new GameObject(TriggerChildName);
            Undo.RegisterCreatedObjectUndo(triggerZone, "Create Door Trigger Zone");
            triggerZone.transform.SetParent(door.transform, false);
            triggerZone.transform.localPosition = new Vector3(0f, 1f, 0.8f);

            BoxCollider col = triggerZone.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(2f, 2f, 1.5f);

            triggerZone.AddComponent<DoorTrigger>();

            setupCount++;
        }

        Debug.Log($"DoorSetupTool: Set up {setupCount} door(s). {doors.Count - setupCount} already had triggers.");
    }

    // ── Fix All Existing Door Triggers ────────────────────────────────────────

    [MenuItem("Tools/Abandoned Asylum/Fix All Existing Door Triggers")]
    public static void FixAllDoorTriggers()
    {
        DoorTrigger[] triggers = GameObject.FindObjectsByType<DoorTrigger>();

        if (triggers.Length == 0)
        {
            Debug.LogWarning("DoorSetupTool: No DoorTrigger components found in the scene.");
            return;
        }

        int fixedCount = 0;

        foreach (DoorTrigger trigger in triggers)
        {
            SerializedObject so = new SerializedObject(trigger);
            bool changed = false;

            SerializedProperty doorTransformProp = so.FindProperty("doorTransform");
            if (doorTransformProp.objectReferenceValue == null && trigger.transform.parent != null)
            {
                doorTransformProp.objectReferenceValue = trigger.transform.parent;
                changed = true;
            }

            SerializedProperty doorAnimatorProp = so.FindProperty("doorAnimator");
            if (doorAnimatorProp.objectReferenceValue == null)
            {
                Animator anim = trigger.GetComponentInParent<Animator>();
                if (anim != null)
                {
                    doorAnimatorProp.objectReferenceValue = anim;
                    changed = true;
                }
            }

            BoxCollider existingBox = trigger.GetComponent<BoxCollider>();
            if (existingBox == null)
            {
                Collider existing = trigger.GetComponent<Collider>();
                MeshCollider meshCol = existing as MeshCollider;
                if (existing != null && meshCol == null)
                {
                    if (!existing.isTrigger)
                    {
                        Undo.RecordObject(existing, "Set Door Collider Is Trigger");
                        existing.isTrigger = true;
                        changed = true;
                    }
                }
                else
                {
                    Undo.RecordObject(trigger.gameObject, "Add Door Trigger BoxCollider");
                    existingBox = trigger.gameObject.AddComponent<BoxCollider>();
                    changed = true;
                }
            }

            if (existingBox != null)
            {
                Vector3 wantedSize   = new Vector3(2f, 4f, 2f);
                Vector3 wantedCenter = new Vector3(0f, -1f, 0f);
                if (existingBox.size != wantedSize || existingBox.center != wantedCenter)
                {
                    Undo.RecordObject(existingBox, "Resize Door Trigger Collider");
                    existingBox.isTrigger = true;
                    existingBox.size      = wantedSize;
                    existingBox.center    = wantedCenter;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(trigger);
                fixedCount++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"DoorSetupTool: Fixed {fixedCount} / {triggers.Length} DoorTrigger(s). Save the scene to keep changes.");
    }
}
#endif
