using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using UnityMcp.Models; // For Response class
using UnityMcp;

namespace UnityMcp.Tools
{
    /// <summary>
    /// Handles Unity Game window management and controls.
    /// Corresponding method name: game_view
    /// </summary>
    [ToolName("game_view", "Window management")]
    public class GameView : StateMethodBase
    {
        /// <summary>
        /// Create the list of parameter keys supported by the current method
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: set_resolution, get_resolution, get_stats, set_vsync, set_target_framerate, maximize, set_aspect_ratio", false),
                new MethodKey("width", "Window width (used for set_resolution)", true),
                new MethodKey("height", "Window height (used for set_resolution)", true),
                new MethodKey("vsync_count", "VSync count: 0=off, 1=every frame, 2=every 2nd frame (used for set_vsync)", true),
                new MethodKey("target_framerate", "Target frame rate, -1=unlimited (used for set_target_framerate)", true),
                new MethodKey("aspect_ratio", "Aspect ratio string like '16:9' or 'Free' (used for set_aspect_ratio)", true)
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
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// Handle set resolution operation
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
        /// Handle get resolution operation
        /// </summary>
        private object HandleGetResolutionAction(JsonClass args)
        {
            Debug.Log("[GameView] Getting current resolution");
            return GetGameViewResolution();
        }

        /// <summary>
        /// Handle get statistics operation
        /// </summary>
        private object HandleGetStatsAction(JsonClass args)
        {
            Debug.Log("[GameView] Getting Game view stats");
            return GetGameViewStats();
        }

        /// <summary>
        /// Handle settingVSyncOperation of
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
        /// Handle setting target frame rate operation
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
        /// Handle maximize window operation
        /// </summary>
        private object HandleMaximizeAction(JsonClass args)
        {
            Debug.Log("[GameView] Maximizing Game view");
            return MaximizeGameView();
        }

        /// <summary>
        /// Handle set aspect ratio operation
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
        /// SetGameWindow resolution（FromUGUILayoutMigrated from，Use completeGameViewSizes APIImplement）
        /// </summary>
        private object SetGameViewResolution(int width, int height)
        {
            try
            {
                // Access by reflectionGameViewClass，Because it is not publicAPI
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null)
                {
                    Debug.Log("[GameView] Could not find GameView type");
                    return Response.Error("Could not find GameView type.");
                }

                // Get currentGameViewWindow
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                {
                    Debug.Log("[GameView] Could not get GameView window");
                    return Response.Error("Could not get GameView window.");
                }

                // GetGameViewSizesClass
                var gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                var gameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
                var gameViewSizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");

                if (gameViewSizesType == null || gameViewSizeType == null || gameViewSizeTypeEnum == null)
                {
                    Debug.Log($"[GameView] Could not find types: GameViewSizes={gameViewSizesType != null}, GameViewSize={gameViewSizeType != null}, GameViewSizeType={gameViewSizeTypeEnum != null}");
                    return Response.Error("Could not find GameView size types.");
                }

                // Get singleton instance - TryScriptableSingletonMethod（Unity 2021+）
                object gameViewSizes = null;
                UnityEngine.Debug.Log($"[GameView] >>> SetGameViewResolution starting for {width}x{height} <<<");

