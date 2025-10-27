using System;
using System.Collections.Generic;
using System.Linq;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEditorInternal; // Required for tag management
using UnityEngine;
using Unity.Mcp.Models; // For Response class
using Unity.Mcp;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Handles Unity Editor state management and controls.
    /// 对应方法名: base_editor
    /// </summary>
    [ToolName("base_editor", "系统管理")]
    public class BaseEditor : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: get_state, get_windows, get_selection, execute_menu, get_menu_items", false),
                new MethodKey("wait_for_completion", "Whether to wait for operation completion", true),
                new MethodKey("menu_path", "Menu path (used when executing menu)", true),
                new MethodKey("root_path", "Root menu path (used when getting menu items)", true),
                new MethodKey("include_submenus", "Include submenus (used when getting menu items)", true),
                new MethodKey("verify_exists", "Verify menu items exist (used when getting menu items)", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    // Editor State/Info
                    .Leaf("get_state", HandleGetStateAction)
                    .Leaf("get_windows", HandleGetWindowsAction)
                    .Leaf("get_selection", HandleGetSelectionAction)

                    // Menu Management
                    .Leaf("execute_menu", MenuUtils.HandleExecuteMenu)
                    .Leaf("get_menu_items", MenuUtils.HandleGetMenuItems)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理获取编辑器状态的操作
        /// </summary>
        private object HandleGetStateAction(JsonClass args)
        {
            McpLogger.Log("[BaseEditor] Getting editor state");
            return GetEditorState();
        }

        /// <summary>
        /// 处理获取编辑器窗口的操作
        /// </summary>
        private object HandleGetWindowsAction(JsonClass args)
        {
            McpLogger.Log("[BaseEditor] Getting editor windows");
            return GetEditorWindows();
        }

        /// <summary>
        /// 处理获取选择对象的操作
        /// </summary>
        private object HandleGetSelectionAction(JsonClass args)
        {
            McpLogger.Log("[BaseEditor] Getting selection");
            return GetSelection();
        }

        // --- Editor State/Info Methods ---
        private object GetEditorState()
        {
            try
            {
                var state = new
                {
                    isPlaying = EditorApplication.isPlaying,
                    isPaused = EditorApplication.isPaused,
                    isCompiling = EditorApplication.isCompiling,
                    isUpdating = EditorApplication.isUpdating,
                    applicationPath = EditorApplication.applicationPath,
                    applicationContentsPath = EditorApplication.applicationContentsPath,
                    timeSinceStartup = EditorApplication.timeSinceStartup,
                };
                return Response.Success("Retrieved editor state.", state);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting editor state: {e.Message}");
            }
        }

        private object GetEditorWindows()
        {
            try
            {
                // Get all types deriving from EditorWindow
                var windowTypes = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsSubclassOf(typeof(EditorWindow)))
                    .ToList();

                var openWindows = new List<object>();

                // Find currently open instances
                // Resources.FindObjectsOfTypeAll seems more reliable than GetWindow for finding *all* open windows
                EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

                foreach (EditorWindow window in allWindows)
                {
                    if (window == null)
                        continue; // Skip potentially destroyed windows

                    try
                    {
                        openWindows.Add(
                            new
                            {
                                title = window.titleContent.text,
                                typeName = window.GetType().FullName,
                                isFocused = EditorWindow.focusedWindow == window,
                                position = new
                                {
                                    x = window.position.x,
                                    y = window.position.y,
                                    width = window.position.width,
                                    height = window.position.height,
                                },
                                instanceID = window.GetInstanceID(),
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogWarning(
                            $"Could not get info for window {window.GetType().Name}: {ex.Message}"
                        );
                    }
                }

                return Response.Success("Retrieved list of open editor windows.", openWindows);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting editor windows: {e.Message}");
            }
        }

        private object GetSelection()
        {
            try
            {
                var selectionInfo = new
                {
                    activeObject = Selection.activeObject?.name,
                    activeGameObject = Selection.activeGameObject?.name,
                    activeTransform = Selection.activeTransform?.name,
                    activeInstanceID = Selection.activeInstanceID,
                    count = Selection.count,
                    objects = Selection
                        .objects.Select(obj => new
                        {
                            name = obj?.name,
                            type = obj?.GetType().FullName,
                            instanceID = obj?.GetInstanceID(),
                        })
                        .ToList(),
                    gameObjects = Selection
                        .gameObjects.Select(go => new
                        {
                            name = go?.name,
                            instanceID = go?.GetInstanceID(),
                        })
                        .ToList(),
                    assetGUIDs = Selection.assetGUIDs, // GUIDs for selected assets in Project view
                };

                return Response.Success("Retrieved current selection details.", selectionInfo);
            }
            catch (Exception e)
            {
                return Response.Error($"Error getting selection: {e.Message}");
            }
        }

        // --- Example Implementations for Settings ---
        /*
        private object SetQualityLevel(JsonNode qualityLevelToken) { ... }
        */
    }
}

