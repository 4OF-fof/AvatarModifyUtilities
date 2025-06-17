using VRC.SDKBase.Editor.BuildPipeline;

using AMU.Editor.Core.Controllers;

namespace AMU.Editor.AutoVariant.Services
{
    /// <summary>
    /// VRCSDKビルド前処理サービス
    /// VRCSDKのビルドプロセスに統合される前処理を管理
    /// </summary>
    public class PrebuildService : IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;

        /// <summary>
        /// VRCSDKビルドが要求された際に呼び出される
        /// </summary>
        /// <param name="requestedBuildType">要求されたビルドタイプ</param>
        /// <returns>ビルドを続行するかどうか</returns>
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            // アバター数の検証
            if (!AvatarValidationService.ValidateAvatarCount())
            {
                return false;
            }

            // 最適化処理の実行
            if (SettingsController.GetSetting<bool>("AutoVariant_enablePrebuild", true))
            {
                MaterialOptimizationService.OptimizeActiveAvatars();
            }

            return true;
        }
    }
}
