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
        {
            AssetDatabase.CreateFolder("Assets/Abandoned_Asylum", "Animations");
        }

        string controllerPath = folder + "/DoorAnimator.controller";

        // If controller already exists, just select it
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log($"Door Animator already exists at {controllerPath}");
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Close", AnimatorControllerParameterType.Trigger);

        var root = controller.layers[0].stateMachine;
        var idle = root.AddState("Idle");
        var open = root.AddState("Open");
        var close = root.AddState("Close");

        var tOpen = root.AddAnyStateTransition(open);
        tOpen.AddCondition(AnimatorConditionMode.If, 0f, "Open");
        tOpen.hasExitTime = false;

        var tClose = root.AddAnyStateTransition(close);
        tClose.AddCondition(AnimatorConditionMode.If, 0f, "Close");
        tClose.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;
        Debug.Log($"Created Door Animator Controller at {controllerPath}");
    }
}
#endif
