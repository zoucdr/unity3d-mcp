using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Migrated from Newtonsoft.Json to SimpleJson
// Migrated from Newtonsoft.Json to SimpleJson

namespace UnityMcp.Tools
{
    /// <summary>
    /// Figma数据简化器 - 将复杂的Figma节点数据简化为AI友好且token高效的格式
    /// </summary>
    public static class FigmaDataSimplifier
    {
        /// <summary>
        /// 简化的节点数据结构
        /// </summary>
        [Serializable]
        public class SimplifiedNode
        {
            public string id;              // 节点ID
            public string name;            // 节点名称
            public string type;            // 节点类型 (FRAME, TEXT, RECTANGLE等)
            // visible字段已移除，因为所有返回的节点都是可见的

            // 文本相关
            public string text;            // 文本内容
            public TextStyle textStyle;    // 文本样式

            // 样式相关
            public ColorInfo backgroundColor; // 背景色（主要填充色，保持向后兼容）
            public ColorInfo textColor;      // 文字颜色
            public List<FillInfo> fills;     // 完整的填充信息列表
            public float cornerRadius;       // 圆角
            public bool hasImage;            // 是否包含图片引用
            public bool hasEffect;           // 是否需要下载为图片（复杂效果）
            public string imageRef;          // 图片引用

            // 布局相关
            public LayoutInfo layout;        // 布局信息

            // 简化的布局信息（使用Figma坐标系）
            public float[] pos;              // 位置 [x, y] (Figma坐标系: 左上角原点)
            public float[] size;             // 控件尺寸 [width, height]

            public List<SimplifiedNode> children; // 子节点

            // 组件列表（仅在根节点包含）
            public List<string> components;   // 组件ID列表
        }

        /// <summary>
        /// 文本样式信息
        /// </summary>
        [Serializable]
        public class TextStyle
        {
            public string fontFamily;      // 字体族
            public string fontWeight;      // 字体粗细
            public float fontSize;         // 字体大小
            public string textAlign;       // 文本对齐
            public float lineHeight;       // 行高
        }

        /// <summary>
        /// 颜色信息
        /// </summary>
        [Serializable]
        public class ColorInfo
        {
            public float r, g, b, a;       // RGBA值
            public string hex;             // 十六进制颜色值
            public string type;            // 颜色类型 (SOLID, GRADIENT等)
        }

        /// <summary>
        /// 填充信息（完整的Figma填充数据）
        /// </summary>
        [Serializable]
        public class FillInfo
        {
            public string type;            // 填充类型 (SOLID, GRADIENT_LINEAR, GRADIENT_RADIAL, IMAGE等)
            public bool visible;           // 填充是否可见
            public float opacity;          // 不透明度
            public string blendMode;       // 混合模式
            public ColorInfo color;        // 纯色填充的颜色信息
            public string imageRef;        // 图片填充的引用
            public GradientInfo gradient;  // 渐变填充信息
        }

        /// <summary>
        /// 渐变信息
        /// </summary>
        [Serializable]
        public class GradientInfo
        {
            public string type;            // 渐变类型 (LINEAR, RADIAL, ANGULAR)
            public List<GradientStop> gradientStops; // 渐变停止点
            public float[] gradientHandlePositions;  // 渐变句柄位置
        }

        /// <summary>
        /// 渐变停止点
        /// </summary>
        [Serializable]
        public class GradientStop
        {
            public float position;         // 位置 (0-1)
            public ColorInfo color;        // 颜色
        }

        /// <summary>
        /// 布局信息
        /// </summary>
        [Serializable]
        public class LayoutInfo
        {
            public string layoutMode;      // 布局模式 (VERTICAL, HORIZONTAL等)
            public string alignItems;      // 对齐方式
            public float itemSpacing;      // 间距
            public float[] padding;        // 内边距 [left, top, right, bottom]
        }



