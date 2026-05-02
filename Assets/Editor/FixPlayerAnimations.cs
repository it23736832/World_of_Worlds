using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public class FixPlayerAnimations
{
    static FixPlayerAnimations()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        string controllerPath = "Assets/Abandoned_Asylum/animations/PlayerLocomotion.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return;

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        AnimatorState locomotionState = null;
        AnimatorState jumpState = null;

        foreach (var state in rootStateMachine.states)
        {
            if (state.state.name == "Locomotion") locomotionState = state.state;
            if (state.state.name == "Jump") jumpState = state.state;
        }

        bool changed = false;

        if (locomotionState != null && !(locomotionState.motion is BlendTree))
        {
            AnimationClip idleClip = LoadClipFromFBX("Assets/Abandoned_Asylum/animations/Player@Idle.fbx");
            AnimationClip walkClip = LoadClipFromFBX("Assets/Abandoned_Asylum/animations/Player@Walking.fbx");
            AnimationClip runClip = LoadClipFromFBX("Assets/Abandoned_Asylum/animations/Player@Fast Run.fbx");

            if (idleClip != null && walkClip != null && runClip != null)
            {
                BlendTree blendTree = new BlendTree();
                blendTree.name = "LocomotionBlendTree";
                blendTree.blendParameter = "Speed";
                
                // FirstPersonMovement normalizes speed to 0-1
                blendTree.AddChild(idleClip, 0f);
                blendTree.AddChild(walkClip, 0.63f); 
                blendTree.AddChild(runClip, 1f);  

                locomotionState.motion = blendTree;
                AssetDatabase.AddObjectToAsset(blendTree, controller);
                changed = true;
                Debug.Log("Successfully assigned Locomotion BlendTree.");
            }
            else
            {
                Debug.LogError("Could not find Idle/Walk/Run clips in Assets/Abandoned_Asylum/animations/");
            }
        }

        if (jumpState != null && jumpState.motion == null)
        {
            AnimationClip jumpClip = LoadClipFromFBX("Assets/Abandoned_Asylum/animations/Player@Jump.fbx");
            if (jumpClip != null)
            {
                jumpState.motion = jumpClip;
                changed = true;
                Debug.Log("Successfully assigned Jump animation.");
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }

    static AnimationClip LoadClipFromFBX(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip && !asset.name.StartsWith("__preview__"))
            {
                return asset as AnimationClip;
            }
        }
        return null;
    }
}
