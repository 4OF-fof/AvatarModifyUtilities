using System;
using System.IO;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class AssetFileUtility
    {
        public static string MoveToCoreSubDirectory(string sourceFilePath, string subDir, string targetFileName = null)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                throw new FileNotFoundException($"File not found: {sourceFilePath}");

            string coreDir = AMU.Editor.Core.Api.SettingAPI.GetSetting<string>("Core_dirPath");

            string absCoreDir = Path.GetFullPath(coreDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string absSource = Path.GetFullPath(sourceFilePath);
            if (absSource.StartsWith(absCoreDir, StringComparison.OrdinalIgnoreCase))
            {
                return absSource.Substring(absCoreDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
            }
            string targetDir = Path.Combine(absCoreDir, subDir);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            string fileName = string.IsNullOrEmpty(targetFileName) ? Path.GetFileName(sourceFilePath) : targetFileName;
            string destPath = Path.Combine(targetDir, fileName);
            int count = 1;
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            while (File.Exists(destPath))
            {
                destPath = Path.Combine(targetDir, $"{baseName}_{count}{ext}");
                count++;
            }
            File.Move(sourceFilePath, destPath);
            return destPath.Substring(absCoreDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
        }
    }
}
