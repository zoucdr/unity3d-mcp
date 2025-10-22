using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Migrated from Newtonsoft.Json to SimpleJson
// Migrated from Newtonsoft.Json to SimpleJson

namespace Unity.Mcp.Tools
{
    /// <summary>
    /// Figma数据简化器 - 将复杂的Figma节点数据简化为AI友好且token高效的格式
    /// </summary>
    public static class FigmaDataSimplifier
    {
        /// 简化Figma节点数据，提取绝对位置和尺寸信息
        /// </summary>
        /// <param name="figmaNode">原始Figma节点数据</param>
        /// <param name="maxDepth">最大深度，默认无限制</param>
        /// <param name="convertToUGUI">保留参数以兼容，现在始终使用Figma坐标系</param>
        /// <param name="cleanupRedundantData">保留参数以兼容</param>
        /// <param name="canvasHeight">保留参数以兼容</param>
        /// <param name="canvasWidth">保留参数以兼容</param>
        /// <returns>简化后的节点数据（JsonNode格式）</returns>
        public static JsonNode SimplifyNode(JsonNode figmaNode, int maxDepth = -1, bool convertToUGUI = true, bool cleanupRedundantData = true, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            var result = SimplifyNodeInternal(figmaNode, maxDepth, convertToUGUI, cleanupRedundantData, null, null, canvasHeight, canvasWidth);

            // 使用Figma坐标系，不需要坐标转换
            return result;
        }

        /// <summary>
        /// 内部简化方法，支持传递父节点信息
        /// </summary>
        private static JsonNode SimplifyNodeInternal(JsonNode figmaNode, int maxDepth, bool convertToUGUI, bool cleanupRedundantData, JsonNode parentNode, JsonNode parentFigmaNode, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            if (figmaNode == null || maxDepth == 0)
                return null;

            // 如果节点不可见，直接返回null，不进行解析
            bool visible = figmaNode["visible"].AsBoolDefault(true);
            if (!visible)
                return null;

            // 创建一个新的JsonClass作为简化后的节点数据
            var simplified = new JsonClass();

            // 复制基本属性
            if (!figmaNode["id"].IsNull())
                simplified["id"] = figmaNode["id"];
            if (!figmaNode["name"].IsNull())
                simplified["name"] = figmaNode["name"];
            if (!figmaNode["type"].IsNull())
                simplified["type"] = figmaNode["type"];
            // visible字段已移除，因为所有返回的节点都是可见的

            // 提取绝对位置和尺寸信息（使用Figma坐标系）
            var absoluteBoundingBox = figmaNode["absoluteBoundingBox"];
            if (absoluteBoundingBox != null)
            {
                float figmaX = absoluteBoundingBox["x"].AsFloatDefault(0);
                float figmaY = absoluteBoundingBox["y"].AsFloatDefault(0);
                float width = absoluteBoundingBox["width"].AsFloatDefault(0);
                float height = absoluteBoundingBox["height"].AsFloatDefault(0);

                // 使用Figma原始坐标系（左上角原点）
                var posArray = new JsonArray();
                posArray.Add(new JsonData((float)Math.Round(figmaX, 2)));
                posArray.Add(new JsonData((float)Math.Round(figmaY, 2)));
                simplified["pos"] = posArray;

                var sizeArray = new JsonArray();
                sizeArray.Add(new JsonData((float)Math.Round(width, 2)));
                sizeArray.Add(new JsonData((float)Math.Round(height, 2)));
                simplified["size"] = sizeArray;
            }

            // 提取文本内容和样式
            ExtractTextInfo(figmaNode, simplified);

            // 提取样式信息
            ExtractStyleInfo(figmaNode, simplified);

            // 提取布局信息
            ExtractLayoutInfo(figmaNode, simplified);

            // 判断是否包含图片引用
            if (HasImageRef(figmaNode))
                simplified["hasImage"] = true;

            // 判断是否需要下载为图片（复杂效果）
            if (IsDownloadableNode(figmaNode))
                simplified["hasEffect"] = true;

            // 递归处理子节点
            var children = figmaNode["children"];
            if (children != null && children.type == JsonNodeType.Array)
            {
                var simplifiedChildren = new JsonArray();
                var nextDepth = maxDepth > 0 ? maxDepth - 1 : -1; // 如果maxDepth为-1则保持无限制

                foreach (JsonNode child in children.Childs) // 处理所有子节点
                {
                    var simplifiedChild = SimplifyNodeInternal(child, nextDepth, convertToUGUI, cleanupRedundantData, simplified, figmaNode, canvasHeight, canvasWidth);
                    if (simplifiedChild != null)
                    {
                        simplifiedChildren.Add(simplifiedChild);
                    }
                }

                if (simplifiedChildren.Count > 0)
                {
                    simplified["children"] = simplifiedChildren;
                }
            }

            // 布局信息已直接提取到absolutePos和size，无需复杂的UGUI转换

            return simplified;
        }

