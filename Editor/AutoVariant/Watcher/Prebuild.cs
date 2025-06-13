using VRC.SDKBase.Editor.BuildPipeline;

namespace AMU.Editor.AutoVariant.Watcher
{
    public class MyPreBuildProcess : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (!AvatarValidator.ValidateAvatarCount())
            {
                return false;
            }

            if (PrebuildSettings.IsOptimizationEnabled)
            {
                MaterialOptimizationManager.OptimizeActiveAvatars();
            }

            return true;
        }
    }
}