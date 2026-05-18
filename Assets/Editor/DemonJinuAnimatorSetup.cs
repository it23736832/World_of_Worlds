using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DemonJinuAnimatorSetup
{
    private const string ModelPath = "Assets/demonjinu01/source/demon-jinu.fbx";
    private const string AnimationFolder = "Assets/demonjinu01/animations";
    private const string PrefabFolder = "Assets/demonjinu01/prefabs";
    private const string ControllerPath = AnimationFolder + "/DemonJinuAnimator.controller";
    private const string PrefabPath = PrefabFolder + "/DemonJinu_Villain.prefab";

    private const string IdleClipPath = "Assets/Abandoned_Asylum/animations/Player@Idle.fbx";
    private const string WalkClipPath = "Assets/Abandoned_Asylum/animations/Player@Walking.fbx";
    private const string RunClipPath = "Assets/Abandoned_Asylum/animations/Player@Fast Run.fbx";

    [InitializeOnLoadMethod]
    private static void SetupOnLoad()
    {
        EditorApplication.delayCall += EnsureSetup;
    }

    [MenuItem("Tools/Characters/Setup Demon Jinu Villain")]
    public static void EnsureSetup()
    {
        ConfigureModelImport();

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogWarning($"Demon Jinu setup skipped. Model not found at {ModelPath}");
            return;
        }

        EnsureFolder(AnimationFolder);
        EnsureFolder(PrefabFolder);

        AnimatorController controller = EnsureAnimatorController();
        EnsurePrefab(model, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureModelImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.importCameras)
        {
            importer.importCameras = false;
            changed = true;
        }

        if (importer.importLights)
        {
            importer.importLights = false;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController EnsureAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            return controller;
        }

        AnimationClip idle = LoadFirstClip(IdleClipPath);
        AnimationClip walk = LoadFirstClip(WalkClipPath);
        AnimationClip run = LoadFirstClip(RunClipPath);

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState locomotionState = root.AddState("Locomotion");
        root.defaultState = locomotionState;

        BlendTree tree = new BlendTree
        {
            name = "DemonJinuLocomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
            minThreshold = 0f,
            maxThreshold = 1f
        };

        AssetDatabase.AddObjectToAsset(tree, ControllerPath);
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 0.5f);
        if (run != null) tree.AddChild(run, 1f);

        locomotionState.motion = tree;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void EnsurePrefab(GameObject model, AnimatorController controller)
    {
        GameObject instance;
        bool prefabAlreadyExists = File.Exists(PrefabPath);

        if (prefabAlreadyExists)
        {
            instance = PrefabUtility.LoadPrefabContents(PrefabPath);
        }
        else
        {
            instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(model);
            }

            instance.name = "DemonJinu_Villain";
        }

        RemoveImportedExtras(instance);

        Animator animator = instance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.avatar = LoadAvatar(ModelPath);
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        if (prefabAlreadyExists)
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
        else
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void RemoveImportedExtras(GameObject root)
    {
        string[] unwantedNames = { "Camera", "Light", "Icosphere" };
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform child = transforms[i];
            if (child == root.transform)
            {
                continue;
            }

            if (HasUnwantedName(child.name, unwantedNames) ||
                child.GetComponent<Camera>() != null ||
                child.GetComponent<Light>() != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static bool HasUnwantedName(string objectName, string[] unwantedNames)
    {
        foreach (string unwantedName in unwantedNames)
        {
            if (objectName.Equals(unwantedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static AnimationClip LoadFirstClip(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            {
                return clip;
            }
        }

        Debug.LogWarning($"Demon Jinu setup could not find an animation clip in {path}");
        return null;
    }

    private static Avatar LoadAvatar(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Avatar avatar)
            {
                return avatar;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