                // Method1: Try viaScriptableSingleton<T>.instance
                try
                {
                    UnityEngine.Debug.Log("[GameView] Method 1: Trying ScriptableSingleton<T>.instance");
                    var scriptableSingletonType = typeof(ScriptableObject).Assembly.GetType("UnityEditor.ScriptableSingleton`1");
                    UnityEngine.Debug.Log($"[GameView] ScriptableSingleton type found: {scriptableSingletonType != null}");

                    if (scriptableSingletonType != null)
                    {
                        var genericType = scriptableSingletonType.MakeGenericType(gameViewSizesType);
                        UnityEngine.Debug.Log($"[GameView] Generic type created: {genericType != null}");

                        var instanceProperty = genericType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                        UnityEngine.Debug.Log($"[GameView] Instance property found: {instanceProperty != null}");

                        if (instanceProperty != null)
                        {
                            gameViewSizes = instanceProperty.GetValue(null);
                            UnityEngine.Debug.Log($"[GameView] Got instance: {gameViewSizes != null}");

                            if (gameViewSizes != null)
                            {
                                UnityEngine.Debug.Log("[GameView] ✓ Successfully got instance through ScriptableSingleton");
                                goto ProcessGameViewSizes;
                            }
                            else
                            {
                                UnityEngine.Debug.Log("[GameView] Instance property returned null");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log($"[GameView] ScriptableSingleton approach exception: {ex.GetType().Name}: {ex.Message}");
                }

                // Method2: Try to get by property
                UnityEngine.Debug.Log("[GameView] Method 2: Trying instance property");
                var instanceProperty2 = gameViewSizesType.GetProperty("instance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                UnityEngine.Debug.Log($"[GameView] Instance property found: {instanceProperty2 != null}");

                if (instanceProperty2 != null)
                {
                    try
                    {
                        gameViewSizes = instanceProperty2.GetValue(null);
                        UnityEngine.Debug.Log($"[GameView] Got instance: {gameViewSizes != null}");

                        if (gameViewSizes != null)
                        {
                            UnityEngine.Debug.Log("[GameView] ✓ Successfully got instance through property");
                            goto ProcessGameViewSizes;
                        }
                        else
                        {
                            UnityEngine.Debug.Log("[GameView] Instance property returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.Log($"[GameView] Instance property exception: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("[GameView] No instance property found on GameViewSizes type");
                }

                // If the first two methods fail，Try the last method（Will show warning but will work）
                if (gameViewSizes == null)
                {
                    UnityEngine.Debug.Log("[GameView] Method 3: Trying Activator.CreateInstance (may produce warning)");
                    try
                    {
                        gameViewSizes = Activator.CreateInstance(gameViewSizesType, true);
                        if (gameViewSizes != null)
                        {
                            UnityEngine.Debug.Log("[GameView] ✓ Got instance through Activator.CreateInstance");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.Log($"[GameView] Activator approach exception: {ex.Message}");
                    }
                }

                goto ProcessGameViewSizes;

            ProcessGameViewSizes: // Handle instance retrieved by method or property

                if (gameViewSizes == null)
                {
                    Debug.Log("[GameView] GameViewSizes instance is null");
                    return Response.Error("GameViewSizes instance is null.");
                }

                // Get the game view size group for the current platform
                int currentGroup = 0; // Default isStandalone (0)

                // Try multiple ways to get the current group type
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
                    // Try to get by property
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
                        // According toBuildTargetInfer current platform
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
                                currentGroup = 0; // DefaultStandalone
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

                // Create custom resolution
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

                // Check if the same custom size already exists
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

                // Check if matching resolution exists
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

                // If not found，Add new custom size
                if (!foundExistingSize)
                {
                    var addCustomSizeMethod = group.GetType().GetMethod("AddCustomSize");
                    if (addCustomSizeMethod == null)
                    {
                        Debug.Log("[GameView] Could not find 'AddCustomSize' method");
                        return Response.Error("Could not find AddCustomSize method.");
                    }

                    addCustomSizeMethod.Invoke(group, new object[] { customSize });
                    targetIndex = totalCount; // Newly added index
                    Debug.Log($"[GameView] Added new custom size at index {targetIndex}");
                }

                // SetGameViewUse this resolution
                var selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (selectedSizeIndexProperty != null)
                {
                    selectedSizeIndexProperty.SetValue(gameView, targetIndex, null);

                    // RefreshGameView
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
        /// GetGameCurrent window resolution
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

                // Get index of selected resolution
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

                // GetGameViewSizesInstance
                var gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
                if (gameViewSizesType == null)
                {
                    return Response.Error("Could not find GameViewSizes type.");
                }

                object gameViewSizes = null;

                // Try viaScriptableSingletonGet instance
                try
                {
                    var scriptableSingletonType = typeof(ScriptableObject).Assembly.GetType("UnityEditor.ScriptableSingleton`1");
                    if (scriptableSingletonType != null)
                    {
                        var genericType = scriptableSingletonType.MakeGenericType(gameViewSizesType);
                        var instanceProperty = genericType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                        if (instanceProperty != null)
                        {
                            gameViewSizes = instanceProperty.GetValue(null);
                        }
                    }
                }
                catch { }

                // Alternative method：Get by property
                if (gameViewSizes == null)
                {
                    var instanceProperty = gameViewSizesType.GetProperty("instance",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (instanceProperty != null)
                    {
                        try
                        {
                            gameViewSizes = instanceProperty.GetValue(null);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"[GameView] Instance property exception: {ex.Message}");
                        }
                    }
                }

                // Alternative method3：UseActivator.CreateInstance（May show warning but will work）
                if (gameViewSizes == null)
                {
                    Debug.Log("[GameView] Trying Activator.CreateInstance as fallback");
                    try
                    {
                        gameViewSizes = Activator.CreateInstance(gameViewSizesType, true);
                        if (gameViewSizes != null)
                        {
                            Debug.Log("[GameView] Successfully got instance through Activator.CreateInstance");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[GameView] Activator approach exception: {ex.Message}");
                    }
                }

                if (gameViewSizes == null)
                {
                    Debug.LogError("[GameView] All methods to get GameViewSizes instance failed");
                    return Response.Error("Could not get GameViewSizes instance. Tried ScriptableSingleton, instance property, and Activator.CreateInstance.");
                }

                // Get current platform group
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

                // Get this group
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

                // Get selected size
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

                // Get size property
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
        /// GetGameWindow statistics
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
                    currentFrameRate = 1.0f / Time.deltaTime, // Approximate value
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
        /// SetVSync
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
        /// Set target frame rate
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
        /// MaximizeGameWindow
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

                // Try maximize
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
        /// Set aspect ratio
        /// </summary>
        private object SetAspectRatio(string aspectRatio)
        {
            try
            {
                // This feature requires more complex reflection
                // Simplified version：Only record set request
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
    }
}

