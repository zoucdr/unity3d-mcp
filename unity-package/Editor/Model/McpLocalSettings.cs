using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UniMcp
{
    /// <summary>
    /// MCP本地设置管理器，用于管理MCP相关的本地配置和偏好设置
    /// 保存到Library/McpLocalSettings.asset，不会被版本控制系统跟踪
    /// </summary>
    [FilePath("Library/McpLocalSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpLocalSettings : ScriptableSingleton<McpLocalSettings>
    {
        [SerializeField]
        private int _mcpServerPort = 8000;

        [SerializeField]
        private int _lastToolCount = -1; // -1表示首次运行

        [SerializeField]
        private bool _mcpOpenState = false;

        [SerializeField]
        private List<string> _disabledTools = new List<string>();

        [SerializeField]
        private bool _resourcesCapability = false;

        /// <summary>
        /// MCP服务器端口
        /// </summary>
        public int McpServerPort
        {
            get => _mcpServerPort;
            set
            {
                if (_mcpServerPort != value)
                {
                    _mcpServerPort = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// 上次保存的工具数量
        /// </summary>
        public int LastToolCount
        {
            get => _lastToolCount;
            set
            {
                if (_lastToolCount != value)
                {
                    _lastToolCount = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// MCP服务开启状态
        /// </summary>
        public bool McpOpenState
        {
            get => _mcpOpenState;
            set
            {
                Debug.Log("McpOpenState: " + value);

                if (_mcpOpenState != value)
                {
                    _mcpOpenState = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Resources功能状态
        /// </summary>
        public bool ResourcesCapability
        {
            get => _resourcesCapability;
            set
            {
                if (_resourcesCapability != value)
                {
                    _resourcesCapability = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// 获取MCP本地设置实例
        /// </summary>
        public static McpLocalSettings Instance => instance;

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            Save(true);
        }

        /// <summary>
        /// 验证端口是否有效
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否有效</returns>
        public static bool IsValidPort(int port)
        {
            return port >= 1024 && port <= 65535;
        }

        /// <summary>
        /// 设置MCP服务器端口（带验证）
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>设置是否成功</returns>
        public bool SetMcpPort(int port)
        {
            if (!IsValidPort(port))
            {
                return false;
            }

            McpServerPort = port;
            return true;
        }

        /// <summary>
        /// 检查工具数量是否发生变化
        /// </summary>
        /// <param name="currentCount">当前工具数量</param>
        /// <returns>是否发生变化</returns>
        public bool HasToolCountChanged(int currentCount)
        {
            return LastToolCount == -1 || currentCount != LastToolCount;
        }

        /// <summary>
        /// 重置工具数量记录（用于测试）
        /// </summary>
        public void ResetToolCountRecord()
        {
            LastToolCount = -1;
        }

        /// <summary>
        /// 检查工具是否启用
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <returns>是否启用</returns>
        public bool IsToolEnabled(string toolName)
        {
            if (_disabledTools == null)
            {
                _disabledTools = new List<string>();
                return true;
            }
            return !_disabledTools.Contains(toolName);
        }

        /// <summary>
        /// 设置工具启用状态
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="enabled">是否启用</param>
        public void SetToolEnabled(string toolName, bool enabled)
        {
            if (_disabledTools == null)
            {
                _disabledTools = new List<string>();
            }

            bool currentlyEnabled = !_disabledTools.Contains(toolName);

            if (enabled && !currentlyEnabled)
            {
                // 启用工具：从禁用列表中移除
                _disabledTools.Remove(toolName);
                SaveSettings();
            }
            else if (!enabled && currentlyEnabled)
            {
                // 禁用工具：添加到禁用列表
                _disabledTools.Add(toolName);
                SaveSettings();
            }
        }

        /// <summary>
        /// 获取所有禁用的工具列表
        /// </summary>
        /// <returns>禁用的工具名称列表</returns>
        public List<string> GetDisabledTools()
        {
            if (_disabledTools == null)
            {
                _disabledTools = new List<string>();
            }
            return new List<string>(_disabledTools);
        }

        /// <summary>
        /// 获取启用的工具数量
        /// </summary>
        /// <param name="allToolNames">所有工具名称列表</param>
        /// <returns>启用的工具数量</returns>
        public int GetEnabledToolCount(List<string> allToolNames)
        {
            if (allToolNames == null) return 0;
            if (_disabledTools == null) return allToolNames.Count;

            return allToolNames.Count - _disabledTools.Count(disabled => allToolNames.Contains(disabled));
        }

        /// <summary>
        /// 过滤出启用的工具列表
        /// </summary>
        /// <param name="allToolNames">所有工具名称列表</param>
        /// <returns>启用的工具名称列表</returns>
        public List<string> FilterEnabledTools(List<string> allToolNames)
        {
            if (allToolNames == null) return new List<string>();
            if (_disabledTools == null) return new List<string>(allToolNames);

            return allToolNames.Where(tool => !_disabledTools.Contains(tool)).ToList();
        }

        /// <summary>
        /// 从EditorPrefs迁移旧设置（向后兼容）
        /// </summary>
        private void MigrateFromEditorPrefs()
        {
            // 迁移端口设置
            if (EditorPrefs.HasKey("mcp_server_port") && _mcpServerPort == 8000)
            {
                _mcpServerPort = EditorPrefs.GetInt("mcp_server_port", 8000);
                EditorPrefs.DeleteKey("mcp_server_port");
                Debug.Log("[UniMcp] 已从EditorPrefs迁移端口设置");
            }

            // 迁移工具数量设置
            if (EditorPrefs.HasKey("mcp_tool_count") && _lastToolCount == -1)
            {
                _lastToolCount = EditorPrefs.GetInt("mcp_tool_count", -1);
                EditorPrefs.DeleteKey("mcp_tool_count");
                Debug.Log("[UniMcp] 已从EditorPrefs迁移工具数量设置");
            }

            // 迁移开启状态设置
            if (EditorPrefs.HasKey("mcp_open_state") && !_mcpOpenState)
            {
                _mcpOpenState = EditorPrefs.GetBool("mcp_open_state", false);
                EditorPrefs.DeleteKey("mcp_open_state");
                Debug.Log("[UniMcp] 已从EditorPrefs迁移开启状态设置");
            }
        }

        /// <summary>
        /// 初始化默认设置
        /// </summary>
        private void OnEnable()
        {
            // 首先尝试从EditorPrefs迁移旧设置
            MigrateFromEditorPrefs();

            // 确保设置被正确初始化
            if (_mcpServerPort < 1024 || _mcpServerPort > 65535)
            {
                _mcpServerPort = 8000;
                SaveSettings();
            }
        }

        /// <summary>
        /// 获取设置摘要信息（用于调试）
        /// </summary>
        public string GetSettingsSummary()
        {
            return $"MCP本地设置摘要:\n" +
                   $"- 服务器端口: {McpServerPort}\n" +
                   $"- 上次工具数量: {LastToolCount}\n" +
                   $"- 服务开启状态: {McpOpenState}\n" +
                   $"- Resources功能状态: {ResourcesCapability}\n" +
                   $"- 禁用工具数量: {(_disabledTools?.Count ?? 0)}";
        }
    }
}
