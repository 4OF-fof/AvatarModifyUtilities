using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;

public class MyPreBuildProcess : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => 0;
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        OptimizeMaterials();
        return true;
    }

    private void OptimizeMaterials()
    //TODO: アップロードするアバターを自動選択
    {
        var selectedObject = Selection.activeGameObject;
        
        if (selectedObject != null)
        {
            MaterialVariantOptimizer.OptimizeMaterials(selectedObject);
            Debug.Log($"[MyPreBuildProcess] Optimized materials for selected object: {selectedObject.name}");
        }
        else
        {
            Debug.LogWarning("[MyPreBuildProcess] No GameObject selected for material optimization");
        }
    }
}