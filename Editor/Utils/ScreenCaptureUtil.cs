using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniMcp.Utils
{
    /// <summary>
    /// 屏幕截图工具类，提供对GameView、SceneView等窗口的截图功能
    /// </summary>
    public static class ScreenCaptureUtil
    {
        /// <summary>
        /// 从GameView窗口捕获截图
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="quality">JPG质量（1-100）</param>
        /// <returns>操作结果</returns>
        public static CaptureResult CaptureGameView(string savePath, int quality = 90)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 获取GameView窗口
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null)
                {
                    return new CaptureResult { success = false, error = "Could not find GameView type." };
                }

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                {
                    return new CaptureResult { success = false, error = "Could not get GameView window." };
                }

                // 获取GameView的RenderTexture
                RenderTexture renderTexture = null;
                var renderTextureField = gameViewType.GetField("m_RenderTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (renderTextureField != null)
                {
                    renderTexture = renderTextureField.GetValue(gameView) as RenderTexture;
                }

                // 如果找不到RenderTexture，尝试其他方法
                if (renderTexture == null)
                {
                    // 尝试获取targetDisplay属性
                    // 注意：我们不能直接使用Display.displays[targetDisplay].colorBuffer，因为它是RenderBuffer类型而不是RenderTexture
                    // 此处改用其他方法尝试获取RenderTexture
                    var targetDisplayProperty = gameViewType.GetProperty("targetDisplay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (targetDisplayProperty != null)
                    {
                        // 尝试通过其他字段获取RenderTexture
                        var renderTextureField2 = gameViewType.GetField("m_TargetTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (renderTextureField2 != null)
                        {
                            renderTexture = renderTextureField2.GetValue(gameView) as RenderTexture;
                        }
                    }
                }

                // 如果仍然找不到RenderTexture，使用备选方法
                if (renderTexture == null)
                {
                    Debug.LogWarning("[ScreenCaptureUtil] Could not get GameView RenderTexture, using fallback method.");
                    // 使用EditorWindow的position来创建一个临时的RenderTexture
                    var position = (gameView as EditorWindow).position;
                    int width = (int)position.width;
                    int height = (int)position.height;
                    renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

                    // 强制GameView重绘
                    (gameView as EditorWindow).Repaint();
                    // 等待一帧以确保渲染完成
                    EditorApplication.QueuePlayerLoopUpdate();
                }

                if (renderTexture == null)
                {
                    return new CaptureResult { success = false, error = "Failed to capture screenshot: Could not get or create RenderTexture." };
                }

                // 捕获RenderTexture内容
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                RenderTexture activeRT = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                RenderTexture.active = activeRT;

                // 修正上下反转的问题
                texture = FlipTextureVertically(texture);

                // 保存图像为JPG格式
                byte[] imageData = texture.EncodeToJPG(quality);
                File.WriteAllBytes(savePath, imageData);

                var result = new CaptureResult
                {
                    success = true,
                    path = Path.GetFullPath(savePath),
                    width = texture.width,
                    height = texture.height,
                    format = "JPG",
                    size = imageData.Length
                };

                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.Refresh();

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenCaptureUtil] GameView screenshot failed: {e.Message}\n{e.StackTrace}");
                return new CaptureResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// 从SceneView窗口捕获截图
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="quality">JPG质量（1-100）</param>
        /// <returns>操作结果</returns>
        public static CaptureResult CaptureSceneView(string savePath, int quality = 90)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    // 尝试获取任何打开的SceneView
                    var sceneViews = UnityEditor.SceneView.sceneViews;
                    if (sceneViews.Count > 0)
                    {
                        sceneView = sceneViews[0] as UnityEditor.SceneView;
                    }
                }

                if (sceneView == null || sceneView.camera == null)
                {
                    return new CaptureResult { success = false, error = "No active SceneView found." };
                }

                var camera = sceneView.camera;
                var width = (int)sceneView.position.width;
                var height = (int)sceneView.position.height;

                // 创建RenderTexture
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var previousTarget = camera.targetTexture;
                var previousAspect = camera.aspect;

                camera.targetTexture = rt;
                camera.aspect = (float)width / height;
                camera.Render();

                // 读取像素
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                var activeRT = RenderTexture.active;
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = activeRT;

                // 恢复相机设置
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                UnityEngine.Object.DestroyImmediate(rt);

                // 保存图像为JPG格式
                byte[] imageData = texture.EncodeToJPG(quality);
                File.WriteAllBytes(savePath, imageData);

                var result = new CaptureResult
                {
                    success = true,
                    path = Path.GetFullPath(savePath),
                    width = width,
                    height = height,
                    format = "JPG",
                    size = imageData.Length
                };

                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.Refresh();

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenCaptureUtil] SceneView screenshot failed: {e.Message}\n{e.StackTrace}");
                return new CaptureResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// 从指定的相机捕获截图
        /// </summary>
        /// <param name="camera">相机</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="quality">JPG质量（1-100）</param>
        /// <returns>操作结果</returns>
        public static CaptureResult CaptureFromCamera(Camera camera, string savePath, int width = 1920, int height = 1080, int quality = 90)
        {
            try
            {
                if (camera == null)
                {
                    return new CaptureResult { success = false, error = "Camera is null." };
                }

                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建RenderTexture
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var previousTarget = camera.targetTexture;
                var previousAspect = camera.aspect;

                camera.targetTexture = rt;
                camera.aspect = (float)width / height;
                camera.Render();

                // 读取像素
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                var activeRT = RenderTexture.active;
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = activeRT;

                // 恢复相机设置
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                UnityEngine.Object.DestroyImmediate(rt);

                // 保存图像为JPG格式
                byte[] imageData = texture.EncodeToJPG(quality);
                File.WriteAllBytes(savePath, imageData);

                var result = new CaptureResult
                {
                    success = true,
                    path = Path.GetFullPath(savePath),
                    width = width,
                    height = height,
                    format = "JPG",
                    size = imageData.Length
                };

                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.Refresh();

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScreenCaptureUtil] Camera screenshot failed: {e.Message}\n{e.StackTrace}");
                return new CaptureResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// 垂直翻转纹理（修正截图上下颠倒的问题）
        /// </summary>
        private static Texture2D FlipTextureVertically(Texture2D original)
        {
            var flipped = new Texture2D(original.width, original.height, original.format, false);

            for (int x = 0; x < original.width; x++)
            {
                for (int y = 0; y < original.height; y++)
                {
                    flipped.SetPixel(x, y, original.GetPixel(x, original.height - y - 1));
                }
            }

            flipped.Apply();
            UnityEngine.Object.DestroyImmediate(original); // 清理原始纹理
            return flipped;
        }
    }

    /// <summary>
    /// 截图结果数据结构
    /// </summary>
    public class CaptureResult
    {
        public bool success;
        public string error;
        public string path;
        public int width;
        public int height;
        public string format;
        public int size;
    }
}