        /// <summary>
        /// 简化Figma节点数据，提取绝对位置和尺寸信息
        /// </summary>
        /// <param name="figmaNode">原始Figma节点数据</param>
        /// <param name="maxDepth">最大深度，默认无限制</param>
        /// <param name="convertToUGUI">保留参数以兼容，现在始终使用Figma坐标系</param>
        /// <param name="cleanupRedundantData">保留参数以兼容</param>
        /// <param name="canvasHeight">保留参数以兼容</param>
        /// <param name="canvasWidth">保留参数以兼容</param>
        /// <returns>简化后的节点数据</returns>
        public static SimplifiedNode SimplifyNode(JsonNode figmaNode, int maxDepth = -1, bool convertToUGUI = true, bool cleanupRedundantData = true, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            var result = SimplifyNodeInternal(figmaNode, maxDepth, convertToUGUI, cleanupRedundantData, null, null, canvasHeight, canvasWidth);

            // 使用Figma坐标系，不需要坐标转换
            return result;
        }

        /// <summary>
        /// 内部简化方法，支持传递父节点信息
        /// </summary>
        private static SimplifiedNode SimplifyNodeInternal(JsonNode figmaNode, int maxDepth, bool convertToUGUI, bool cleanupRedundantData, SimplifiedNode parentNode, JsonNode parentFigmaNode, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            if (figmaNode == null || maxDepth == 0)
                return null;

            // 如果节点不可见，直接返回null，不进行解析
            bool visible = figmaNode["visible"].AsBoolDefault(true);
            if (!visible)
                return null;

            var simplified = new SimplifiedNode
            {
                id = figmaNode["id"]?.Value,
                name = figmaNode["name"]?.Value,
                type = figmaNode["type"]?.Value
                // visible字段已移除，因为所有返回的节点都是可见的
            };

            // 提取绝对位置和尺寸信息（使用Figma坐标系）
            var absoluteBoundingBox = figmaNode["absoluteBoundingBox"];
            if (absoluteBoundingBox != null)
            {
                float figmaX = absoluteBoundingBox["x"].AsFloatDefault(0);
                float figmaY = absoluteBoundingBox["y"].AsFloatDefault(0);
                float width = absoluteBoundingBox["width"].AsFloatDefault(0);
                float height = absoluteBoundingBox["height"].AsFloatDefault(0);

                // 使用Figma原始坐标系（左上角原点）
                simplified.pos = new float[]
                {
                    (float)Math.Round(figmaX, 2),
                    (float)Math.Round(figmaY, 2)
                };

                simplified.size = new float[]
                {
                    (float)Math.Round(width, 2),
                    (float)Math.Round(height, 2)
                };
            }

            // 提取文本内容和样式
            ExtractTextInfo(figmaNode, simplified);

            // 提取样式信息
            ExtractStyleInfo(figmaNode, simplified);

            // 提取布局信息
            ExtractLayoutInfo(figmaNode, simplified);

            // 判断是否包含图片引用
            simplified.hasImage = HasImageRef(figmaNode);

            // 判断是否需要下载为图片（复杂效果）
            simplified.hasEffect = IsDownloadableNode(figmaNode);

            // 递归处理子节点
            var children = figmaNode["children"];
            if (children != null && children.type == JsonNodeType.Array)
            {
                simplified.children = new List<SimplifiedNode>();
                foreach (JsonNode child in children.Childs) // 处理所有子节点
                {
                    var nextDepth = maxDepth > 0 ? maxDepth - 1 : -1; // 如果maxDepth为-1则保持无限制
                    var simplifiedChild = SimplifyNodeInternal(child, nextDepth, convertToUGUI, cleanupRedundantData, simplified, figmaNode, canvasHeight, canvasWidth);
                    if (simplifiedChild != null)
                    {
                        simplified.children.Add(simplifiedChild);
                    }
                }

                // 如果没有子节点，设为null节省空间
                if (simplified.children.Count == 0)
                    simplified.children = null;
            }

            // 布局信息已直接提取到absolutePos和size，无需复杂的UGUI转换

            return simplified;
        }


        /// <summary>
        /// 提取文本信息
        /// </summary>
        private static void ExtractTextInfo(JsonNode node, SimplifiedNode simplified)
        {
            // 文本内容
            simplified.text = node["characters"]?.Value;

            // 文本样式
            var style = node["style"];
            if (style != null && style.type == JsonNodeType.Object)
            {
                simplified.textStyle = new TextStyle
                {
                    fontFamily = style["fontFamily"]?.Value,
                    fontWeight = style["fontWeight"]?.Value,
                    fontSize = (float)Math.Round(style["fontSize"].AsFloatDefault(0), 2),
                    textAlign = style["textAlignHorizontal"]?.Value,
                    lineHeight = (float)Math.Round(style["lineHeightPx"].AsFloatDefault(0), 2)
                };
            }
        }

