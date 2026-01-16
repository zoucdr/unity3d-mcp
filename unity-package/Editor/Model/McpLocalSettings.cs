using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UniMcp
{
    /// <summary>
    /// MCP本地设置管理器，用于管理MCP相关的本地配置和偏好设置
    /// 保存到UserSettings/McpLocalSettings.asset，不会被版本控制系统跟踪   
    /// </summary>
    [FilePath("UserSettings/McpLocalSettings.asset", FilePathAttribute.Location.ProjectFolder)]
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
        private List<string> _disabledResources = new List<string>();

        [SerializeField]
        private List<string> _disabledPrompts = new List<string>();

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
            Debug.Log($"[McpLocalSettings] SaveSettings 调用完成 - 文件路径: UserSettings/McpLocalSettings.asset");
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
        /// 批量设置工具启用状态（用于组开关，更高效）
        /// </summary>
        /// <param name="toolNames">工具名称列表</param>
        /// <param name="enabled">是否启用</param>
        public void SetToolsEnabled(IEnumerable<string> toolNames, bool enabled)
        {
            if (_disabledTools == null)
            {
                _disabledTools = new List<string>();
            }

            bool changed = false;

            foreach (var toolName in toolNames)
            {
                bool currentlyEnabled = !_disabledTools.Contains(toolName);

                if (enabled && !currentlyEnabled)
                {
                    // 启用工具：从禁用列表中移除
                    _disabledTools.Remove(toolName);
                    changed = true;
                }
                else if (!enabled && currentlyEnabled)
                {
                    // 禁用工具：添加到禁用列表
                    _disabledTools.Add(toolName);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveSettings();
                Debug.Log($"[McpLocalSettings] 批量设置完成，当前禁用工具数量: {_disabledTools.Count}");
            }
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
            bool changed = false;

            if (enabled && !currentlyEnabled)
            {
                // 启用工具：从禁用列表中移除
                _disabledTools.Remove(toolName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 启用工具: {toolName}");
            }
            else if (!enabled && currentlyEnabled)
            {
                // 禁用工具：添加到禁用列表
                _disabledTools.Add(toolName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 禁用工具: {toolName}");
            }

            if (changed)
            {
                SaveSettings();
                Debug.Log($"[McpLocalSettings] 设置已保存，当前禁用工具数量: {_disabledTools.Count}");
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
        /// 检查资源是否启用（仅用于配置的资源）
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <returns>是否启用</returns>
        public bool IsResourceEnabled(string resourceName)
        {
            if (_disabledResources == null)
            {
                _disabledResources = new List<string>();
                return true;
            }
            return !_disabledResources.Contains(resourceName);
        }

        /// <summary>
        /// 设置资源启用状态（仅用于配置的资源）
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <param name="enabled">是否启用</param>
        public void SetResourceEnabled(string resourceName, bool enabled)
        {
            if (_disabledResources == null)
            {
                _disabledResources = new List<string>();
            }

            bool currentlyEnabled = !_disabledResources.Contains(resourceName);
            bool changed = false;

            if (enabled && !currentlyEnabled)
            {
                _disabledResources.Remove(resourceName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 启用资源: {resourceName}");
            }
            else if (!enabled && currentlyEnabled)
            {
                _disabledResources.Add(resourceName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 禁用资源: {resourceName}");
            }

            if (changed)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// 批量设置资源启用状态
        /// </summary>
        /// <param name="resourceNames">资源名称列表</param>
        /// <param name="enabled">是否启用</param>
        public void SetResourcesEnabled(IEnumerable<string> resourceNames, bool enabled)
        {
            if (_disabledResources == null)
            {
                _disabledResources = new List<string>();
            }

            bool changed = false;
            foreach (var resourceName in resourceNames)
            {
                bool currentlyEnabled = !_disabledResources.Contains(resourceName);

                if (enabled && !currentlyEnabled)
                {
                    _disabledResources.Remove(resourceName);
                    changed = true;
                }
                else if (!enabled && currentlyEnabled)
                {
                    _disabledResources.Add(resourceName);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// 过滤出启用的资源列表（仅用于配置的资源）
        /// </summary>
        /// <param name="allResourceNames">所有资源名称列表</param>
        /// <returns>启用的资源名称列表</returns>
        public List<string> FilterEnabledResources(List<string> allResourceNames)
        {
            if (allResourceNames == null) return new List<string>();
            if (_disabledResources == null) return new List<string>(allResourceNames);

            return allResourceNames.Where(resource => !_disabledResources.Contains(resource)).ToList();
        }

        /// <summary>
        /// 检查提示词是否启用（仅用于配置的提示词）
        /// </summary>
        /// <param name="promptName">提示词名称</param>
        /// <returns>是否启用</returns>
        public bool IsPromptEnabled(string promptName)
        {
            if (_disabledPrompts == null)
            {
                _disabledPrompts = new List<string>();
                return true;
            }
            return !_disabledPrompts.Contains(promptName);
        }

        /// <summary>
        /// 设置提示词启用状态（仅用于配置的提示词）
        /// </summary>
        /// <param name="promptName">提示词名称</param>
        /// <param name="enabled">是否启用</param>
        public void SetPromptEnabled(string promptName, bool enabled)
        {
            if (_disabledPrompts == null)
            {
                _disabledPrompts = new List<string>();
            }

            bool currentlyEnabled = !_disabledPrompts.Contains(promptName);
            bool changed = false;

            if (enabled && !currentlyEnabled)
            {
                _disabledPrompts.Remove(promptName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 启用提示词: {promptName}");
            }
            else if (!enabled && currentlyEnabled)
            {
                _disabledPrompts.Add(promptName);
                changed = true;
                Debug.Log($"[McpLocalSettings] 禁用提示词: {promptName}");
            }

            if (changed)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// 批量设置提示词启用状态
        /// </summary>
        /// <param name="promptNames">提示词名称列表</param>
        /// <param name="enabled">是否启用</param>
        public void SetPromptsEnabled(IEnumerable<string> promptNames, bool enabled)
        {
            if (_disabledPrompts == null)
            {
                _disabledPrompts = new List<string>();
            }

            bool changed = false;
            foreach (var promptName in promptNames)
            {
                bool currentlyEnabled = !_disabledPrompts.Contains(promptName);

                if (enabled && !currentlyEnabled)
                {
                    _disabledPrompts.Remove(promptName);
                    changed = true;
                }
                else if (!enabled && currentlyEnabled)
                {
                    _disabledPrompts.Add(promptName);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// 过滤出启用的提示词列表（仅用于配置的提示词）
        /// </summary>
        /// <param name="allPromptNames">所有提示词名称列表</param>
        /// <returns>启用的提示词名称列表</returns>
        public List<string> FilterEnabledPrompts(List<string> allPromptNames)
        {
            if (allPromptNames == null) return new List<string>();
            if (_disabledPrompts == null) return new List<string>(allPromptNames);

            return allPromptNames.Where(prompt => !_disabledPrompts.Contains(prompt)).ToList();
        }

        /// <summary>
        /// 获取设置摘要信息（用于调试）
        /// </summary>
        public string GetSettingsSummary()
        {
            var disabledToolsList = _disabledTools != null ? string.Join(", ", _disabledTools) : "无";
            var disabledResourcesList = _disabledResources != null ? string.Join(", ", _disabledResources) : "无";
            var disabledPromptsList = _disabledPrompts != null ? string.Join(", ", _disabledPrompts) : "无";
            return $"MCP本地设置摘要:\n" +
                   $"- 服务器端口: {McpServerPort}\n" +
                   $"- 上次工具数量: {LastToolCount}\n" +
                   $"- 服务开启状态: {McpOpenState}\n" +
                   $"- Resources功能状态: {ResourcesCapability}\n" +
                   $"- 禁用工具数量: {(_disabledTools?.Count ?? 0)}\n" +
                   $"- 禁用工具列表: {disabledToolsList}\n" +
                   $"- 禁用资源数量: {(_disabledResources?.Count ?? 0)}\n" +
                   $"- 禁用资源列表: {disabledResourcesList}\n" +
                   $"- 禁用提示词数量: {(_disabledPrompts?.Count ?? 0)}\n" +
                   $"- 禁用提示词列表: {disabledPromptsList}";
        }

    }
}
