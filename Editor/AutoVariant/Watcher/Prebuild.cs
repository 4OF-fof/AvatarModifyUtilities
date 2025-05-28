using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

public class MyPreBuildProcess : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => 0;
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        Debug.Log("ビルド前処理");
        return true;
    }
}