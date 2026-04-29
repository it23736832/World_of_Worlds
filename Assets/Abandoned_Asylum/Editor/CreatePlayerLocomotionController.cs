#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class CreatePlayerLocomotionController
{
    [MenuItem("Tools/Abandoned Asylum/Create Player Locomotion Controller")]
    public static void Create()
    {
        const string folder = "Assets/Abandoned_Asylum/Animations";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Abandoned_Asylum", "Animations");
        }

        string controllerPath = folder + "/PlayerLocomotion.controller";

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine root = controller.layers[0].stateMachine;

        AnimatorState locomotionState = root.AddState("Locomotion");
        BlendTree locomotionTree;
        locomotionState.motion = CreateBlendTree(controller, "LocomotionBlendTree", out locomotionTree);
        locomotionTree.blendParameter = "Speed";
        locomotionTree.blendType = BlendTreeType.Simple1D;

        AnimatorState jumpState = root.AddState("Jump");

        AnimationClip[] clips = GetCandidateClips();
        AnimationClip idleClip = FindClip(clips, new[] { "idle" });
        AnimationClip walkClip = FindClip(clips, new[] { "walk", "walking" });
        AnimationClip runClip = FindClip(clips, new[] { "run", "running", "sprint", "fast run", "jog" });
        AnimationClip jumpClip = FindClip(clips, new[] { "jump", "jumping", "takeoff", "leap" });

        if (idleClip != null)
        {
            locomotionTree.AddChild(idleClip, 0f);
        }

        if (walkClip != null)
        {
            locomotionTree.AddChild(walkClip, 1f);
        }

        if (runClip != null)
        {
            locomotionTree.AddChild(runClip, 3f);
        }

        if (jumpClip != null)
        {
            jumpState.motion = jumpClip;
        }

        root.defaultState = locomotionState;

        AnimatorStateTransition anyToJump = root.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.05f;

        AnimatorStateTransition jumpToLocomotion = jumpState.AddTransition(locomotionState);
        jumpToLocomotion.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        jumpToLocomotion.hasExitTime = false;
        jumpToLocomotion.duration = 0.1f;

        Animator animator = GetSelectedAnimator();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;

        Debug.Log($"Created Player Locomotion Controller at {controllerPath}");
        LogClipStatus(idleClip, walkClip, runClip, jumpClip);
    }

    private static BlendTree CreateBlendTree(AnimatorController controller, string name, out BlendTree tree)
    {
        tree = new BlendTree
        {
            name = name
        };

        controller.AddMotion(tree);
        return tree;
    }

    private static AnimationClip[] GetCandidateClips()
    {
        AnimationClip[] folderClips = GetClipsFromFolder();

        Animator animator = GetSelectedAnimator();
        if (animator == null)
        {
            return folderClips;
        }

        AnimationClip[] animatorClips = AnimationUtility.GetAnimationClips(animator.gameObject)
            .Where(clip => clip != null)
            .ToArray();

        return folderClips
            .Concat(animatorClips)
            .Where(clip => clip != null)
            .Distinct()
            .ToArray();
    }

    private static AnimationClip[] GetClipsFromFolder()
    {
        string[] searchFolders =
        {
            "Assets/Abandoned_Asylum/Animations",
            "Assets/Abandoned_Asylum/animations"
        };

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", searchFolders);
        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
            .Where(clip => clip != null)
            .Distinct()
            .ToArray();
    }

    private static Animator GetSelectedAnimator()
    {
        if (Selection.activeGameObject == null)
        {
            return null;
        }

        Animator animator = Selection.activeGameObject.GetComponent<Animator>();
        if (animator != null)
        {
            return animator;
        }

        return Selection.activeGameObject.GetComponentInChildren<Animator>();
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string[] keywords)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        string[] loweredKeywords = keywords.Select(keyword => keyword.ToLowerInvariant()).ToArray();

        foreach (AnimationClip clip in clips)
        {
            string name = clip.name.ToLowerInvariant();
            if (name.Contains("tpose") || name.Contains("bind") || name.Contains("preview"))
            {
                continue;
            }

            foreach (string keyword in loweredKeywords)
            {
                if (name.Contains(keyword))
                {
                    return clip;
                }
            }
        }

        return null;
    }

    private static void LogClipStatus(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip jump)
    {
        if (idle == null || walk == null || run == null || jump == null)
        {
            Debug.LogWarning("Some clips were not auto-found. Assign missing clips in the Blend Tree manually.");
        }

        Debug.Log($"Idle: {(idle != null ? idle.name : "(missing)")}, Walk: {(walk != null ? walk.name : "(missing)")}, Run: {(run != null ? run.name : "(missing)")}, Jump: {(jump != null ? jump.name : "(missing)")}");
    }
}
#endif
