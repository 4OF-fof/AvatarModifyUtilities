# API

## Core API

#### namespace
```csharp
using AMU.Editor.Core.Api;
```

### ObjectCaptureAPI

#### Methods

##### CaptureObject
```csharp
public static Texture2D CaptureObject(
    GameObject targetObject, 
    string savePath, 
    int width = 512, 
    int height = 512
)
```

**Parameters:**
- `targetObject`: GameObject to capture
- `savePath`: File path to save the captured image
- `width`: Width of the captured image (default: 512)
- `height`: Height of the captured image (default: 512)

**Return Value:**
- `Texture2D`: Captured texture. Returns `null` on failure

### VRChatAPI

#### Methods

##### GetBlueprintId
```csharp
public static string GetBlueprintId(GameObject go)
```

**Parameters:**
- `go`: GameObject with PipelineManager component

**Return Value:**
- `string`: Blueprint ID (only if it starts with "avtr"). Returns `null` if not available

##### IsVRCAvatar
```csharp
public static bool IsVRCAvatar(GameObject obj)
```

**Parameters:**
- `obj`: GameObject to check

**Return Value:**
- `bool`: Returns `true` if it's a VRC avatar, `false` otherwise

### LocalFileAPI

#### Methods

##### ScanDirectory
```csharp
public static List<string> ScanDirectory(string directoryPath, bool recursive = true)
```

**Parameters:**
- `directoryPath`: Directory path to scan
- `recursive`: Whether to scan subdirectories recursively (default: true)

**Return Value:**
- `List<string>`: List of file paths found in the directory

### SettingAPI

#### Methods

##### GetSetting
```csharp
public static T GetSetting<T>(string settingName)
```

**Parameters:**
- `settingName`: Setting name

**Return Value:**
- `T`: Setting value

### LocalizationAPI

#### Methods

##### GetText
```csharp
public static string GetText(string key)
```

**Parameters:**
- `key`: Text key

**Return Value:**
- `string`: Corresponding text

##### CurrentLanguage
```csharp
public static string CurrentLanguage { get; }
```

**Return Value:**
- `string`: Current language code