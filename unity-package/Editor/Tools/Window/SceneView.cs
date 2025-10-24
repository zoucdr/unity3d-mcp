using System;
using System.IO;
using System.Reflection;
// Migrated from Newtonsoft.Json to SimpleJson
using UnityEditor;
using UnityEngine;
using Unity.Mcp.Models;
using Unity.Mcp.Utils;

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// 处理Unity Scene窗口管理和控制
    /// 对应方法名: scene_view
    /// </summary>
    [ToolName("scene_view", "场景窗口")]
    public class SceneView : StateMethodBase
    {
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "Operation type: get_info, focus, maximize, screenshot, set_pivot, set_rotation, set_2d_mode, align_with_view, frame_selected", false),
                new MethodKey("save_path", "Path to save screenshot", true),
                new MethodKey("pivot_position", "Pivot position [x,y,z]", true),
                new MethodKey("rotation", "Camera rotation [x,y,z]", true),
                new MethodKey("orthographic", "Set orthographic mode", true),
                new MethodKey("align_view", "Align view direction: top, bottom, left, right, front, back", true)
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("get_info", HandleGetInfoAction)
                    .Leaf("focus", HandleFocusAction)
                    .Leaf("maximize", HandleMaximizeAction)
                    .Leaf("screenshot", HandleScreenshotAction)
                    .Leaf("set_pivot", HandleSetPivotAction)
                    .Leaf("set_rotation", HandleSetRotationAction)
                    .Leaf("set_2d_mode", HandleSet2DModeAction)
                    .Leaf("align_with_view", HandleAlignWithViewAction)
                    .Leaf("frame_selected", HandleFrameSelectedAction)
                .Build();
        }

        /// <summary>
        /// 处理获取Scene窗口信息的操作
        /// </summary>
        private object HandleGetInfoAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                var info = new
                {
                    pivot = sceneView.pivot,
                    rotation = sceneView.rotation.eulerAngles,
                    size = sceneView.size,
                    orthographic = sceneView.orthographic,
                    in2DMode = sceneView.in2DMode,
                    camera = new
                    {
                        position = sceneView.camera.transform.position,
                        rotation = sceneView.camera.transform.rotation.eulerAngles,
                        fieldOfView = sceneView.camera.fieldOfView
                    },
                    window = new
                    {
                        width = sceneView.position.width,
                        height = sceneView.position.height,
                        x = sceneView.position.x,
                        y = sceneView.position.y
                    }
                };

                return Response.Success("SceneView information retrieved.", info);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error getting SceneView info: {e.Message}");
                return Response.Error($"Failed to get SceneView info: {e.Message}");
            }
        }

        /// <summary>
        /// 处理聚焦Scene窗口的操作
        /// </summary>
        private object HandleFocusAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                sceneView.Focus();
                return Response.Success("SceneView focused.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error focusing SceneView: {e.Message}");
                return Response.Error($"Failed to focus SceneView: {e.Message}");
            }
        }

        /// <summary>
        /// 处理最大化Scene窗口的操作
        /// </summary>
        private object HandleMaximizeAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                sceneView.maximized = !sceneView.maximized;
                return Response.Success($"SceneView {(sceneView.maximized ? "maximized" : "restored")}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error maximizing SceneView: {e.Message}");
                return Response.Error($"Failed to maximize SceneView: {e.Message}");
            }
        }

        /// <summary>
        /// 处理Scene窗口截图的操作
        /// </summary>
        private object HandleScreenshotAction(JsonClass args)
        {
            try
            {
                var savePath = args["save_path"]?.Value;
                if (string.IsNullOrEmpty(savePath)) savePath = "Assets/Screenshots/sceneview_screenshot.jpg";

                Debug.Log($"[SceneView] Taking screenshot to {savePath}");

                var result = ScreenCaptureUtil.CaptureSceneView(savePath);

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
                return Response.Success("SceneView screenshot saved successfully", data, resources);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Screenshot failed: {e.Message}");
                return Response.Error($"Screenshot failed: {e.Message}");
            }
        }

        /// <summary>
        /// 处理设置Scene窗口pivot点的操作
        /// </summary>
        private object HandleSetPivotAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                var pivotArray = args["pivot_position"].AsArray;
                if (pivotArray == null || pivotArray.Count < 3)
                {
                    return Response.Error("Invalid pivot_position. Expected [x,y,z] array.");
                }

                float x = pivotArray[0].AsFloatDefault(0);
                float y = pivotArray[1].AsFloatDefault(0);
                float z = pivotArray[2].AsFloatDefault(0);

                Vector3 pivot = new Vector3(x, y, z);
                sceneView.pivot = pivot;
                sceneView.Repaint();

                return Response.Success($"SceneView pivot set to {pivot}.", new
                {
                    pivot = new { x, y, z }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error setting pivot: {e.Message}");
                return Response.Error($"Failed to set pivot: {e.Message}");
            }
        }

        /// <summary>
        /// 处理设置Scene窗口旋转的操作
        /// </summary>
        private object HandleSetRotationAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                var rotationArray = args["rotation"].AsArray;
                if (rotationArray == null || rotationArray.Count < 3)
                {
                    return Response.Error("Invalid rotation. Expected [x,y,z] array.");
                }

                float x = rotationArray[0].AsFloatDefault(0);
                float y = rotationArray[1].AsFloatDefault(0);
                float z = rotationArray[2].AsFloatDefault(0);

                Quaternion rotation = Quaternion.Euler(x, y, z);
                sceneView.rotation = rotation;
                sceneView.Repaint();

                return Response.Success($"SceneView rotation set to {rotation.eulerAngles}.", new
                {
                    rotation = new { x, y, z }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error setting rotation: {e.Message}");
                return Response.Error($"Failed to set rotation: {e.Message}");
            }
        }

        /// <summary>
        /// 处理设置Scene窗口2D模式的操作
        /// </summary>
        private object HandleSet2DModeAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                bool orthographic = args["orthographic"].AsBoolDefault(true);
                sceneView.orthographic = orthographic;

                // 设置2D模式
                sceneView.in2DMode = orthographic;
                sceneView.Repaint();

                return Response.Success($"SceneView 2D mode set to {orthographic}.", new
                {
                    orthographic = orthographic,
                    in2DMode = sceneView.in2DMode
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error setting 2D mode: {e.Message}");
                return Response.Error($"Failed to set 2D mode: {e.Message}");
            }
        }

        /// <summary>
        /// 处理与视图对齐的操作
        /// </summary>
        private object HandleAlignWithViewAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                var alignView = args["align_view"]?.Value;
                if (string.IsNullOrEmpty(alignView))
                {
                    return Response.Error("align_view parameter is required.");
                }

                Quaternion rotation;
                switch (alignView.ToLower())
                {
                    case "top":
                        rotation = Quaternion.Euler(90, 0, 0);
                        break;
                    case "bottom":
                        rotation = Quaternion.Euler(-90, 0, 0);
                        break;
                    case "left":
                        rotation = Quaternion.Euler(0, -90, 0);
                        break;
                    case "right":
                        rotation = Quaternion.Euler(0, 90, 0);
                        break;
                    case "front":
                        rotation = Quaternion.Euler(0, 0, 0);
                        break;
                    case "back":
                        rotation = Quaternion.Euler(0, 180, 0);
                        break;
                    default:
                        return Response.Error("Invalid align_view value. Expected: top, bottom, left, right, front, back");
                }

                sceneView.rotation = rotation;
                sceneView.Repaint();

                return Response.Success($"SceneView aligned to {alignView} view.", new
                {
                    alignView = alignView,
                    rotation = rotation.eulerAngles
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error aligning view: {e.Message}");
                return Response.Error($"Failed to align view: {e.Message}");
            }
        }

        /// <summary>
        /// 处理将视图帧定位到选中对象的操作
        /// </summary>
        private object HandleFrameSelectedAction(JsonClass args)
        {
            try
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return Response.Error("No active SceneView found.");
                }

                if (Selection.gameObjects.Length == 0)
                {
                    return Response.Error("No objects selected.");
                }

                // 使用SceneView的FrameSelected方法
                sceneView.FrameSelected();

                return Response.Success($"SceneView framed to selected objects ({Selection.gameObjects.Length}).");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneView] Error framing selected: {e.Message}");
                return Response.Error($"Failed to frame selected: {e.Message}");
            }
        }
    }
}