        /// <summary>
        /// 提取样式信息
        /// </summary>
        private static void ExtractStyleInfo(JsonNode node, SimplifiedNode simplified)
        {
            // 提取完整的填充信息
            var fills = node["fills"];
            if (fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                simplified.fills = ExtractFillsInfo(fills);

                // 保持向后兼容：设置第一个可见填充作为背景色
                var firstVisibleFill = simplified.fills?.FirstOrDefault(f => f.visible);
                if (firstVisibleFill?.color != null)
                {
                    simplified.backgroundColor = firstVisibleFill.color;
                }
            }

            // 文字颜色
            if (simplified.textStyle != null && fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                var firstFill = fills.Childs.FirstOrDefault();
                if (firstFill != null && firstFill.type == JsonNodeType.Object)
                {
                    simplified.textColor = ExtractColor(firstFill);
                }
            }

            // 圆角
            simplified.cornerRadius = (float)Math.Round(node["cornerRadius"].AsFloatDefault(0), 2);

            // 图片信息 - 检查是否包含图片引用
            if (simplified.fills != null)
            {
                var imageFill = simplified.fills.FirstOrDefault(f => f.type == "IMAGE" && !string.IsNullOrEmpty(f.imageRef));
                if (imageFill != null)
                {
                    simplified.imageRef = imageFill.imageRef;
                }
            }
        }

        /// <summary>
        /// 提取完整的填充信息列表
        /// </summary>
        private static List<FillInfo> ExtractFillsInfo(JsonNode fills)
        {
            var fillInfos = new List<FillInfo>();

            if (fills == null || fills.type != JsonNodeType.Array)
                return fillInfos;

            foreach (JsonNode fill in fills.Childs)
            {
                if (fill == null)
                    continue;

                var fillInfo = new FillInfo
                {
                    type = fill["type"]?.Value,
                    visible = fill["visible"].AsBoolDefault(true),
                    opacity = (float)Math.Round(fill["opacity"].AsFloatDefault(1.0f), 2),
                    blendMode = fill["blendMode"]?.Value
                };

                // 根据填充类型提取具体信息
                switch (fillInfo.type)
                {
                    case "SOLID":
                        fillInfo.color = ExtractColor(fill);
                        break;

                    case "IMAGE":
                        fillInfo.imageRef = fill["imageRef"]?.Value;
                        break;

                    case "GRADIENT_LINEAR":
                    case "GRADIENT_RADIAL":
                    case "GRADIENT_ANGULAR":
                        fillInfo.gradient = ExtractGradientInfo(fill);
                        break;
                }

                fillInfos.Add(fillInfo);
            }

            return fillInfos;
        }

        /// <summary>
        /// 提取渐变信息
        /// </summary>
        private static GradientInfo ExtractGradientInfo(JsonNode fill)
        {
            var gradientInfo = new GradientInfo
            {
                type = fill["type"]?.Value
            };

            // 提取渐变停止点
            var gradientStops = fill["gradientStops"];
            if (gradientStops != null && gradientStops.type == JsonNodeType.Array)
            {
                gradientInfo.gradientStops = new List<GradientStop>();
                foreach (JsonNode stop in gradientStops.Childs)
                {
                    if (stop != null)
                    {
                        var gradientStop = new GradientStop
                        {
                            position = (float)Math.Round(stop["position"].AsFloatDefault(0), 2),
                            color = ExtractColor(stop)
                        };
                        gradientInfo.gradientStops.Add(gradientStop);
                    }
                }
            }

            // 提取渐变句柄位置
            var gradientHandlePositions = fill["gradientHandlePositions"];
            if (gradientHandlePositions != null && gradientHandlePositions.type == JsonNodeType.Array)
            {
                var positions = new List<float>();
                foreach (JsonNode position in gradientHandlePositions.Childs)
                {
                    if (position != null && position.Count >= 2)
                    {
                        positions.Add((float)Math.Round(position[0].AsFloatDefault(0), 2));
                        positions.Add((float)Math.Round(position[1].AsFloatDefault(0), 2));
                    }
                }
                gradientInfo.gradientHandlePositions = positions.ToArray();
            }

            return gradientInfo;
        }

