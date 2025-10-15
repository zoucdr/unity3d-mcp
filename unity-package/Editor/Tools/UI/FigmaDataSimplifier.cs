using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Migrated from Newtonsoft.Json to SimpleJson
// Migrated from Newtonsoft.Json to SimpleJson

namespace UnityMcp.Tools
{
    /// <summary>
    /// Figma数据Simplify器 - Make complexFigmaNode dataSimplifyAsAIUser-friendly andtoken高效的格式
    /// </summary>
    public static class FigmaDataSimplifier
    {
        /// <summary>
        /// Simplified node数据结构
        /// </summary>
        [Serializable]
        public class SimplifiedNode
        {
            public string id;              // NodeID
            public string name;            // Node name
            public string type;            // Node type (FRAME, TEXT, RECTANGLEEtc.)
            // visibleField has been removed，因As所有返回的Node都是可见的

            // Text related
            public string text;            // Text content
            public TextStyle textStyle;    // Text style

            // Style related
            public ColorInfo backgroundColor; // Background color（Main fill color，保持向后兼容）
            public ColorInfo textColor;      // Text color
            public List<FillInfo> fills;     // CompleteFill infoList
            public float cornerRadius;       // Corner radius
            public bool hasImage;            // 是否包含Image reference
            public bool hasEffect;           // 是否需要下载AsImage（Complex effect）
            public string imageRef;          // Image reference

            // Layout related
            public LayoutInfo layout;        // Layout info

            // Simplify的Layout info（UseFigmaCoordinate system）
            public float[] pos;              // Position [x, y] (FigmaCoordinate system: 左上角原点)
            public float[] size;             // Control size [width, height]

            public List<SimplifiedNode> children; // Child node

            // Component list（仅在Root node包含）
            public List<string> components;   // ComponentIDList
        }

        /// <summary>
        /// Text styleInfo
        /// </summary>
        [Serializable]
        public class TextStyle
        {
            public string fontFamily;      // Font family
            public string fontWeight;      // Font weight
            public float fontSize;         // Font size
            public string textAlign;       // Text alignment
            public float lineHeight;       // Line height
        }

        /// <summary>
        /// Color info
        /// </summary>
        [Serializable]
        public class ColorInfo
        {
            public float r, g, b, a;       // RGBAValue
            public string hex;             // 十六进制ColorValue
            public string type;            // Color type (SOLID, GRADIENTEtc.)
        }

        /// <summary>
        /// Fill info（CompleteFigmaFill data）
        /// </summary>
        [Serializable]
        public class FillInfo
        {
            public string type;            // Fill type (SOLID, GRADIENT_LINEAR, GRADIENT_RADIAL, IMAGEEtc.)
            public bool visible;           // Fill是否可见
            public float opacity;          // Opacity
            public string blendMode;       // Blend mode
            public ColorInfo color;        // Solid colorFill的Color info
            public string imageRef;        // Image fill的引用
            public GradientInfo gradient;  // GradientFill info
        }

        /// <summary>
        /// Gradient info
        /// </summary>
        [Serializable]
        public class GradientInfo
        {
            public string type;            // Gradient type (LINEAR, RADIAL, ANGULAR)
            public List<GradientStop> gradientStops; // Gradient stop point
            public float[] gradientHandlePositions;  // Gradient句柄Position
        }

        /// <summary>
        /// Gradient stop point
        /// </summary>
        [Serializable]
        public class GradientStop
        {
            public float position;         // Position (0-1)
            public ColorInfo color;        // Color
        }

        /// <summary>
        /// Layout info
        /// </summary>
        [Serializable]
        public class LayoutInfo
        {
            public string layoutMode;      // Layout mode (VERTICAL, HORIZONTALEtc.)
            public string alignItems;      // Alignment
            public float itemSpacing;      // Spacing
            public float[] padding;        // Padding [left, top, right, bottom]
        }



