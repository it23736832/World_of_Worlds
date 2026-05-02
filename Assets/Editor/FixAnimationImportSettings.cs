using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FixAnimationImportSettings
{
    static FixAnimationImportSettings()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        string[] animationPaths = new string[]
        {
            "Assets/Abandoned_Asylum/animations/Player@Idle.fbx",
            "Assets/Abandoned_Asylum/animations/Player@Walking.fbx",
            "Assets/Abandoned_Asylum/animations/Player@Fast Run.fbx"
        };

        bool changedAny = false;

        foreach (string path in animationPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    bool changed = false;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        var clip = clips[i];
                        
                        // We want Loop Time enabled
                        if (!clip.loopTime)
                        {
                            clip.loopTime = true;
                            changed = true;
                        }

                        // For XZ, we DO NOT want it baked into the pose if it's a moving animation, 
                        // so that the root motion is extracted and then safely ignored by the Animator component.
                        if (clip.lockRootPositionXZ)
                        {
                            clip.lockRootPositionXZ = false; // UNCHECK Bake into Pose XZ
                            changed = true;
                        }

                        // Y position should be baked
                        if (!clip.lockRootHeightY)
                        {
                            clip.keepOriginalPositionY = true;
                            clip.lockRootHeightY = true; // CHECK Bake into Pose Y
                            changed = true;
                        }

                        // Rotation should be baked
                        if (!clip.lockRootRotation)
                        {
                            clip.keepOriginalOrientation = true;
                            clip.lockRootRotation = true; // CHECK Bake into Pose Rotation
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        importer.clipAnimations = clips;
                        importer.SaveAndReimport();
                        changedAny = true;
                        Debug.Log($"Fixed animation import settings for {path}");
                    }
                }
            }
        }

        if (changedAny)
        {
            Debug.Log("Successfully fixed player animation looping issues!");
        }
    }
}
