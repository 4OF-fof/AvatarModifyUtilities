using UnityEngine;
using UnityEditor;
using AMU.Data.TagType;
using AMU.Data.Setting;
using System.Linq;

namespace AMU.Editor.Initializer
{
    /// <summary>
    /// AMUの初期化処理を行うクラス
    /// </summary>
    [InitializeOnLoad]
    public static class AMUInitializer
    {
        static AMUInitializer()
        {
            // エディター起動時の初期化
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            // EditorPrefsの初期化
            InitializeEditorPrefs();

            // TagTypeManagerの初期化
            InitializeTagTypeManager();
        }

        /// <summary>
        /// EditorPrefsが設定されていない場合に初期値を設定します
        /// </summary>
        private static void InitializeEditorPrefs()
        {
            try
            {
                // 全ての設定項目を取得
                var allSettingItems = GetAllSettingItems();

                foreach (var category in allSettingItems)
                {
                    foreach (var item in category.Value)
                    {
                        string key = $"Setting.{item.Name}";

                        // EditorPrefsに値が存在しない場合のみ初期値を設定
                        if (!EditorPrefs.HasKey(key))
                        {
                            SetDefaultValue(item, key);
                        }
                    }
                }

                Debug.Log("[AMU] EditorPrefs initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AMU] EditorPrefs initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 全ての設定項目を取得します
        /// </summary>
        private static System.Collections.Generic.Dictionary<string, SettingItem[]> GetAllSettingItems()
        {
            var dictList = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, SettingItem[]>>();
            var asm = typeof(AMUInitializer).Assembly;
            var types = asm.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Namespace == "AMU.Data.Setting");

            foreach (var type in types)
            {
                var field = type.GetField("SettingItems", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.FieldType == typeof(System.Collections.Generic.Dictionary<string, SettingItem[]>))
                {
                    var dict = field.GetValue(null) as System.Collections.Generic.Dictionary<string, SettingItem[]>;
                    if (dict != null) dictList.Add(dict);
                }
            }

            return dictList.SelectMany(d => d).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// 指定された設定項目の初期値をEditorPrefsに設定します
        /// </summary>
        private static void SetDefaultValue(SettingItem item, string key)
        {
            switch (item.Type)
            {
                case SettingType.String:
                    var stringItem = (StringSettingItem)item;
                    EditorPrefs.SetString(key, stringItem.DefaultValue);
                    break;
                case SettingType.Int:
                    var intItem = (IntSettingItem)item;
                    EditorPrefs.SetInt(key, intItem.DefaultValue);
                    break;
                case SettingType.Bool:
                    var boolItem = (BoolSettingItem)item;
                    EditorPrefs.SetBool(key, boolItem.DefaultValue);
                    break;
                case SettingType.Float:
                    var floatItem = (FloatSettingItem)item;
                    EditorPrefs.SetFloat(key, floatItem.DefaultValue);
                    break;
                case SettingType.Choice:
                    var choiceItem = (ChoiceSettingItem)item;
                    EditorPrefs.SetString(key, choiceItem.DefaultValue);
                    break;
                case SettingType.FilePath:
                    var fileItem = (FilePathSettingItem)item;
                    EditorPrefs.SetString(key, fileItem.DefaultValue);
                    break;
                case SettingType.TextArea:
                    var textAreaItem = (TextAreaSettingItem)item;
                    EditorPrefs.SetString(key, textAreaItem.DefaultValue);
                    break;
            }
        }

        private static void InitializeTagTypeManager()
        {
            try
            {
                // TagTypeManagerのデータを読み込み
                TagTypeManager.LoadData();
                Debug.Log("[AMU] TagTypeManager initialized successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AMU] TagTypeManager initialization failed: {ex.Message}");
            }
        }
    }
}
