/*-*-* Copyright (c) Mycoria@
 * Author: AI Assistant
 * Creation Date: 2025-12-17
 * Version: 1.0.0
 * Description: MCP资源索引管理工具，实现资源索引记录与查询功能
 * Features:
 *   - 支持资源索引的增删改查操作
 *   - 支持分类管理和正则搜索
 *   - 支持通过GUID定位Unity资源
 *   - 支持从选中对象快速添加索引
 *_*/
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UniMcp.Models;
using UniMcp;

namespace UniMcp.Tools
{
    /// <summary>
    /// 资源索引管理工具，支持添加、删除、查询、定位等操作
    /// 对应方法名: asset_index
    /// </summary>
    [ToolName("asset_index", "系统工具")]
    public class AssetIndexManage : StateMethodBase
    {
        public override string Description => "资源索引管理工具，支持添加、删除、查询、定位等操作";
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型 - 枚举
                new MethodStr("action", "操作类型", false)
                    .SetEnumValues("add", "remove", "update", "search", "categories", "locate", "details", "show", "lost"),
                
                // 资源名称
                new MethodStr("name", "资源名称")
                    .AddExamples("PlayerController", "MainScene", "PlayerPrefab"),
                
                // 资源路径
                new MethodStr("path", "资源路径")
                    .AddExamples("Assets/Scripts/Player/PlayerController.cs", "Assets/Scenes/MainScene.unity"),
                
                // 分类标签
                new MethodStr("category", "分类标签，用于组织资源")
                    .AddExamples("脚本", "场景", "预制体", "配置")
                    .SetDefault("默认"),
                
                // 备注信息
                new MethodStr("note", "备注信息")
                    .AddExamples("核心脚本", "重要资源", "待优化"),
                
                // 搜索正则表达式
                new MethodStr("pattern", "正则表达式模式（用于search操作，匹配名称、备注、路径）")
                    .AddExamples("Player.*", "^Assets/Scripts", "Controller|Manager"),
                
                // 唯一ID
                new MethodInt("id", "资源索引唯一ID（用于remove、update、locate操作，推荐使用）")
                    .AddExample(1)
                    .AddExample(5),
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("add", HandleAddAssetIndex)
                    .Leaf("remove", HandleRemoveAssetIndex)
                    .Leaf("update", HandleUpdateAssetIndex)
                    .Leaf("search", HandleSearchAssetIndex)
                    .Leaf("categories", HandleGetCategories)
                    .Leaf("locate", HandleLocateAsset)
                    .Leaf("details", HandleGetDetails)
                    .Leaf("show", HandleShowInExplorer)
                    .Leaf("lost", HandleGetLostAssets)
                    .DefaultLeaf(HandleUnknownAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理添加资源索引的操作
        /// </summary>
        private object HandleAddAssetIndex(JsonClass args)
        {
            try
            {
                string name = ExtractName(args);
                string path = ExtractPath(args);
                string category = ExtractCategory(args);
                string note = ExtractNote(args);

                // 验证路径并转换为GUID
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    return Response.Error($"无效的资源路径或资源不存在: {path}");
                }

                // 添加到设置中
                AssetIndexSetting.instance.AddAssetIndex(name, guid, category, note);

                var resultData = BuildSimplifiedAssetData(name, path, category);
                resultData.Add("total", new JsonData(AssetIndexSetting.instance.GetCount()));

                McpLogger.Log($"[AssetIndexManager] 添加资源索引成功: {name} - {path}");
                return Response.Success($"已添加: {name}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 添加资源索引失败: {e.Message}");
                return Response.Error($"添加资源索引失败: {e.Message}");
            }
        }


