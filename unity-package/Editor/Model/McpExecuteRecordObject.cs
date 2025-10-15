using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace UnityMcp.Models
{
    [FilePath("Library/McpExecuteRecordObject.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpExecuteRecordObject : ScriptableSingleton<McpExecuteRecordObject>
    {
        public List<McpExecuteRecord> records = new List<McpExecuteRecord>();
        public List<McpExecuteRecordGroup> recordGroups = new List<McpExecuteRecordGroup>();
        public string currentGroupId = "default"; // Currently selected groupID
        public bool useGrouping = true; // Whether to enable grouping feature（Enabled by default）

        // Initialization flag，Ensure initialization only occurs once
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
            public double duration; // Execution time（Millisecond）
            public string source; // Source of record："MCP Client" Or "Debug Window"
        }
        [System.Serializable]
        public class McpExecuteRecordGroup
        {
            public string id; // Group uniqueID
            public string name; // Group display name
            public string description; // Group description
            public List<McpExecuteRecord> records = new List<McpExecuteRecord>();
            public System.DateTime createdTime; // Creation time
            public bool isDefault; // Whether it is the default group
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
            saveRecords(); // Ensure saving
        }

        public void addRecord(string name, string cmd, string result, string error, double duration, string source)
        {
            // Ensure grouping feature is initialized
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
                // Use grouping mode，Add to the current group
                AddRecordToCurrentGroup(record);
            }
            else
            {
                // Traditional mode，Add to global record list
                records.Add(record);
                saveRecords(); // Need to save in traditional mode as well
            }
        }
        public void clearRecords()
        {
            // Prohibit clearing all，Clear only the current group
            EnsureGroupingEnabled();
            ClearCurrentGroupRecords();
            Debug.Log("[McpExecuteRecordObject] Current group records have been cleared（Prohibit clearing all）");
        }
        public void saveRecords()
        {
            // ScriptableSingleton Use Save() Method to persist data
            Save(true);
        }
        public void loadRecords()
        {
            records = new List<McpExecuteRecord>();
        }

        #region Group management function

        /// <summary>
        /// Ensure grouping feature is enabled and initialized
        /// This method is called before any record operation，Ensure even if Debug Window Not enabled，Records can also be saved correctly
        /// </summary>
        private void EnsureGroupingEnabled()
        {
            // If already initialized，Return directly
            if (isInitialized)
                return;

            // Enable grouping feature
            if (!useGrouping)
            {
                useGrouping = true;
            }

            // Initialize the default group
            InitializeDefaultGroup();

            // If global records There is old data in the list，Migrate to the default group
            if (records.Count > 0)
            {
                var defaultGroup = GetGroup("default");
                if (defaultGroup != null)
                {
                    // Migrate old records to default group
                    defaultGroup.records.AddRange(records);
                    records.Clear();
                    Debug.Log($"[McpExecuteRecordObject] Already {defaultGroup.records.Count} Old records migrated to default group");
                }
            }

            isInitialized = true;
            saveRecords();
        }

        /// <summary>
        /// Initialize the default group
        /// </summary>
        public void InitializeDefaultGroup()
        {
            if (recordGroups.Count == 0)
            {
                CreateGroup("default", "Default group", "System default group，Used to store uncategorized records", true);
            }

            // Ensure there is a default group
            if (!HasGroup("default"))
            {
                CreateGroup("default", "Default group", "System default group，Used to store uncategorized records", true);
            }

            // If current groupIDInvalid，Reset to default
            if (!HasGroup(currentGroupId))
            {
                currentGroupId = "default";
            }
        }

        /// <summary>
        /// Create new group
        /// </summary>
        public bool CreateGroup(string id, string name, string description = "", bool isDefault = false)
        {
            if (HasGroup(id))
            {
                Debug.LogWarning($"GroupID '{id}' Already exists");
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
        /// Delete group
        /// </summary>
        public bool DeleteGroup(string groupId)
        {
            if (groupId == "default")
            {
                Debug.LogWarning("Cannot delete default group");
                return false;
            }

            var group = GetGroup(groupId);
            if (group == null) return false;

            // Move the records of this group to the default group
            var defaultGroup = GetGroup("default");
            if (defaultGroup != null && group.records.Count > 0)
            {
                defaultGroup.records.AddRange(group.records);
            }

            recordGroups.Remove(group);

            // If the deleted group is the current group，Switch to the default group
            if (currentGroupId == groupId)
            {
                currentGroupId = "default";
            }

            saveRecords();
            return true;
        }

        /// <summary>
        /// Get group
        /// </summary>
        public McpExecuteRecordGroup GetGroup(string groupId)
        {
            return recordGroups.Find(g => g.id == groupId);
        }

        /// <summary>
        /// Check if the group exists
        /// </summary>
        public bool HasGroup(string groupId)
        {
            return recordGroups.Exists(g => g.id == groupId);
        }

        /// <summary>
        /// Get current group
        /// </summary>
        public McpExecuteRecordGroup GetCurrentGroup()
        {
            return GetGroup(currentGroupId);
        }

        /// <summary>
        /// Switch to the specified group
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
        /// Add records to the specified group
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
        /// Add records to the current group
        /// </summary>
        public void AddRecordToCurrentGroup(McpExecuteRecord record)
        {
            InitializeDefaultGroup(); // Ensure there is a default group
            AddRecordToGroup(currentGroupId, record);
        }

        /// <summary>
        /// Move records to the specified group
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
        /// Get records in the current group
        /// </summary>
        public List<McpExecuteRecord> GetCurrentGroupRecords()
        {
            // Ensure grouping feature is initialized
            EnsureGroupingEnabled();

            if (!useGrouping)
            {
                return records; // Return global records when grouping is not used
            }

            var currentGroup = GetCurrentGroup();
            return currentGroup?.records ?? new List<McpExecuteRecord>();
        }

        /// <summary>
        /// Clear records in the current group
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
        /// Rename group
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
        /// Get group statistics info
        /// </summary>
        public string GetGroupStatistics(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null) return "Group does not exist";

            int successCount = group.records.Count(r => r.success);
            int errorCount = group.records.Count - successCount;

            return $"{group.records.Count}Record(s) (Success:{successCount} Failure:{errorCount})";
        }

        #endregion
    }
}