using System;

using UnityEditor;
using UnityEngine;

using AMU.Editor.VrcAssetManager.Controller;

namespace AMU.Editor.VrcAssetManager.UI.Components
{
    public class ToolbarComponent
    {
        public void Draw(AssetLibraryController _controller)
        {
            Guid assetId = new Guid("680a295b-4dde-427a-aea6-3e2587dd15e3");
            var asset = _controller.GetAsset(assetId);
            GUILayout.Label(asset.Metadata.Name, EditorStyles.boldLabel);
        }
    }
}