        /// <summary>
        /// 提取文本信息
        /// </summary>
        private static void ExtractTextInfo(JsonNode node, JsonNode simplified)
        {
            // 文本内容
            if (!node["characters"].IsNull())
            {
                simplified["text"] = node["characters"];
            }
            // 文本样式
            var style = node["style"];
            if (!node["style"].IsNull() && style != null && style.type == JsonNodeType.Object)
            {
                var textStyle = new JsonClass();
                textStyle["fontFamily"] = style["fontFamily"];
                textStyle["fontWeight"] = style["fontWeight"];
                textStyle["fontSize"] = new JsonData((float)Math.Round(style["fontSize"].AsFloatDefault(0), 2));
                textStyle["textAlign"] = style["textAlignHorizontal"];
                textStyle["lineHeight"] = new JsonData((float)Math.Round(style["lineHeightPx"].AsFloatDefault(0), 2));

                simplified["textStyle"] = textStyle;
            }
        }

        /// <summary>
        /// 提取样式信息
        /// </summary>
        private static void ExtractStyleInfo(JsonNode node, JsonNode simplified)
        {
            // 提取完整的填充信息
            var fills = node["fills"];
            if (!node["fills"].IsNull() && fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                simplified["fills"] = ExtractFillsInfo(fills);

                // 保持向后兼容：设置第一个可见填充作为背景色
                var fillsArray0 = simplified["fills"] as JsonArray;
                if (fillsArray0 != null && fillsArray0.Count > 0)
                {
                    var firstVisibleFill = fillsArray0.Childs.FirstOrDefault(f => f["visible"].AsBoolDefault(true));
                    if (firstVisibleFill != null && !firstVisibleFill["color"].IsNull())
                    {
                        simplified["backgroundColor"] = firstVisibleFill["color"];
                    }
                }
            }

            // 文字颜色
            var textStyle = simplified["textStyle"] as JsonClass;
            if (textStyle != null && fills != null && fills.type == JsonNodeType.Array && fills.Count > 0)
            {
                var firstFill = fills.Childs.FirstOrDefault();
                if (firstFill != null && firstFill.type == JsonNodeType.Object)
                {
                    simplified["textColor"] = ExtractColor(firstFill);
                }
            }

            // 圆角
            var cornerRadius = (float)Math.Round(node["cornerRadius"].AsFloatDefault(0));
            if (cornerRadius > 0)
            {
                simplified["cornerRadius"] = cornerRadius;
            }

            // 图片信息 - 检查是否包含图片引用
            var fillsArray = simplified["fills"] as JsonArray;
            if (fillsArray != null && fillsArray.Count > 0)
            {
                var imageFill = fillsArray.Childs.FirstOrDefault(f =>
                    f["type"].Value == "IMAGE" &&
                    !f["imageRef"].IsNull() &&
                    !string.IsNullOrEmpty(f["imageRef"].Value));

                if (imageFill != null)
                {
                    simplified["imageRef"] = imageFill["imageRef"];
                }
            }
        }

        /// <summary>
        /// 提取完整的填充信息列表
        /// </summary>
        private static JsonArray ExtractFillsInfo(JsonNode fills)
        {
            var fillInfos = new JsonArray();

            if (fills == null || fills.type != JsonNodeType.Array)
                return fillInfos;

            foreach (JsonNode fill in fills.Childs)
            {
                if (fill == null)
                    continue;

                var fillInfo = new JsonClass();
                if (!fill["type"].IsNull())
                    fillInfo["type"] = fill["type"];
                fillInfo["visible"] = new JsonData(fill["visible"].AsBoolDefault(true));
                fillInfo["opacity"] = new JsonData((float)Math.Round(fill["opacity"].AsFloatDefault(1.0f), 2));
                if (!fill["blendMode"].IsNull())
                    fillInfo["blendMode"] = fill["blendMode"];

                // 根据填充类型提取具体信息
                string fillType = fillInfo["type"].Value;
                switch (fillType)
                {
                    case "SOLID":
                        fillInfo["color"] = ExtractColor(fill);
                        break;

                    case "IMAGE":
                        if (!fill["imageRef"].IsNull())
                            fillInfo["imageRef"] = fill["imageRef"];
                        break;

                    case "GRADIENT_LINEAR":
                    case "GRADIENT_RADIAL":
                    case "GRADIENT_ANGULAR":
                        fillInfo["gradient"] = ExtractGradientInfo(fill);
                        break;
                }

                fillInfos.Add(fillInfo);
            }

            return fillInfos;
        }

