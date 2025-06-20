using VRC.SDKBase.Editor.BuildPipeline;

using AMU.Editor.Core.Api;

namespace AMU.Editor.AutoVariant.Services
{
    public class PrebuildService : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (!AvatarValidationService.ValidateAvatarCount())
            {
                return false;
            }

            if (SettingsAPI.GetSetting<bool>("AutoVariant_enablePrebuild"))
            {
                MaterialOptimizationService.OptimizeActiveAvatars();
            }

            return true;
        }
    }
}
