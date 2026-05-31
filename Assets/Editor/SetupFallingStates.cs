using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupFallingStates
{
    [MenuItem("Tools/Setup Falling States for Altar")]
    static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/PlayerAnimator.controller");
        if (controller == null) { Debug.LogError("[SetupFalling] PlayerAnimator.controller not found at Assets/PlayerAnimator.controller"); return; }

        AnimationClip fallingIdleClip = FindClip("Assets/Animations/Falling Idle.fbx");
        AnimationClip landingClip     = FindClip("Assets/Animations/Falling To Landing.fbx");

        if (fallingIdleClip == null) { Debug.LogError("[SetupFalling] No AnimationClip found in 'Falling Idle.fbx'"); return; }
        if (landingClip == null)     { Debug.LogError("[SetupFalling] No AnimationClip found in 'Falling To Landing.fbx'"); return; }

        Debug.Log($"[SetupFalling] Clips found: '{fallingIdleClip.name}', '{landingClip.name}'");

        // Add IsFalling bool param if not already present
        bool hasParam = false;
        foreach (var p in controller.parameters)
            if (p.name == "IsFalling") { hasParam = true; break; }
        if (!hasParam)
            controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Remove any stale FallingIdle / Landing states left from a previous attempt
        foreach (var cs in sm.states)
            if (cs.state.name == "FallingIdle" || cs.state.name == "Landing")
                sm.RemoveState(cs.state);

        // Find Locomotion state (transition target after landing)
        AnimatorState locomotion = null;
        foreach (var cs in sm.states)
            if (cs.state.name == "Locomotion") { locomotion = cs.state; break; }
        if (locomotion == null) { Debug.LogError("[SetupFalling] 'Locomotion' state not found in Base Layer"); return; }

        // Add states
        var fallingIdle = sm.AddState("FallingIdle", new Vector3(560, 580, 0));
        fallingIdle.motion = fallingIdleClip;

        var landing = sm.AddState("Landing", new Vector3(760, 580, 0));
        landing.motion = landingClip;

        // AnyState → FallingIdle  (IsFalling = true, no exit time)
        var t1 = sm.AddAnyStateTransition(fallingIdle);
        t1.AddCondition(AnimatorConditionMode.If, 0, "IsFalling");
        t1.duration          = 0.1f;
        t1.hasExitTime       = false;
        t1.canTransitionToSelf = false;

        // FallingIdle → Landing  (IsFalling = false)
        var t2 = fallingIdle.AddTransition(landing);
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFalling");
        t2.duration    = 0.1f;
        t2.hasExitTime = false;

        // Landing → Locomotion  (exit time 90%)
        var t3 = landing.AddTransition(locomotion);
        t3.hasExitTime = true;
        t3.exitTime    = 0.9f;
        t3.duration    = 0.2f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupFalling] Done — FallingIdle and Landing states added to PlayerAnimator.");
    }

    static AnimationClip FindClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                return clip;
        return null;
    }
}
