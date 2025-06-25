using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class LocalizeChecker : EditorWindow
{
    private string folderPath = "";
    private Vector2 scrollPos;
    private List<string> foundKeys = new List<string>();
    private Dictionary<string, string> langJsonContents = new Dictionary<string, string>();
    private bool searched = false;
    private Dictionary<string, string> keyEditBuffer = new Dictionary<string, string>(); // 追加: 編集用バッファ

    [MenuItem("Dev/Localize Checker")]
    public static void ShowWindow()
    {
        var window = GetWindow<LocalizeChecker>("Localize Checker");
        window.minSize = new Vector2(900, 600); // ウィンドウの最小サイズを拡大
    }

    private void OnGUI()
    {
        GUILayout.Label("LocalizationAPI.GetText キー抽出", EditorStyles.boldLabel);
        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("対象フォルダ", folderPath);
        if (GUILayout.Button("選択", GUILayout.Width(60)))
        {
            // デフォルトパスをEditor以下に変更
            string defaultPath = Path.Combine(Application.dataPath, "AvatarModifyUtilities/Editor");
            string selected = EditorUtility.OpenFolderPanel("フォルダを選択", defaultPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                folderPath = selected;
                searched = false;
                foundKeys.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);
        if (GUILayout.Button("検索", GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                foundKeys = FindLocalizationKeys(folderPath);
                langJsonContents = FindLangJsonFiles(folderPath);
                searched = true;
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "有効なフォルダを選択してください。", "OK");
            }
        }

        GUILayout.Space(16);
        if (searched)
        {
            GUILayout.Label($"検出キー数: {foundKeys.Count}", EditorStyles.label);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("キー", GUILayout.Width(300)); // キー列を拡大
            var jsonFileNames = new List<string>();
            foreach (var kv in langJsonContents)
            {
                string fileName = Path.GetFileName(kv.Key);
                jsonFileNames.Add(fileName);
                EditorGUILayout.LabelField(fileName, GUILayout.Width(300)); // 各json列も拡大
            }
            EditorGUILayout.EndHorizontal();

            // 各キーごとに横並びで値を表示
            foreach (var key in foundKeys)
            {
                EditorGUILayout.BeginHorizontal();
                // 編集用TextField
                if (!keyEditBuffer.ContainsKey(key)) keyEditBuffer[key] = key;
                keyEditBuffer[key] = EditorGUILayout.TextField(keyEditBuffer[key], GUILayout.Width(220));
                if (GUILayout.Button("置換", GUILayout.Width(60)))
                {
                    string newKey = keyEditBuffer[key];
                    if (!string.IsNullOrEmpty(newKey) && newKey != key)
                    {
                        ReplaceKeyInAllJsonAndCs(key, newKey, folderPath);
                        // 再検索して画面を更新
                        foundKeys = FindLocalizationKeys(folderPath);
                        langJsonContents = FindLangJsonFiles(folderPath);
                        keyEditBuffer.Clear();
                        GUI.FocusControl(null);
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.TextField(key, GUILayout.Width(80)); // 旧キー表示（参考用）
                foreach (var kv in langJsonContents)
                {
                    string value = "";
                    try
                    {
                        // jsonから値を抽出
                        var match = Regex.Match(kv.Value, $@"""{Regex.Escape(key)}""\s*:\s*""(.*?)""");
                        if (match.Success && match.Groups.Count > 1)
                        {
                            value = match.Groups[1].Value;
                        }
                    }
                    catch { }
                    EditorGUILayout.TextField(value, GUILayout.Width(300)); // 各json列も拡大
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(16);
            // jsonにのみ存在するキーを下部に表示
            var allJsonKeys = new HashSet<string>();
            foreach (var kv in langJsonContents)
            {
                try
                {
                    // "key": "value" のkey部分をすべて抽出
                    var matches = Regex.Matches(kv.Value, @"""(.*?)""\s*:\s*""");
                    foreach (Match m in matches)
                    {
                        if (m.Groups.Count > 1)
                        {
                            allJsonKeys.Add(m.Groups[1].Value);
                        }
                    }
                }
                catch { }
            }
            var onlyInJson = new List<string>();
            foreach (var k in allJsonKeys)
            {
                if (!foundKeys.Contains(k)) onlyInJson.Add(k);
            }
            if (onlyInJson.Count > 0)
            {
                GUILayout.Label("jsonにのみ存在するキー", EditorStyles.boldLabel);
                if (GUILayout.Button("一括削除", GUILayout.Width(100)))
                {
                    RemoveKeysFromAllJson(onlyInJson);
                    // 再検索して画面を更新
                    foundKeys = FindLocalizationKeys(folderPath);
                    langJsonContents = FindLangJsonFiles(folderPath);
                    GUI.FocusControl(null);
                    GUIUtility.ExitGUI(); // レイアウトエラー防止
                }
                foreach (var k in onlyInJson)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.TextField(k);
                    if (GUILayout.Button("削除", GUILayout.Width(60)))
                    {
                        RemoveKeyFromAllJson(k);
                        // 再検索して画面を更新
                        foundKeys = FindLocalizationKeys(folderPath);
                        langJsonContents = FindLangJsonFiles(folderPath);
                        GUI.FocusControl(null); // フォーカスを外して即時反映
                        GUIUtility.ExitGUI(); // レイアウトエラー防止
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private List<string> FindLocalizationKeys(string rootFolder)
    {
        var result = new HashSet<string>();
        var files = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        // LocalizationAPI.GetText("...") と LocalizationController.GetText("...") の両方を対象に
        var regex = new Regex(@"(LocalizationAPI|LocalizationController)\.GetText\s*\(\s*\""(.*?)\""", RegexOptions.Compiled);
        foreach (var file in files)
        {
            string text = File.ReadAllText(file);
            foreach (Match m in regex.Matches(text))
            {
                if (m.Groups.Count > 2)
                {
                    result.Add(m.Groups[2].Value);
                }
            }
            // Setting.cs内の文字列リテラルも追加
            if (Path.GetFileName(file).Equals("Setting.cs", System.StringComparison.OrdinalIgnoreCase))
            {
                // "..." のみを抽出
                var strRegex = new Regex(@"\""(.*?)\""", RegexOptions.Compiled);
                foreach (Match m in strRegex.Matches(text))
                {
                    if (m.Groups.Count > 1 && !string.IsNullOrEmpty(m.Groups[1].Value))
                    {
                        result.Add(m.Groups[1].Value);
                    }
                }
            }
        }
        return new List<string>(result);
    }

    private Dictionary<string, string> FindLangJsonFiles(string rootFolder)
    {
        var result = new Dictionary<string, string>();
        var dirs = Directory.GetDirectories(rootFolder, "Data", SearchOption.AllDirectories);
        foreach (var dir in dirs)
        {
            var langDir = Path.Combine(dir, "lang");
            if (Directory.Exists(langDir))
            {
                var jsonFiles = Directory.GetFiles(langDir, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        result.Add(file, content);
                    }
                    catch { }
                }
            }
        }
        return result;
    }

    // jsonから指定キーを削除しファイルに保存
    private void RemoveKeyFromAllJson(string key)
    {
        foreach (var kv in langJsonContents)
        {
            try
            {
                // "key": "value", の形を削除（末尾や1件のみも考慮）
                string pattern = $@"[\r\n\s]*""{Regex.Escape(key)}""\s*:\s*"".*?""\s*,?";
                string newContent = Regex.Replace(kv.Value, pattern, "", RegexOptions.Multiline);
                // 先頭や末尾のカンマや余分な改行も整理
                newContent = Regex.Replace(newContent, @",\s*([\}\]])", "$1");
                newContent = Regex.Replace(newContent, @"([\{\[])[\s,]*", "$1");
                File.WriteAllText(kv.Key, newContent);
            }
            catch { }
        }
    }

    // 複数キーをまとめて削除
    private void RemoveKeysFromAllJson(List<string> keys)
    {
        foreach (var kv in langJsonContents)
        {
            try
            {
                string newContent = kv.Value;
                foreach (var key in keys)
                {
                    string pattern = $@"[\r\n\s]*""{Regex.Escape(key)}""\s*:\s*"".*?""\s*,?";
                    newContent = Regex.Replace(newContent, pattern, "", RegexOptions.Multiline);
                }
                newContent = Regex.Replace(newContent, @",\s*([\}\]])", "$1");
                newContent = Regex.Replace(newContent, @"([\{\[])[\s,]*", "$1");
                File.WriteAllText(kv.Key, newContent);
            }
            catch { }
        }
    }

    // --- 追加: キー名一括置換 ---
    private void ReplaceKeyInAllJsonAndCs(string oldKey, string newKey, string rootFolder)
    {
        // 1. jsonファイル内のキー名を置換
        foreach (var kv in langJsonContents)
        {
            try
            {
                // "oldKey": "value" → "newKey": "value"
                string pattern = $@"""{Regex.Escape(oldKey)}""\s*:";
                string replaced = Regex.Replace(kv.Value, pattern, $"\"{newKey}\":", RegexOptions.Multiline);
                File.WriteAllText(kv.Key, replaced);
            }
            catch { }
        }
        // 2. csファイル内のキー名を置換
        var files = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            try
            {
                string text = File.ReadAllText(file);
                // LocalizationAPI.GetText("oldKey") と LocalizationController.GetText("oldKey") の両方を置換
                string patternApi = $@"(LocalizationAPI\.GetText\s*\(\s*)""{Regex.Escape(oldKey)}""";
                string patternCtrl = $@"(LocalizationController\.GetText\s*\(\s*)""{Regex.Escape(oldKey)}""";
                string replaced = Regex.Replace(text, patternApi, $"$1\"{newKey}\"", RegexOptions.Multiline);
                replaced = Regex.Replace(replaced, patternCtrl, $"$1\"{newKey}\"", RegexOptions.Multiline);
                // Setting.cs内のリテラルも置換
                if (Path.GetFileName(file).Equals("Setting.cs", System.StringComparison.OrdinalIgnoreCase))
                {
                    string literalPattern = $@"""{Regex.Escape(oldKey)}""";
                    replaced = Regex.Replace(replaced, literalPattern, $"\"{newKey}\"", RegexOptions.Multiline);
                }
                File.WriteAllText(file, replaced);
            }
            catch { }
        }
    }
}