        /// <summary>
        /// 提取颜色信息
        /// </summary>
        private static ColorInfo ExtractColor(JsonNode fill)
        {
            if (fill == null || fill.type != JsonNodeType.Object) return null;

            var colorInfo = new ColorInfo
            {
                type = fill["type"]?.Value
            };

            var color = fill["color"];
            if (color != null && color.type == JsonNodeType.Object)
            {
                colorInfo.r = (float)Math.Round(color["r"].AsFloatDefault(0), 2);
                colorInfo.g = (float)Math.Round(color["g"].AsFloatDefault(0), 2);
                colorInfo.b = (float)Math.Round(color["b"].AsFloatDefault(0), 2);
                colorInfo.a = (float)Math.Round(color["a"].AsFloatDefault(1), 2);

                // 转换为十六进制
                int r = Mathf.RoundToInt(colorInfo.r * 255);
                int g = Mathf.RoundToInt(colorInfo.g * 255);
                int b = Mathf.RoundToInt(colorInfo.b * 255);
                colorInfo.hex = $"#{r:X2}{g:X2}{b:X2}";
            }

            return colorInfo;
        }

        /// <summary>
        /// 提取布局信息
        /// </summary>
        private static void ExtractLayoutInfo(JsonNode node, SimplifiedNode simplified)
        {
            var layoutMode = node["layoutMode"]?.Value;
            if (!string.IsNullOrEmpty(layoutMode))
            {
                simplified.layout = new LayoutInfo
                {
                    layoutMode = layoutMode,
                    alignItems = node["primaryAxisAlignItems"]?.Value ?? node["counterAxisAlignItems"]?.Value,
                    itemSpacing = (float)Math.Round(node["itemSpacing"].AsFloatDefault(0), 2)
                };

                // 内边距
                var paddingLeft = (float)Math.Round(node["paddingLeft"].AsFloatDefault(0), 2);
                var paddingTop = (float)Math.Round(node["paddingTop"].AsFloatDefault(0), 2);
                var paddingRight = (float)Math.Round(node["paddingRight"].AsFloatDefault(0), 2);
                var paddingBottom = (float)Math.Round(node["paddingBottom"].AsFloatDefault(0), 2);

                if (paddingLeft > 0 || paddingTop > 0 || paddingRight > 0 || paddingBottom > 0)
                {
                    simplified.layout.padding = new float[] { paddingLeft, paddingTop, paddingRight, paddingBottom };
                }
            }
        }


        /// <summary>
        /// 将简化的节点数据转换为紧凑的JSON字符串
        /// </summary>
        /// <param name="simplifiedNode">简化的节点数据</param>
        /// <param name="prettyPrint">是否格式化输出，默认false以减少token</param>
        /// <returns>JSON字符串</returns>
        public static string ToCompactJson(SimplifiedNode simplifiedNode, bool prettyPrint = false)
        {
            // 使用SimpleJson序列化
            return Json.FromObject(simplifiedNode);
        }

