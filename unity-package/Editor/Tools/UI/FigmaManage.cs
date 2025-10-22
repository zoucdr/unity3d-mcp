using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Unity.Mcp.Tools;
using Unity.Mcp.Models;
using Object = UnityEngine.Object;

namespace Unity.Mcp.Tools
{
    [ToolName("figma_manage", "UI管理")]
    public class FigmaManage : StateMethodBase
    {
        private const string FIGMA_API_BASE = "https://api.figma.com/v1";

        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new[]
            {
                new MethodKey("action", "操作类型: fetch_nodes(拉取节点数据), download_images(批量下载指定节点图片), preview(预览图片并返回base64编码), get_conversion_rules(获取UI框架转换规则)", false),
                new MethodKey("file_key", "Figma文件Key", true),
                new MethodKey("node_imgs", "节点图片映射：JSON格式的节点名称映射，如\"{\\\"1:4\\\":\\\"image1.png\\\",\\\"1:5\\\":\\\"image2.jpg\\\"}\"。提供此参数时将直接使用指定文件名，无需额外API调用", true),
                new MethodKey("node_id", "节点ID：用于智能扫描下载或预览的节点ID", true),
                new MethodKey("save_path", "保存路径，默认为由ProjectSettings → MCP → Figma中的img_save_to配置", true),
                new MethodKey("image_format", "图片格式: png, jpg, svg, pdf，默认为png", true),
                new MethodKey("image_scale", "图片缩放比例，默认为1", true),
                new MethodKey("include_children", "是否包含子节点，默认为true", true),
                new MethodKey("local_json_path", "本地JSON文件路径（可选，用于从FetchNodes保存的JSON文件中读取节点数据）", true),
                new MethodKey("ui_framework", "UI框架类型: ugui, uitoolkit, all（默认为all，返回所有框架的规则）", true)
            };
        }

