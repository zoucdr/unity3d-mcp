using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace Unity.Mcp.Models
{
    [FilePath("Library/McpExecuteRecordObject.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpExecuteRecordObject : ScriptableSingleton<McpExecuteRecordObject>
    {
        public List<McpExecuteRecord> records = new List<McpExecuteRecord>();
        public List<McpExecuteRecordGroup> recordGroups = new List<McpExecuteRecordGroup>();
        public string currentGroupId = "default"; // 当前选中的分组ID
        public bool useGrouping = true; // 是否启用分组功能（默认启用）

        // 初始化标记，确保只初始化一次
        private bool isInitialized = false;
        [System.Serializable]
        public class McpExecuteRecord
        {
            public string name;
            public string cmd;
            public string result;
            public string error;
            public string timestamp;
            public bool success;
            public double duration; // 执行时间（毫秒）
            public string source; // 记录来源："MCP Client" 或 "Debug Window"
        }
        [System.Serializable]
        public class McpExecuteRecordGroup
        {
            public string id; // 分组唯一ID
            public string name; // 分组显示名称
            public string description; // 分组描述
            public List<McpExecuteRecord> records = new List<McpExecuteRecord>();
            public System.DateTime createdTime; // 创建时间
            public bool isDefault; // 是否为默认分组
        }
        public void addRecord(string name, string cmd, string result, string error)
        {
            records.Add(new McpExecuteRecord()
            {
                name = name,
                cmd = cmd,
                result = result,
                error = error,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                success = string.IsNullOrEmpty(error),
                duration = 0,
                source = "Legacy"
            });
            saveRecords(); // 确保保存
        }

        public void addRecord(string name, string cmd, string result, string error, double duration, string source)
        {
            // 确保分组功能已初始化
            EnsureGroupingEnabled();

            var record = new McpExecuteRecord()
            {
                name = name,
                cmd = cmd,
                result = result,
                error = error,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                success = string.IsNullOrEmpty(error),
                duration = duration,
                source = source
            };

            if (useGrouping)
            {
                // 使用分组模式，添加到当前分组
                AddRecordToCurrentGroup(record);
            }
            else
            {
                // 传统模式，添加到全局记录列表
                records.Add(record);
                saveRecords(); // 传统模式下也需要保存
            }
        }
        public void clearRecords()
        {
            // 禁止清空全部，仅清空当前分组
            EnsureGroupingEnabled();
            ClearCurrentGroupRecords();
            Debug.Log("[McpExecuteRecordObject] 已清空当前分组记录（禁止清空全部）");
        }
        public void saveRecords()
        {
            // ScriptableSingleton 使用 Save() 方法来持久化数据
            Save(true);
        }
        public void loadRecords()
        {
            records = new List<McpExecuteRecord>();
        }

        #region 分组管理功能

        /// <summary>
        /// 确保分组功能已启用并初始化
        /// 这个方法会在任何记录操作前被调用，确保即使 Debug Window 未开启，记录也能正确保存
        /// </summary>
        private void EnsureGroupingEnabled()
        {
            // 如果已经初始化过，直接返回
            if (isInitialized)
                return;

            // 启用分组功能
            if (!useGrouping)
            {
                useGrouping = true;
            }

            // 初始化默认分组
            InitializeDefaultGroup();

            // 如果全局 records 列表中有旧数据，迁移到默认分组
            if (records.Count > 0)
            {
                var defaultGroup = GetGroup("default");
                if (defaultGroup != null)
                {
                    // 迁移旧记录到默认分组
                    defaultGroup.records.AddRange(records);
                    records.Clear();
                    Debug.Log($"[McpExecuteRecordObject] 已将 {defaultGroup.records.Count} 条旧记录迁移到默认分组");
                }
            }

            isInitialized = true;
            saveRecords();
        }

        /// <summary>
        /// 初始化默认分组
        /// </summary>
        public void InitializeDefaultGroup()
        {
            if (recordGroups.Count == 0)
            {
                CreateGroup("default", "默认分组", "系统默认分组，用于存放未分类的记录", true);
            }

            // 确保有默认分组
            if (!HasGroup("default"))
            {
                CreateGroup("default", "默认分组", "系统默认分组，用于存放未分类的记录", true);
            }

            // 如果当前分组ID无效，重置为默认
            if (!HasGroup(currentGroupId))
            {
                currentGroupId = "default";
            }
        }

        /// <summary>
        /// 创建新分组
        /// </summary>
        public bool CreateGroup(string id, string name, string description = "", bool isDefault = false)
        {
            if (HasGroup(id))
            {
                Debug.LogWarning($"分组ID '{id}' 已存在");
                return false;
            }

            var newGroup = new McpExecuteRecordGroup()
            {
                id = id,
                name = name,
                description = description,
                createdTime = System.DateTime.Now,
                isDefault = isDefault
            };

            recordGroups.Add(newGroup);
            saveRecords();
            return true;
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        public bool DeleteGroup(string groupId)
        {
            if (groupId == "default")
            {
                Debug.LogWarning("不能删除默认分组");
                return false;
            }

            var group = GetGroup(groupId);
            if (group == null) return false;

            // 将该分组的记录移动到默认分组
            var defaultGroup = GetGroup("default");
            if (defaultGroup != null && group.records.Count > 0)
            {
                defaultGroup.records.AddRange(group.records);
            }

            recordGroups.Remove(group);

            // 如果删除的是当前分组，切换到默认分组
            if (currentGroupId == groupId)
            {
                currentGroupId = "default";
            }

            saveRecords();
            return true;
        }

        /// <summary>
        /// 获取分组
        /// </summary>
        public McpExecuteRecordGroup GetGroup(string groupId)
        {
            return recordGroups.Find(g => g.id == groupId);
        }

        /// <summary>
        /// 检查分组是否存在
        /// </summary>
        public bool HasGroup(string groupId)
        {
            return recordGroups.Exists(g => g.id == groupId);
        }

        /// <summary>
        /// 获取当前分组
        /// </summary>
        public McpExecuteRecordGroup GetCurrentGroup()
        {
            return GetGroup(currentGroupId);
        }

        /// <summary>
        /// 切换到指定分组
        /// </summary>
        public void SwitchToGroup(string groupId)
        {
            if (HasGroup(groupId))
            {
                currentGroupId = groupId;
                saveRecords();
            }
        }

        /// <summary>
        /// 向指定分组添加记录
        /// </summary>
        public void AddRecordToGroup(string groupId, McpExecuteRecord record)
        {
            var group = GetGroup(groupId);
            if (group != null)
            {
                group.records.Add(record);
                saveRecords();
            }
        }

        /// <summary>
        /// 向当前分组添加记录
        /// </summary>
        public void AddRecordToCurrentGroup(McpExecuteRecord record)
        {
            InitializeDefaultGroup(); // 确保有默认分组
            AddRecordToGroup(currentGroupId, record);
        }

        /// <summary>
        /// 移动记录到指定分组
        /// </summary>
        public bool MoveRecordToGroup(McpExecuteRecord record, string fromGroupId, string toGroupId)
        {
            var fromGroup = GetGroup(fromGroupId);
            var toGroup = GetGroup(toGroupId);

            if (fromGroup == null || toGroup == null) return false;

            if (fromGroup.records.Remove(record))
            {
                toGroup.records.Add(record);
                saveRecords();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前分组的记录
        /// </summary>
        public List<McpExecuteRecord> GetCurrentGroupRecords()
        {
            // 确保分组功能已初始化
            EnsureGroupingEnabled();

            if (!useGrouping)
            {
                return records; // 不使用分组时返回全局记录
            }

            var currentGroup = GetCurrentGroup();
            return currentGroup?.records ?? new List<McpExecuteRecord>();
        }

        /// <summary>
        /// 清空当前分组的记录
        /// </summary>
        public void ClearCurrentGroupRecords()
        {
            if (!useGrouping)
            {
                records.Clear();
            }
            else
            {
                var currentGroup = GetCurrentGroup();
                currentGroup?.records.Clear();
            }
            saveRecords();
        }

        /// <summary>
        /// 重命名分组
        /// </summary>
        public bool RenameGroup(string groupId, string newName)
        {
            var group = GetGroup(groupId);
            if (group != null)
            {
                group.name = newName;
                saveRecords();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取分组统计信息
        /// </summary>
        public string GetGroupStatistics(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null) return "分组不存在";

            int successCount = group.records.Count(r => r.success);
            int errorCount = group.records.Count - successCount;

            return $"{group.records.Count}个记录 (成功:{successCount} 失败:{errorCount})";
        }

        #endregion
    }
}