using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
using AMU.Editor.Core.Api;

namespace AMU.Editor.VrcAssetManager.Helper
{
    public static class ZipFileUtility
    {
        private static Dictionary<string, string> _tempExtractionDirs = new Dictionary<string, string>();

        public static bool IsZipFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".zip";
        }

        public static List<string> GetZipFileList(string zipFilePath)
        {
            var fileList = new List<string>();

            try
            {
                string fullPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullPath) || !IsZipFile(fullPath))
                {
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_zipFileNotFoundOrInvalid"), fullPath));
                    return fileList;
                }

                string tempDir = ExtractZipToTemp(fullPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToGetTempExtractionDir"), fullPath));
                    return fileList;
                }

                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(tempDir, file);
                    fileList.Add(relativePath.Replace('\\', '/'));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToReadZipFile"), zipFilePath, ex.Message));
            }

            return fileList;
        }

        public static bool ExtractFileFromZip(string zipFilePath, string entryPath, string outputPath)
        {
            try
            {
                string fullZipPath = GetFullPath(zipFilePath);
                if (!File.Exists(fullZipPath) || !IsZipFile(fullZipPath))
                {
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_zipFileNotFoundOrInvalid"), fullZipPath));
                    return false;
                }

                string tempDir = GetTempExtractionDir(fullZipPath);
                if (string.IsNullOrEmpty(tempDir))
                {
                    Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToGetTempExtractionDir"), fullZipPath));
                    return false;
                }

                string normalizedEntryPath = entryPath.Replace('/', Path.DirectorySeparatorChar);
                string sourceFile = Path.Combine(tempDir, normalizedEntryPath);

                if (!File.Exists(sourceFile))
                {
                    string fileName = Path.GetFileName(entryPath);
                    var foundFiles = Directory.GetFiles(tempDir, fileName, SearchOption.AllDirectories);

                    if (foundFiles.Length > 0)
                    {
                        sourceFile = foundFiles[0];
                        Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_foundFileAtAlternativePath") + $": {sourceFile}");
                    }
                    else
                    {
                        Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_sourceFileNotFoundInTemp"), sourceFile));
                        return false;
                    }
                }

                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.Copy(sourceFile, outputPath, true);
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_successfullyExtractedFile"), entryPath, outputPath));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToExtractFile"), entryPath, zipFilePath, ex.Message));
                return false;
            }
        }

        public static string GetUnzipDirectory()
        {
            string coreDir = GetCoreDirectory();
            string unzipDir = Path.Combine(coreDir, "VrcAssetManager", "Unzip");

            if (!Directory.Exists(unzipDir))
            {
                Directory.CreateDirectory(unzipDir);
            }

            return unzipDir;
        }

        public static string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            string coreDir = GetCoreDirectory();
            return Path.Combine(coreDir, relativePath);
        }

        private static string GetCoreDirectory()
        {
            return SettingAPI.GetSetting<string>("Core_dirPath");
        }

        private static string ExtractZipToTemp(string zipFilePath)
        {
            try
            {
                if (_tempExtractionDirs.TryGetValue(zipFilePath, out string existingTempDir))
                {
                    if (Directory.Exists(existingTempDir))
                    {
                        Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_usingExistingTempDirectory") + $": {existingTempDir}");
                        return existingTempDir;
                    }
                    else
                    {
                        _tempExtractionDirs.Remove(zipFilePath);
                    }
                }

                string tempDir = Path.Combine(Path.GetTempPath(), "VrcAssetManager_ZipExtract", Path.GetRandomFileName());
                Directory.CreateDirectory(tempDir);
                Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_createdTempDirectory") + $": {tempDir}");

                int extractedCount = 0;

                using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false, Encoding.GetEncoding("Shift_JIS")))
                    {
                        Debug.Log(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_archiveContainsEntries") + $": {archive.Entries.Count}");

                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                continue;
                            }

                            try
                            {
                                string entryPath = Path.Combine(tempDir, entry.FullName);
                                string entryDir = Path.GetDirectoryName(entryPath);

                                if (!Directory.Exists(entryDir))
                                {
                                    Directory.CreateDirectory(entryDir);
                                }

                                entry.ExtractToFile(entryPath, true);
                                extractedCount++;
                                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_extracted"), entry.FullName, entryPath));
                            }
                            catch (Exception entryEx)
                            {
                                Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToExtractEntry"), entry.FullName, entryEx.Message));
                            }
                        }
                    }
                }
                Debug.Log(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_successfullyExtractedFiles"), extractedCount));
                _tempExtractionDirs[zipFilePath] = tempDir;
                return tempDir;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToExtractZipToTemp"), ex.Message));
                return null;
            }
        }

        private static string GetTempExtractionDir(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                if (Directory.Exists(tempDir))
                {
                    return tempDir;
                }
                else
                {
                    _tempExtractionDirs.Remove(zipFilePath);
                }
            }

            return ExtractZipToTemp(zipFilePath);
        }

        public static void CleanupTempExtraction(string zipFilePath)
        {
            if (_tempExtractionDirs.TryGetValue(zipFilePath, out string tempDir))
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(string.Format(LocalizationAPI.GetText("VrcAssetManager_message_zipFileUtility_failedToCleanupTempDirectory"), tempDir, ex.Message));
                }
                _tempExtractionDirs.Remove(zipFilePath);
            }
        }

        public static void CleanupAllTempExtractions()
        {
            foreach (var kvp in _tempExtractionDirs.Keys.ToList())
            {
                CleanupTempExtraction(kvp);
            }
        }
    }
}
