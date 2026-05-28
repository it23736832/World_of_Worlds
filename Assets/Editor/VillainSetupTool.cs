using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UObject = UnityEngine.Object;

public static class VillainSetupTool
{
    private const string MenuPath = "Tools/Villain/Setup Selected Mixamo";

    [MenuItem(MenuPath)]
    public static void SetupSelectedMixamo()
    {
        string[] fbxPaths = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToArray();

        if (fbxPaths.Length == 0)
        {
            Debug.LogError("Select the villain model FBX and its animation FBX files in the Project window.");
            return;
        }

        string modelPath = fbxPaths.FirstOrDefault(path => !Path.GetFileName(path).Contains("@"));
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            modelPath = fbxPaths[0];
        }

        Avatar avatar = FindAvatar(modelPath);
        if (avatar == null)
        {
            Debug.LogError("Could not find an Avatar on the selected model FBX. Import the model as Humanoid first.");
            return;
        }

        foreach (string path in fbxPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            importer.animationType = ResolveHumanoidAnimationType();
            importer.avatarSetup = ResolveCopyFromAvatarSetup();
            importer.sourceAvatar = avatar;
            importer.SaveAndReimport();
        }

        List<AnimationClip> clips = CollectClips(fbxPaths);
        AnimationClip idleClip = FindClip(clips, new[] { "idle", "breathing" });
        AnimationClip runClip = FindClip(clips, new[] { "run", "running", "fast run" });
        AnimationClip attackClip = FindClip(clips, new[] { "attack", "hit", "punch", "slash", "bite" });

        string controllerPath = EditorUtility.SaveFilePanelInProject(
            "Create Villain Animator Controller",
            "Villain",
            "controller",
            "Choose where to save the villain controller");

        if (string.IsNullOrWhiteSpace(controllerPath))
        {
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState runState = stateMachine.AddState("Run");
        AnimatorState attackState = stateMachine.AddState("Attack");

        if (idleClip != null) idleState.motion = idleClip;
        if (runClip != null) runState.motion = runClip;
        if (attackClip != null) attackState.motion = attackClip;

        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;

        AssetDatabase.SaveAssets();

        if (idleClip == null || runClip == null || attackClip == null)
        {
            Debug.LogWarning("Villain controller created, but one or more clips were not found by name. Assign them manually if needed.");
        }
    }

    private static Avatar FindAvatar(string modelPath)
    {
        UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        return assets.OfType<Avatar>().FirstOrDefault();
    }

    private static List<AnimationClip> CollectClips(IEnumerable<string> fbxPaths)
    {
        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (string path in fbxPaths)
        {
            UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UObject asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    clips.Add(clip);
                }
            }
        }

        return clips;
    }

    private static AnimationClip FindClip(IEnumerable<AnimationClip> clips, IEnumerable<string> keywords)
    {
        foreach (string keyword in keywords)
        {
            AnimationClip clip = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(keyword));
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private static ModelImporterAnimationType ResolveHumanoidAnimationType()
    {
        return ResolveEnum<ModelImporterAnimationType>(
            new[] { "Humanoid", "Human" },
            defaultValueName: "Generic",
            fallback: default(ModelImporterAnimationType));
    }

    private static ModelImporterAvatarSetup ResolveCopyFromAvatarSetup()
    {
        return ResolveEnum<ModelImporterAvatarSetup>(
            new[] { "CopyFromOtherAvatar", "CopyFromOther", "CopyFromAvatar" },
            defaultValueName: "CreateFromThisModel",
            fallback: default(ModelImporterAvatarSetup));
    }

    private static TEnum ResolveEnum<TEnum>(IEnumerable<string> preferredNames, string defaultValueName, TEnum fallback)
        where TEnum : struct
    {
        foreach (string name in preferredNames)
        {
            if (Enum.TryParse(name, out TEnum parsed))
            {
                return parsed;
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultValueName) && Enum.TryParse(defaultValueName, out TEnum defaultParsed))
        {
            return defaultParsed;
        }

        return fallback;
    }
}