        /// <summary>
        /// SimplifyFigmaNode data，提取绝对PositionAndSizeInfo
        /// </summary>
        /// <param name="figmaNode">OriginalFigmaNode data</param>
        /// <param name="maxDepth">Maximum depth，Default unlimited</param>
        /// <param name="convertToUGUI">保留参数以兼容，现在始终UseFigmaCoordinate system</param>
        /// <param name="cleanupRedundantData">保留参数以兼容</param>
        /// <param name="canvasHeight">保留参数以兼容</param>
        /// <param name="canvasWidth">保留参数以兼容</param>
        /// <returns>SimplifiedNode data</returns>
        public static SimplifiedNode SimplifyNode(JsonNode figmaNode, int maxDepth = -1, bool convertToUGUI = true, bool cleanupRedundantData = true, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            var result = SimplifyNodeInternal(figmaNode, maxDepth, convertToUGUI, cleanupRedundantData, null, null, canvasHeight, canvasWidth);

            // UseFigmaCoordinate system，不需要坐标Convert
            return result;
        }

        /// <summary>
        /// 内部Simplify方法，支持传递父NodeInfo
        /// </summary>
        private static SimplifiedNode SimplifyNodeInternal(JsonNode figmaNode, int maxDepth, bool convertToUGUI, bool cleanupRedundantData, SimplifiedNode parentNode, JsonNode parentFigmaNode, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            if (figmaNode == null || maxDepth == 0)
                return null;

            // IfNode不可见，Return directlynull，Do not parse
            bool visible = figmaNode["visible"].AsBoolDefault(true);
            if (!visible)
                return null;

            var simplified = new SimplifiedNode
            {
                id = figmaNode["id"]?.Value,
                name = figmaNode["name"]?.Value,
                type = figmaNode["type"]?.Value
                // visibleField has been removed，因As所有返回的Node都是可见的
            };

            // 提取绝对PositionAndSizeInfo（UseFigmaCoordinate system）
            var absoluteBoundingBox = figmaNode["absoluteBoundingBox"];
            if (absoluteBoundingBox != null)
            {
                float figmaX = absoluteBoundingBox["x"].AsFloatDefault(0);
                float figmaY = absoluteBoundingBox["y"].AsFloatDefault(0);
                float width = absoluteBoundingBox["width"].AsFloatDefault(0);
                float height = absoluteBoundingBox["height"].AsFloatDefault(0);

                // UseFigmaOriginal coordinate system（左上角原点）
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

            // 提取Text contentAnd样式
            ExtractTextInfo(figmaNode, simplified);

            // 提取样式Info
            ExtractStyleInfo(figmaNode, simplified);

            // 提取Layout info
            ExtractLayoutInfo(figmaNode, simplified);

            // 判断是否包含Image reference
            simplified.hasImage = HasImageRef(figmaNode);

            // 判断是否需要下载AsImage（Complex effect）
            simplified.hasEffect = IsDownloadableNode(figmaNode);

            // 递归处理Child node
            var children = figmaNode["children"];
            if (children != null && children.type == JsonNodeType.Array)
            {
                simplified.children = new List<SimplifiedNode>();
                foreach (JsonNode child in children.Childs) // 处理所有Child node
                {
                    var nextDepth = maxDepth > 0 ? maxDepth - 1 : -1; // IfmaxDepthAs-1则保持无限制
                    var simplifiedChild = SimplifyNodeInternal(child, nextDepth, convertToUGUI, cleanupRedundantData, simplified, figmaNode, canvasHeight, canvasWidth);
                    if (simplifiedChild != null)
                    {
                        simplified.children.Add(simplifiedChild);
                    }
                }

                // If没有Child node，Set asnullSave space
                if (simplified.children.Count == 0)
                    simplified.children = null;
            }

            // Layout info已直接提取到absolutePosAndsize，No complexity requiredUGUIConvert

            return simplified;
        }


        /// <summary>
        /// 提取TextInfo
        /// </summary>
        private static void ExtractTextInfo(JsonNode node, SimplifiedNode simplified)
        {
            // Text content
            simplified.text = node["characters"]?.Value;

            // Text style
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
        /// 提取样式Info
        /// </summary>
        private static void ExtractStyleInfo(JsonNode node, SimplifiedNode simplified)
        {
            // 提取CompleteFill info
            var fills = node["fills"];
            if (fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                simplified.fills = ExtractFillsInfo(fills);

                // 保持向后兼容：设置第一Item可见Fill作AsBackground color
                var firstVisibleFill = simplified.fills?.FirstOrDefault(f => f.visible);
                if (firstVisibleFill?.color != null)
                {
                    simplified.backgroundColor = firstVisibleFill.color;
                }
            }

            // Text color
            if (simplified.textStyle != null && fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                var firstFill = fills.Childs.FirstOrDefault();
                if (firstFill != null && firstFill.type == JsonNodeType.Object)
                {
                    simplified.textColor = ExtractColor(firstFill);
                }
            }

            // Corner radius
            simplified.cornerRadius = (float)Math.Round(node["cornerRadius"].AsFloatDefault(0), 2);

            // Image info - 检查是否包含Image reference
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
        /// 提取CompleteFill infoList
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

                // 根据Fill type提取具体Info
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
        /// 提取Gradient info
        /// </summary>
        private static GradientInfo ExtractGradientInfo(JsonNode fill)
        {
            var gradientInfo = new GradientInfo
            {
                type = fill["type"]?.Value
            };

            // 提取Gradient stop point
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

            // 提取Gradient句柄Position
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
        /// 提取Color info
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

                // ConvertAs十六进制
                int r = Mathf.RoundToInt(colorInfo.r * 255);
                int g = Mathf.RoundToInt(colorInfo.g * 255);
                int b = Mathf.RoundToInt(colorInfo.b * 255);
                colorInfo.hex = $"#{r:X2}{g:X2}{b:X2}";
            }

            return colorInfo;
        }

        /// <summary>
        /// 提取Layout info
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

                // Padding
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
        /// 将Simplified node数据ConvertAs紧凑的JSONString
        /// </summary>
        /// <param name="simplifiedNode">Simplified node数据</param>
        /// <param name="prettyPrint">是否格式化输出，DefaultfalseTo reducetoken</param>
        /// <returns>JSONString</returns>
        public static string ToCompactJson(SimplifiedNode simplifiedNode, bool prettyPrint = false)
        {
            // UseSimpleJsonSerialization
            return Json.FromObject(simplifiedNode);
        }

        /// <summary>
        /// 批量Simplify多ItemNode
        /// </summary>
        /// <param name="figmaNodes">OriginalNode data字典</param>
        /// <param name="maxDepth">Maximum depth，Default unlimited</param>
        /// <param name="convertToUGUI">Whether to convert toUnityCoordinate system，Defaulttrue</param>
        /// <param name="canvasHeight">CanvasHeight，ForUnityCoordinate system conversion，Default720</param>
        /// <param name="canvasWidth">CanvasWidth，ForUnityCoordinate system conversion，Default1200</param>
        /// <returns>SimplifiedNode dictionary</returns>
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
                        // Extract and simplify components
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
        /// Extract componentIDList
        /// </summary>
        /// <param name="componentsData">Component数据对象</param>
        /// <returns>ComponentIDList</returns>
        private static List<string> ExtractComponentIds(JsonNode componentsData)
        {
            var componentIds = new List<string>();

            if (componentsData == null || componentsData.type != JsonNodeType.Object)
                return componentIds;

            foreach (string key in ((JsonClass)componentsData).GetKeys())
            {
                // key Is componentID
                componentIds.Add(key);
            }

            return componentIds;
        }

        /// <summary>
        /// GenerateAI友好的Node摘要
        /// </summary>
        /// <param name="simplifiedNode">Simplified node数据</param>
        /// <returns>Text summary</returns>
        public static string GenerateNodeSummary(SimplifiedNode simplifiedNode)
        {
            if (simplifiedNode == null) return "";

            var summary = new List<string>();

            // Basic info
            summary.Add($"Node: {simplifiedNode.name} ({simplifiedNode.type})");

            // 显示SizeAndPositionInfo
            if (simplifiedNode.size != null)
            {
                summary.Add($"Size: {simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}");
                if (simplifiedNode.pos != null)
                    summary.Add($"Position: [{simplifiedNode.pos[0]:F0}, {simplifiedNode.pos[1]:F0}]");
            }

            if (!string.IsNullOrEmpty(simplifiedNode.text))
            {
                summary.Add($"Text: \"{simplifiedNode.text}\"");
                if (simplifiedNode.textStyle != null)
                {
                    summary.Add($"Font: {simplifiedNode.textStyle.fontFamily} {simplifiedNode.textStyle.fontSize:F0}px");
                }
            }

            // 显示BackgroundInfo（Include completefillsInfo）
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
                                    fillDescriptions.Add($"Solid color({fill.color.hex})");
                                break;
                            case "IMAGE":
                                fillDescriptions.Add("Image fill");
                                break;
                            case "GRADIENT_LINEAR":
                                fillDescriptions.Add("Linear gradient");
                                break;
                            case "GRADIENT_RADIAL":
                                fillDescriptions.Add("Radial gradient");
                                break;
                            case "GRADIENT_ANGULAR":
                                fillDescriptions.Add("Angular gradient");
                                break;
                            default:
                                fillDescriptions.Add(fill.type);
                                break;
                        }
                    }
                    if (fillDescriptions.Count > 0)
                        summary.Add($"Fill: {string.Join(", ", fillDescriptions)}");
                }
            }
            else if (simplifiedNode.backgroundColor != null)
            {
                summary.Add($"Background: {simplifiedNode.backgroundColor.hex}");
            }

