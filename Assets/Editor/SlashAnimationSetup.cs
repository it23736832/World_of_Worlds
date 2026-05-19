using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class SlashAnimationSetup
{
    const string FbxA = "Assets/Animations/Sword And Shield Slash.fbx";
    const string FbxB = "Assets/Animations/Stable Sword Outward Slash.fbx";

    [MenuItem("Tools/World of Worlds/Setup Slash Animation")]
    static void Setup()
    {
        // Always reimport both FBXes to pick up any meta-file changes, then wire up on next frame
        FixRigType(FbxA);
        FixRigType(FbxB);
        Debug.Log("[SlashSetup] FBXes reimported — wiring up controller on next frame...");
        EditorApplication.delayCall += SetupController;
    }

    static void SetupController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/PlayerAnimator.controller");
        if (controller == null) { Debug.LogError("[SlashSetup] PlayerAnimator.controller not found"); return; }

        AnimationClip slashClip = LoadClipFromFbx(FbxA) ?? LoadClipFromFbx(FbxB);
        if (slashClip == null) { Debug.LogError("[SlashSetup] No slash clip found — check FBX rig type"); return; }

        // Add Slash trigger if missing
        bool hasParam = false;
        foreach (var p in controller.parameters)
            if (p.name == "Slash") { hasParam = true; break; }
        if (!hasParam)
            controller.AddParameter("Slash", AnimatorControllerParameterType.Trigger);

        // Remove old Slash state from Base Layer
        var baseSM = controller.layers[0].stateMachine;
        foreach (var s in baseSM.states)
            if (s.state.name == "Slash") { baseSM.RemoveState(s.state); break; }
        foreach (var t in baseSM.anyStateTransitions)
            if (t.destinationState != null && t.destinationState.name == "Slash")
                baseSM.RemoveAnyStateTransition(t);

        // Create / load upper-body AvatarMask
        const string maskPath = "Assets/UpperBodyMask.mask";
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            mask.name = "UpperBodyMask";
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root,         false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body,         true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head,         true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg,      false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg,     false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm,      true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm,     true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers,  true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK,   false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK,  false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK,   false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK,  false);
            AssetDatabase.CreateAsset(mask, maskPath);
        }

        // Remove existing Attack layer then rebuild
        for (int i = controller.layers.Length - 1; i >= 0; i--)
            if (controller.layers[i].name == "Attack") { controller.RemoveLayer(i); break; }

        var attackSM = new AnimatorStateMachine();
        attackSM.name      = "Attack";
        attackSM.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(attackSM, controller);

        var emptyState = attackSM.AddState("Empty", new Vector3(200, 120, 0));
        emptyState.motion            = null;
        emptyState.writeDefaultValues = true;
        attackSM.defaultState        = emptyState;

        var slashState = attackSM.AddState("Slash", new Vector3(450, 120, 0));
        slashState.motion            = slashClip;
        slashState.writeDefaultValues = true;

        var anyToSlash = attackSM.AddAnyStateTransition(slashState);
        anyToSlash.AddCondition(AnimatorConditionMode.If, 0, "Slash");
        anyToSlash.duration            = 0.05f;
        anyToSlash.hasFixedDuration    = false;
        anyToSlash.canTransitionToSelf = false;

        var slashToEmpty = slashState.AddTransition(emptyState);
        slashToEmpty.hasExitTime      = true;
        slashToEmpty.exitTime         = 0.9f;
        slashToEmpty.duration         = 0.1f;
        slashToEmpty.hasFixedDuration = false;

        controller.AddLayer(new AnimatorControllerLayer
        {
            name          = "Attack",
            stateMachine  = attackSM,
            avatarMask    = mask,
            blendingMode  = AnimatorLayerBlendingMode.Override,
            defaultWeight = 0f,
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SlashSetup] Done — Attack layer wired with clip: {slashClip.name}");
    }

    static AnimationClip LoadClipFromFbx(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Debug.Log($"[SlashSetup] Scanning '{path}' — {assets.Length} sub-assets:");
        foreach (var a in assets)
            Debug.Log($"   type={(a != null ? a.GetType().Name : "NULL")}  name={(a != null ? a.name : "NULL")}");

        foreach (var a in assets)
            if (a is AnimationClip c && !c.name.Contains("__preview__"))
                return c;
        return null;
    }

    static void FixRigType(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogWarning($"[SlashSetup] No ModelImporter for: {path}"); return; }

        // Humanoid with empty HumanDescription → Unity auto-maps mixamorig: bones,
        // producing a retargetable clip that plays on any Humanoid avatar (e.g. Rumi).
        importer.animationType    = ModelImporterAnimationType.Human;
        importer.avatarSetup      = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.humanDescription = new HumanDescription
        {
            human    = new HumanBone[0],
            skeleton = new SkeletonBone[0],
        };
        importer.importAnimation  = true;

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[SlashSetup] Reimported as Humanoid (auto-map): {path}");
    }
}