        /// <summary>
        /// 提取渐变信息
        /// </summary>
        private static JsonClass ExtractGradientInfo(JsonNode fill)
        {
            var gradientInfo = new JsonClass();
            if (!fill["type"].IsNull())
                gradientInfo["type"] = fill["type"];

            // 提取渐变停止点
            var gradientStops = fill["gradientStops"];
            if (!fill["gradientStops"].IsNull() && gradientStops != null && gradientStops.type == JsonNodeType.Array)
            {
                var stopsArray = new JsonArray();
                foreach (JsonNode stop in gradientStops.Childs)
                {
                    if (stop != null)
                    {
                        var gradientStop = new JsonClass();
                        gradientStop["position"] = stop["position"];
                        gradientStop["color"] = ExtractColor(stop);
                        stopsArray.Add(gradientStop);
                    }
                }
                gradientInfo["gradientStops"] = stopsArray;
            }

            // 提取渐变句柄位置
            var gradientHandlePositions = fill["gradientHandlePositions"];
            if (!fill["gradientHandlePositions"].IsNull() && gradientHandlePositions != null && gradientHandlePositions.type == JsonNodeType.Array)
            {
                var positionsArray = new JsonArray();
                foreach (JsonNode position in gradientHandlePositions.Childs)
                {
                    if (position != null && position.Count >= 2)
                    {
                        var posArray = new JsonArray();
                        posArray.Add(new JsonData((float)Math.Round(position[0].AsFloatDefault(0), 2)));
                        posArray.Add(new JsonData((float)Math.Round(position[1].AsFloatDefault(0), 2)));
                        positionsArray.Add(posArray);
                    }
                }
                gradientInfo["gradientHandlePositions"] = positionsArray;
            }

            return gradientInfo;
        }

        /// <summary>
        /// 提取颜色信息
        /// </summary>
        private static JsonNode ExtractColor(JsonNode fill)
        {
            if (fill == null || fill.type != JsonNodeType.Object) return new JsonData("");

            var color = fill["color"];
            if (!fill["color"].IsNull() && color != null && color.type == JsonNodeType.Object)
            {
                // 转换为十六进制，包含透明度通道
                int r = Mathf.RoundToInt(color["r"].AsFloatDefault(0) * 255);
                int g = Mathf.RoundToInt(color["g"].AsFloatDefault(0) * 255);
                int b = Mathf.RoundToInt(color["b"].AsFloatDefault(0) * 255);
                int a = Mathf.RoundToInt(color["a"].AsFloatDefault(1) * 255);
                return new JsonData($"#{r:X2}{g:X2}{b:X2}{a:X2}");
            }

            return new JsonData("");
        }

        /// <summary>
        /// 提取布局信息
        /// </summary>
        private static void ExtractLayoutInfo(JsonNode node, JsonNode simplified)
        {
            var layoutMode = !node["layoutMode"].IsNull() ? node["layoutMode"].Value : "";
            if (!string.IsNullOrEmpty(layoutMode))
            {
                var layout = new JsonClass();
                layout["layoutMode"] = layoutMode;

                string alignItems = "";
                if (!node["primaryAxisAlignItems"].IsNull())
                    alignItems = node["primaryAxisAlignItems"].Value;
                else if (!node["counterAxisAlignItems"].IsNull())
                    alignItems = node["counterAxisAlignItems"].Value;

                layout["alignItems"] = alignItems;
                layout["itemSpacing"] = (float)Math.Round(node["itemSpacing"].AsFloatDefault(0), 2);

                simplified["layout"] = layout;

                // 内边距
                var paddingLeft = (float)Math.Round(node["paddingLeft"].AsFloatDefault(0), 2);
                var paddingTop = (float)Math.Round(node["paddingTop"].AsFloatDefault(0), 2);
                var paddingRight = (float)Math.Round(node["paddingRight"].AsFloatDefault(0), 2);
                var paddingBottom = (float)Math.Round(node["paddingBottom"].AsFloatDefault(0), 2);

                if (paddingLeft > 0 || paddingTop > 0 || paddingRight > 0 || paddingBottom > 0)
                {
                    layout = simplified["layout"] as JsonClass;
                    if (layout != null)
                    {
                        var paddingArray = new JsonArray();
                        paddingArray.Add(new JsonData(paddingLeft));
                        paddingArray.Add(new JsonData(paddingTop));
                        paddingArray.Add(new JsonData(paddingRight));
                        paddingArray.Add(new JsonData(paddingBottom));
                        layout["padding"] = paddingArray;
                    }
                }
            }
        }


        /// <summary>
        /// 批量简化多个节点
        /// </summary>
        /// <param name="figmaNodes">原始节点数据字典</param>
        /// <param name="maxDepth">最大深度，默认无限制</param>
        /// <param name="convertToUGUI">是否转换为Unity坐标系，默认true</param>
        /// <param name="canvasHeight">Canvas高度，用于Unity坐标系转换，默认720</param>
        /// <param name="canvasWidth">Canvas宽度，用于Unity坐标系转换，默认1200</param>
        /// <returns>简化后的节点数据（JsonNode, 以对象形式返回）</returns>
        public static JsonNode SimplifyNodes(JsonClass figmaNodes, int maxDepth = -1, bool convertToUGUI = true, float canvasHeight = 720f, float canvasWidth = 1200f)
        {
            var result = new JsonClass();

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
                            var componentsList = ExtractComponentIds(componentsData);
                            if (componentsList.Count > 0)
                            {
                                var componentsArray = new JsonArray();
                                foreach (var componentId in componentsList)
                                {
                                    componentsArray.Add(componentId);
                                }
                                simplified["components"] = componentsArray;
                            }
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
    }
}