        /// <summary>
        /// 创建状态树
        /// </summary>
        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("preview", PreviewImage)
                    .Leaf("fetch_nodes", FetchNodes)
                    .Leaf("download_images", DownloadNodeImages)
                    .Leaf("get_conversion_rules", GetConversionRules)
                .Build();
        }

        /// <summary>
        /// 拉取节点数据功能
        /// 
        /// 支持两种模式：
        /// 1. 指定节点模式：通过node_imgs指定要拉取的节点ID列表
        /// 2. 节点扫描模式：通过node_id指定节点，自动扫描所有子节点
        /// </summary>
        private object FetchNodes(StateTreeContext ctx)
        {
            string fileKey = ctx["file_key"]?.ToString();
            string nodeImgsParam = ctx["node_imgs"]?.ToString();
            string nodeId = ctx["node_id"]?.ToString();
            string token = GetFigmaToken();
            bool includeChildren = bool.Parse(ctx["include_children"]?.ToString() ?? "true");

            if (string.IsNullOrEmpty(fileKey))
                return Response.Error("file_key是必需的参数");

            // 检查参数：至少提供node_imgs或node_id之一
            if (string.IsNullOrEmpty(nodeImgsParam) && string.IsNullOrEmpty(nodeId))
                return Response.Error("必须提供node_imgs或node_id之一");

            if (string.IsNullOrEmpty(token))
                return Response.Error("Figma访问令牌未配置，请在Project Settings → MCP → Figma中配置");

            // 优先处理node_id（智能扫描模式）
            if (!string.IsNullOrEmpty(nodeId))
            {
                try
                {
                    Debug.Log($"[FigmaManage] 启动节点智能扫描: {nodeId}");
                    var nodeIdList = new List<string> { nodeId };
                    // 使用同一个协程，通过includeChildren=true来获取完整的子节点树
                    // 为长时间运行的网络请求设置更长的超时时间（120秒）
                    return ctx.AsyncReturn(FetchNodesCoroutine(fileKey, nodeIdList, token, true, nodeId), 120f);
                }
                catch (Exception ex)
                {
                    return Response.Error($"根节点数据拉取失败: {ex.Message}");
                }
            }

            // 解析节点参数（标准模式）
            if (!ParseNodeParameters(nodeImgsParam, out List<string> nodeIds, out Dictionary<string, string> nodeNames))
            {
                return Response.Error("节点参数格式无效，请提供有效的node_imgs");
            }

            try
            {
                Debug.Log($"[FigmaManage] 启动异步节点数据拉取: {nodeIds.Count}个节点");
                // 使用ctx.AsyncReturn处理异步操作
                // 为长时间运行的网络请求设置更长的超时时间（120秒）
                return ctx.AsyncReturn(FetchNodesCoroutine(fileKey, nodeIds, token, includeChildren, null), 120f);
            }
            catch (Exception ex)
            {
                return Response.Error($"拉取节点数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量下载指定节点图片功能 - 按需下载指定的节点图片
        /// 
        /// 使用方式：
        /// 1. 在线下载：提供file_key和必需的node_id（多个用逗号分隔），直接从Figma API下载指定节点
        /// 2. 本地JSON下载：提供file_key、node_id和local_json_path，从本地JSON文件中读取指定节点数据并下载
        /// 
        /// 本地JSON文件格式支持：
        /// - FetchNodes保存的完整JSON格式（包含nodes字段）
        /// - 简化后的单节点JSON数据
        /// - 包含document字段的节点数据
        /// </summary>
        private object DownloadNodeImages(StateTreeContext ctx)
        {
            string fileKey = ctx["file_key"]?.ToString();
            string nodeImgsParam = ctx["node_imgs"]?.ToString();
            string localJsonPath = ctx["local_json_path"]?.ToString(); // 本地JSON文件路径
            string token = GetFigmaToken();
            string savePath = ctx["save_path"]?.ToString() ?? GetFigmaAssetsPath();
            string imageFormat = ctx["image_format"]?.ToString() ?? "png";
            float imageScale = float.Parse(ctx["image_scale"]?.ToString() ?? "2");

            if (string.IsNullOrEmpty(fileKey))
                return Response.Error("file_key是下载图片必需的参数");

            if (string.IsNullOrEmpty(nodeImgsParam))
                return Response.Error("node_imgs是下载图片必需的参数");

            if (string.IsNullOrEmpty(token))
                return Response.Error("Figma访问令牌未配置，请在Project Settings → MCP → Figma中配置");

            // 解析节点参数
            if (!ParseNodeParameters(nodeImgsParam, out List<string> nodeIds, out Dictionary<string, string> nodeNames))
            {
                return Response.Error("节点参数格式无效，请提供有效的node_id或node_imgs");
            }

            // 如果提供了本地JSON文件路径，则使用本地数据
            if (!string.IsNullOrEmpty(localJsonPath))
            {
                try
                {
                    Debug.Log($"[FigmaManage] 启动本地JSON文件指定节点下载: {localJsonPath}, 节点: {string.Join(", ", nodeIds)}");
                }
                catch (Exception ex)
                {
                    return Response.Error($"日志记录失败: {ex.Message}");
                }

                // 为长时间运行的下载操作设置更长的超时时间（180秒）
                return ctx.AsyncReturn(DownloadSpecificNodesFromLocalJsonCoroutine(fileKey, nodeIds, localJsonPath, token, savePath, imageFormat, imageScale, nodeNames), 180f);
            }
            else
            {
                try
                {
                    Debug.Log($"[FigmaManage] 启动指定节点下载: {string.Join(", ", nodeIds)}");
                }
                catch (Exception ex)
                {
                    return Response.Error($"日志记录失败: {ex.Message}");
                }

                // 为直接下载图片设置超时时间（180秒）
                return ctx.AsyncReturn(DownloadImagesDirectCoroutine(fileKey, nodeIds, token, savePath, imageFormat, imageScale, "SpecifiedNodes", nodeNames), 180f);
            }
        }

        /// <summary>
        /// 获取UI框架转换规则
        /// </summary>
        private object GetConversionRules(StateTreeContext ctx)
        {
            string uiFramework = ctx["ui_framework"]?.ToString() ?? "all";
            uiFramework = uiFramework.ToLower();

            try
            {
                // 复用 GetCoordinateSystemInfo 获取基础转换信息
                var baseInfo = GetCoordinateSystemInfo();
                var result = new JsonClass();

                // 从基础信息中提取数据
                var conversionFormulas = baseInfo["conversion_formulas"] as JsonClass;

                // 获取各个 UI 框架的专用提示词
                var figmaSettings = McpSettings.Instance?.figmaSettings;
                string uguiPrompt = figmaSettings?.GetPromptForUIType(UIType.UGUI) ?? "";
                string uitoolkitPrompt = figmaSettings?.GetPromptForUIType(UIType.UIToolkit) ?? "";

                // 构建 UGUI 规则
                var uguiRules = new JsonClass();
                uguiRules["framework"] = "Unity UGUI";
                uguiRules["coordinate_system"] = baseInfo["ugui_coordinate_system"];
                uguiRules["formulas"] = conversionFormulas["ugui"];

                var uguiSettings = new JsonClass();
                uguiSettings["anchor"] = "[0.5, 0.5] (中心锚点)";
                uguiSettings["pivot"] = "[0.5, 0.5] (中心轴点)";
                uguiSettings["clamp"] = "不启用 Clamp 选项";
                uguiSettings["canvas_pixel_perfect"] = "禁用 Pixel Perfect";
                uguiSettings["canvas_render_mode"] = "Screen Space - Overlay";
                uguiRules["recommended_settings"] = uguiSettings;
                uguiRules["conversion_prompt"] = uguiPrompt;

                // 构建 UIToolkit 规则
                var uitoolkitRules = new JsonClass();
                uitoolkitRules["framework"] = "Unity UI Toolkit";
                uitoolkitRules["coordinate_system"] = baseInfo["uitoolkit_coordinate_system"];
                uitoolkitRules["formulas"] = conversionFormulas["uitoolkit"];

                var uitoolkitSettings = new JsonClass();
                uitoolkitSettings["position"] = "absolute (使用 left/top/right/bottom)";
                uitoolkitSettings["note"] = "坐标系与 Figma 一致，无需 Y 轴翻转";
                uitoolkitRules["recommended_settings"] = uitoolkitSettings;
                uitoolkitRules["conversion_prompt"] = uitoolkitPrompt;

                // 根据参数返回相应的规则
                if (uiFramework == "ugui")
                {
                    result["framework"] = "ugui";
                    result["coordinate_system"] = baseInfo["ugui_coordinate_system"];
                    result["rules"] = uguiRules;
                    result["conversion_prompt"] = uguiPrompt;
                    return Response.Success("成功获取 UGUI 转换规则", result);
                }
                else if (uiFramework == "uitoolkit")
                {
                    result["framework"] = "uitoolkit";
                    result["coordinate_system"] = baseInfo["uitoolkit_coordinate_system"];
                    result["rules"] = uitoolkitRules;
                    result["conversion_prompt"] = uitoolkitPrompt;
                    return Response.Success("成功获取 UI Toolkit 转换规则", result);
                }
                else // all 或其他
                {
                    var allRules = new JsonClass();
                    allRules["ugui"] = uguiRules;
                    allRules["uitoolkit"] = uitoolkitRules;

                    var coordinateSystems = new JsonClass();
                    coordinateSystems["figma"] = baseInfo["figma_coordinate_system"];
                    coordinateSystems["ugui"] = baseInfo["ugui_coordinate_system"];
                    coordinateSystems["uitoolkit"] = baseInfo["uitoolkit_coordinate_system"];

                    // 为所有框架返回各自的提示词
                    var allPrompts = new JsonClass();
                    allPrompts["ugui"] = uguiPrompt;
                    allPrompts["uitoolkit"] = uitoolkitPrompt;

                    result["frameworks"] = new JsonArray { "ugui", "uitoolkit" };
                    result["rules"] = allRules;
                    result["coordinate_systems"] = coordinateSystems;
                    result["conversion_prompts"] = allPrompts;

                    return Response.Success("成功获取所有 UI 框架转换规则", result);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FigmaManage] 获取转换规则失败: {ex.Message}\n{ex.StackTrace}");
                return Response.Error($"获取转换规则失败: {ex.Message}");
            }
        }

        /// </summary>
        private object PreviewImage(StateTreeContext ctx)
        {
            string fileKey = ctx["file_key"]?.ToString();
            string nodeId = ctx["node_id"]?.ToString();
            string token = GetFigmaToken();
            string imageFormat = ctx["image_format"]?.ToString() ?? "png";
            float imageScale = float.Parse(ctx["image_scale"]?.ToString() ?? "1");

            if (string.IsNullOrEmpty(fileKey))
                return Response.Error("file_key是必需的参数");

            // 检查必要参数
            if (string.IsNullOrEmpty(nodeId))
                return Response.Error("必须提供 node_id 参数");

            if (string.IsNullOrEmpty(token))
                return Response.Error("Figma访问令牌未配置，请在Project Settings → MCP → Figma中配置");

            try
            {
                // 将节点ID中的"-"替换为":"（兼容URL中的格式）
                nodeId = nodeId.Replace("-", ":");

                Debug.Log($"[FigmaManage] 启动图片预览: 节点{nodeId}");

                // 使用异步协程下载图片并转换为base64
                // 为图片预览设置超时时间（90秒）
                return ctx.AsyncReturn(PreviewImageCoroutine(fileKey, nodeId, token, imageFormat, imageScale), 90f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FigmaManage] 预览图片异常: {ex.Message}\n{ex.StackTrace}");
                return Response.Error($"预览图片失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接下载图片协程 - 不获取节点数据，直接下载图片（使用提供的节点名称映射）
        /// </summary>
        private IEnumerator DownloadImagesDirectCoroutine(string fileKey, List<string> nodeIds, string token, string savePath, string imageFormat, float imageScale, string rootNodeName = "DirectDownload", Dictionary<string, string> nodeNamesDict = null)
        {
            Dictionary<string, string> imageLinks = null;

            // 直接获取图片链接，不获取节点数据
            yield return GetImageLinksCoroutine(fileKey, nodeIds, token, imageFormat, imageScale, (links) =>
            {
                imageLinks = links;
            });

            if (imageLinks == null || imageLinks.Count == 0)
            {
                yield return Response.Error("获取图片链接失败或没有找到图片");
                yield break;
            }

            // 使用提供的节点名称映射或节点ID作为文件名
            var nodeNames = nodeNamesDict ?? nodeIds.ToDictionary(id => id, id => id);

            // 下载图片文件
            yield return DownloadImageFilesCoroutine(imageLinks, nodeNames, savePath, imageFormat, fileKey, imageScale, rootNodeName);
        }

        /// <summary>
        /// 从本地JSON文件下载指定节点协程 - 从本地JSON文件中读取指定节点数据并下载图片
        /// </summary>
        private IEnumerator DownloadSpecificNodesFromLocalJsonCoroutine(string fileKey, List<string> nodeIds, string localJsonPath, string token, string savePath, string imageFormat, float imageScale, Dictionary<string, string> nodeNamesDict = null)
        {
            // 检查本地JSON文件是否存在
            if (!File.Exists(localJsonPath))
            {
                yield return Response.Error($"本地JSON文件不存在: {localJsonPath}");
                yield break;
            }

            Debug.Log($"[FigmaManage] 从本地JSON文件下载指定节点: {string.Join(", ", nodeIds)}");

            // 如果提供了节点名称映射，直接使用；否则使用节点ID作为文件名
            if (nodeNamesDict != null)
            {
                // 直接下载，使用提供的节点名称映射
                yield return DownloadImagesDirectCoroutine(fileKey, nodeIds, token, savePath, imageFormat, imageScale, "LocalJsonSpecificNodes", nodeNamesDict);
            }
            else
            {
                // 使用原来的方式，先获取节点数据再下载
                yield return DownloadImagesCoroutine(fileKey, nodeIds, token, savePath, imageFormat, imageScale, "LocalJsonSpecificNodes");
            }
        }

        /// <summary>
        /// 预览图片协程 - 直接下载图片并转换为base64格式（简化版）
        /// </summary>
        private IEnumerator PreviewImageCoroutine(string fileKey, string nodeId, string token, string imageFormat, float imageScale)
        {
            EditorUtility.DisplayProgressBar("预览Figma图片", $"正在获取图片: 节点 {nodeId}...", 0.1f);

            // 第一步：直接通过Figma API获取图片URL
            string apiUrl = $"{FIGMA_API_BASE}/images/{fileKey}?ids={nodeId}&format={imageFormat}&scale={imageScale}";
            UnityWebRequest apiRequest = UnityWebRequest.Get(apiUrl);
            apiRequest.SetRequestHeader("X-Figma-Token", token);
            apiRequest.timeout = 15;

            yield return apiRequest.SendWebRequest();

            if (apiRequest.result != UnityWebRequest.Result.Success)
            {
                apiRequest.Dispose();
                EditorUtility.ClearProgressBar();
                yield return Response.Error($"获取图片URL失败: {apiRequest.error}");
                yield break;
            }

            // 解析响应获取图片URL
            string imageUrl = null;
            string parseError = null;

            try
            {
                var response = Json.Parse(apiRequest.downloadHandler.text) as JsonClass;
                var imagesNode = response["images"] as JsonClass;
                if (imagesNode != null && imagesNode.ContainsKey(nodeId))
                {
                    imageUrl = imagesNode[nodeId]?.Value;
                }
                else
                {
                    parseError = "响应中未找到图片URL";
                }
            }
            catch (Exception ex)
            {
                parseError = $"解析响应失败: {ex.Message}";
            }

            apiRequest.Dispose();

            if (!string.IsNullOrEmpty(parseError) || string.IsNullOrEmpty(imageUrl))
            {
                EditorUtility.ClearProgressBar();
                yield return Response.Error(parseError ?? "图片URL为空");
                yield break;
            }

            // 第二步：下载图片
            EditorUtility.DisplayProgressBar("预览Figma图片", $"正在下载图片: 节点 {nodeId}...", 0.4f);

            UnityWebRequest imageRequest = UnityWebRequest.Get(imageUrl);
            imageRequest.timeout = 30;

            yield return imageRequest.SendWebRequest();

            if (imageRequest.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = imageRequest.error;
                imageRequest.Dispose();
                EditorUtility.ClearProgressBar();
                yield return Response.Error($"下载图片失败: {errorMsg}");
                yield break;
            }

            // 第三步：获取图片数据并压缩
            EditorUtility.DisplayProgressBar("预览Figma图片", "正在处理图片数据...", 0.6f);

            byte[] imageData = imageRequest.downloadHandler.data;
            imageRequest.Dispose();

            // 检查图片大小
            if (imageData == null || imageData.Length == 0)
            {
                EditorUtility.ClearProgressBar();
                yield return Response.Error("下载的图片数据为空");
                yield break;
            }

            if (imageData.Length > 5 * 1024 * 1024)
            {
                EditorUtility.ClearProgressBar();
                yield return Response.Error($"图片过大 ({imageData.Length / (1024 * 1024)}MB)，超过5MB限制");
                yield break;
            }

            // 第四步：将图片加载为Texture2D并压缩
            EditorUtility.DisplayProgressBar("预览Figma图片", "正在压缩图片...", 0.7f);

            Texture2D originalTexture = new Texture2D(2, 2);
            bool loadSuccess = false;
            string loadError = null;

            try
            {
                loadSuccess = originalTexture.LoadImage(imageData);
            }
            catch (Exception ex)
            {
                loadError = $"加载图片失败: {ex.Message}";
                Object.DestroyImmediate(originalTexture);
            }

            if (!string.IsNullOrEmpty(loadError))
            {
                EditorUtility.ClearProgressBar();
                yield return Response.Error(loadError);
                yield break;
            }

            if (!loadSuccess)
            {
                Object.DestroyImmediate(originalTexture);
                EditorUtility.ClearProgressBar();
                yield return Response.Error("加载图片失败");
                yield break;
            }

            // 记录原始尺寸
            int originalWidth = originalTexture.width;
            int originalHeight = originalTexture.height;

            // 计算压缩后的尺寸（保持宽高比，最大尺寸由设置决定）
            int newWidth = originalWidth;
            int newHeight = originalHeight;
            int maxSize = GetPreviewMaxSize();

            if (originalWidth > maxSize || originalHeight > maxSize)
            {
                float ratio = Mathf.Min((float)maxSize / originalWidth, (float)maxSize / originalHeight);
                newWidth = Mathf.RoundToInt(originalWidth * ratio);
                newHeight = Mathf.RoundToInt(originalHeight * ratio);
            }

            // 创建压缩后的纹理
            Texture2D compressedTexture = null;
            byte[] compressedData = null;
            string compressionError = null;

            try
            {
                // 如果需要缩放
                if (newWidth != originalWidth || newHeight != originalHeight)
                {
                    // 创建RenderTexture进行高质量缩放
                    RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
                    RenderTexture.active = rt;

                    // 使用Graphics.Blit进行缩放
                    Graphics.Blit(originalTexture, rt);

                    // 创建新纹理并读取像素
                    compressedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                    compressedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                    compressedTexture.Apply();

                    // 清理
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);
                }
                else
                {
                    // 不需要缩放，直接使用原图
                    compressedTexture = originalTexture;
                }

                // 根据格式编码
                if (imageFormat.ToLower() == "jpg" || imageFormat.ToLower() == "jpeg")
                {
                    compressedData = compressedTexture.EncodeToJPG(85); // 85%质量
                }
                else
                {
                    compressedData = compressedTexture.EncodeToPNG();
                }
            }
            catch (Exception ex)
            {
                compressionError = $"压缩图片失败: {ex.Message}";
            }
            finally
            {
                // 清理纹理对象
                if (compressedTexture != null && compressedTexture != originalTexture)
                {
                    Object.DestroyImmediate(compressedTexture);
                }
                if (originalTexture != null)
                {
                    Object.DestroyImmediate(originalTexture);
                }
            }

            if (!string.IsNullOrEmpty(compressionError) || compressedData == null)
            {
                EditorUtility.ClearProgressBar();
                yield return Response.Error(compressionError ?? "压缩图片失败");
                yield break;
            }

            // 第五步：转换为base64
            EditorUtility.DisplayProgressBar("预览Figma图片", "正在生成预览...", 0.9f);

            string base64String = null;
            string conversionError = null;

            try
            {
                base64String = Convert.ToBase64String(compressedData);
            }
            catch (Exception ex)
            {
                conversionError = $"转换base64失败: {ex.Message}";
            }

            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(conversionError))
            {
                yield return Response.Error(conversionError);
                yield break;
            }

            // 保存图片到预览路径
            string previewPath = GetFigmaPreviewPath();
            string savedFilePath = null;
            string assetPath = null;
            bool convertedToSprite = false;

            try
            {
                // 确保目录存在
                if (!Directory.Exists(previewPath))
                {
                    Directory.CreateDirectory(previewPath);
                }

                // 生成文件名：节点ID_时间戳.格式
                string fileName = $"{nodeId.Replace(":", "_")}_preview.{imageFormat.ToLower()}";
                savedFilePath = Path.Combine(previewPath, fileName);

                // 保存文件
                File.WriteAllBytes(savedFilePath, compressedData);

                // 获取相对于Assets的路径
                assetPath = savedFilePath;
                if (savedFilePath.StartsWith(Application.dataPath))
                {
                    assetPath = "Assets" + savedFilePath.Substring(Application.dataPath.Length);
                }

                // 刷新资源数据库
                AssetDatabase.Refresh();

                // 如果配置了自动转换为Sprite，则进行转换
                if (GetAutoConvertToSprite() && !string.IsNullOrEmpty(assetPath))
                {
                    ConvertToSprite(assetPath);
                    convertedToSprite = true;
                    Debug.Log($"[FigmaManage] 预览图已转换为Sprite: {assetPath}");
                }

                Debug.Log($"[FigmaManage] 预览图已保存到: {savedFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FigmaManage] 保存预览图失败: {ex.Message}");
            }

            // 返回成功响应
            string mimeType = $"image/{imageFormat.ToLower()}";
            string dataUrl = $"data:{mimeType};base64,{base64String}";

            yield return Response.Success($"图片预览成功: 节点{nodeId}", new
            {
                node_id = nodeId,
                image_format = imageFormat,
                image = dataUrl,  // ✅ 改为 image，Cursor 更容易识别
                mime_type = mimeType,
                original_size = new { width = originalWidth, height = originalHeight },
                compressed_size = new { width = newWidth, height = newHeight },
                data_size = compressedData.Length,
                saved_path = savedFilePath,
                asset_path = assetPath,
                converted_to_sprite = convertedToSprite,
                preview_path = previewPath
            });
        }

        /// <summary>
        /// 检查文件是否属于McpUISettings中配置的公共sprite或texture目录，
        /// 通过content hash识别相同的图片，如找到则返回已存在文件的路径。
        /// </summary>
        /// <param name="filePath">当前图片本地路径</param>
        /// <param name="imageData">图片的二进制数据（用于计算hash）</param>
        /// <param name="remappedPath">重定向后的路径（或原始路径）</param>
        /// <returns>是否被重定向</returns>
        private bool TryRemapPathIfCommon(string filePath, byte[] imageData, out string remappedPath)
        {
            remappedPath = filePath;

            // 加载McpUISettings
            var settings = Unity.Mcp.McpSettings.Instance?.uiSettings;
            if (settings == null)
                return false;

            // 获取所有可能的公共目录（sprite 和 texture）
            var commonFolders = new List<string>();
            if (settings.commonSpriteFolders != null)
                commonFolders.AddRange(settings.commonSpriteFolders);
            if (settings.commonTextureFolders != null)
                commonFolders.AddRange(settings.commonTextureFolders);
            // 将filePath所在的文件夹也加入到commonFolders（如果不为空且未重复）
            string fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !commonFolders.Contains(fileDir))
                commonFolders.Add(fileDir);
            if (commonFolders.Count == 0)
                return false;

            // 计算当前图片的content hash
            string currentHash = CalculateBatesHash(imageData);
            string currentExtension = Path.GetExtension(filePath).ToLower();

            // 遍历所有公共目录，查找具有相同hash的文件
            foreach (var folder in commonFolders)
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    continue;

                try
                {
                    // 获取该目录下所有相同扩展名的文件
                    string searchPattern = $"*{currentExtension}";
                    string[] existingFiles = Directory.GetFiles(folder, searchPattern, SearchOption.TopDirectoryOnly);

                    foreach (string existingFile in existingFiles)
                    {
                        try
                        {
                            // 从文件名中提取hash（格式：名字_hash.扩展名）
                            string existingFileName = Path.GetFileNameWithoutExtension(existingFile);
                            int lastUnderscoreIndex = existingFileName.LastIndexOf('_');

                            if (lastUnderscoreIndex > 0 && lastUnderscoreIndex < existingFileName.Length - 1)
                            {
                                string existingHash = existingFileName.Substring(lastUnderscoreIndex + 1);

                                // 比较hash值
                                if (existingHash.Equals(currentHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    // 找到相同hash的文件，重定向
                                    remappedPath = existingFile;
                                    Debug.Log($"[FigmaManage] 检测到相同内容的图片，重定向: {Path.GetFileName(filePath)} -> {Path.GetFileName(existingFile)}");
                                    return true;
                                }
                            }
                            else
                            {
                                // 如果文件名格式不符合预期，直接计算文件的hash进行比较（兼容性处理）
                                byte[] existingData = File.ReadAllBytes(existingFile);
                                string existingHash = CalculateBatesHash(existingData);

                                if (existingHash.Equals(currentHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    // 找到相同hash的文件，重定向
                                    remappedPath = existingFile;
                                    Debug.Log($"[FigmaManage] 检测到相同内容的图片（通过文件比对），重定向: {Path.GetFileName(filePath)} -> {Path.GetFileName(existingFile)}");
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 单个文件处理失败不影响其他文件
                            Debug.LogWarning($"[FigmaManage] 检查文件失败: {existingFile}, 错误: {ex.Message}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FigmaManage] 扫描公共目录失败: {folder}, 错误: {ex.Message}");
                    continue;
                }
            }

            // 不在公共目录或未找到相同hash的文件
            return false;
        }


        /// <summary>
        /// 下载图片文件协程 - 核心下载逻辑
        /// </summary>
        private IEnumerator DownloadImageFilesCoroutine(Dictionary<string, string> imageLinks, Dictionary<string, string> nodeNames, string savePath, string imageFormat, string fileKey, float imageScale, string rootNodeName)
        {
            // 下载图片文件
            int downloadedCount = 0;
            int totalCount = imageLinks.Count;

            // 创建索引信息字典：node ID -> 文件名
            var nodeIndexMapping = new Dictionary<string, string>();
            // 创建下载成功的文件路径列表，用于后续统一处理
            var downloadedFiles = new List<string>();
            // 用于记录是否被取消
            bool operationCancelled = false;

            foreach (var kvp in imageLinks)
            {
                // 检查是否已被取消
                if (operationCancelled)
                {
                    break;
                }

                string nodeId = kvp.Key;
                string imageUrl = kvp.Value;
                string nodeName = nodeNames?.ContainsKey(nodeId) == true ? nodeNames[nodeId] : nodeId;

                if (string.IsNullOrEmpty(imageUrl))
                {
                    Debug.LogWarning($"节点 {nodeId} 的图片URL为空，跳过下载");
                    continue;
                }

                UnityWebRequest imageRequest = UnityWebRequest.Get(imageUrl);
                var operation = imageRequest.SendWebRequest();

                // 监听下载进度
                float lastProgressUpdate = 0f;
                bool downloadCancelled = false;
                while (!operation.isDone && !downloadCancelled)
                {
                    float currentTime = Time.realtimeSinceStartup;
                    if (currentTime - lastProgressUpdate >= 1f) // 每1秒更新一次
                    {
                        float progress = operation.progress;
                        float totalProgress = ((float)downloadedCount + progress) / totalCount;
                        downloadCancelled = EditorUtility.DisplayCancelableProgressBar("下载Figma图片",
                            $"正在下载节点 {nodeId} {nodeName} ({downloadedCount + 1}/{totalCount}) - {progress:P0}\n\n点击取消可中止操作",
                            totalProgress);
                        lastProgressUpdate = currentTime;

                        // 如果被取消，设置整体操作取消标志
                        if (downloadCancelled)
                        {
                            operationCancelled = true;
                        }
                    }
                    yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
                }

                // 处理取消操作
                if (downloadCancelled || operationCancelled)
                {
                    imageRequest.Abort();
                    imageRequest.Dispose();
                    break;
                }

                if (imageRequest.result == UnityWebRequest.Result.Success)
                {
                    // 清理文件名中的无效字符
                    nodeName = SanitizeFileName(nodeName);

                    // 计算文件内容hash
                    byte[] imageData = imageRequest.downloadHandler.data;
                    string contentHash = CalculateBatesHash(imageData);

                    // 按照 名字_hash.扩展名 格式命名
                    string fileExtension = imageFormat.ToLower();
                    string fileName = $"{nodeName}_{contentHash}.{fileExtension}";
                    string filePath = Path.Combine(savePath, fileName);
                    string relativePath = Path.GetRelativePath(Application.dataPath, filePath).Replace("\\", "/");
                    if (!relativePath.StartsWith("../"))
                    {
                        relativePath = "Assets/" + relativePath;
                    }
                    if (TryRemapPathIfCommon(filePath, imageData, out string remappedPath))
                    {
                        // 找到了相同内容的图片，使用已存在的文件
                        filePath = remappedPath;
                        fileName = Path.GetFileName(remappedPath);

                        // 记录索引信息：使用重定向后的文件名
                        nodeIndexMapping[nodeId] = filePath;

                        // 获取重定向后文件的相对路径
                        string remappedRelativePath = Path.GetRelativePath(Application.dataPath, remappedPath).Replace("\\", "/");
                        if (!remappedRelativePath.StartsWith("../"))
                        {
                            remappedRelativePath = "Assets/" + remappedRelativePath;
                        }
                        downloadedFiles.Add(remappedRelativePath);

                        downloadedCount++;
                        Debug.Log($"[FigmaManage] 图片已重定向到已存在文件: {remappedPath}");
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.WriteAllBytes(filePath, imageData);
                        downloadedCount++;

                        // 记录索引信息：只保存文件名
                        nodeIndexMapping[nodeId] = filePath;
                        // 记录下载成功的文件相对路径，用于后续转换为Sprite
                        downloadedFiles.Add(relativePath);

                        Debug.Log($"[FigmaManage] 成功下载图片: {filePath}");
                    }
                }
                else
                {
                    Debug.LogError($"下载图片失败 (节点: {nodeId}): {imageRequest.error}");
                }

                imageRequest.Dispose();
            }

            EditorUtility.ClearProgressBar();

            // 检查是否被取消
            if (operationCancelled)
            {
                Debug.LogWarning($"[FigmaManage] 用户取消了图片下载操作，已下载 {downloadedCount}/{totalCount} 个文件");

                // 即使被取消，也要处理已下载的文件
                if (downloadedCount > 0)
                {
                    AssetDatabase.Refresh();

                    // 保存已下载的索引信息
                    string partialIndexFilePath = SaveIndexToFile(fileKey, nodeIndexMapping, savePath, imageScale, rootNodeName);

                    // 转换为Sprite（如果需要）
                    if (GetAutoConvertToSprite())
                    {
                        ConvertDownloadedImagesToSprites(downloadedFiles);
                    }

                    yield return Response.Success($"操作被取消，但已成功下载 {downloadedCount} 个文件", new
                    {
                        downloaded_count = downloadedCount,
                        total_count = totalCount,
                        cancelled = true,
                        save_path = savePath,
                        node_index_mapping = Json.FromObject(nodeIndexMapping),
                        index_file_path = partialIndexFilePath
                    });
                }
                else
                {
                    yield return Response.Error("用户取消了图片下载操作，未下载任何文件");
                }
                yield break;
            }

            // 所有图片下载完成后，统一刷新资源数据库
            Debug.Log($"[FigmaManage] 图片下载完成，开始统一处理后续操作...");
            AssetDatabase.Refresh();

            // 统一保存索引信息到文件
            string indexFilePath = null;
            if (downloadedCount > 0)
            {
                Debug.Log($"[FigmaManage] 保存索引信息到文件");
                indexFilePath = SaveIndexToFile(fileKey, nodeIndexMapping, savePath, imageScale, rootNodeName);
            }

            // 统一转换下载的图片为Sprite格式（根据配置决定）
            if (downloadedCount > 0 && GetAutoConvertToSprite())
            {
                Debug.Log($"[FigmaManage] 开始批量转换 {downloadedFiles.Count} 个图片为Sprite格式");
                bool conversionCompleted = ConvertDownloadedImagesToSprites(downloadedFiles);

                // 更新消息以反映转换状态
                string message = GetAutoConvertToSprite()
                              ? $"图片下载并转换为Sprite完成，成功处理 {downloadedCount}/{totalCount} 个文件"
                              : $"图片下载完成，成功下载 {downloadedCount}/{totalCount} 个文件";

                if (!conversionCompleted)
                {
                    message = $"图片下载完成，但Sprite转换被取消。成功下载 {downloadedCount}/{totalCount} 个文件";
                }

                yield return Response.Success(message, new
                {
                    downloaded_count = downloadedCount,
                    total_count = totalCount,
                    save_path = savePath,
                    node_index_mapping = Json.FromObject(nodeIndexMapping),
                    index_file_path = indexFilePath
                });
            }
            else
            {
                string message = $"图片下载完成，成功下载 {downloadedCount}/{totalCount} 个文件";
                yield return Response.Success(message, new
                {
                    downloaded_count = downloadedCount,
                    total_count = totalCount,
                    save_path = savePath,
                    node_index_mapping = Json.FromObject(nodeIndexMapping),
                    index_file_path = indexFilePath
                });
            }
        }

        /// <summary>
        /// 下载图片协程
        /// </summary>
        private IEnumerator DownloadImagesCoroutine(string fileKey, List<string> nodeIds, string token, string savePath, string imageFormat, float imageScale, string rootNodeName = "UnknownNode")
        {
            Dictionary<string, string> imageLinks = null;
            Dictionary<string, string> nodeNames = null;

            // 首先获取节点信息（包含名称）
            yield return FetchNodesCoroutine(fileKey, nodeIds, token, false, null, (nodes) =>
            {
                nodeNames = nodes;
            });

            // 然后获取图片链接
            yield return GetImageLinksCoroutine(fileKey, nodeIds, token, imageFormat, imageScale, (links) =>
            {
                imageLinks = links;
            });

            if (imageLinks == null || imageLinks.Count == 0)
            {
                yield return Response.Error("获取图片链接失败或没有找到图片");
                yield break;
            }

            // 使用新的下载文件协程
            yield return DownloadImageFilesCoroutine(imageLinks, nodeNames, savePath, imageFormat, fileKey, imageScale, rootNodeName);
        }

        /// <summary>
        /// 获取图片链接协程
        /// </summary>
        /// <param name="fileKey">Figma文件密钥</param>
        /// <param name="nodeIds">节点ID列表</param>
        /// <param name="token">访问令牌</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="imageScale">图片缩放比例</param>
        /// <param name="callback">回调函数，接收图片链接字典</param>
        /// <param name="isPreview">是否是预览模式，预览模式下会使用更高效的处理方式</param>
        private IEnumerator GetImageLinksCoroutine(string fileKey, List<string> nodeIds, string token, string imageFormat, float imageScale, System.Action<Dictionary<string, string>> callback, bool isPreview = false)
        {
            // 如果是预览模式且只有一个节点，使用更直接的API调用
            if (isPreview && nodeIds.Count == 1)
            {
                string nodeId = nodeIds[0];
                string previewUrl = $"{FIGMA_API_BASE}/images/{fileKey}?ids={nodeId}&format={imageFormat}&scale={imageScale}";

                UnityWebRequest previewRequest = UnityWebRequest.Get(previewUrl);
                previewRequest.SetRequestHeader("X-Figma-Token", token);
                previewRequest.timeout = 15; // 设置15秒超时

                yield return previewRequest.SendWebRequest();

                if (previewRequest.result == UnityWebRequest.Result.Success)
                {
                    Dictionary<string, string> images = null;
                    bool success = false;
                    string errorMessage = null;

                    try
                    {
                        var response = Json.Parse(previewRequest.downloadHandler.text) as JsonClass;
                        var imagesNode = response["images"] as JsonClass;
                        images = new Dictionary<string, string>();

                        if (imagesNode != null && imagesNode.ContainsKey(nodeId))
                        {
                            images[nodeId] = imagesNode[nodeId]?.Value;
                            success = true;
                        }
                        else
                        {
                            errorMessage = $"[FigmaManage] 预览模式: 获取节点 {nodeId} 的图片链接失败，节点不存在或无法导出";
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessage = $"[FigmaManage] 解析图片链接响应失败: {ex.Message}";
                    }

                    // try-catch块外调用回调和输出日志
                    if (success)
                    {
                        Debug.Log($"[FigmaManage] 预览模式: 成功获取节点 {nodeId} 的图片链接");
                        callback?.Invoke(images);
                    }
                    else
                    {
                        Debug.LogError(errorMessage);
                        callback?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[FigmaManage] 获取图片链接失败: {previewRequest.error}");
                    callback?.Invoke(null);
                }

                previewRequest.Dispose();
                yield break;
            }

            // 标准模式处理多个节点
            string nodeIdsStr = string.Join(",", nodeIds);
            string url = $"{FIGMA_API_BASE}/images/{fileKey}?ids={nodeIdsStr}&format={imageFormat}&scale={imageScale}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("X-Figma-Token", token);

            var operation = request.SendWebRequest();

            // 监听获取图片链接的进度
            float lastProgressUpdate = 0f;
            bool cancelled = false;
            while (!operation.isDone && !cancelled)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastProgressUpdate >= 0.5f) // 每0.5秒更新一次，提高响应速度
                {
                    cancelled = EditorUtility.DisplayCancelableProgressBar("获取Figma图片链接",
                        $"正在获取图片链接... {operation.progress:P0}\n\n点击取消可中止操作",
                        operation.progress);
                    lastProgressUpdate = currentTime;
                }
                yield return new WaitForSeconds(0.05f); // 每0.05秒检查一次，提高响应速度
            }

            // 处理取消操作
            if (cancelled)
            {
                request.Abort();
                EditorUtility.ClearProgressBar();
                Debug.LogWarning("[FigmaManage] 用户取消了获取图片链接操作");
                callback?.Invoke(null);
                request.Dispose();
                yield break;
            }

            EditorUtility.ClearProgressBar();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Dictionary<string, string> images = null;
                bool success = false;
                string errorMessage = null;
                int imageCount = 0;

                try
                {
                    var response = Json.Parse(request.downloadHandler.text) as JsonClass;
                    var imagesNode = response["images"] as JsonClass;
                    images = new Dictionary<string, string>();
                    if (imagesNode != null)
                    {
                        foreach (var key in imagesNode.Keys)
                        {
                            images[key] = imagesNode[key]?.Value;
                        }
                        imageCount = images.Count;
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"[FigmaManage] 解析图片链接响应失败: {ex.Message}";
                }

                // try-catch块外调用回调和输出日志
                if (success)
                {
                    Debug.Log($"[FigmaManage] 成功获取{imageCount}个图片链接");
                    callback?.Invoke(images);
                }
                else
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Debug.LogError(errorMessage);
                    }
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[FigmaManage] 获取图片链接失败: url:{url}, error:{request.error}");
                callback?.Invoke(null);
            }

            request.Dispose();
        }

        /// <summary>
        /// 拉取节点数据协程
        /// 
        /// 支持两种模式：
        /// 1. 标准模式（rootNodeIdForScan为null）：拉取指定节点列表的数据
        /// 2. 根节点扫描模式（rootNodeIdForScan不为null）：从根节点扫描所有子节点并拉取数据
        /// </summary>
        /// <param name="fileKey">Figma文件Key</param>
        /// <param name="nodeIds">节点ID列表</param>
        /// <param name="token">访问令牌</param>
        /// <param name="includeChildren">是否包含子节点</param>
        /// <param name="rootNodeIdForScan">根节点ID（用于智能扫描模式）</param>
        /// <param name="nodeNamesCallback">节点名称回调</param>
        /// <param name="fullDataCallback">完整数据回调</param>
        private IEnumerator FetchNodesCoroutine(string fileKey, List<string> nodeIds, string token, bool includeChildren, string rootNodeIdForScan = null, System.Action<Dictionary<string, string>> nodeNamesCallback = null, System.Action<JsonNode> fullDataCallback = null)
        {
            string nodeIdsStr = string.Join(",", nodeIds);
            string url = $"{FIGMA_API_BASE}/files/{fileKey}/nodes?ids={nodeIdsStr}&geometry=paths";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("X-Figma-Token", token);

            var operation = request.SendWebRequest();

            // 监听节点数据拉取进度
            float lastProgressUpdate = 0f;
            bool cancelled = false;
            while (!operation.isDone && !cancelled)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastProgressUpdate >= 1f) // 每1秒更新一次
                {
                    cancelled = EditorUtility.DisplayCancelableProgressBar("拉取Figma节点数据",
                        $"正在拉取 {nodeIds.Count} 个节点数据... {operation.progress:P0}\n\n点击取消可中止操作",
                        operation.progress);
                    lastProgressUpdate = currentTime;
                }
                yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
            }

            // 处理取消操作
            if (cancelled)
            {
                request.Abort();
                EditorUtility.ClearProgressBar();
                yield return Response.Error("用户取消了拉取Figma节点数据操作");
                request.Dispose();
                yield break;
            }

            EditorUtility.ClearProgressBar();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = Json.Parse(request.downloadHandler.text);
                var nodes = response["nodes"];

                if (nodes != null)
                {
                    // 提取节点名称
                    var nodeNamesDict = new Dictionary<string, string>();
                    foreach (var nodeId in nodeIds)
                    {
                        var nodeData = nodes[nodeId];
                        if (nodeData != null)
                        {
                            var document = nodeData["document"];
                            if (document != null)
                            {
                                string nodeName = document["name"]?.Value ?? nodeId;
                                // 清理文件名中的无效字符
                                nodeName = SanitizeFileName(nodeName);
                                nodeNamesDict[nodeId] = nodeName;
                            }
                        }
                    }

                    // 如果有回调，调用回调返回节点名称
                    nodeNamesCallback?.Invoke(nodeNamesDict);

                    // 如果有完整数据回调，调用回调返回完整响应
                    fullDataCallback?.Invoke(response);

                    // 将节点数据保存到文件（如果没有回调，说明是正常的fetch_nodes调用）
                    if (nodeNamesCallback == null && fullDataCallback == null)
                    {
                        var nodesObject = nodes as JsonClass ?? new JsonClass();

                        // 简化节点数据
                        var simplifiedNodes = FigmaDataSimplifier.SimplifyNodes(nodesObject);
                        // 保存原始数据和简化数据
                        string assetsPath = GetFigmaAssetsPath();
                        // 用 nodeIds 拼接并将不能作路径的特殊符号转换为下划线
                        string nodeIdsJoined = string.Join("_", nodeIds).Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_').Replace(":", "_").Replace("*", "_").Replace("?", "_").Replace("\"", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_");
                        string originalPath = Path.Combine(assetsPath, $"original_nodes_{fileKey}_{nodeIdsJoined}.json");
                        string simplifiedPath = Path.Combine(assetsPath, $"simplified_nodes_{fileKey}_{nodeIdsJoined}.json");
                        Directory.CreateDirectory(Path.GetDirectoryName(simplifiedPath));
                        File.WriteAllText(originalPath, Json.FromObject(nodesObject));
                        string simplifiedJson = simplifiedNodes.ToString();
                        File.WriteAllText(simplifiedPath, simplifiedJson);

                        AssetDatabase.Refresh();

                        string scanModeInfo = !string.IsNullOrEmpty(rootNodeIdForScan) ? $"（根节点扫描模式: {rootNodeIdForScan}）" : "";

                        Debug.Log($"节点数据拉取完成{scanModeInfo}，共{nodeIds.Count}个节点");
                        Debug.Log($"简化数据: {simplifiedPath}");

                        var responseData = new JsonClass();
                        responseData["file_key"] = fileKey;
                        responseData["node_count"] = nodeIds.Count;
                        responseData["simplified_path"] = simplifiedPath;
                        responseData["include_children"] = includeChildren;
                        responseData["scan_mode"] = !string.IsNullOrEmpty(rootNodeIdForScan);
                        responseData["node_id"] = rootNodeIdForScan;
                        responseData["simplified_data"] = simplifiedNodes;

                        yield return Response.Success($"节点数据拉取完成{scanModeInfo}，共{nodeIds.Count}个节点", responseData);
                    }
                }
                else
                {
                    yield return Response.Error("响应中没有找到nodes数据");
                }
            }
            else
            {
                Debug.LogError($"获取节点数据失败: {request.error}");
                yield return Response.Error($"获取节点数据失败: {request.error}");
            }

            request.Dispose();
        }



        /// <summary>
        /// 解析节点参数，支持node_id（单个节点ID）和node_imgs（JSON格式的名称映射）
        /// </summary>
        /// <param name="nodeIdParam">node_id参数值（单个节点ID）</param>
        /// <param name="nodeImgsParam">node_imgs参数值（JSON格式的名称映射）</param>
        /// <param name="nodeIds">输出：节点ID列表</param>
        /// <param name="nodeNames">输出：节点名称映射（如果提供了node_imgs）</param>
        /// <returns>是否解析成功</returns>
        private bool ParseNodeParameters(string nodeImgsParam, out List<string> nodeIds, out Dictionary<string, string> nodeNames)
        {
            nodeIds = new List<string>();
            nodeNames = null;
            // 优先解析node_imgs（JSON格式）
            if (!string.IsNullOrEmpty(nodeImgsParam))
            {
                if (nodeImgsParam.Trim().StartsWith("{") && nodeImgsParam.Trim().EndsWith("}"))
                {
                    try
                    {
                        var jsonObj = Json.Parse(nodeImgsParam) as JsonClass;
                        if (jsonObj != null)
                        {
                            nodeNames = new Dictionary<string, string>();
                            foreach (KeyValuePair<string, JsonNode> kvp in jsonObj.AsEnumerable())
                            {
                                nodeNames[kvp.Key] = kvp.Value.Value;
                            }
                        }
                        if (nodeNames != null && nodeNames.Count > 0)
                        {
                            nodeIds = nodeNames.Keys.ToList();
                            Debug.Log($"[FigmaManage] 解析node_imgs为JSON格式，包含 {nodeNames.Count} 个节点映射:{string.Join(",", nodeNames.Keys.ToList())}");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FigmaManage] node_imgs JSON格式解析失败: {ex.Message}");
                        // 继续尝试解析node_ids
                    }
                }
            }
            // 如果两个参数都未能解析出有效数据
            return false;
        }

        /// <summary>
        /// 将下载的图片转换为Sprite格式
        /// </summary>
        private void ConvertToSprite(string assetPath)
        {
            try
            {
                // 获取纹理导入器
                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter != null)
                {
                    // 设置纹理类型为Sprite
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteImportMode = SpriteImportMode.Single;

                    // 设置其他Sprite相关属性
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.filterMode = FilterMode.Bilinear;

                    // 设置压缩格式（可选，根据需要调整）
                    var platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
                    platformSettings.format = TextureImporterFormat.RGBA32; // 保持高质量
                    textureImporter.SetPlatformTextureSettings(platformSettings);

                    // 应用导入设置
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                    Debug.Log($"[FigmaManage] 成功将图片转换为Sprite: {assetPath}");
                }
                else
                {
                    Debug.LogWarning($"[FigmaManage] 无法获取纹理导入器: {assetPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FigmaManage] 转换Sprite失败: {assetPath}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量转换下载的图片为Sprite格式
        /// </summary>
        private bool ConvertDownloadedImagesToSprites(List<string> downloadedFiles)
        {
            try
            {
                Debug.Log($"[FigmaManage] 开始批量转换 {downloadedFiles.Count} 个图片为Sprite格式");

                int convertedCount = 0;
                int totalCount = downloadedFiles.Count;
                bool cancelled = false;

                foreach (var relativePath in downloadedFiles)
                {
                    string fileName = Path.GetFileName(relativePath);

                    // 显示可取消的进度条
                    float progress = (float)convertedCount / totalCount;
                    cancelled = EditorUtility.DisplayCancelableProgressBar("转换图片为Sprite",
                        $"正在转换 {fileName} ({convertedCount + 1}/{totalCount})\n\n点击取消可中止操作",
                        progress);

                    // 检查是否被取消
                    if (cancelled)
                    {
                        Debug.LogWarning($"[FigmaManage] 用户取消了Sprite转换操作，已转换 {convertedCount}/{totalCount} 个文件");
                        break;
                    }

                    // 转换为Sprite
                    ConvertToSprite(relativePath);
                    convertedCount++;
                }

                EditorUtility.ClearProgressBar();

                if (cancelled)
                {
                    Debug.Log($"[FigmaManage] Sprite转换被取消，已成功转换 {convertedCount} 个图片为Sprite");
                    return false; // 返回false表示被取消
                }
                else
                {
                    Debug.Log($"[FigmaManage] 批量转换完成，成功转换 {convertedCount} 个图片为Sprite");
                    return true; // 返回true表示完成
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[FigmaManage] 批量转换Sprite失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存索引信息到文件
        /// </summary>
        private string SaveIndexToFile(string fileKey, Dictionary<string, string> nodeIndexMapping, string savePath, float imageScale, string rootNodeName)
        {
            try
            {
                // 生成文件名：figma_index_{fileKey}_{rootNodeName}_scale{imageScale}.json
                string scaleStr = imageScale.ToString("F1").Replace(".", "");
                string safeRootNodeName = SanitizeFileName(rootNodeName);
                string indexFileName = $"figma_index_{fileKey}_{safeRootNodeName}_scale{scaleStr}.json";
                string indexFilePath = Path.Combine(savePath, indexFileName);

                // 检查文件是否已存在，如果存在则解析并合并
                Dictionary<string, string> existingMapping = new Dictionary<string, string>();
                if (File.Exists(indexFilePath))
                {
                    try
                    {
                        string existingContent = File.ReadAllText(indexFilePath);
                        var existingData = Json.Parse(existingContent) as JsonClass;
                        var existingMappingObj = existingData["node_index_mapping"];
                        if (existingMappingObj != null)
                        {
                            // Convert JsonClass to Dictionary
                            var mappingNode = existingMappingObj as JsonClass;
                            existingMapping = new Dictionary<string, string>();
                            if (mappingNode != null)
                            {
                                foreach (var key in mappingNode.Keys)
                                {
                                    existingMapping[key] = mappingNode[key]?.Value;
                                }
                            }
                            Debug.Log($"[FigmaManage] 读取到现有索引文件，包含 {existingMapping.Count} 个条目");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FigmaManage] 解析现有索引文件失败，将创建新文件: {ex.Message}");
                    }
                }

                // 合并新的映射到现有映射中
                foreach (var kvp in nodeIndexMapping)
                {
                    existingMapping[kvp.Key] = kvp.Value; // 新数据覆盖旧数据
                }

                var indexData = new
                {
                    file_key = fileKey,
                    root_node_name = rootNodeName,
                    image_scale = imageScale,
                    last_updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    total_files = existingMapping.Count,
                    node_index_mapping = existingMapping
                };

                string jsonContent = Json.FromObject(indexData);
                File.WriteAllText(indexFilePath, jsonContent);

                Debug.Log($"[FigmaManage] 索引文件已保存/更新: {indexFilePath}");
                Debug.Log($"[FigmaManage] 总索引条目: {existingMapping.Count} (新增/更新: {nodeIndexMapping.Count})");
                return indexFilePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FigmaManage] 保存索引文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取Figma访问令牌
        /// </summary>
        private string GetFigmaToken()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                return null;

            return settings.figmaSettings.figma_access_token;
        }

        /// <summary>
        /// 获取Figma资产数据路径
        /// </summary>
        private string GetFigmaAssetsPath()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                return "Assets/FigmaAssets";

            return settings.figmaSettings.figma_assets_path;
        }

        /// <summary>
        /// 获取Figma预览图保存路径
        /// </summary>
        private string GetFigmaPreviewPath()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                return "Assets/FigmaAssets/Previews";

            return settings.figmaSettings.figma_preview_path;
        }

        /// <summary>
        /// 获取是否自动转换为Sprite的配置
        /// </summary>
        private bool GetAutoConvertToSprite()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                return true; // 默认开启

            return settings.figmaSettings.auto_convert_to_sprite;
        }

        /// <summary>
        /// 获取预览图最大尺寸
        /// </summary>
        private int GetPreviewMaxSize()
        {
            var settings = McpSettings.Instance;
            if (settings.figmaSettings == null)
                return 100; // 默认值

            return settings.figmaSettings.preview_max_size;
        }


        /// <summary>
        /// 计算文件内容的hash值
        /// </summary>
        private string CalculateBatesHash(byte[] data)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
            }
        }

        /// <summary>
        /// 清理文件名中的无效字符
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // 移除或替换无效字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // 替换一些特殊字符
            fileName = fileName.Replace(":", "_")
                              .Replace(";", "_")
                              .Replace(" ", "_")
                              .Replace("#", "_")
                              .Replace("&", "_")
                              .Replace("?", "_")
                              .Replace("=", "_");

            // 确保文件名不为空且不以点开头
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("."))
                fileName = "unnamed";

            return fileName;
        }

        /// <summary>
        /// 智能分析节点，判断是否需要下载为图片
        /// </summary>
        private bool IsDownloadableNode(JsonNode node)
        {
            if (node == null) return false;

            string nodeType = node["type"]?.Value;
            bool visible = node["visible"].AsBoolDefault(true);

            if (!visible) return false;

            // 1. 包含图片引用的节点
            if (HasImageRef(node))
            {
                return true;
            }

            // 2. Vector类型节点（矢量图形）
            if (nodeType == "VECTOR" || nodeType == "BOOLEAN_OPERATION")
            {
                return true;
            }

            // 3. 有填充且非简单颜色的节点
            if (HasComplexFills(node))
            {
                return true;
            }

            // 4. 有描边的节点
            if (HasStrokes(node))
            {
                return true;
            }

            // 5. 有效果的节点（阴影、模糊等）
            if (HasEffects(node))
            {
                return true;
            }

            // 6. 椭圆节点
            if (nodeType == "ELLIPSE")
            {
                return true;
            }

            // 7. 有圆角的矩形
            if (nodeType == "RECTANGLE" && HasRoundedCorners(node))
            {
                return true;
            }

            // 8. 复杂的Frame（包含多个子元素且有样式）
            if (nodeType == "FRAME" && IsComplexFrame(node))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查节点是否包含图片引用
        /// </summary>
        private bool HasImageRef(JsonNode node)
        {
            var fills = node["fills"];
            if (fills != null)
            {
                foreach (JsonNode fill in fills.Childs)
                {
                    if (fill["type"]?.Value == "IMAGE" && fill["imageRef"] != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否有复杂填充（渐变、图片等）
        /// </summary>
        private bool HasComplexFills(JsonNode node)
        {
            var fills = node["fills"];
            if (fills != null)
            {
                foreach (JsonNode fill in fills.Childs)
                {
                    string fillType = fill["type"]?.Value;
                    if (fillType == "GRADIENT_LINEAR" ||
                        fillType == "GRADIENT_RADIAL" ||
                        fillType == "GRADIENT_ANGULAR" ||
                        fillType == "IMAGE")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否有描边
        /// </summary>
        private bool HasStrokes(JsonNode node)
        {
            var strokes = node["strokes"];
            return strokes != null && strokes.Count > 0;
        }

        /// <summary>
        /// 检查是否有效果
        /// </summary>
        private bool HasEffects(JsonNode node)
        {
            var effects = node["effects"];
            return effects != null && effects.Count > 0;
        }

        /// <summary>
        /// 检查是否有圆角
        /// </summary>
        private bool HasRoundedCorners(JsonNode node)
        {
            var cornerRadius = node["cornerRadius"];
            if (cornerRadius != null)
            {
                float radius = cornerRadius.AsFloat;
                return radius > 0;
            }

            var rectangleCornerRadii = node["rectangleCornerRadii"];
            if (rectangleCornerRadii != null)
            {
                foreach (JsonNode radius in rectangleCornerRadii.Childs)
                {
                    if (radius.AsFloat > 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否为复杂Frame
        /// </summary>
        private bool IsComplexFrame(JsonNode node)
        {
            var children = node["children"];
            if (children == null || children.Count == 0)
                return false;

            // 如果Frame有背景色、效果或者包含多个不同类型的子元素，认为是复杂Frame
            if (HasComplexFills(node) || HasEffects(node) || HasStrokes(node))
                return true;

            // 检查子元素数量和类型多样性
            int childCount = children.Count;
            if (childCount > 3) // 超过3个子元素的复杂布局
                return true;

            return false;
        }

        /// <summary>
        /// 递归扫描节点树，找出所有需要下载的图片节点
        /// </summary>
        private List<string> FindDownloadableNodes(JsonNode node, List<string> result = null)
        {
            if (result == null)
                result = new List<string>();

            if (node == null)
                return result;

            string nodeId = node["id"]?.Value;
            if (!string.IsNullOrEmpty(nodeId) && IsDownloadableNode(node))
            {
                result.Add(nodeId);
            }

            // 递归检查子节点
            var children = node["children"];
            if (children != null)
            {
                foreach (JsonNode child in children.Childs)
                {
                    FindDownloadableNodes(child, result);
                }
            }

            return result;
        }

        /// <summary>
        /// 从节点树中收集所有节点ID（包括所有子节点）
        /// </summary>
        /// <param name="node">节点数据</param>
        /// <param name="result">节点ID列表（输出）</param>
        private void CollectAllNodeIds(JsonNode node, List<string> result)
        {
            if (node == null) return;

            // 添加当前节点ID
            string nodeId = node["id"]?.Value;
            if (!string.IsNullOrEmpty(nodeId))
            {
                result.Add(nodeId);
            }

            // 递归收集所有子节点ID
            var children = node["children"];
            if (children != null && children.Childs != null)
            {
                foreach (JsonNode child in children.Childs)
                {
                    CollectAllNodeIds(child, result);
                }
            }
        }

        /// <summary>
        /// 获取坐标系转换信息
        /// </summary>
        /// <returns>坐标系转换信息的JsonClass对象</returns>
        private JsonClass GetCoordinateSystemInfo()
        {
            var info = new JsonClass();
            info["figma_coordinate_system"] = "原点在左上角，Y轴向下为正";
            info["ugui_coordinate_system"] = "原点在容器中心，Y轴向上为正";
            info["uitoolkit_coordinate_system"] = "原点在左上角，Y轴向下为正（与Figma一致）";

            // UGUI转换公式
            var uguiFormula = new JsonClass();
            // 根容器下的元素
            uguiFormula["x"] = "anchored_position_x = figma_x + (width/2) - (container_width/2)";
            uguiFormula["y"] = "anchored_position_y = (container_height/2) - figma_y - (height/2)";
            // 嵌套元素（需要进行自身尺寸偏移）
            uguiFormula["nested_x"] = "anchored_position_x = (figma_x - parent_figma_x) - (parent_width/2) + (width/2)";
            uguiFormula["nested_y"] = "anchored_position_y = (parent_figma_y - figma_y) + (parent_height/2) - (height/2)";
            // 尺寸
            uguiFormula["size"] = "size_delta = [width, height] (保持不变)";
            uguiFormula["note"] = "公式适用于RectTransform锚点在中心的情况（anchor=[0.5,0.5]）";

            // UIToolkit转换公式
            var uitoolkitFormula = new JsonClass();
            uitoolkitFormula["left"] = "left = figma_x";
            uitoolkitFormula["top"] = "top = figma_y";
            uitoolkitFormula["right"] = "right = container_width - (figma_x + width)";
            uitoolkitFormula["bottom"] = "bottom = container_height - (figma_y + height)";
            uitoolkitFormula["width"] = "width = figma_width";
            uitoolkitFormula["height"] = "height = figma_height";
            uitoolkitFormula["note"] = "UIToolkit使用left/top/right/bottom定位，坐标系与Figma一致，无需Y轴翻转";

            var conversionFormulas = new JsonClass();
            conversionFormulas["ugui"] = uguiFormula;
            conversionFormulas["uitoolkit"] = uitoolkitFormula;
            info["conversion_formulas"] = conversionFormulas;
            info["conversion_prompt"] = McpSettings.Instance.figmaSettings.ai_conversion_prompt;
            return info;
        }
    }
}