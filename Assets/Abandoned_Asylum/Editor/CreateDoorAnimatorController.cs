#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class CreateDoorAnimatorController
{
    [MenuItem("Tools/Abandoned Asylum/Create Door Animator Controller")]
    public static void Create()
    {
        const string folder = "Assets/Abandoned_Asylum/Animations";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Abandoned_Asylum", "Animations");

        string controllerPath = folder + "/DoorAnimator.controller";

        // Delete existing controller so it gets rebuilt cleanly.
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Open",  AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Close", AnimatorControllerParameterType.Trigger);

        AnimationClip openClip  = GetOrCreateDoorClip(folder + "/DoorOpen.anim", 0f, 90f);
        AnimationClip closeClip = GetOrCreateDoorClip(folder + "/DoorClose.anim", 90f, 0f);

        var root  = controller.layers[0].stateMachine;

        // States
        AnimatorState idle  = root.AddState("Idle");
        AnimatorState open  = root.AddState("Open");
        AnimatorState close = root.AddState("Close");

        open.motion  = openClip;
        close.motion = closeClip;

        // Set Idle as the default state.
        root.defaultState = idle;

        // Idle → Open
        var toOpen = idle.AddTransition(open);
        toOpen.AddCondition(AnimatorConditionMode.If, 0f, "Open");
        toOpen.hasExitTime        = false;
        toOpen.duration           = 0f;

        // Open → Idle  (wait for animation to finish)
        var openToIdle = open.AddTransition(idle);
        openToIdle.hasExitTime    = true;
        openToIdle.exitTime       = 1f;
        openToIdle.duration       = 0f;

        // Idle → Close  (in case door starts open via script)
        var toClose = idle.AddTransition(close);
        toClose.AddCondition(AnimatorConditionMode.If, 0f, "Close");
        toClose.hasExitTime       = false;
        toClose.duration          = 0f;

        // Close → Idle
        var closeToIdle = close.AddTransition(idle);
        closeToIdle.hasExitTime   = true;
        closeToIdle.exitTime      = 1f;
        closeToIdle.duration      = 0f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = controller;
        Debug.Log($"Door Animator Controller created at {controllerPath}");
    }

    private static AnimationClip GetOrCreateDoorClip(string path, float startAngle, float endAngle)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip
        {
            name = System.IO.Path.GetFileNameWithoutExtension(path),
            legacy = false,
            frameRate = 30f
        };

        AnimationCurve curve = AnimationCurve.EaseInOut(0f, startAngle, 0.75f, endAngle);
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", curve);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
#endif