            if (simplifiedNode.hasImage)
            {
                summary.Add("包含Image reference");
            }

            if (simplifiedNode.hasEffect)
            {
                summary.Add("需要下载AsImage");
            }

            if (simplifiedNode.layout != null)
            {
                summary.Add($"Layout: {simplifiedNode.layout.layoutMode}");
            }

            if (simplifiedNode.children != null && simplifiedNode.children.Count > 0)
            {
                summary.Add($"Child node: {simplifiedNode.children.Count}Item");
            }

            if (simplifiedNode.components != null && simplifiedNode.components.Count > 0)
            {
                summary.Add($"Component: {simplifiedNode.components.Count}Item");
            }

            return string.Join(", ", summary);
        }

        /// <summary>
        /// 计算数据压缩率
        /// </summary>
        /// <param name="originalJson">OriginalJSON</param>
        /// <param name="simplifiedJson">SimplifiedJSON</param>
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
        /// 提取关键NodeInfo（Further compression）
        /// </summary>
        /// <param name="simplifiedNode">Simplified node</param>
        /// <returns>关键Info字典</returns>
        public static Dictionary<string, object> ExtractKeyInfo(SimplifiedNode simplifiedNode)
        {
            var keyInfo = new Dictionary<string, object>
            {
                ["id"] = simplifiedNode.id,
                ["name"] = simplifiedNode.name,
                ["type"] = simplifiedNode.type,
                ["size"] = simplifiedNode.size != null ? $"{simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}" : "0x0"
            };

            // 添加PositionInfo
            if (simplifiedNode.pos != null)
                keyInfo["position"] = $"[{simplifiedNode.pos[0]:F0},{simplifiedNode.pos[1]:F0}]";

            // 只添加非空的关键Info
            if (!string.IsNullOrEmpty(simplifiedNode.text))
                keyInfo["text"] = simplifiedNode.text;

            if (simplifiedNode.textStyle?.fontSize > 0)
                keyInfo["fontSize"] = simplifiedNode.textStyle.fontSize;

            // Prefer usefillsInfo，Fallback tobackgroundColor
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
                // 只包含Child node的关键Info
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
        /// Generate超简洁的AIHint text
        /// </summary>
        /// <param name="simplifiedNode">Simplified node</param>
        /// <returns>AIHint text</returns>
        public static string GenerateAIPrompt(SimplifiedNode simplifiedNode)
        {
            var parts = new List<string>();

            // Basic structure
            parts.Add($"{simplifiedNode.name}({simplifiedNode.type})");

            // Size（只在重要时显示）
            if (simplifiedNode.size != null && (simplifiedNode.size[0] > 100 || simplifiedNode.size[1] > 100))
                parts.Add($"{simplifiedNode.size[0]:F0}x{simplifiedNode.size[1]:F0}");

            // Text content
            if (!string.IsNullOrEmpty(simplifiedNode.text))
            {
                var text = simplifiedNode.text.Length > 20 ?
                    simplifiedNode.text.Substring(0, 20) + "..." :
                    simplifiedNode.text;
                parts.Add($"\"{text}\"");

                if (simplifiedNode.textStyle?.fontSize > 0)
                    parts.Add($"{simplifiedNode.textStyle.fontSize:F0}px");
            }

            // Color（Prefer usefillsInfo，只显示主要Color）
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

            // Special mark
            if (simplifiedNode.hasImage) parts.Add("📷");
            if (simplifiedNode.hasEffect) parts.Add("🎨");
            if (simplifiedNode.layout?.layoutMode == "HORIZONTAL") parts.Add("→");
            if (simplifiedNode.layout?.layoutMode == "VERTICAL") parts.Add("↓");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Batch generateAIHint text
        /// </summary>
        /// <param name="nodes">Node dictionary</param>
        /// <returns>AI友好的结构化Text</returns>
        public static string GenerateBatchAIPrompt(Dictionary<string, SimplifiedNode> nodes)
        {
            var result = new List<string>();

            foreach (var kvp in nodes) // 处理所有Node
            {
                var nodePrompt = GenerateAIPrompt(kvp.Value);
                result.Add($"• {nodePrompt}");

                // 显示重要Child node
                if (kvp.Value.children != null)
                {
                    var importantChildren = kvp.Value.children
                        .Where(child => !string.IsNullOrEmpty(child.text) || child.hasImage || child.hasEffect); // 显示所有重要Child node

                    foreach (var child in importantChildren)
                    {
                        var childPrompt = GenerateAIPrompt(child);
                        result.Add($"  ◦ {childPrompt}");
                    }
                }
            }

            return string.Join("\n", result);
        }

        #region Layout info处理

        /// <summary>
        /// 获取Node的SimplifyLayout parameterString（ForMCPCall）
        /// </summary>
        /// <param name="node">Simplified node</param>
        /// <returns>Layout parameter</returns>
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
        /// GenerateMCPLayoutCall code（UseFigmaCoordinate system）
        /// </summary>
        /// <param name="node">Simplified node</param>
        /// <param name="parentPath">Parent node path</param>
        /// <returns>MCPCall code</returns>
        public static string GenerateMCPLayoutCall(SimplifiedNode node, string parentPath = "")
        {
            if (node?.size == null) return "";

            string nodePath = string.IsNullOrEmpty(parentPath) ? node.name : $"{parentPath}/{node.name}";

            // GenerateLayoutCall，UseFigmaCoordinate system
            var parts = new List<string>();
            parts.Add($"path=\"{nodePath}\"");
            parts.Add("action=\"layout_anchor\"");
            parts.Add("anchor_min=[0, 1]");  // Top-left anchor point
            parts.Add("anchor_max=[0, 1]");  // Top-left anchor point

            if (node.pos != null)
                parts.Add($"anchored_pos=[{node.pos[0]:F2}, {-node.pos[1]:F2}]");  // Y坐标取负Value以适配Unity

            if (node.size != null)
                parts.Add($"size_delta=[{node.size[0]:F2}, {node.size[1]:F2}]");

            return $"ugui_layout({string.Join(", ", parts)})";
        }

        #endregion

        #region 下载判断逻辑

        /// <summary>
        /// 智能分析Node，判断是否需要下载AsImage
        /// </summary>
        private static bool IsDownloadableNode(JsonNode node)
        {
            if (node == null) return false;

            string nodeType = node["type"]?.Value;
            // 不需要检查visible，因As不可见的Node已经在外层被过滤掉了

            // 1. 包含Image reference的Node
            if (HasImageRef(node))
            {
                return true;
            }

            // 2. VectorType node（Vector graphics）
            if (nodeType == "VECTOR" || nodeType == "BOOLEAN_OPERATION")
            {
                return true;
            }

            // 3. 有Fill且非简单Color的Node
            if (HasComplexFills(node))
            {
                return true;
            }

            // 4. 有描边的Node
            if (HasStrokes(node))
            {
                return true;
            }

            // 5. 有效果的Node（Shadow、Blur）
            if (HasEffects(node))
            {
                return true;
            }

            // 6. Ellipse node
            if (nodeType == "ELLIPSE")
            {
                return true;
            }

            // 7. 有Corner radius的矩形
            if (nodeType == "RECTANGLE" && HasRoundedCorners(node))
            {
                return true;
            }

            // 8. ComplexFrame（包含多Item子元素且有样式）
            if (nodeType == "FRAME" && IsComplexFrame(node))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查Node是否包含Image reference
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
        /// 检查是否有复杂Fill（Gradient、Image）
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
        /// 检查是否有Corner radius
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
        /// 检查是否As复杂Frame
        /// </summary>
        private static bool IsComplexFrame(JsonNode node)
        {
            var children = node["children"];
            if (children == null || children.Count == 0)
                return false;

            // IfFrameWith background color、效果或者包含多Item不同类型的子元素，Considered complexFrame
            if (HasComplexFills(node) || HasEffects(node) || HasStrokes(node))
                return true;

            // 检查子元素数量And类型多样性
            int childCount = children.Count;
            if (childCount > 3) // Exceed3Item子元素的复杂Layout
                return true;

            return false;
        }


        #endregion

        #region Use示例And工具方法

        /// <summary>
        /// Batch generate所有Node的MCPLayoutCall code
        /// </summary>
        /// <param name="rootNode">Root node</param>
        /// <param name="parentPath">Parent path</param>
        /// <returns>MCPCall codeList</returns>
        public static List<string> GenerateAllMCPLayoutCalls(SimplifiedNode rootNode, string parentPath = "")
        {
            var calls = new List<string>();

            if (rootNode == null) return calls;

            // As当前NodeGenerateCall
            var call = GenerateMCPLayoutCall(rootNode, parentPath);
            if (!string.IsNullOrEmpty(call))
            {
                calls.Add(call);
            }

            // 递归处理Child node
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
        /// Generate completeMCP批量Call code（UseFigmaCoordinate system）
        /// </summary>
        /// <param name="rootNode">Root node</param>
        /// <returns>Completefunctions_callCode</returns>
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

        #region 调试And测试方法

        /// <summary>
        /// GeneratefillsInfo的详细描述（For debugging）
        /// </summary>
        /// <param name="simplifiedNode">Simplified node</param>
        /// <returns>fillsDetails</returns>
        public static string GetFillsDebugInfo(SimplifiedNode simplifiedNode)
        {
            if (simplifiedNode?.fills == null || simplifiedNode.fills.Count == 0)
                return "No fill info";

            var info = new List<string>();
            for (int i = 0; i < simplifiedNode.fills.Count; i++)
            {
                var fill = simplifiedNode.fills[i];
                var fillDesc = $"Fill[{i}]: {fill.type}";

                if (!fill.visible)
                    fillDesc += " (Hide)";

                if (fill.opacity < 1.0f)
                    fillDesc += $" Transparency:{fill.opacity:P0}";

                switch (fill.type)
                {
                    case "SOLID":
                        if (fill.color != null)
                            fillDesc += $" Color:{fill.color.hex}";
                        break;
                    case "IMAGE":
                        if (!string.IsNullOrEmpty(fill.imageRef))
                            fillDesc += $" Image:{fill.imageRef}";
                        break;
                    case "GRADIENT_LINEAR":
                    case "GRADIENT_RADIAL":
                    case "GRADIENT_ANGULAR":
                        if (fill.gradient?.gradientStops != null)
                            fillDesc += $" Gradient stop point:{fill.gradient.gradientStops.Count}Item";
                        break;
                }

                if (!string.IsNullOrEmpty(fill.blendMode) && fill.blendMode != "NORMAL")
                    fillDesc += $" Blend:{fill.blendMode}";

                info.Add(fillDesc);
            }

            return string.Join("\n", info);
        }

        #endregion
    }
}