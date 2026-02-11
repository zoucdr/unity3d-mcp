# GamePlay 截图功能改进 - 支持非运行时截图

## 问题
原来的截图实现只能在运行时（Play Mode）通过Game窗口的RenderTexture进行截图，在编辑器模式（非运行时）下无法正常工作。

## 解决方案

### 多级截图策略
实现了三种截图方案，按优先级自动选择：

#### 方案1：Game窗口截图（运行时优先）
- **适用场景**：游戏运行时（Play Mode）
- **优点**：直接获取Game窗口渲染内容，最准确
- **实现**：通过反射获取GameView的RenderTexture
- **调用方法**：`CaptureFromRenderTexture()`

#### 方案2：Scene视图截图（编辑器模式）
- **适用场景**：编辑器模式，Scene视图打开时
- **优点**：可以捕获Scene视图中的内容
- **实现**：使用SceneView.lastActiveSceneView的相机进行渲染
- **调用方法**：`CaptureFromSceneView()`
- **特点**：
  - 自动获取当前活动的Scene视图
  - 使用Scene视图相机进行临时渲染
  - 保持视图原有的宽高比

#### 方案3：相机渲染截图（最终备选）
- **适用场景**：场景中有相机对象时
- **优点**：不依赖任何编辑器窗口
- **实现**：查找场景中的主相机或任何相机，手动渲染到RenderTexture
- **调用方法**：`CaptureFromCamera()`
- **特点**：
  - 优先使用Camera.main
  - 找不到主相机时使用FindObjectOfType查找
  - 默认分辨率：1920x1080

## 使用方法

### 基本截图
```json
{
  "action": "screenshot",
  "save_path": "Assets/Screenshots/test.png",
  "format": "PNG"
}
```

### 高级选项
```json
{
  "action": "screenshot",
  "save_path": "Assets/Screenshots/test.png",
  "format": "JPG",
  "quality": 90,
  "scale": 1.5,
  "delay": 1.0
}
```

## 参数说明

| 参数 | 类型 | 说明 | 默认值 |
|------|------|------|--------|
| action | string | 操作类型，固定为"screenshot" | - |
| save_path | string | 保存路径 | Assets/Screenshots/screenshot.png |
| format | string | 图片格式（PNG/JPG） | PNG |
| quality | int | JPG质量（1-100） | 90 |
| scale | float | 缩放比例 | 1.0 |
| delay | float | 延迟执行（秒） | 0 |

## 返回信息

成功时返回：
```json
{
  "success": true,
  "message": "Screenshot saved successfully",
  "data": {
    "path": "Assets/Screenshots/test.png",
    "width": 1920,
    "height": 1080,
    "format": "PNG",
    "size_bytes": 1234567,
    "is_playing": false
  }
}
```

## 工作流程

```
开始截图
    ↓
是否在Play Mode？
    ↓ 是
从Game窗口截图 → 成功？ → 保存
    ↓ 否
从Scene视图截图 → 成功？ → 保存
    ↓ 否
从场景相机截图 → 成功？ → 保存
    ↓ 否
返回错误：无可用截图源
```

## 技术细节

### 1. CaptureFromRenderTexture
```csharp
private Texture2D CaptureFromRenderTexture(RenderTexture renderTexture)
{
    // 直接从RenderTexture读取像素
    var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
    RenderTexture.active = renderTexture;
    texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
    texture.Apply();
    return texture;
}
```

### 2. CaptureFromSceneView
```csharp
private CaptureResult CaptureFromSceneView()
{
    var sceneView = UnityEditor.SceneView.lastActiveSceneView;
    var camera = sceneView.camera;
    
    // 创建临时RenderTexture
    var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
    camera.targetTexture = rt;
    camera.Render();
    
    // 读取并恢复
    texture.ReadPixels(...);
    camera.targetTexture = previousTarget;
    
    return new CaptureResult { texture, width, height };
}
```

### 3. CaptureFromCamera
```csharp
private CaptureResult CaptureFromCamera()
{
    // 查找相机
    Camera camera = Camera.main ?? FindObjectOfType<Camera>();
    
    // 渲染到RenderTexture
    var rt = new RenderTexture(1920, 1080, 24);
    camera.targetTexture = rt;
    camera.Render();
    
    // 读取并恢复
    texture.ReadPixels(...);
    camera.targetTexture = previousTarget;
    
    return new CaptureResult { texture, width, height };
}
```

## 注意事项

1. **Scene视图截图**：需要Scene视图窗口打开
2. **相机截图**：场景中至少需要一个相机对象
3. **纹理翻转**：所有截图都会自动修正上下翻转问题
4. **资源清理**：临时创建的RenderTexture会自动清理
5. **性能考虑**：相机渲染截图会临时改变相机设置，完成后恢复

## 兼容性

- ✅ Unity 2021.3+
- ✅ Windows/Mac/Linux编辑器
- ✅ Play Mode和Edit Mode
- ✅ 所有渲染管线（Built-in/URP/HDRP）

## 示例场景

### 场景1：游戏运行时截图
```
用户: 截图游戏画面
操作: {"action": "screenshot"}
结果: 使用Game窗口截图（方案1）
```

### 场景2：编辑器中截图Scene视图
```
用户: 截图当前场景
操作: {"action": "screenshot"}
结果: Scene视图打开 → 使用Scene视图截图（方案2）
```

### 场景3：无窗口截图
```
用户: 截图
操作: {"action": "screenshot"}
结果: 无Game窗口，无Scene视图 → 使用场景相机截图（方案3）
```

## 常见问题

### Q: 为什么截图是黑屏？
A: 可能原因：
- 场景中没有相机
- Scene视图未打开
- Game窗口未渲染

解决方法：确保至少满足一个条件：
- 运行游戏（Play Mode）
- 打开Scene视图
- 场景中有相机对象

### Q: 截图分辨率太小？
A: 使用`scale`参数放大：
```json
{"action": "screenshot", "scale": 2.0}
```

### Q: 如何截取特定相机的视角？
A: 确保该相机是场景中的主相机（MainCamera标签）或唯一相机

## 未来改进

- [ ] 支持指定相机名称截图
- [ ] 支持多相机分别截图
- [ ] 支持自定义分辨率
- [ ] 支持后处理效果
- [ ] 支持透明背景截图

