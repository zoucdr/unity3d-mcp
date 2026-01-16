/*-*-* Copyright (c) Mycoria@
 * Author: zouhangte
 * Creation Date: 2025-12-12
 * Version: 1.2.0
 * Description: MCP网页管理工具，实现网页记录与查询功能
 * 安全说明: 已移除危险的clear操作，改为安全的update修改功能
 * Features:
 *   - 支持网页的增删改查操作
 *   - 支持分类管理和搜索
 *   - 禁用危险的clear操作，改为update修改功能
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
    /// 网页管理工具，支持添加、删除、查询、列表等操作
    /// 对应方法名: webpage_manage
    /// </summary>
    [ToolName("webpage_manage", "网络工具")]
    public class WebPageManage : StateMethodBase
    {
        public override string Description => "网页管理工具，支持添加、删除、查询、列表等操作";
        /// <summary>
        /// 创建当前方法支持的参数键列表
        /// </summary>
        protected override MethodKey[] CreateKeys()
        {
            return new MethodKey[]
            {
                // 操作类型 - 枚举
                new MethodStr("action", "操作类型", false)
                    .SetEnumValues("add", "remove", "update", "search","categories", "open", "details"),
                
                // 网站URL
                new MethodStr("url", "网站URL地址")
                    .AddExamples("https://docs.unity3d.com", "https://github.com/user/repo"),
                
                // 网站描述
                new MethodStr("description", "网站名称或说明")
                    .AddExamples("Unity官方文档", "GitHub仓库", "项目管理后台"),
                
                // 分类标签
                new MethodStr("category", "分类标签，用于组织网页")
                    .AddExamples("文档", "工具", "社区", "项目")
                    .SetDefault("默认"),
                
                // 备注信息
                new MethodStr("note", "备注信息")
                    .AddExamples("常用参考", "重要资源", "待整理"),
                
                // 搜索正则表达式
                new MethodStr("pattern", "正则表达式模式（用于search操作，匹配URL、描述、分类、备注）")
                    .AddExamples("Unity.*API", "^https://github", "文档|工具"),
                
                // 唯一ID
                new MethodInt("id", "网页唯一ID（用于remove和update操作，推荐使用）")
                    .AddExample(1)
                    .AddExample(5),
            };
        }

        protected override StateTree CreateStateTree()
        {
            return StateTreeBuilder
                .Create()
                .Key("action")
                    .Leaf("add", HandleAddWebPage)
                    .Leaf("remove", HandleRemoveWebPage)
                    .Leaf("update", HandleUpdateWebPage)
                    .Leaf("search", HandleSearchWebPage)
                    .Leaf("categories", HandleGetCategories)
                    .Leaf("open", HandleOpenWebPage)
                    .Leaf("details", HandleGetDetails)
                    .DefaultLeaf(HandleUnknownAction)
                .Build();
        }

        // --- State Tree Action Handlers ---

        /// <summary>
        /// 处理添加网页的操作
        /// </summary>
        private object HandleAddWebPage(JsonClass args)
        {
            try
            {
                string description = ExtractDescription(args);
                string url = ExtractUrl(args);
                string category = ExtractCategory(args);
                string note = ExtractNote(args);

                // 验证URL格式
                if (!IsValidUrl(url))
                {
                    return Response.Error($"无效的URL格式: {url}");
                }

                // 添加到设置中
                WebPageSetting.instance.AddWebPage(description, url, category, note);

                var resultData = BuildSimplifiedPageData(description, url, category);
                resultData.Add("total", new JsonData(WebPageSetting.instance.GetCount()));

                McpLogger.Log($"[WebPageManager] 添加网页成功: {description} - {url}");
                return Response.Success($"已添加: {description}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 添加网页失败: {e.Message}");
                return Response.Error($"添加网页失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理删除网页的操作
        /// </summary>
        private object HandleRemoveWebPage(JsonClass args)
        {
            try
            {
                // 优先使用ID删除（推荐）
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    var page = WebPageSetting.instance.GetWebPageById(id);

                    if (page != null)
                    {
                        bool success = WebPageSetting.instance.RemoveWebPageById(id);

                        if (success)
                        {
                            var resultData = new JsonClass();
                            resultData.Add("total", new JsonData(WebPageSetting.instance.GetCount()));

                            McpLogger.Log($"[WebPageManager] 删除网页成功: {page.description} (ID: {id})");
                            return Response.Success($"已删除: {page.description}", resultData);
                        }
                    }

                    return Response.Error($"未找到ID: {id}");
                }
                // 使用URL删除
                else if (args.ContainsKey("url"))
                {
                    string url = ExtractUrl(args);
                    bool success = WebPageSetting.instance.RemoveWebPage(url);

                    if (success)
                    {
                        var resultData = new JsonClass();
                        resultData.Add("total", new JsonData(WebPageSetting.instance.GetCount()));

                        McpLogger.Log($"[WebPageManager] 删除网页成功: {url}");
                        return Response.Success($"已删除", resultData);
                    }
                    else
                    {
                        return Response.Error($"未找到URL: {url}");
                    }
                }
                // 使用索引删除（已废弃）
                else if (args.ContainsKey("index"))
                {
                    int index = ExtractIndex(args);
                    var page = WebPageSetting.instance.GetWebPageAt(index);

                    if (page != null)
                    {
                        bool success = WebPageSetting.instance.RemoveWebPageAt(index);

                        if (success)
                        {
                            var resultData = new JsonClass();
                            resultData.Add("total", new JsonData(WebPageSetting.instance.GetCount()));

                            McpLogger.Log($"[WebPageManager] 删除网页成功: {page.description}");
                            return Response.Success($"已删除: {page.description}", resultData);
                        }
                    }

                    return Response.Error($"无效的索引: {index}");
                }
                else
                {
                    return Response.Error("删除操作需要提供 'id'（推荐）、'url' 或 'index' 参数");
                }
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 删除网页失败: {e.Message}");
                return Response.Error($"删除网页失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理搜索网页的操作（使用正则表达式）
        /// </summary>
        private object HandleSearchWebPage(JsonClass args)
        {
            try
            {
                string pattern = ExtractPattern(args);

                if (string.IsNullOrEmpty(pattern))
                {
                    return Response.Error("pattern 参数不能为空");
                }

                var allPages = WebPageSetting.instance.WebPages;
                var matchedPages = new HashSet<int>(); // 使用HashSet去重
                var results = new List<WebPageInfo>();

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

                foreach (var page in allPages)
                {
                    // 检查是否已经添加过（去重）
                    if (matchedPages.Contains(page.id))
                        continue;

                    // 对URL、描述、分类、备注进行匹配
                    bool matched = false;

                    if (!string.IsNullOrEmpty(page.url) && regex.IsMatch(page.url))
                        matched = true;
                    else if (!string.IsNullOrEmpty(page.description) && regex.IsMatch(page.description))
                        matched = true;
                    else if (!string.IsNullOrEmpty(page.category) && regex.IsMatch(page.category))
                        matched = true;
                    else if (!string.IsNullOrEmpty(page.note) && regex.IsMatch(page.note))
                        matched = true;

                    if (matched)
                    {
                        matchedPages.Add(page.id);
                        results.Add(page);
                    }
                }

                // 只返回id和name的简化列表
                var resultData = BuildSimpleListData(results);

                McpLogger.Log($"[WebPageManager] 正则搜索 '{pattern}'，找到 {results.Count} 个");
                return Response.Success($"找到 {results.Count} 个", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 搜索网页失败: {e.Message}");
                return Response.Error($"搜索网页失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理修改网页的操作
        /// </summary>
        private object HandleUpdateWebPage(JsonClass args)
        {
            try
            {
                // 支持通过ID、URL或索引定位要修改的网页
                WebPageInfo targetPage = null;
                string identifier = "";

                // 优先使用ID（推荐）
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    targetPage = WebPageSetting.instance.GetWebPageById(id);
                    identifier = $"ID {id}";

                    if (targetPage == null)
                    {
                        return Response.Error($"未找到ID: {id}");
                    }
                }
                // 使用URL
                else if (args.ContainsKey("url"))
                {
                    string url = ExtractUrl(args);
                    targetPage = WebPageSetting.instance.GetWebPageByUrl(url);
                    identifier = url;

                    if (targetPage == null)
                    {
                        return Response.Error($"未找到URL: {url}");
                    }
                }
                // 使用索引（已废弃）
                else if (args.ContainsKey("index"))
                {
                    int index = ExtractIndex(args);
                    targetPage = WebPageSetting.instance.GetWebPageAt(index);
                    identifier = $"索引 {index}";

                    if (targetPage == null)
                    {
                        return Response.Error($"无效的索引: {index}");
                    }
                }
                else
                {
                    return Response.Error("修改操作需要提供 'id'（推荐）、'url' 或 'index' 参数来定位要修改的网页");
                }

                // 保存原始信息用于日志
                string originalDesc = targetPage.description;
                bool hasChanges = false;

                // 更新各个字段（如果提供了新值）
                if (args.ContainsKey("description"))
                {
                    string newDesc = args["description"]?.Value;
                    if (!string.IsNullOrEmpty(newDesc) && newDesc != targetPage.description)
                    {
                        targetPage.description = newDesc;
                        hasChanges = true;
                    }
                }

                if (args.ContainsKey("category"))
                {
                    string newCategory = args["category"]?.Value;
                    if (newCategory != null && newCategory != targetPage.category)
                    {
                        targetPage.category = string.IsNullOrEmpty(newCategory) ? "默认" : newCategory;
                        hasChanges = true;
                    }
                }

                if (args.ContainsKey("note"))
                {
                    string newNote = args["note"]?.Value ?? "";
                    if (newNote != targetPage.note)
                    {
                        targetPage.note = newNote;
                        hasChanges = true;
                    }
                }

                if (!hasChanges)
                {
                    return Response.Error("未提供任何要修改的字段（description、category、note）");
                }

                // 保存更改
                WebPageSetting.Save();

                // 构建返回数据
                var resultData = new JsonClass();
                resultData.Add("id", new JsonData(targetPage.id));
                resultData.Add("description", new JsonData(targetPage.description));
                resultData.Add("url", new JsonData(targetPage.url));

                if (!string.IsNullOrEmpty(targetPage.category) && targetPage.category != "默认")
                {
                    resultData.Add("category", new JsonData(targetPage.category));
                }

                if (!string.IsNullOrEmpty(targetPage.note))
                {
                    resultData.Add("note", new JsonData(targetPage.note));
                }

                McpLogger.Log($"[WebPageManager] 修改网页成功: {originalDesc} ({identifier})");
                return Response.Success($"已修改: {targetPage.description}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 修改网页失败: {e.Message}");
                return Response.Error($"修改网页失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理获取所有分类的操作
        /// </summary>
        private object HandleGetCategories(JsonClass args)
        {
            try
            {
                var categories = WebPageSetting.instance.GetAllCategories();

                var resultData = new JsonClass();
                var categoriesArray = new JsonArray();

                foreach (var category in categories)
                {
                    var categoryObj = new JsonClass();
                    categoryObj.Add("name", new JsonData(category));

                    var pagesInCategory = WebPageSetting.instance.GetWebPagesByCategory(category);
                    categoryObj.Add("count", new JsonData(pagesInCategory.Count));

                    categoriesArray.Add(categoryObj);
                }

                resultData.Add("categories", categoriesArray);
                resultData.Add("total", new JsonData(categories.Count));

                McpLogger.Log($"[WebPageManager] 获取分类，共 {categories.Count} 个");
                return Response.Success($"{categories.Count} 个分类", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 获取分类失败: {e.Message}");
                return Response.Error($"获取分类失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理打开网页的操作
        /// </summary>
        private object HandleOpenWebPage(JsonClass args)
        {
            try
            {
                string url = null;

                // 支持通过ID、URL或索引打开
                if (args.ContainsKey("id"))
                {
                    int id = ExtractId(args);
                    var page = WebPageSetting.instance.GetWebPageById(id);
                    if (page != null)
                    {
                        url = page.url;
                    }
                    else
                    {
                        return Response.Error($"未找到ID: {id}");
                    }
                }
                else if (args.ContainsKey("url"))
                {
                    url = ExtractUrl(args);
                }
                else if (args.ContainsKey("index"))
                {
                    int index = ExtractIndex(args);
                    var page = WebPageSetting.instance.GetWebPageAt(index);
                    if (page != null)
                    {
                        url = page.url;
                    }
                    else
                    {
                        return Response.Error($"无效的索引: {index}");
                    }
                }
                else
                {
                    return Response.Error("打开操作需要提供 'id'（推荐）、'url' 或 'index' 参数");
                }

                if (string.IsNullOrEmpty(url))
                {
                    return Response.Error("URL不能为空");
                }

                // 在浏览器中打开URL
                Application.OpenURL(url);

                var resultData = new JsonClass();
                resultData.Add("url", new JsonData(url));

                McpLogger.Log($"[WebPageManager] 打开网页: {url}");
                return Response.Success($"已打开: {url}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 打开网页失败: {e.Message}");
                return Response.Error($"打开网页失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理获取网页详细信息的操作
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
                var page = WebPageSetting.instance.GetWebPageById(id);

                if (page == null)
                {
                    return Response.Error($"未找到ID: {id}");
                }

                // 返回完整详细信息
                var resultData = new JsonClass();
                resultData.Add("id", new JsonData(page.id));
                resultData.Add("description", new JsonData(page.description ?? ""));
                resultData.Add("url", new JsonData(page.url ?? ""));
                resultData.Add("category", new JsonData(page.category ?? "默认"));
                resultData.Add("note", new JsonData(page.note ?? ""));
                resultData.Add("addTime", new JsonData(page.addTime ?? ""));

                McpLogger.Log($"[WebPageManager] 获取详情: ID {id} - {page.description}");
                return Response.Success($"详情: {page.description}", resultData);
            }
            catch (Exception e)
            {
                LogError($"[WebPageManager] 获取详情失败: {e.Message}");
                return Response.Error($"获取详情失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理未知操作的回调方法
        /// </summary>
        private object HandleUnknownAction(JsonClass args)
        {
            string action = args["action"]?.Value;
            if (string.IsNullOrEmpty(action)) action = "null";
            return Response.Error($"未知操作: '{action}'。有效操作: 'add'(添加), 'remove'(删除), 'update'(修改), 'search'(正则搜索), 'list'(列表), 'categories'(分类), 'open'(打开), 'details'(详情)");
        }

        // --- Parameter Extraction Helper Methods ---

        /// <summary>
        /// 提取描述参数
        /// </summary>
        private string ExtractDescription(JsonClass args)
        {
            string description = args["description"]?.Value;
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("description 参数是必需的且不能为空");
            }
            return description;
        }

        /// <summary>
        /// 提取URL参数
        /// </summary>
        private string ExtractUrl(JsonClass args)
        {
            string url = args["url"]?.Value;
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("url 参数是必需的且不能为空");
            }
            return url;
        }

        /// <summary>
        /// 提取分类参数
        /// </summary>
        private string ExtractCategory(JsonClass args)
        {
            string category = args["category"]?.Value;
            return string.IsNullOrEmpty(category) ? "默认" : category;
        }

        /// <summary>
        /// 提取备注参数
        /// </summary>
        private string ExtractNote(JsonClass args)
        {
            return args["note"]?.Value ?? "";
        }

        /// <summary>
        /// 提取关键词参数
        /// </summary>
        private string ExtractKeyword(JsonClass args)
        {
            return args["keyword"]?.Value ?? "";
        }

        /// <summary>
        /// 提取正则表达式模式参数
        /// </summary>
        private string ExtractPattern(JsonClass args)
        {
            return args["pattern"]?.Value ?? "";
        }

        /// <summary>
        /// 提取ID参数
        /// </summary>
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

        /// <summary>
        /// 提取索引参数（已废弃，建议使用ID）
        /// </summary>
        private int ExtractIndex(JsonClass args)
        {
            var indexNode = args["index"];
            if (indexNode != null)
            {
                if (int.TryParse(indexNode.Value, out int index))
                {
                    return index;
                }
            }
            throw new ArgumentException("index 参数必须是有效的整数");
        }

        // --- Helper Methods ---

        /// <summary>
        /// 验证URL格式
        /// </summary>
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // 简单的URL验证
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 构建简化的列表数据（只包含id和name）
        /// </summary>
        private JsonClass BuildSimpleListData(List<WebPageInfo> pages)
        {
            var resultData = new JsonClass();
            var pagesArray = new JsonArray();

            foreach (var page in pages)
            {
                var pageObj = new JsonClass();
                pageObj.Add("id", new JsonData(page.id));
                pageObj.Add("name", new JsonData(page.description ?? ""));
                pagesArray.Add(pageObj);
            }

            resultData.Add("pages", pagesArray);
            resultData.Add("count", new JsonData(pages.Count));

            return resultData;
        }

        /// <summary>
        /// 构建网页列表数据（优化版：移除空字段，简化数据结构）
        /// </summary>
        private JsonClass BuildWebPageListData(List<WebPageInfo> pages)
        {
            var resultData = new JsonClass();
            var pagesArray = new JsonArray();

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var pageObj = new JsonClass();

                // 唯一ID（最重要，推荐使用）
                pageObj.Add("id", new JsonData(page.id));

                // 索引（用于向后兼容）
                pageObj.Add("i", new JsonData(i));

                // 必需字段
                if (!string.IsNullOrEmpty(page.description))
                {
                    pageObj.Add("d", new JsonData(page.description));
                }

                if (!string.IsNullOrEmpty(page.url))
                {
                    pageObj.Add("u", new JsonData(page.url));
                }

                // 可选字段：只在非空且不是默认值时添加
                if (!string.IsNullOrEmpty(page.category) && page.category != "默认")
                {
                    pageObj.Add("c", new JsonData(page.category));
                }

                if (!string.IsNullOrEmpty(page.note))
                {
                    pageObj.Add("n", new JsonData(page.note));
                }

                // 时间字段：简化格式（只保留日期部分）
                if (!string.IsNullOrEmpty(page.addTime))
                {
                    // 提取日期部分，去掉时间
                    string dateOnly = page.addTime.Split(' ')[0];
                    pageObj.Add("t", new JsonData(dateOnly));
                }

                pagesArray.Add(pageObj);
            }

            resultData.Add("pages", pagesArray);
            resultData.Add("count", new JsonData(pages.Count));

            return resultData;
        }

        /// <summary>
        /// 构建简化的网页数据（用于add和remove操作的返回）
        /// </summary>
        private JsonClass BuildSimplifiedPageData(string description, string url, string category = null)
        {
            var data = new JsonClass();

            if (!string.IsNullOrEmpty(description))
            {
                data.Add("description", new JsonData(description));
            }

            if (!string.IsNullOrEmpty(url))
            {
                data.Add("url", new JsonData(url));
            }

            // 只在非默认值时添加分类
            if (!string.IsNullOrEmpty(category) && category != "默认")
            {
                data.Add("category", new JsonData(category));
            }

            return data;
        }
    }
}

