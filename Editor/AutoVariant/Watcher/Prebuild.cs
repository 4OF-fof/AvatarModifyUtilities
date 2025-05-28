using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;
using Untitled.Editor.Core.Helper;

public class MyPreBuildProcess : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => 0;
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        OptimizeMaterials();
        return true;
    }

    private void OptimizeMaterials()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int optimizedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (PipelineManagerHelper.isVRCAvatar(obj))
            {
                MaterialVariantOptimizer.OptimizeMaterials(obj);
                Debug.Log($"[MyPreBuildProcess] Optimized materials for VRC Avatar: {obj.name}");
            }
        }
    }
}