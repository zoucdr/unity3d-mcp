using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models; // For Response class
using Unity.Mcp;
using Unity.Mcp.Utils;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles Unity Game window management and controls.
    /// 对应方法名: game_view
    /// </summary>
    [ToolName("game_view", "窗口管理")]
    public class GameView : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: set_resolution, get_resolution, get_stats, set_vsync, set_target_framerate, maximize, set_aspect_ratio, screenshot", false),
                new MethodKey("width", "Window width (used for set_resolution)", true),
                new MethodKey("height", "Window height (used for set_resolution)", true),
                new MethodKey("vsync_count", "VSync count: 0=off, 1=every frame, 2=every 2nd frame (used for set_vsync)", true),
                new MethodKey("target_framerate", "Target frame rate, -1=unlimited (used for set_target_framerate)", true),
                new MethodKey("aspect_ratio", "Aspect ratio string like '16:9' or 'Free' (used for set_aspect_ratio)", true),
                // 截图相关参数
                new MethodKey("save_path", "Path to save screenshot", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("set_resolution", HandleSetResolutionAction)
                    .Leaf("get_resolution", HandleGetResolutionAction)
                    .Leaf("get_stats", HandleGetStatsAction)
                    .Leaf("set_vsync", HandleSetVSyncAction)
                    .Leaf("set_target_framerate", HandleSetTargetFramerateAction)
                    .Leaf("maximize", HandleMaximizeAction)
                    .Leaf("set_aspect_ratio", HandleSetAspectRatioAction)
                    .Leaf("screenshot", HandleScreenshotAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理设置分辨率的操作
        /// </summary>
        private object HandleSetResolutionAction(JsonClass args)
        {
            if (args["width"] == null || args["width"].IsNull() ||
                args["height"] == null || args["height"].IsNull())
            {
                return Response.Error("'width' and 'height' parameters are required for set_resolution.");
            }

            int width = args["width"].AsInt;
            int height = args["height"].AsInt;

            if (width <= 0 || height <= 0)
            {
                return Response.Error("'width' and 'height' must be positive integers.");
            }

            Debug.Log($"[GameView] Setting resolution to {width}x{height}");
            return SetGameViewResolution(width, height);
        }

        /// <summary>
        /// 处理获取分辨率的操作
        /// </summary>
        private object HandleGetResolutionAction(JsonClass args)
        {
            Debug.Log("[GameView] Getting current resolution");
            return GetGameViewResolution();
        }

        /// <summary>
        /// 处理获取统计信息的操作
        /// </summary>
        private object HandleGetStatsAction(JsonClass args)
        {
            Debug.Log("[GameView] Getting Game view stats");
            return GetGameViewStats();
        }

        /// <summary>
        /// 处理设置VSync的操作
        /// </summary>
        private object HandleSetVSyncAction(JsonClass args)
        {
            if (args["vsync_count"] == null || args["vsync_count"].IsNull())
            {
                return Response.Error("'vsync_count' parameter is required for set_vsync.");
            }

            int vsyncCount = args["vsync_count"].AsInt;

            Debug.Log($"[GameView] Setting VSync count to {vsyncCount}");
            return SetVSync(vsyncCount);
        }

        /// <summary>
        /// 处理设置目标帧率的操作
        /// </summary>
        private object HandleSetTargetFramerateAction(JsonClass args)
        {
            if (args["target_framerate"] == null || args["target_framerate"].IsNull())
            {
                return Response.Error("'target_framerate' parameter is required for set_target_framerate.");
            }

            int targetFramerate = args["target_framerate"].AsInt;

            Debug.Log($"[GameView] Setting target framerate to {targetFramerate}");
            return SetTargetFramerate(targetFramerate);
        }

        /// <summary>
        /// 处理最大化窗口的操作
        /// </summary>
        private object HandleMaximizeAction(JsonClass args)
        {
            Debug.Log("[GameView] Maximizing Game view");
            return MaximizeGameView();
        }

        /// <summary>
        /// 处理设置宽高比的操作
        /// </summary>
        private object HandleSetAspectRatioAction(JsonClass args)
        {
            string aspectRatio = args["aspect_ratio"]?.Value;
            if (string.IsNullOrEmpty(aspectRatio))
            {
                return Response.Error("'aspect_ratio' parameter is required for set_aspect_ratio.");
            }

            Debug.Log($"[GameView] Setting aspect ratio to {aspectRatio}");
            return SetAspectRatio(aspectRatio);
        }

        // --- Game View Methods ---

        /// <summary>
        /// 设置Game窗口分辨率（从UGUILayout迁移而来，使用完整的GameViewSizes API实现）
        /// </summary>
        private object SetGameViewResolution(int width, int height)
        {
            try
            {
                // 使用反射访问GameView类，因为它不是公开的API
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null)
                {
                    Debug.Log("[GameView] Could not find GameView type");
                    return Response.Error("Could not find GameView type.");
                }

                // 获取当前的GameView窗口
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                {
                    Debug.Log("[GameView] Could not get GameView window");
                    return Response.Error("Could not get GameView window.");
                }

                // 获取GameViewSizes类
                var gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                var gameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
                var gameViewSizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");

                if (gameViewSizesType == null || gameViewSizeType == null || gameViewSizeTypeEnum == null)
                {
                    Debug.Log($"[GameView] Could not find types: GameViewSizes={gameViewSizesType != null}, GameViewSize={gameViewSizeType != null}, GameViewSizeType={gameViewSizeTypeEnum != null}");
                    return Response.Error("Could not find GameView size types.");
                }

                // 获取单例实例 - 使用优化的ScriptableSingleton方式
                object gameViewSizes = null;
                UnityEngine.Debug.Log($"[GameView] >>> SetGameViewResolution starting for {width}x{height} <<<");

                // 使用GetGameViewSizesInstance方法获取实例
                gameViewSizes = GetGameViewSizesInstance(gameViewSizesType);

                if (gameViewSizes == null)
                {
                    Debug.LogError("[GameView] Failed to get GameViewSizes instance");
                    return Response.Error("Could not access GameViewSizes instance. Operation failed.");
                }

                goto ProcessGameViewSizes;

            ProcessGameViewSizes: // 处理通过方法或属性获取的实例

                if (gameViewSizes == null)
                {
                    Debug.Log("[GameView] GameViewSizes instance is null");
                    return Response.Error("GameViewSizes instance is null.");
                }

                // 获取当前平台的游戏视图尺寸组
                int currentGroup = 0; // 默认为Standalone (0)

                // 尝试多种方式获取当前组类型
                var currentGroupMethod = gameViewSizes.GetType().GetMethod("GetCurrentGroupType",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (currentGroupMethod != null)
                {
                    try
                    {
                        currentGroup = (int)currentGroupMethod.Invoke(gameViewSizes, null);
                        Debug.Log($"[GameView] Got current group type via method: {currentGroup}");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[GameView] Failed to invoke GetCurrentGroupType: {ex.Message}");
                    }
                }
                else
                {
                    // 尝试通过属性获取
                    var currentGroupProp = gameViewSizes.GetType().GetProperty("currentGroupType",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (currentGroupProp != null)
                    {
                        try
                        {
                            currentGroup = (int)currentGroupProp.GetValue(gameViewSizes);
                            Debug.Log($"[GameView] Got current group type via property: {currentGroup}");
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameView] Failed to get currentGroupType property: {ex.Message}");
                        }
                    }
                    else
                    {
                        // 根据BuildTarget推断当前平台
                        switch (EditorUserBuildSettings.activeBuildTarget)
                        {
                            case BuildTarget.Android:
                            case BuildTarget.iOS:
                                currentGroup = 4; // GameViewSizeGroupType.Android/iOS
                                break;
                            case BuildTarget.StandaloneWindows:
                            case BuildTarget.StandaloneWindows64:
                            case BuildTarget.StandaloneOSX:
                            case BuildTarget.StandaloneLinux64:
                                currentGroup = 0; // GameViewSizeGroupType.Standalone
                                break;
                            default:
                                currentGroup = 0; // 默认Standalone
                                break;
                        }
                        Debug.Log($"[GameView] Using inferred group type based on BuildTarget: {currentGroup} ({EditorUserBuildSettings.activeBuildTarget})");
                    }
                }

                Debug.Log($"[GameView] Current group type: {currentGroup}");

                var getGroupMethod = gameViewSizes.GetType().GetMethod("GetGroup");
                if (getGroupMethod == null)
                {
                    Debug.Log("[GameView] Could not find 'GetGroup' method");
                    return Response.Error("Could not find GetGroup method.");
                }

                var group = getGroupMethod.Invoke(gameViewSizes, new object[] { currentGroup });
                if (group == null)
                {
                    Debug.Log("[GameView] Group is null");
                    return Response.Error("Failed to get GameView size group.");
                }

                // 创建自定义分辨率
                var freeAspectValue = Enum.GetValues(gameViewSizeTypeEnum).GetValue(1); // GameViewSizeType.FreeAspectRatio
                if (freeAspectValue == null)
                {
                    Debug.Log("[GameView] Could not get FreeAspectRatio enum value");
                    return Response.Error("Could not get GameViewSizeType.FreeAspectRatio.");
                }

                var customSize = Activator.CreateInstance(gameViewSizeType, freeAspectValue, width, height, $"Custom {width}x{height}");
                if (customSize == null)
                {
                    Debug.Log("[GameView] Failed to create custom size instance");
                    return Response.Error("Failed to create custom GameViewSize.");
                }

                // 检查是否已存在相同的自定义尺寸
                var getTotalCountMethod = group.GetType().GetMethod("GetTotalCount");
                if (getTotalCountMethod == null)
                {
                    Debug.Log("[GameView] Could not find 'GetTotalCount' method");
                    return Response.Error("Could not find GetTotalCount method.");
                }

                int totalCount = (int)getTotalCountMethod.Invoke(group, null);
                Debug.Log($"[GameView] Total size count: {totalCount}");

                bool foundExistingSize = false;
                int targetIndex = -1;

                // 查找是否有匹配的分辨率
                var getSizeAtMethod = group.GetType().GetMethod("GetGameViewSize");
                if (getSizeAtMethod == null)
                {
                    Debug.Log("[GameView] Could not find 'GetGameViewSize' method");
                    return Response.Error("Could not find GetGameViewSize method.");
                }

                for (int i = 0; i < totalCount; i++)
                {
                    var size = getSizeAtMethod.Invoke(group, new object[] { i });
                    if (size == null) continue;

                    var widthProp = size.GetType().GetProperty("width");
                    var heightProp = size.GetType().GetProperty("height");

                    if (widthProp == null || heightProp == null) continue;

                    var sizeWidth = (int)widthProp.GetValue(size);
                    var sizeHeight = (int)heightProp.GetValue(size);

                    if (sizeWidth == width && sizeHeight == height)
                    {
                        foundExistingSize = true;
                        targetIndex = i;
                        Debug.Log($"[GameView] Found existing size at index {i}");
                        break;
                    }
                }

                // 如果没找到，添加新的自定义尺寸
                if (!foundExistingSize)
                {
                    var addCustomSizeMethod = group.GetType().GetMethod("AddCustomSize");
                    if (addCustomSizeMethod == null)
                    {
                        Debug.Log("[GameView] Could not find 'AddCustomSize' method");
                        return Response.Error("Could not find AddCustomSize method.");
                    }

                    addCustomSizeMethod.Invoke(group, new object[] { customSize });
                    targetIndex = totalCount; // 新添加的索引
                    Debug.Log($"[GameView] Added new custom size at index {targetIndex}");
                }

                // 设置GameView使用该分辨率
                var selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (selectedSizeIndexProperty != null)
                {
                    selectedSizeIndexProperty.SetValue(gameView, targetIndex, null);

                    // 刷新GameView
                    gameView.Repaint();

                    Debug.Log($"[GameView] Successfully set resolution to {width}x{height} at index {targetIndex}");

                    return Response.Success($"Game view resolution set to {width}x{height}", new
                    {
                        width = width,
                        height = height,
                        index = targetIndex,
                        isNewSize = !foundExistingSize
                    });
                }
                else
                {
                    Debug.Log("[GameView] Could not find 'selectedSizeIndex' property");
                    return Response.Error("Could not set game view resolution: selectedSizeIndex property not found.");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[GameView] Exception in SetGameViewResolution: {e.GetType().Name}: {e.Message}");
                Debug.Log($"[GameView] Stack trace: {e.StackTrace}");
                return Response.Error($"Error setting game view resolution: {e.Message}\nStack: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 获取Game窗口当前分辨率
        /// </summary>
        private object GetGameViewResolution()
        {
            try
            {
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null)
                {
                    return Response.Error("Could not find GameView type.");
                }

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                {
                    return Response.Error("Could not get GameView window.");
                }

                // 获取选中的分辨率索引
                var selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (selectedSizeIndexProperty == null)
                {
                    Debug.Log("[GameView] Could not find 'selectedSizeIndex' property, falling back to window size");
                    var position = gameView.position;
                    return Response.Success("Retrieved Game view window size (fallback).", new
                    {
                        width = (int)position.width,
                        height = (int)position.height,
                        x = (int)position.x,
                        y = (int)position.y,
                        note = "This is the window size, not the selected resolution"
                    });
                }

                int selectedIndex = (int)selectedSizeIndexProperty.GetValue(gameView);

                // 获取GameViewSizes实例
                var gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                if (gameViewSizesType == null)
                {
                    return Response.Error("Could not find GameViewSizes type.");
                }

                object gameViewSizes = GetGameViewSizesInstance(gameViewSizesType);

                if (gameViewSizes == null)
                {
                    Debug.LogError("[GameView] All methods to get GameViewSizes instance failed");
                    return Response.Error("Could not get GameViewSizes instance. Tried ScriptableSingleton, instance property, and Activator.CreateInstance.");
                }

                // 获取当前平台组
                int currentGroup = 0;
                var currentGroupMethod = gameViewSizes.GetType().GetMethod("GetCurrentGroupType",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (currentGroupMethod != null)
                {
                    currentGroup = (int)currentGroupMethod.Invoke(gameViewSizes, null);
                }
                else
                {
                    var currentGroupProp = gameViewSizes.GetType().GetProperty("currentGroupType",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (currentGroupProp != null)
                    {
                        currentGroup = (int)currentGroupProp.GetValue(gameViewSizes);
                    }
                }

                // 获取该组
                var getGroupMethod = gameViewSizes.GetType().GetMethod("GetGroup");
                if (getGroupMethod == null)
                {
                    return Response.Error("Could not find GetGroup method.");
                }

                var group = getGroupMethod.Invoke(gameViewSizes, new object[] { currentGroup });
                if (group == null)
                {
                    return Response.Error("Failed to get GameView size group.");
                }

                // 获取选中的尺寸
                var getGameViewSizeMethod = group.GetType().GetMethod("GetGameViewSize");
                if (getGameViewSizeMethod == null)
                {
                    return Response.Error("Could not find GetGameViewSize method.");
                }

                var selectedSize = getGameViewSizeMethod.Invoke(group, new object[] { selectedIndex });
                if (selectedSize == null)
                {
                    return Response.Error("Could not get selected GameViewSize.");
                }

                // 获取尺寸属性
                var widthProp = selectedSize.GetType().GetProperty("width");
                var heightProp = selectedSize.GetType().GetProperty("height");
                var displayTextProp = selectedSize.GetType().GetProperty("displayText");
                var baseTextProp = selectedSize.GetType().GetProperty("baseText");
                var sizeTypeProp = selectedSize.GetType().GetProperty("sizeType");

                if (widthProp == null || heightProp == null)
                {
                    return Response.Error("Could not access size properties.");
                }

                int width = (int)widthProp.GetValue(selectedSize);
                int height = (int)heightProp.GetValue(selectedSize);
                string displayText = displayTextProp?.GetValue(selectedSize)?.ToString() ?? "Unknown";
                string baseText = baseTextProp?.GetValue(selectedSize)?.ToString() ?? "";
                string sizeType = sizeTypeProp?.GetValue(selectedSize)?.ToString() ?? "Unknown";

                var resolutionInfo = new
                {
                    width = width,
                    height = height,
                    selectedIndex = selectedIndex,
                    displayText = displayText,
                    baseText = baseText,
                    sizeType = sizeType,
                    currentGroup = currentGroup
                };

                return Response.Success($"Retrieved Game view resolution: {displayText}", resolutionInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameView] Exception in GetGameViewResolution: {e.Message}\n{e.StackTrace}");
                return Response.Error($"Failed to get Game view resolution: {e.Message}");
            }
        }


        /// <summary>
        /// 获取Game窗口统计信息
        /// </summary>
        private object GetGameViewStats()
        {
            try
            {
                var stats = new
                {
                    targetFrameRate = Application.targetFrameRate,
                    vSyncCount = QualitySettings.vSyncCount,
                    isPlaying = EditorApplication.isPlaying,
                    isPaused = EditorApplication.isPaused,
                    currentFrameRate = 1.0f / Time.deltaTime, // 近似值
                    screenWidth = Screen.width,
                    screenHeight = Screen.height,
                    fullScreen = Screen.fullScreen,
                    currentResolution = new
                    {
                        width = Screen.currentResolution.width,
                        height = Screen.currentResolution.height,
                        refreshRate = Screen.currentResolution.refreshRate
                    }
                };

                return Response.Success("Retrieved Game view statistics.", stats);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get Game view stats: {e.Message}");
            }
        }

        /// <summary>
        /// 设置VSync
        /// </summary>
        private object SetVSync(int vSyncCount)
        {
            try
            {
                if (vSyncCount < 0 || vSyncCount > 4)
                {
                    return Response.Error("VSync count must be between 0 and 4.");
                }

                QualitySettings.vSyncCount = vSyncCount;

                string message = vSyncCount == 0
                    ? "VSync disabled."
                    : $"VSync set to every {vSyncCount} frame(s).";

                return Response.Success(message, new { vSyncCount = vSyncCount });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set VSync: {e.Message}");
            }
        }

        /// <summary>
        /// 设置目标帧率
        /// </summary>
        private object SetTargetFramerate(int targetFramerate)
        {
            try
            {
                Application.targetFrameRate = targetFramerate;

                string message = targetFramerate == -1
                    ? "Target framerate set to unlimited."
                    : $"Target framerate set to {targetFramerate} FPS.";

                return Response.Success(message, new { targetFrameRate = targetFramerate });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set target framerate: {e.Message}");
            }
        }

        /// <summary>
        /// 最大化Game窗口
        /// </summary>
        private object MaximizeGameView()
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                {
                    return Response.Error("Could not find GameView type.");
                }

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                {
                    return Response.Error("Could not get GameView window.");
                }

                // 尝试最大化
                gameView.maximized = true;
                gameView.Repaint();

                return Response.Success("Game view maximized.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to maximize Game view: {e.Message}");
            }
        }

        /// <summary>
        /// 设置宽高比
        /// </summary>
        private object SetAspectRatio(string aspectRatio)
        {
            try
            {
                // 这个功能需要更复杂的反射实现
                // 简化版本：只记录设置请求
                Debug.Log($"[GameView] Aspect ratio setting requested: {aspectRatio}");

                return Response.Success($"Aspect ratio set to '{aspectRatio}' (Note: Full implementation requires complex reflection).", new
                {
                    aspectRatio = aspectRatio,
                    note = "This is a simplified implementation. Full aspect ratio control requires accessing internal GameView APIs."
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set aspect ratio: {e.Message}");
            }
        }

        /// <summary>
        /// 处理截图操作
        /// </summary>
        private object HandleScreenshotAction(JsonClass args)
        {
            try
            {
                var savePath = args["save_path"]?.Value;
                if (string.IsNullOrEmpty(savePath)) savePath = "Assets/Screenshots/gameview_screenshot.jpg";

                Debug.Log($"[GameView] Taking screenshot to {savePath}");

                var result = ScreenCaptureUtil.CaptureGameView(savePath);

                if (!result.success)
                {
                    return Response.Error(result.error);
                }

                var data = new JsonClass
                {
                    { "path", result.path },
                    { "width", result.width },
                    { "height", result.height },
                    { "format", result.format },
                    { "size_bytes", result.size }
                };
                var resources = new JsonClass
                {
                    { "type", "image" },
                    { "path", result.path },
                    { "format", result.format }
                };

                return Response.Success("GameView screenshot saved successfully", data, resources);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameView] Screenshot failed: {e.Message}");
                return Response.Error($"Screenshot failed: {e.Message}");
            }
        }


    }

    /// <summary>
    /// 获取GameViewSizes单例实例的优化方法
    /// 参考ScriptableSingleton实现，使用多种策略尝试获取实例
    /// </summary>
    /// <param name="gameViewSizesType">GameViewSizes类型</param>
    /// <returns>GameViewSizes实例或null</returns>
    private object GetGameViewSizesInstance(Type gameViewSizesType)
        {
            if (gameViewSizesType == null) return null;

            object instance = null;

            // 策略1: 通过ScriptableSingleton的s_Instance字段获取实例（避免触发instance属性创建新实例）
            try
            {
                var scriptableSingletonType = typeof(ScriptableObject).Assembly.GetType("UnityEditor.ScriptableSingleton`1");
                if (scriptableSingletonType != null)
                {
                    var genericType = scriptableSingletonType.MakeGenericType(gameViewSizesType);
                    var instanceField = genericType.GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);

                    if (instanceField != null)
                    {
                        instance = instanceField.GetValue(null);
                        if (instance != null)
                        {
                            Debug.Log("[GameView] ✓ Got instance from ScriptableSingleton.s_Instance field");
                            return instance;
                        }
                    }

                    // 如果s_Instance为空，尝试通过instance属性获取（这可能会创建实例）
                    var instanceProperty = genericType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                    if (instanceProperty != null)
                    {
                        try
                        {
                            instance = instanceProperty.GetValue(null);
                            if (instance != null)
                            {
                                Debug.Log("[GameView] ✓ Got instance from ScriptableSingleton.instance property");
                                return instance;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameView] Error accessing instance property: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameView] ScriptableSingleton strategy exception: {ex.Message}");
            }

            // 策略2: 查找类型中的静态实例字段
            try
            {
                var staticFields = gameViewSizesType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in staticFields)
                {
                    if (field.FieldType == gameViewSizesType || field.FieldType.IsAssignableFrom(gameViewSizesType))
                    {
                        try
                        {
                            var fieldValue = field.GetValue(null);
                            if (fieldValue != null)
                            {
                                Debug.Log($"[GameView] ✓ Got instance from static field: {field.Name}");
                                return fieldValue;
                            }
                        }
                        catch { /* 忽略单个字段的访问错误 */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameView] Static fields strategy exception: {ex.Message}");
            }

            // 策略3: 查找实例属性或其他获取实例的方法
            try
            {
                // 尝试查找命名为"instance"的静态属性
                var instanceProperty = gameViewSizesType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (instanceProperty != null)
                {
                    try
                    {
                        instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            Debug.Log("[GameView] ✓ Got instance from instance property");
                            return instance;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[GameView] instance property exception: {ex.Message}");
                    }
                }
                
                // 尝试查找其他可能的获取实例方法
                var possibleMethodNames = new[] { "get_Instance", "GetInstanceID", "GetSingleton", "get_Singleton" };
                
                foreach (var methodName in possibleMethodNames)
                {
                    var method = gameViewSizesType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        try
                        {
                            instance = method.Invoke(null, null);
                            if (instance != null)
                            {
                                Debug.Log($"[GameView] ✓ Got instance from {methodName} method");
                                return instance;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameView] {methodName} method exception: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[GameView] Instance property/method search exception: {ex.Message}");
            }

            // 策略4: 最后尝试创建实例（如果必要）
            try
            {
                Debug.LogWarning("[GameView] All non-creating methods failed, attempting to create instance");

                // 首先尝试使用CreateInstance方法
                var createMethod = gameViewSizesType.GetMethod("CreateInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (createMethod != null)
                {
                    instance = createMethod.Invoke(null, null);
                    if (instance != null)
                    {
                        Debug.Log("[GameView] ✓ Created instance using CreateInstance method");
                        return instance;
                    }
                }

                // 最后尝试使用Activator.CreateInstance
                instance = Activator.CreateInstance(gameViewSizesType, true);
                if (instance != null)
                {
                    Debug.Log("[GameView] ✓ Created instance using Activator.CreateInstance");
                    return instance;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameView] Instance creation exception: {ex.Message}");
            }

            Debug.LogError("[GameView] Failed to get or create GameViewSizes instance");
            return null;
        }
    }

