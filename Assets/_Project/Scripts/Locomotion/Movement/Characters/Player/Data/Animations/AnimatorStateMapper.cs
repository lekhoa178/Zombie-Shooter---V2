#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class AnimatorStateMapper
{
    [MenuItem("Tools/Generate Animator State Mapping")]
    public static void GenerateAnimatorStateMapping()
    {
        // Prompt user to select the animator controller
        string path = EditorUtility.OpenFilePanel("Select Animator Controller", "Assets", "controller");
        if (string.IsNullOrEmpty(path)) return;
        
        // Convert to asset path
        path = FileUtil.GetProjectRelativePath(path);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) return;
        
        // Create the mapping scriptable object
        var mapping = ScriptableObject.CreateInstance<AnimatorStateClipMapping>();
        mapping.Initialize();
        
        // Process all layers in the animator controller
        foreach (var layer in controller.layers)
        {
            ProcessStateMachine(layer.stateMachine, mapping);
        }
        
        // Save the mapping
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save State Mapping",
            controller.name + "Mapping",
            "asset",
            "Save animator state to clip mapping"
        );
        
        if (!string.IsNullOrEmpty(savePath))
        {
            AssetDatabase.CreateAsset(mapping, savePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Saved animator mapping to {savePath}");
        }
    }
    
    private static void ProcessStateMachine(AnimatorStateMachine stateMachine, AnimatorStateClipMapping mapping)
    {
        // Process all states in this state machine
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                mapping.AddMapping(state.state.name, clip.name);
            }
        }
        
        // Process all sub-state machines
        foreach (var childStateMachine in stateMachine.stateMachines)
        {
            ProcessStateMachine(childStateMachine.stateMachine, mapping);
        }
    }
}
#endif