        /// <summary>
        /// 处理删除资源索引的操作
        /// </summary>
        private object HandleRemoveAssetIndex(JsonClass args)
        {
            try
            {
                // 优先使用ID删除（推荐）
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    var asset = AssetIndexSetting.instance.GetAssetIndexById(id);

                    if (asset != null)
                    {
                        bool success = AssetIndexSetting.instance.RemoveAssetIndexById(id);

                        if (success)
                        {
                            var resultData = new JsonClass();
                            resultData.Add("total", new JsonData(AssetIndexSetting.instance.GetCount()));

                            McpLogger.Log($"[AssetIndexManager] 删除资源索引成功: {asset.name} (ID: {id})");
                            return Response.Success($"已删除: {asset.name}", resultData);
                        }
                    }

                    return Response.Error($"未找到ID: {id}");
                }
                // 使用路径删除
                else if (args.ContainsKey("path"))
                {
                    string path = ExtractPath(args);
                    string guid = AssetDatabase.AssetPathToGUID(path);

                    if (string.IsNullOrEmpty(guid))
                    {
                        return Response.Error($"无效的资源路径: {path}");
                    }

                    var asset = AssetIndexSetting.instance.GetAssetIndexByGuid(guid);
                    bool success = AssetIndexSetting.instance.RemoveAssetIndex(guid);

                    if (success)
                    {
                        var resultData = new JsonClass();
                        resultData.Add("total", new JsonData(AssetIndexSetting.instance.GetCount()));

                        McpLogger.Log($"[AssetIndexManager] 删除资源索引成功: {path}");
                        return Response.Success($"已删除", resultData);
                    }
                    else
                    {
                        return Response.Error($"未找到路径: {path}");
                    }
                }
                else
                {
                    return Response.Error("删除操作需要提供 'id'（推荐）或 'path' 参数");
                }
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 删除资源索引失败: {e.Message}");
                return Response.Error($"删除资源索引失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理搜索资源索引的操作（使用正则表达式）
        /// </summary>
        private object HandleSearchAssetIndex(JsonClass args)
        {
            try
            {
                string pattern = ExtractPattern(args);

                if (string.IsNullOrEmpty(pattern))
                {
                    return Response.Error("pattern 参数不能为空");
                }

                var allAssets = AssetIndexSetting.instance.AssetIndices;
                var matchedAssets = new HashSet<int>();
                var results = new List<AssetIndexInfo>();

                // 使用正则表达式匹配
                System.Text.RegularExpressions.Regex regex;
                try
                {
                    regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    return Response.Error($"无效的正则表达式: {ex.Message}");
                }

                foreach (var asset in allAssets)
                {
                    if (matchedAssets.Contains(asset.id))
                        continue;

                    bool matched = false;
                    string assetPath = asset.GetAssetPath();

                    if (!string.IsNullOrEmpty(asset.name) && regex.IsMatch(asset.name))
                        matched = true;
                    else if (!string.IsNullOrEmpty(assetPath) && regex.IsMatch(assetPath))
                        matched = true;
                    else if (!string.IsNullOrEmpty(asset.category) && regex.IsMatch(asset.category))
                        matched = true;
                    else if (!string.IsNullOrEmpty(asset.note) && regex.IsMatch(asset.note))
                        matched = true;

                    // 额外检查：通过GUID获取资源对象名称并匹配
                    if (!matched && !string.IsNullOrEmpty(assetPath))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (obj != null && regex.IsMatch(obj.name))
                        {
                            matched = true;
                        }
                    }

                    if (matched)
                    {
                        matchedAssets.Add(asset.id);
                        results.Add(asset);
                    }
                }

                var resultData = BuildSimpleListData(results);

                McpLogger.Log($"[AssetIndexManager] 正则搜索 '{pattern}'，找到 {results.Count} 个");
                return Response.Success($"找到 {results.Count} 个", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 搜索资源索引失败: {e.Message}");
                return Response.Error($"搜索资源索引失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理修改资源索引的操作
        /// </summary>
        private object HandleUpdateAssetIndex(JsonClass args)
        {
            try
            {
                AssetIndexInfo targetAsset = null;
                string identifier = "";

                // 优先使用ID（推荐）
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    targetAsset = AssetIndexSetting.instance.GetAssetIndexById(id);
                    identifier = $"ID {id}";

                    if (targetAsset == null)
                    {
                        return Response.Error($"未找到ID: {id}");
                    }
                }
                // 使用路径
                else if (args.ContainsKey("path"))
                {
                    string path = ExtractPath(args);
                    string guid = AssetDatabase.AssetPathToGUID(path);

                    if (string.IsNullOrEmpty(guid))
                    {
                        return Response.Error($"无效的资源路径: {path}");
                    }

                    targetAsset = AssetIndexSetting.instance.GetAssetIndexByGuid(guid);
                    identifier = path;

                    if (targetAsset == null)
                    {
                        return Response.Error($"未找到路径: {path}");
                    }
                }
                else
                {
                    return Response.Error("修改操作需要提供 'id'（推荐）或 'path' 参数来定位要修改的资源索引");
                }

                string originalName = targetAsset.name;
                bool hasChanges = false;

                // 更新各个字段
                if (args.ContainsKey("name"))
                {
                    string newName = args["name"]?.Value;
                    if (!string.IsNullOrEmpty(newName) && newName != targetAsset.name)
                    {
                        targetAsset.name = newName;
                        hasChanges = true;
                    }
                }

                if (args.ContainsKey("category"))
                {
                    string newCategory = args["category"]?.Value;
                    if (newCategory != null && newCategory != targetAsset.category)
                    {
                        targetAsset.category = string.IsNullOrEmpty(newCategory) ? "默认" : newCategory;
                        hasChanges = true;
                    }
                }

                if (args.ContainsKey("note"))
                {
                    string newNote = args["note"]?.Value ?? "";
                    if (newNote != targetAsset.note)
                    {
                        targetAsset.note = newNote;
                        hasChanges = true;
                    }
                }

                if (!hasChanges)
                {
                    return Response.Error("未提供任何要修改的字段（name、category、note）");
                }

                // 保存更改
                AssetIndexSetting.Save();

                var resultData = new JsonClass();
                resultData.Add("id", new JsonData(targetAsset.id));
                resultData.Add("name", new JsonData(targetAsset.name));
                resultData.Add("path", new JsonData(targetAsset.GetAssetPath()));

                McpLogger.Log($"[AssetIndexManager] 修改资源索引成功: {originalName} ({identifier})");
                return Response.Success($"已修改: {targetAsset.name}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 修改资源索引失败: {e.Message}");
                return Response.Error($"修改资源索引失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理获取所有分类的操作
        /// </summary>
        private object HandleGetCategories(JsonClass args)
        {
            try
            {
                var categories = AssetIndexSetting.instance.GetAllCategories();

                var resultData = new JsonClass();
                var categoriesArray = new JsonArray();

                foreach (var category in categories)
                {
                    var categoryObj = new JsonClass();
                    categoryObj.Add("name", new JsonData(category));

                    var assetsInCategory = AssetIndexSetting.instance.GetAssetIndicesByCategory(category);
                    categoryObj.Add("count", new JsonData(assetsInCategory.Count));

                    categoriesArray.Add(categoryObj);
                }

                resultData.Add("categories", categoriesArray);
                resultData.Add("total", new JsonData(categories.Count));

                McpLogger.Log($"[AssetIndexManager] 获取分类，共 {categories.Count} 个");
                return Response.Success($"{categories.Count} 个分类", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 获取分类失败: {e.Message}");
                return Response.Error($"获取分类失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理定位资源的操作
        /// </summary>
        private object HandleLocateAsset(JsonClass args)
        {
            try
            {
                AssetIndexInfo assetInfo = null;

                // 支持通过ID或路径定位
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    assetInfo = AssetIndexSetting.instance.GetAssetIndexById(id);
                    if (assetInfo == null)
                    {
                        return Response.Error($"未找到ID: {id}");
                    }
                }
                else if (args.ContainsKey("path"))
                {
                    string path = ExtractPath(args);
                    assetInfo = AssetIndexSetting.instance.GetAssetIndexByPath(path);
                    if (assetInfo == null)
                    {
                        return Response.Error($"未找到路径: {path}");
                    }
                }
                else
                {
                    return Response.Error("定位操作需要提供 'id'（推荐）或 'path' 参数");
                }

                // 通过GUID获取资源路径
                string assetPath = assetInfo.GetAssetPath();
                if (string.IsNullOrEmpty(assetPath))
                {
                    return Response.Error($"无法找到资源路径，GUID可能已失效");
                }

                // 加载并选中资源
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                {
                    return Response.Error($"无法加载资源: {assetPath}");
                }

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                var resultData = new JsonClass();
                resultData.Add("path", new JsonData(assetPath));
                resultData.Add("name", new JsonData(asset.name));

                McpLogger.Log($"[AssetIndexManager] 定位资源: {asset.name} - {assetPath}");
                return Response.Success($"已定位: {asset.name}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 定位资源失败: {e.Message}");
                return Response.Error($"定位资源失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理获取资源详细信息的操作
        /// </summary>
        private object HandleGetDetails(JsonClass args)
        {
            try
            {
                if (!args.ContainsKey("id"))
                {
                    return Response.Error("details操作需要提供 'id' 参数");
                }

                int id = ExtractId(args);
                var asset = AssetIndexSetting.instance.GetAssetIndexById(id);

                if (asset == null)
                {
                    return Response.Error($"未找到ID: {id}");
                }

                // 返回完整详细信息
                var resultData = new JsonClass();
                resultData.Add("id", new JsonData(asset.id));
                resultData.Add("name", new JsonData(asset.name ?? ""));
                resultData.Add("path", new JsonData(asset.GetAssetPath() ?? ""));
                resultData.Add("category", new JsonData(asset.category ?? "默认"));
                resultData.Add("note", new JsonData(asset.note ?? ""));
                resultData.Add("addTime", new JsonData(asset.addTime ?? ""));

                // 尝试获取当前资源对象名称
                string currentName = "";
                string assetPath = asset.GetAssetPath();
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (obj != null)
                    {
                        currentName = obj.name;
                    }
                }
                resultData.Add("current_object_name", new JsonData(currentName));

                McpLogger.Log($"[AssetIndexManager] 获取详情: ID {id} - {asset.name}");
                return Response.Success($"详情: {asset.name}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 获取详情失败: {e.Message}");
                return Response.Error($"获取详情失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理在文件夹中显示资源的操作
        /// </summary>
        private object HandleShowInExplorer(JsonClass args)
        {
            try
            {
                AssetIndexInfo assetInfo = null;

                // 支持通过ID或路径定位
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    assetInfo = AssetIndexSetting.instance.GetAssetIndexById(id);
                    if (assetInfo == null)
                    {
                        return Response.Error($"未找到ID: {id}");
                    }
                }
                else if (args.ContainsKey("path"))
                {
                    string path = ExtractPath(args);
                    assetInfo = AssetIndexSetting.instance.GetAssetIndexByPath(path);
                    if (assetInfo == null)
                    {
                        return Response.Error($"未找到路径: {path}");
                    }
                }
                else
                {
                    return Response.Error("show操作需要提供 'id'（推荐）或 'path' 参数");
                }

                // 通过GUID获取资源路径
                string assetPath = assetInfo.GetAssetPath();
                if (string.IsNullOrEmpty(assetPath))
                {
                    return Response.Error($"无法找到资源路径，GUID可能已失效");
                }

                // 加载资源
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                {
                    return Response.Error($"无法加载资源: {assetPath}");
                }

                // 在Project窗口中显示并高亮
                EditorUtility.RevealInFinder(assetPath);

                var resultData = new JsonClass();
                resultData.Add("path", new JsonData(assetPath));
                resultData.Add("name", new JsonData(asset.name));
                resultData.Add("full_path", new JsonData(System.IO.Path.GetFullPath(assetPath)));

                McpLogger.Log($"[AssetIndexManager] 在文件夹中显示: {asset.name} - {assetPath}");
                return Response.Success($"已在文件夹中显示: {asset.name}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 在文件夹中显示失败: {e.Message}");
                return Response.Error($"在文件夹中显示失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理获取丢失资源列表的操作
        /// </summary>
        private object HandleGetLostAssets(JsonClass args)
        {
            try
            {
                var allAssets = AssetIndexSetting.instance.AssetIndices;
                var lostAssets = new List<AssetIndexInfo>();

                foreach (var asset in allAssets)
                {
                    string assetPath = asset.GetAssetPath();

                    // 检查路径是否为空或资源是否不存在
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        lostAssets.Add(asset);
                    }
                    else
                    {
                        // 检查文件是否实际存在
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (obj == null)
                        {
                            lostAssets.Add(asset);
                        }
                    }
                }

                var resultData = new JsonClass();
                var lostArray = new JsonArray();

                foreach (var asset in lostAssets)
                {
                    var assetObj = new JsonClass();
                    assetObj.Add("id", new JsonData(asset.id));
                    assetObj.Add("name", new JsonData(asset.name ?? ""));
                    assetObj.Add("category", new JsonData(asset.category ?? "默认"));
                    assetObj.Add("note", new JsonData(asset.note ?? ""));
                    assetObj.Add("guid", new JsonData(asset.guid ?? ""));
                    assetObj.Add("last_known_path", new JsonData(asset.GetAssetPath() ?? "未知"));
                    lostArray.Add(assetObj);
                }

                resultData.Add("lost_assets", lostArray);
                resultData.Add("count", new JsonData(lostAssets.Count));
                resultData.Add("total", new JsonData(allAssets.Count));

                string message = lostAssets.Count > 0
                    ? $"找到 {lostAssets.Count} 个丢失的资源"
                    : "所有资源索引都有效";

                McpLogger.Log($"[AssetIndexManager] 检查丢失资源: {lostAssets.Count}/{allAssets.Count}");
                return Response.Success(message, resultData);
            }
            catch (Exception e)
            {
                LogError($"[AssetIndexManager] 获取丢失资源列表失败: {e.Message}");
                return Response.Error($"获取丢失资源列表失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理未知操作的回调方法
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"未知操作: '{action}'。有效操作: 'add'(添加), 'remove'(删除), 'update'(修改), 'search'(搜索), 'categories'(分类), 'locate'(定位), 'details'(详情), 'show'(在文件夹中显示), 'lost'(丢失列表)");
        }

        // --- Parameter Extraction Helper Methods ---

        private string ExtractName(JsonClass args)
        {
            string name = args["name"]?.Value;
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name 参数是必需的且不能为空");
            }
            return name;
        }

        private string ExtractPath(JsonClass args)
        {
            string path = args["path"]?.Value;
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path 参数是必需的且不能为空");
            }
            return path;
        }

        private string ExtractCategory(JsonClass args)
        {
            string category = args["category"]?.Value;
            return string.IsNullOrEmpty(category) ? "默认" : category;
        }

        private string ExtractNote(JsonClass args)
        {
            return args["note"]?.Value ?? "";
        }

        private string ExtractPattern(JsonClass args)
        {
            return args["pattern"]?.Value ?? "";
        }

        private int ExtractId(JsonClass args)
        {
            var idNode = args["id"];
            if (idNode != null)
            {
                if (int.TryParse(idNode.Value, out int id))
                {
                    return id;
                }
            }
            throw new ArgumentException("id 参数必须是有效的整数");
        }

        // --- Helper Methods ---

        private JsonClass BuildSimpleListData(List<AssetIndexInfo> assets)
        {
            var resultData = new JsonClass();
            var assetsArray = new JsonArray();

            foreach (var asset in assets)
            {
                var assetObj = new JsonClass();
                assetObj.Add("id", new JsonData(asset.id));
                assetObj.Add("name", new JsonData(asset.name ?? ""));
                assetsArray.Add(assetObj);
            }

            resultData.Add("assets", assetsArray);
            resultData.Add("count", new JsonData(assets.Count));

            return resultData;
        }

        private JsonClass BuildSimplifiedAssetData(string name, string path, string category = null)
        {
            var data = new JsonClass();

            if (!string.IsNullOrEmpty(name))
            {
                data.Add("name", new JsonData(name));
            }

            if (!string.IsNullOrEmpty(path))
            {
                data.Add("path", new JsonData(path));
            }

            if (!string.IsNullOrEmpty(category) && category != "默认")
            {
                data.Add("category", new JsonData(category));
            }

            return data;
        }
    }
}

