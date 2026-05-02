using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

[InitializeOnLoad]
public class AutoCreateDoorAnimations
{
    static AutoCreateDoorAnimations()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        string flagPath = "Assets/Editor/DoorAnimationsCreated.flag";
        if (File.Exists(flagPath)) return;

        CreateAnimations();

        File.WriteAllText(flagPath, "done");
        AssetDatabase.Refresh();
    }

    public static void CreateAnimations()
    {
        string folderPath = "Assets/Abandoned_Asylum/Animations";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Abandoned_Asylum", "Animations");
        }

        // 1. Create Open Animation Clip
        AnimationClip openClip = new AnimationClip();
        openClip.name = "DoorOpen";
        AnimationCurve curveYOpen = AnimationCurve.EaseInOut(0f, 0f, 1f, 90f);
        openClip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", curveYOpen);
        AssetDatabase.CreateAsset(openClip, folderPath + "/DoorOpen.anim");

        // 2. Create Close Animation Clip
        AnimationClip closeClip = new AnimationClip();
        closeClip.name = "DoorClose";
        AnimationCurve curveYClose = AnimationCurve.EaseInOut(0f, 90f, 1f, 0f);
        closeClip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", curveYClose);
        AssetDatabase.CreateAsset(closeClip, folderPath + "/DoorClose.anim");

        // 3. Create Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(folderPath + "/DoorController.controller");
        
        controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Close", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = rootStateMachine.AddState("Idle");
        AnimatorState openState = rootStateMachine.AddState("Open");
        openState.motion = openClip;
        AnimatorState closeState = rootStateMachine.AddState("Close");
        closeState.motion = closeClip;

        AnimatorStateTransition idleToOpen = idleState.AddTransition(openState);
        idleToOpen.AddCondition(AnimatorConditionMode.If, 0, "Open");
        idleToOpen.hasExitTime = false;

        AnimatorStateTransition openToClose = openState.AddTransition(closeState);
        openToClose.AddCondition(AnimatorConditionMode.If, 0, "Close");
        openToClose.hasExitTime = false;

        AnimatorStateTransition closeToOpen = closeState.AddTransition(openState);
        closeToOpen.AddCondition(AnimatorConditionMode.If, 0, "Open");
        closeToOpen.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Debug.Log("Door Animations and Controller created successfully at " + folderPath);
    }
}
