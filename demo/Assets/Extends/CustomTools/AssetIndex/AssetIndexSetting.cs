/*-*-* Copyright (c) Mycoria@
 * Author: AI Assistant
 * Creation Date: 2025-12-17
 * Version: 1.0.0
 * Description: 项目资源索引设置，用于存储和管理项目资源信息
 * Features:
 *   - 持久化存储资源索引信息
 *   - 支持JSON批量导入导出
 *   - 自动处理重复GUID
 *   - 分类和搜索功能
 *   - 自增唯一ID支持
 *_*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniMcp.Tools
{
    /// <summary>
    /// 资源索引信息数据结构
    /// </summary>
    [Serializable]
    public class AssetIndexInfo
    {
        [Tooltip("唯一ID（自增）")]
        public int id;

        [Tooltip("资源名称")]
        public string name;

        [Tooltip("资源GUID（用于存储，运行时转换为路径）")]
        public string guid;

        [Tooltip("分类标签")]
        public string category;

        [Tooltip("添加时间")]
        public string addTime;

        [Tooltip("备注信息")]
        public string note;

        public AssetIndexInfo()
        {
            id = 0;
            name = "";
            guid = "";
            category = "默认";
            addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            note = "";
        }

        public AssetIndexInfo(string assetName, string assetGuid, string cat = "默认", string noteText = "")
        {
            id = 0;
            name = assetName;
            guid = assetGuid;
            category = cat;
            addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            note = noteText;
        }

        /// <summary>
        /// 获取资源路径（实时从GUID转换）
        /// </summary>
        public string GetAssetPath()
        {
            return string.IsNullOrEmpty(guid) ? "" : AssetDatabase.GUIDToAssetPath(guid);
        }
    }

    /// <summary>
    /// 资源索引设置，持久化存储资源索引信息
    /// </summary>
    [FilePath("ProjectSettings/AssetIndexSetting.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AssetIndexSetting : ScriptableSingleton<AssetIndexSetting>
    {
        [SerializeField]
        [Tooltip("所有保存的资源索引信息列表")]
        private List<AssetIndexInfo> assetIndices = new List<AssetIndexInfo>();

        [SerializeField]
        [Tooltip("下一个可用的ID")]
        private int nextId = 1;

        /// <summary>
        /// 获取所有资源索引信息
        /// </summary>
        public List<AssetIndexInfo> AssetIndices => assetIndices;

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

            var usedIds = new HashSet<int>();
            foreach (var asset in assetIndices)
            {
                if (asset.id <= 0 || usedIds.Contains(asset.id))
                {
                    needsFix = true;
                    break;
                }
                usedIds.Add(asset.id);
                if (asset.id > maxId)
                {
                    maxId = asset.id;
                }
            }

            if (needsFix)
            {
                Debug.LogWarning("[AssetIndexSetting] 检测到无效或重复的ID，正在修复...");
                int currentId = 1;
                foreach (var asset in assetIndices)
                {
                    asset.id = currentId++;
                }
                nextId = currentId;
                Save();
                Debug.Log($"[AssetIndexSetting] ID修复完成，共修复 {assetIndices.Count} 个资源索引");
            }
            else
            {
                if (nextId <= maxId)
                {
                    nextId = maxId + 1;
                }
            }
        }

        /// <summary>
        /// 添加资源索引信息
        /// </summary>
        public void AddAssetIndex(string name, string guid, string category = "默认", string note = "")
        {
            // 检查是否已存在相同GUID
            var existing = assetIndices.Find(a => a.guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Debug.LogWarning($"[AssetIndexSetting] GUID已存在: {guid}，将更新其信息");
                existing.name = name;
                existing.category = category;
                existing.note = note;
                existing.addTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                var newAsset = new AssetIndexInfo(name, guid, category, note);
                newAsset.id = nextId++;
                assetIndices.Add(newAsset);
                Debug.Log($"[AssetIndexSetting] 添加资源索引: {name} - {guid} (ID: {newAsset.id})");
            }

            Save();
        }

        /// <summary>
        /// 通过路径添加资源索引
        /// </summary>
        public void AddAssetIndexByPath(string name, string assetPath, string category = "默认", string note = "")
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AssetIndexSetting] 无法获取路径的GUID: {assetPath}");
                return;
            }
            AddAssetIndex(name, guid, category, note);
        }

        /// <summary>
        /// 删除资源索引信息（通过GUID）
        /// </summary>
        public bool RemoveAssetIndex(string guid)
        {
            var asset = assetIndices.Find(a => a.guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
            if (asset != null)
            {
                assetIndices.Remove(asset);
                Save();
                Debug.Log($"[AssetIndexSetting] 删除资源索引: {guid}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除资源索引信息（通过ID）
        /// </summary>
        public bool RemoveAssetIndexById(int id)
        {
            var asset = assetIndices.Find(a => a.id == id);
            if (asset != null)
            {
                assetIndices.Remove(asset);
                Save();
                Debug.Log($"[AssetIndexSetting] 删除资源索引: {asset.name} (ID: {id})");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除资源索引信息（通过索引）
        /// </summary>
        public bool RemoveAssetIndexAt(int index)
        {
            if (index >= 0 && index < assetIndices.Count)
            {
                var asset = assetIndices[index];
                assetIndices.RemoveAt(index);
                Save();
                Debug.Log($"[AssetIndexSetting] 删除资源索引: {asset.name}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 查询资源索引信息（通过关键词）
        /// </summary>
        public List<AssetIndexInfo> SearchAssetIndices(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return new List<AssetIndexInfo>(assetIndices);
            }

            var results = new List<AssetIndexInfo>();
            foreach (var asset in assetIndices)
            {
                string assetPath = asset.GetAssetPath();

                if (asset.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    assetPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.category.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.note.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add(asset);
                }
            }
            return results;
        }

        /// <summary>
        /// 根据分类查询
        /// </summary>
        public List<AssetIndexInfo> GetAssetIndicesByCategory(string category)
        {
            return assetIndices.FindAll(a => a.category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public List<string> GetAllCategories()
        {
            var categories = new HashSet<string>();
            foreach (var asset in assetIndices)
            {
                if (!string.IsNullOrEmpty(asset.category))
                {
                    categories.Add(asset.category);
                }
            }
            return new List<string>(categories);
        }

        /// <summary>
        /// 清空所有资源索引信息
        /// </summary>
        public void ClearAll()
        {
            assetIndices.Clear();
            nextId = 1;
            Save();
            Debug.Log("[AssetIndexSetting] 清空所有资源索引信息");
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
        /// 获取资源索引数量
        /// </summary>
        public int GetCount()
        {
            return assetIndices.Count;
        }

        /// <summary>
        /// 通过索引获取资源
        /// </summary>
        public AssetIndexInfo GetAssetIndexAt(int index)
        {
            if (index >= 0 && index < assetIndices.Count)
            {
                return assetIndices[index];
            }
            return null;
        }

        /// <summary>
        /// 通过ID获取资源
        /// </summary>
        public AssetIndexInfo GetAssetIndexById(int id)
        {
            return assetIndices.Find(a => a.id == id);
        }

        /// <summary>
        /// 通过GUID获取资源
        /// </summary>
        public AssetIndexInfo GetAssetIndexByGuid(string guid)
        {
            return assetIndices.Find(a => a.guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 导出所有资源索引为JSON
        /// </summary>
        public string ExportToJson()
        {
            var wrapper = new AssetIndexListWrapper { assetIndices = assetIndices };
            return JsonUtility.ToJson(wrapper, true);
        }

        /// <summary>
        /// 从JSON导入资源索引
        /// </summary>
        public int ImportFromJson(string jsonContent, bool overwrite = false)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<AssetIndexListWrapper>(jsonContent);
                if (wrapper == null || wrapper.assetIndices == null)
                {
                    Debug.LogError("[AssetIndexSetting] JSON格式错误");
                    return 0;
                }

                if (overwrite)
                {
                    assetIndices.Clear();
                    nextId = 1;
                }

                int addedCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                foreach (var asset in wrapper.assetIndices)
                {
                    if (string.IsNullOrEmpty(asset.guid) || string.IsNullOrEmpty(asset.name))
                    {
                        skippedCount++;
                        continue;
                    }

                    var existing = assetIndices.Find(a => a.guid.Equals(asset.guid, StringComparison.OrdinalIgnoreCase));
                    if (existing != null && !overwrite)
                    {
                        existing.name = asset.name;
                        existing.category = asset.category;
                        existing.note = asset.note;
                        existing.addTime = asset.addTime;
                        updatedCount++;
                    }
                    else if (existing == null)
                    {
                        asset.id = nextId++;
                        assetIndices.Add(asset);
                        addedCount++;
                    }
                }

                Save();
                Debug.Log($"[AssetIndexSetting] 导入完成 - 新增: {addedCount}, 更新: {updatedCount}, 跳过: {skippedCount}");
                return addedCount + updatedCount;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetIndexSetting] 导入失败: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 通过路径获取资源索引
        /// </summary>
        public AssetIndexInfo GetAssetIndexByPath(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return null;
            return GetAssetIndexByGuid(guid);
        }
    }

    /// <summary>
    /// JSON序列化包装类
    /// </summary>
    [Serializable]
    public class AssetIndexListWrapper
    {
        public List<AssetIndexInfo> assetIndices;
    }
}

