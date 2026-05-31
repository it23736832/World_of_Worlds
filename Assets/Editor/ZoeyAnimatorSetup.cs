#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class ZoeyAnimatorSetup
{
    private const string ControllerPath = "Assets/NPC/ZoeyAnimatorController.controller";
    private const string PrefabSavePath  = "Assets/NPC/ZoeyNPC.prefab";
    private const string GlbPath         = "Assets/NPC/zoey_kpop_demon_hunters.glb";

    // -------------------------------------------------------------------------
    // Step 1: Run this first to create the Animator Controller
    // -------------------------------------------------------------------------
    [MenuItem("Tools/Zoey/1 - Create Animator Controller")]
    public static void CreateController()
    {
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        CreateState(sm, "Fist Fight A",                  "Assets/Animations/Fist Fight A.fbx");
        CreateState(sm, "Fist Fight B",                  "Assets/Animations/Fist Fight B.fbx");
        CreateState(sm, "Kicking",                       "Assets/Animations/Kicking.fbx");
        CreateState(sm, "Standing React Death Backward", "Assets/Animations/Standing React Death Backward.fbx");

        sm.defaultState = sm.states[0].state;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ZoeySetup] Animator Controller saved to {ControllerPath}");
        EditorGUIUtility.PingObject(ctrl);
    }

    // -------------------------------------------------------------------------
    // Step 2: Run this after Step 1 to create the ZoeyNPC prefab
    // -------------------------------------------------------------------------
    [MenuItem("Tools/Zoey/2 - Create Zoey Prefab")]
    public static void CreateZoeyPrefab()
    {
        GameObject modelSource = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
        if (modelSource == null)
        {
            Debug.LogError($"[ZoeySetup] GLB not found at {GlbPath}. Make sure zoey_kpop_demon_hunters.glb is imported.");
            return;
        }

        // Object.Instantiate fully detaches from the source so overrides bake in correctly
        GameObject zoey = Object.Instantiate(modelSource);
        zoey.name = "ZoeyNPC";

        // Ensure Animator exists and assign the controller
        Animator animator = zoey.GetComponent<Animator>();
        if (animator == null) animator = zoey.AddComponent<Animator>();

        RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (ctrl != null)
            animator.runtimeAnimatorController = ctrl;
        else
            Debug.LogWarning($"[ZoeySetup] Controller not found at {ControllerPath}. Run Step 1 first.");

        // Auto-assign the humanoid avatar from the GLB sub-assets
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(GlbPath))
        {
            if (asset is Avatar avatar)
            {
                animator.avatar = avatar;
                Debug.Log($"[ZoeySetup] Avatar '{avatar.name}' assigned.");
                break;
            }
        }

        animator.applyRootMotion  = false;
        animator.cullingMode      = AnimatorCullingMode.AlwaysAnimate;

        zoey.transform.localScale    = Vector3.one * 5.79f;
        zoey.transform.localRotation = Quaternion.identity;

        // Add the fight sequence script
        zoey.AddComponent<ZoeyFightSequence>();

        PrefabUtility.SaveAsPrefabAsset(zoey, PrefabSavePath);
        Object.DestroyImmediate(zoey);

        AssetDatabase.Refresh();
        Debug.Log($"[ZoeySetup] ZoeyNPC prefab saved to {PrefabSavePath}. Assign it to ZoeyHelpUI._zoeyPrefab in the Inspector.");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSavePath));
    }

    // -------------------------------------------------------------------------
    // Step 3: Run this if the Animator Controller states have no motions assigned
    // -------------------------------------------------------------------------
    [MenuItem("Tools/Zoey/3 - Fix Animator Motions")]
    public static void FixMotions()
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null)
        {
            Debug.LogError("[ZoeySetup] Controller not found. Run Step 1 first.");
            return;
        }

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AssignMotion(sm, "Fist Fight A",                  "Assets/Animations/Fist Fight A.fbx");
        AssignMotion(sm, "Fist Fight B",                  "Assets/Animations/Fist Fight B.fbx");
        AssignMotion(sm, "Kicking",                       "Assets/Animations/Kicking.fbx");
        AssignMotion(sm, "Standing React Death Backward", "Assets/Animations/Standing React Death Backward.fbx");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[ZoeySetup] Motions assigned. Check the Animator window — each state should now show a clip name.");
    }

    private static void AssignMotion(AnimatorStateMachine sm, string stateName, string fbxPath)
    {
        AnimationClip clip = FindClipInFbx(fbxPath);
        if (clip == null)
        {
            Debug.LogWarning($"[ZoeySetup] No clip found in '{fbxPath}'. Check the file exists and is imported.");
            return;
        }

        foreach (ChildAnimatorState s in sm.states)
        {
            if (s.state.name == stateName)
            {
                s.state.motion = clip;
                Debug.Log($"[ZoeySetup] Assigned '{clip.name}' → state '{stateName}'");
                return;
            }
        }
        Debug.LogWarning($"[ZoeySetup] State '{stateName}' not found in controller.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    private static void CreateState(AnimatorStateMachine sm, string stateName, string fbxPath)
    {
        AnimatorState state = sm.AddState(stateName);
        AnimationClip clip  = FindClipInFbx(fbxPath);
        if (clip != null)
            state.motion = clip;
        else
            Debug.LogWarning($"[ZoeySetup] No clip found in {fbxPath}. Assign the motion manually in the controller.");
    }

    private static AnimationClip FindClipInFbx(string fbxPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
#endif
