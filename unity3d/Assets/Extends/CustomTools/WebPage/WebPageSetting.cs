/*-*-* Copyright (c) Mycoria@
 * Author: zouhangte
 * Creation Date: 2025-12-12
 * Version: 1.2.0
 * Description: 网页记录设置，用于存储常用网站信息
 * Features:
 *   - 持久化存储网页信息
 *   - 支持JSON批量导入导出
 *   - 自动处理重复URL
 *   - 分类和搜索功能
 *   - 自增唯一ID支持
 *_*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 网页信息数据结构
/// </summary>
[Serializable]
public class WebPageInfo
{
    [Tooltip("唯一ID（自增）")]
    public int id;

    [Tooltip("网站名称或说明")]
    public string description;

    [Tooltip("网站URL地址")]
    public string url;

    [Tooltip("分类标签")]
    public string category;

    [Tooltip("添加时间")]
    public string addTime;

    [Tooltip("备注信息")]
    public string note;

    public WebPageInfo()
    {
        id = 0; // 将由 WebPageSetting 自动分配
        description = "";
        url = "";
        category = "默认";
        addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        note = "";
    }

    public WebPageInfo(string desc, string urlAddress, string cat = "默认", string noteText = "")
    {
        id = 0; // 将由 WebPageSetting 自动分配
        description = desc;
        url = urlAddress;
        category = cat;
        addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        note = noteText;
    }
}

/// <summary>
/// 网页记录设置，持久化存储网页信息
/// </summary>
[FilePath("ProjectSettings/WebPageSetting.asset", FilePathAttribute.Location.ProjectFolder)]
public class WebPageSetting : ScriptableSingleton<WebPageSetting>
{
    [SerializeField]
    [Tooltip("所有保存的网页信息列表")]
    private List<WebPageInfo> webPages = new List<WebPageInfo>();

    [SerializeField]
    [Tooltip("下一个可用的ID")]
    private int nextId = 1;

    /// <summary>
    /// 获取所有网页信息
    /// </summary>
    public List<WebPageInfo> WebPages => webPages;

    /// <summary>
    /// 初始化时检查并修复ID
    /// </summary>
    private void OnEnable()
    {
        ValidateAndFixIds();
    }

    /// <summary>
    /// 验证并修复ID（用于升级旧数据）
    /// </summary>
    private void ValidateAndFixIds()
    {
        bool needsFix = false;
        int maxId = 0;

        // 检查是否有重复ID或无效ID
        var usedIds = new HashSet<int>();
        foreach (var page in webPages)
        {
            if (page.id <= 0 || usedIds.Contains(page.id))
            {
                needsFix = true;
                break;
            }
            usedIds.Add(page.id);
            if (page.id > maxId)
            {
                maxId = page.id;
            }
        }

        // 如果需要修复，重新分配所有ID
        if (needsFix)
        {
            Debug.LogWarning("[WebPageSetting] 检测到无效或重复的ID，正在修复...");
            int currentId = 1;
            foreach (var page in webPages)
            {
                page.id = currentId++;
            }
            nextId = currentId;
            Save();
            Debug.Log($"[WebPageSetting] ID修复完成，共修复 {webPages.Count} 个网页");
        }
        else
        {
            // 确保nextId正确
            if (nextId <= maxId)
            {
                nextId = maxId + 1;
            }
        }
    }

    /// <summary>
    /// 添加网页信息
    /// </summary>
    public void AddWebPage(string description, string url, string category = "默认", string note = "")
    {
        // 检查是否已存在相同URL
        var existing = webPages.Find(w => w.url.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            Debug.LogWarning($"[WebPageSetting] URL已存在: {url}，将更新其信息");
            existing.description = description;
            existing.category = category;
            existing.note = note;
            existing.addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // 保持原有的 ID 不变
        }
        else
        {
            var newPage = new WebPageInfo(description, url, category, note);
            newPage.id = nextId++;
            webPages.Add(newPage);
            Debug.Log($"[WebPageSetting] 添加网页: {description} - {url} (ID: {newPage.id})");
        }

        Save();
    }

    /// <summary>
    /// 删除网页信息（通过URL）
    /// </summary>
    public bool RemoveWebPage(string url)
    {
        var page = webPages.Find(w => w.url.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (page != null)
        {
            webPages.Remove(page);
            Save();
            Debug.Log($"[WebPageSetting] 删除网页: {url}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除网页信息（通过ID）
    /// </summary>
    public bool RemoveWebPageById(int id)
    {
        var page = webPages.Find(w => w.id == id);
        if (page != null)
        {
            webPages.Remove(page);
            Save();
            Debug.Log($"[WebPageSetting] 删除网页: {page.description} (ID: {id})");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除网页信息（通过索引）
    /// </summary>
    public bool RemoveWebPageAt(int index)
    {
        if (index >= 0 && index < webPages.Count)
        {
            var page = webPages[index];
            webPages.RemoveAt(index);
            Save();
            Debug.Log($"[WebPageSetting] 删除网页: {page.description}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 查询网页信息（通过描述或URL关键词）
    /// </summary>
    public List<WebPageInfo> SearchWebPages(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return new List<WebPageInfo>(webPages);
        }

        var results = new List<WebPageInfo>();
        foreach (var page in webPages)
        {
            if (page.description.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                page.url.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                page.category.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                page.note.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(page);
            }
        }
        return results;
    }

    /// <summary>
    /// 根据分类查询
    /// </summary>
    public List<WebPageInfo> GetWebPagesByCategory(string category)
    {
        return webPages.FindAll(w => w.category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有分类
    /// </summary>
    public List<string> GetAllCategories()
    {
        var categories = new HashSet<string>();
        foreach (var page in webPages)
        {
            if (!string.IsNullOrEmpty(page.category))
            {
                categories.Add(page.category);
            }
        }
        return new List<string>(categories);
    }

    /// <summary>
    /// 清空所有网页信息
    /// </summary>
    public void ClearAll()
    {
        webPages.Clear();
        Save();
        Debug.Log("[WebPageSetting] 清空所有网页信息");
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public static void Save()
    {
        EditorUtility.SetDirty(instance);
        instance.Save(true);
    }

    /// <summary>
    /// 获取网页数量
    /// </summary>
    public int GetCount()
    {
        return webPages.Count;
    }

    /// <summary>
    /// 通过索引获取网页
    /// </summary>
    public WebPageInfo GetWebPageAt(int index)
    {
        if (index >= 0 && index < webPages.Count)
        {
            return webPages[index];
        }
        return null;
    }

    /// <summary>
    /// 通过ID获取网页
    /// </summary>
    public WebPageInfo GetWebPageById(int id)
    {
        return webPages.Find(w => w.id == id);
    }

    /// <summary>
    /// 通过URL获取网页
    /// </summary>
    public WebPageInfo GetWebPageByUrl(string url)
    {
        return webPages.Find(w => w.url.Equals(url, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 导出所有网页为JSON
    /// </summary>
    public string ExportToJson()
    {
        var wrapper = new WebPageListWrapper { webpages = webPages };
        return JsonUtility.ToJson(wrapper, true);
    }

    /// <summary>
    /// 从JSON导入网页（追加模式）
    /// </summary>
    public int ImportFromJson(string jsonContent, bool overwrite = false)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<WebPageListWrapper>(jsonContent);
            if (wrapper == null || wrapper.webpages == null)
            {
                Debug.LogError("[WebPageSetting] JSON格式错误");
                return 0;
            }

            if (overwrite)
            {
                webPages.Clear();
                nextId = 1; // 重置ID计数器
            }

            int addedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var page in wrapper.webpages)
            {
                // 验证必填字段
                if (string.IsNullOrEmpty(page.url) || string.IsNullOrEmpty(page.description))
                {
                    skippedCount++;
                    continue;
                }

                // 检查URL是否已存在
                var existing = webPages.Find(w => w.url.Equals(page.url, StringComparison.OrdinalIgnoreCase));
                if (existing != null && !overwrite)
                {
                    // 更新已存在的记录（保持原有ID）
                    existing.description = page.description;
                    existing.category = page.category;
                    existing.note = page.note;
                    existing.addTime = page.addTime;
                    updatedCount++;
                }
                else if (existing == null)
                {
                    // 添加新记录，分配新ID
                    page.id = nextId++;
                    webPages.Add(page);
                    addedCount++;
                }
            }

            Save();
            Debug.Log($"[WebPageSetting] 导入完成 - 新增: {addedCount}, 更新: {updatedCount}, 跳过: {skippedCount}");
            return addedCount + updatedCount;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebPageSetting] 导入失败: {e.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 批量添加网页
    /// </summary>
    public void AddWebPages(List<WebPageInfo> pages)
    {
        foreach (var page in pages)
        {
            AddWebPage(page.description, page.url, page.category, page.note);
        }
    }
}

/// <summary>
/// JSON序列化包装类
/// </summary>
[Serializable]
public class WebPageListWrapper
{
    public List<WebPageInfo> webpages;
}

