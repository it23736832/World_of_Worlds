using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupFallingStates
{
    [MenuItem("Tools/Setup Falling States for Altar")]
    static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/PlayerAnimator.controller");
        if (controller == null) { Debug.LogError("[SetupFalling] PlayerAnimator.controller not found"); return; }

        AnimationClip fallingIdleClip = FindClip("Assets/Animations/Falling Idle.fbx");
        AnimationClip landingClip     = FindClip("Assets/Animations/Falling To Landing.fbx");
        if (fallingIdleClip == null) { Debug.LogError("[SetupFalling] Falling Idle clip not found"); return; }
        if (landingClip == null)     { Debug.LogError("[SetupFalling] Falling To Landing clip not found"); return; }

        var sm = controller.layers[0].stateMachine;

        // --- Parameters ---
        EnsureParam(controller, "IsFalling", AnimatorControllerParameterType.Bool);
        EnsureParam(controller, "Land",      AnimatorControllerParameterType.Trigger);

        // --- Clean up old FallingIdle / Landing states ---
        foreach (var cs in sm.states)
            if (cs.state.name == "FallingIdle" || cs.state.name == "Landing")
                sm.RemoveState(cs.state);

        // Remove stale AnyState → FallingIdle transitions
        var anyList = new System.Collections.Generic.List<AnimatorStateTransition>(sm.anyStateTransitions);
        anyList.RemoveAll(t => t.destinationState != null && t.destinationState.name == "FallingIdle");
        sm.anyStateTransitions = anyList.ToArray();

        // --- Find Locomotion ---
        AnimatorState locomotion = null;
        foreach (var cs in sm.states)
            if (cs.state.name == "Locomotion") { locomotion = cs.state; break; }
        if (locomotion == null) { Debug.LogError("[SetupFalling] Locomotion state not found"); return; }

        // --- Add states ---
        var fallingIdle = sm.AddState("FallingIdle", new Vector3(560, 580, 0));
        fallingIdle.motion = fallingIdleClip;

        var landing = sm.AddState("Landing", new Vector3(820, 580, 0));
        landing.motion = landingClip;

        // --- FallingIdle → Landing  (Land trigger — altar landing path) ---
        var t1 = fallingIdle.AddTransition(landing);
        t1.AddCondition(AnimatorConditionMode.If, 0, "Land");
        t1.hasExitTime = false;
        t1.duration    = 0.1f;

        // --- FallingIdle → Locomotion  (IsFalling=false, instant — asylum escape) ---
        var t2 = fallingIdle.AddTransition(locomotion);
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFalling");
        t2.hasExitTime = false;
        t2.duration    = 0f;

        // --- Landing → Locomotion  (exit time) ---
        var t3 = landing.AddTransition(locomotion);
        t3.hasExitTime = true;
        t3.exitTime    = 0.9f;
        t3.duration    = 0.2f;

        // --- Make FallingIdle the default (Entry) state ---
        sm.defaultState = fallingIdle;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupFalling] Done — FallingIdle is now the Entry state. Land trigger drives FallingIdle→Landing.");
    }

    static void EnsureParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters)
            if (p.name == name) return;
        ctrl.AddParameter(name, type);
    }

    static AnimationClip FindClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                return clip;
        return null;
    }
}