        /// <summary>
        /// 批量简化多个节点
        /// </summary>
        /// <param name="figmaNodes">原始节点数据字典</param>
        /// <param name="maxDepth">最大深度，默认无限制</param>
        /// <param name="convertToUGUI">是否转换为Unity坐标系，默认true</param>
        /// <param name="canvasHeight">Canvas高度，用于Unity坐标系转换，默认720</param>
        /// <param name="canvasWidth">Canvas宽度，用于Unity坐标系转换，默认1200</param>
        /// <returns>简化后的节点字典</returns>
        public static Dictionary<string, SimplifiedNode> SimplifyNodes(JsonClass figmaNodes, int maxDepth = -1, bool convertToUGUI = true, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            var result = new Dictionary<string, SimplifiedNode>();

            if (figmaNodes == null) return result;

            foreach (KeyValuePair<string, JsonNode> kvp in figmaNodes.AsEnumerable())
            {
                var nodeData = kvp.Value["document"];
                if (nodeData != null)
                {
                    var simplified = SimplifyNode(nodeData, maxDepth, convertToUGUI, true, canvasHeight, canvasWidth);
                    if (simplified != null)
                    {
                        // 提取并简化 components
                        var componentsData = kvp.Value["components"];
                        if (componentsData != null && componentsData is JsonClass)
                        {
                            simplified.components = ExtractComponentIds(componentsData);
                        }

                        result[kvp.Key] = simplified;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 提取组件ID列表
        /// </summary>
        /// <param name="componentsData">组件数据对象</param>
        /// <returns>组件ID列表</returns>
        private static List<string> ExtractComponentIds(JsonNode componentsData)
        {
            var componentIds = new List<string>();

            if (componentsData == null || componentsData.type != JsonNodeType.Object)
                return componentIds;

            foreach (string key in ((JsonClass)componentsData).GetKeys())
            {
                // key 就是组件ID
                componentIds.Add(key);
            }

            return componentIds;
        }

        /// <summary>
        /// 生成AI友好的节点摘要
        /// </summary>
        /// <param name="simplifiedNode">简化的节点数据</param>
        /// <returns>文本摘要</returns>
        public static string GenerateNodeSummary(SimplifiedNode simplifiedNode)
        {
            if (simplifiedNode == null) return "";

            var summary = new List<string>();

            // 基本信息
            summary.Add($"节点: {simplifiedNode.name} ({simplifiedNode.type})");

            // 显示尺寸和位置信息
            if (simplifiedNode.size != null)
            {
                summary.Add($"尺寸: {simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}");
                if (simplifiedNode.pos != null)
                    summary.Add($"位置: [{simplifiedNode.pos[0]:F0}, {simplifiedNode.pos[1]:F0}]");
            }

            if (!string.IsNullOrEmpty(simplifiedNode.text))
            {
                summary.Add($"文本: \"{simplifiedNode.text}\"");
                if (simplifiedNode.textStyle != null)
                {
                    summary.Add($"字体: {simplifiedNode.textStyle.fontFamily} {simplifiedNode.textStyle.fontSize:F0}px");
                }
            }

            // 显示背景信息（包含完整fills信息）
            if (simplifiedNode.fills != null && simplifiedNode.fills.Count > 0)
            {
                var visibleFills = simplifiedNode.fills.Where(f => f.visible).ToList();
                if (visibleFills.Count > 0)
                {
                    var fillDescriptions = new List<string>();
                    foreach (var fill in visibleFills)
                    {
                        switch (fill.type)
                        {
                            case "SOLID":
                                if (fill.color?.hex != null)
                                    fillDescriptions.Add($"纯色({fill.color.hex})");
                                break;
                            case "IMAGE":
                                fillDescriptions.Add("图片填充");
                                break;
                            case "GRADIENT_LINEAR":
                                fillDescriptions.Add("线性渐变");
                                break;
                            case "GRADIENT_RADIAL":
                                fillDescriptions.Add("径向渐变");
                                break;
                            case "GRADIENT_ANGULAR":
                                fillDescriptions.Add("角度渐变");
                                break;
                            default:
                                fillDescriptions.Add(fill.type);
                                break;
                        }
                    }
                    if (fillDescriptions.Count > 0)
                        summary.Add($"填充: {string.Join(", ", fillDescriptions)}");
                }
            }
            else if (simplifiedNode.backgroundColor != null)
            {
                summary.Add($"背景: {simplifiedNode.backgroundColor.hex}");
            }

            if (simplifiedNode.hasImage)
            {
                summary.Add("包含图片引用");
            }

            if (simplifiedNode.hasEffect)
            {
                summary.Add("需要下载为图片");
            }

            if (simplifiedNode.layout != null)
            {
                summary.Add($"布局: {simplifiedNode.layout.layoutMode}");
            }

            if (simplifiedNode.children != null && simplifiedNode.children.Count > 0)
            {
                summary.Add($"子节点: {simplifiedNode.children.Count}个");
            }

            if (simplifiedNode.components != null && simplifiedNode.components.Count > 0)
            {
                summary.Add($"组件: {simplifiedNode.components.Count}个");
            }

            return string.Join(", ", summary);
        }

        /// <summary>
        /// 计算数据压缩率
        /// </summary>
        /// <param name="originalJson">原始JSON</param>
        /// <param name="simplifiedJson">简化后的JSON</param>
        /// <returns>压缩率百分比</returns>
        public static float CalculateCompressionRatio(string originalJson, string simplifiedJson)
        {
            if (string.IsNullOrEmpty(originalJson) || string.IsNullOrEmpty(simplifiedJson))
                return 0f;

            float originalSize = originalJson.Length;
            float simplifiedSize = simplifiedJson.Length;

            return (1f - simplifiedSize / originalSize) * 100f;
        }

        /// <summary>
        /// 提取关键节点信息（进一步压缩）
        /// </summary>
        /// <param name="simplifiedNode">简化的节点</param>
        /// <returns>关键信息字典</returns>
        public static Dictionary<string, object> ExtractKeyInfo(SimplifiedNode simplifiedNode)
        {
            var keyInfo = new Dictionary<string, object>
            {
                ["id"] = simplifiedNode.id,
                ["name"] = simplifiedNode.name,
                ["type"] = simplifiedNode.type,
                ["size"] = simplifiedNode.size != null ? $"{simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}" : "0x0"
            };

            // 添加位置信息
            if (simplifiedNode.pos != null)
                keyInfo["position"] = $"[{simplifiedNode.pos[0]:F0},{simplifiedNode.pos[1]:F0}]";

            // 只添加非空的关键信息
            if (!string.IsNullOrEmpty(simplifiedNode.text))
                keyInfo["text"] = simplifiedNode.text;

            if (simplifiedNode.textStyle?.fontSize > 0)
                keyInfo["fontSize"] = simplifiedNode.textStyle.fontSize;

            // 优先使用fills信息，回退到backgroundColor
            if (simplifiedNode.fills != null && simplifiedNode.fills.Count > 0)
            {
                var visibleFills = simplifiedNode.fills.Where(f => f.visible).ToList();
                if (visibleFills.Count > 0)
                {
                    keyInfo["fillsCount"] = visibleFills.Count;
                    var firstFill = visibleFills.First();
                    keyInfo["fillType"] = firstFill.type;
                    if (firstFill.color?.hex != null)
                        keyInfo["bgColor"] = firstFill.color.hex;
                }
            }
            else if (simplifiedNode.backgroundColor?.hex != null)
                keyInfo["bgColor"] = simplifiedNode.backgroundColor.hex;

            if (simplifiedNode.hasImage)
                keyInfo["hasImage"] = true;

            if (simplifiedNode.hasEffect)
                keyInfo["hasEffect"] = true;

            if (simplifiedNode.layout?.layoutMode != null)
                keyInfo["layout"] = simplifiedNode.layout.layoutMode;

            if (simplifiedNode.children?.Count > 0)
            {
                keyInfo["childCount"] = simplifiedNode.children.Count;
                // 只包含子节点的关键信息
                keyInfo["children"] = simplifiedNode.children.Select(child => new
                {
                    id = child.id,
                    name = child.name,
                    type = child.type,
                    text = child.text,
                    hasImage = child.hasImage,
                    hasEffect = child.hasEffect
                }).Where(child => !string.IsNullOrEmpty(child.text) || child.hasImage || child.hasEffect).ToList();
            }

            if (simplifiedNode.components?.Count > 0)
            {
                keyInfo["componentCount"] = simplifiedNode.components.Count;
                keyInfo["components"] = simplifiedNode.components;
            }

            return keyInfo;
        }

        /// <summary>
        /// 生成超简洁的AI提示文本
        /// </summary>
        /// <param name="simplifiedNode">简化的节点</param>
        /// <returns>AI提示文本</returns>
        public static string GenerateAIPrompt(SimplifiedNode simplifiedNode)
        {
            var parts = new List<string>();

            // 基础结构
            parts.Add($"{simplifiedNode.name}({simplifiedNode.type})");

            // 尺寸（只在重要时显示）
            if (simplifiedNode.size != null && (simplifiedNode.size[0] > 100 || simplifiedNode.size[1] > 100))
                parts.Add($"{simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}");

            // 文本内容
            if (!string.IsNullOrEmpty(simplifiedNode.text))
            {
                var text = simplifiedNode.text.Length > 20 ?
                    simplifiedNode.text.Substring(0, 20) + "..." :
                    simplifiedNode.text;
                parts.Add($"\"{text}\"");

                if (simplifiedNode.textStyle?.fontSize > 0)
                    parts.Add($"{simplifiedNode.textStyle.fontSize:F0}px");
            }

            // 颜色（优先使用fills信息，只显示主要颜色）
            string primaryColor = null;
            if (simplifiedNode.fills != null && simplifiedNode.fills.Count > 0)
            {
                var firstVisibleFill = simplifiedNode.fills.FirstOrDefault(f => f.visible);
                if (firstVisibleFill != null)
                {
                    switch (firstVisibleFill.type)
                    {
                        case "SOLID":
                            primaryColor = firstVisibleFill.color?.hex;
                            break;
                        case "GRADIENT_LINEAR":
                            parts.Add("🌈");
                            break;
                        case "GRADIENT_RADIAL":
                            parts.Add("⭕");
                            break;
                        case "IMAGE":
                            parts.Add("🖼️");
                            break;
                    }
                }
            }
            else if (simplifiedNode.backgroundColor?.hex != null)
            {
                primaryColor = simplifiedNode.backgroundColor.hex;
            }

            if (primaryColor != null &&
                primaryColor != "#FFFFFF" &&
                primaryColor != "#000000")
            {
                parts.Add(primaryColor);
            }

            // 特殊标记
            if (simplifiedNode.hasImage) parts.Add("📷");
            if (simplifiedNode.hasEffect) parts.Add("🎨");
            if (simplifiedNode.layout?.layoutMode == "HORIZONTAL") parts.Add("→");
            if (simplifiedNode.layout?.layoutMode == "VERTICAL") parts.Add("↓");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// 批量生成AI提示文本
        /// </summary>
        /// <param name="nodes">节点字典</param>
        /// <returns>AI友好的结构化文本</returns>
        public static string GenerateBatchAIPrompt(Dictionary<string, SimplifiedNode> nodes)
        {
            var result = new List<string>();

            foreach (var kvp in nodes) // 处理所有节点
            {
                var nodePrompt = GenerateAIPrompt(kvp.Value);
                result.Add($"• {nodePrompt}");

                // 显示重要子节点
                if (kvp.Value.children != null)
                {
                    var importantChildren = kvp.Value.children
                        .Where(child => !string.IsNullOrEmpty(child.text) || child.hasImage || child.hasEffect); // 显示所有重要子节点

                    foreach (var child in importantChildren)
                    {
                        var childPrompt = GenerateAIPrompt(child);
                        result.Add($"  ◦ {childPrompt}");
                    }
                }
            }

            return string.Join("\n", result);
        }

        #region 布局信息处理

        /// <summary>
        /// 获取节点的简化布局参数字符串（用于MCP调用）
        /// </summary>
        /// <param name="node">简化节点</param>
        /// <returns>布局参数</returns>
        public static string GetLayoutParams(SimplifiedNode node)
        {
            if (node?.size == null) return "";
            var parts = new List<string>();

            if (node.pos != null)
                parts.Add($"\"pos\": [{node.pos[0]:F2}, {node.pos[1]:F2}]");

            if (node.size != null)
                parts.Add($"\"size_delta\": [{node.size[0]:F2}, {node.size[1]:F2}]");

            return "{" + string.Join(", ", parts) + "}";
        }

        /// <summary>
        /// 生成MCP布局调用代码（使用Figma坐标系）
        /// </summary>
        /// <param name="node">简化节点</param>
        /// <param name="parentPath">父节点路径</param>
        /// <returns>MCP调用代码</returns>
        public static string GenerateMCPLayoutCall(SimplifiedNode node, string parentPath = "")
        {
            if (node?.size == null) return "";

            string nodePath = string.IsNullOrEmpty(parentPath) ? node.name : $"{parentPath}/{node.name}";

            // 生成布局调用，使用Figma坐标系
            var parts = new List<string>();
            parts.Add($"path=\"{nodePath}\"");
            parts.Add("action=\"layout_anchor\"");
            parts.Add("anchor_min=[0, 1]");  // 左上角锚点
            parts.Add("anchor_max=[0, 1]");  // 左上角锚点

            if (node.pos != null)
                parts.Add($"anchored_pos=[{node.pos[0]:F2}, {-node.pos[1]:F2}]");  // Y坐标取负值以适配Unity

            if (node.size != null)
                parts.Add($"size_delta=[{node.size[0]:F2}, {node.size[1]:F2}]");

            return $"ugui_layout({string.Join(", ", parts)})";
        }

        #endregion

        #region 下载判断逻辑

        /// <summary>
        /// 智能分析节点，判断是否需要下载为图片
        /// </summary>
        private static bool IsDownloadableNode(JsonNode node)
        {
            if (node == null) return false;

            string nodeType = node["type"]?.Value;
            // 不需要检查visible，因为不可见的节点已经在外层被过滤掉了

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
        private static bool HasImageRef(JsonNode node)
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
        private static bool HasComplexFills(JsonNode node)
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
        private static bool HasStrokes(JsonNode node)
        {
            var strokes = node["strokes"];
            return strokes != null && strokes.Count > 0;
        }

        /// <summary>
        /// 检查是否有效果
        /// </summary>
        private static bool HasEffects(JsonNode node)
        {
            var effects = node["effects"];
            return effects != null && effects.Count > 0;
        }

        /// <summary>
        /// 检查是否有圆角
        /// </summary>
        private static bool HasRoundedCorners(JsonNode node)
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
        private static bool IsComplexFrame(JsonNode node)
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


        #endregion

        #region 使用示例和工具方法

        /// <summary>
        /// 批量生成所有节点的MCP布局调用代码
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="parentPath">父路径</param>
        /// <returns>MCP调用代码列表</returns>
        public static List<string> GenerateAllMCPLayoutCalls(SimplifiedNode rootNode, string parentPath = "")
        {
            var calls = new List<string>();

            if (rootNode == null) return calls;

            // 为当前节点生成调用
            var call = GenerateMCPLayoutCall(rootNode, parentPath);
            if (!string.IsNullOrEmpty(call))
            {
                calls.Add(call);
            }

            // 递归处理子节点
            if (rootNode.children != null)
            {
                string currentPath = string.IsNullOrEmpty(parentPath) ? rootNode.name : $"{parentPath}/{rootNode.name}";
                foreach (var child in rootNode.children)
                {
                    calls.AddRange(GenerateAllMCPLayoutCalls(child, currentPath));
                }
            }

            return calls;
        }

        /// <summary>
        /// 生成完整的MCP批量调用代码（使用Figma坐标系）
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <returns>完整的functions_call代码</returns>
        public static string GenerateBatchMCPCall(SimplifiedNode rootNode)
        {
            var calls = GenerateAllMCPLayoutCalls(rootNode);
            if (calls.Count == 0) return "";

            var funcCalls = calls.Select(call =>
            {
                // 提取参数部分
                var argsStart = call.IndexOf('(') + 1;
                var argsEnd = call.LastIndexOf(')');
                var args = call.Substring(argsStart, argsEnd - argsStart);

                return $"{{\"func\": \"ugui_layout\", \"args\": {{{args}}}}}";
            });

            return $"functions_call(funcs=[{string.Join(", ", funcCalls)}])";
        }

        #endregion

        #region 调试和测试方法

        /// <summary>
        /// 生成fills信息的详细描述（用于调试）
        /// </summary>
        /// <param name="simplifiedNode">简化节点</param>
        /// <returns>fills详细信息</returns>
        public static string GetFillsDebugInfo(SimplifiedNode simplifiedNode)
        {
            if (simplifiedNode?.fills == null || simplifiedNode.fills.Count == 0)
                return "无填充信息";

            var info = new List<string>();
            for (int i = 0; i < simplifiedNode.fills.Count; i++)
            {
                var fill = simplifiedNode.fills[i];
                var fillDesc = $"Fill[{i}]: {fill.type}";

                if (!fill.visible)
                    fillDesc += " (隐藏)";

                if (fill.opacity < 1.0f)
                    fillDesc += $" 透明度:{fill.opacity:P0}";

                switch (fill.type)
                {
                    case "SOLID":
                        if (fill.color != null)
                            fillDesc += $" 颜色:{fill.color.hex}";
                        break;
                    case "IMAGE":
                        if (!string.IsNullOrEmpty(fill.imageRef))
                            fillDesc += $" 图片:{fill.imageRef}";
                        break;
                    case "GRADIENT_LINEAR":
                    case "GRADIENT_RADIAL":
                    case "GRADIENT_ANGULAR":
                        if (fill.gradient?.gradientStops != null)
                            fillDesc += $" 渐变停止点:{fill.gradient.gradientStops.Count}个";
                        break;
                }

                if (!string.IsNullOrEmpty(fill.blendMode) && fill.blendMode != "NORMAL")
                    fillDesc += $" 混合:{fill.blendMode}";

                info.Add(fillDesc);
            }

            return string.Join("\n", info);
        }

        #endregion
    }
}