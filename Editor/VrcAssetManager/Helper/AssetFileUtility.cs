using System;
using System.IO;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class AssetFileUtility
    {
        /// <summary>
        /// 指定ファイルをCore_dirPath/サブディレクトリ以下に移動またはコピーし、Core_dirPathからの相対パスを返す。
        /// </summary>
        /// <param name="sourceFilePath">移動元ファイルの絶対パス</param>
        /// <param name="coreDir">Core_dirPathの絶対パス</param>
        /// <param name="subDir">Core_dirPathからのサブディレクトリ（例: "VrcAssetManager/package"）</param>
        /// <param name="move">trueで移動、falseでコピー（デフォルト）</param>
        /// <param name="targetFileName">移動/コピー後のファイル名（省略時は元ファイル名）</param>
        /// <returns>Core_dirPathからの相対パス（スラッシュ区切り）</returns>
        public static string MoveToCoreSubDirectory(string sourceFilePath, string coreDir, string subDir, bool move = false, string targetFileName = null)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                throw new FileNotFoundException($"File not found: {sourceFilePath}");

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
            if (move)
                File.Move(sourceFilePath, destPath);
            else
                File.Copy(sourceFilePath, destPath);
            return destPath.Substring(absCoreDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
        }
    }
}
