using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        // 保存主线程的线程ID
        private static int mainThreadId = -1;
        
        [SerializeField]
        private int _mcpServerPort = 8000;

        [SerializeField]
        private int _lastToolCount = -1; // -1表示首次运行
        
        [SerializeField]
        private int _lastPromptCount = -1; // -1表示首次运行
        
        [SerializeField]
        private int _lastResourceCount = -1; // -1表示首次运行

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

        [SerializeField]
        private string _currentLanguage = "English";

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
        /// 当前语言设置
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// 获取MCP本地设置实例
        /// </summary>
        public static McpLocalSettings Instance => instance;

        /// <summary>
        /// 保存设置（线程安全版本）
        /// </summary>
        public void SaveSettings()
        {
            // 检查是否在主线程
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                // 主线程：直接保存
                EditorUtility.SetDirty(this);
                Save(true);
                Debug.Log($"[McpLocalSettings] SaveSettings 调用完成 - 文件路径: UserSettings/McpLocalSettings.asset");
            }
            else
            {
                // 非主线程：调度到主线程执行
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        EditorUtility.SetDirty(this);
                        Save(true);
                        Debug.Log($"[McpLocalSettings] SaveSettings 已在主线程完成 - 文件路径: UserSettings/McpLocalSettings.asset");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[McpLocalSettings] 保存设置失败: {ex.Message}");
                    }
                };
            }
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
        /// <param name="updateIfChanged">如果发生变化，是否自动更新 LastToolCount（默认为 true）</param>
        /// <returns>是否发生变化</returns>
        public bool HasToolCountChanged(int currentCount, bool updateIfChanged = true)
        {
            bool hasChanged = LastToolCount == -1 || currentCount != LastToolCount;
            
            // 如果发生变化且需要更新，则自动更新 LastToolCount
            if (hasChanged && updateIfChanged)
            {
                LastToolCount = currentCount;
                SaveSettings();
                Debug.Log($"[McpLocalSettings] 工具数量已变化，自动更新并保存: {LastToolCount}");
            }
            
            return hasChanged;
        }
        
        /// <summary>
        /// 检查 Prompt 数量是否发生变化
        /// </summary>
        /// <param name="currentCount">当前 Prompt 数量</param>
        /// <param name="updateIfChanged">如果发生变化，是否自动更新 LastPromptCount（默认为 true）</param>
        /// <returns>是否发生变化</returns>
        public bool HasPromptCountChanged(int currentCount, bool updateIfChanged = true)
        {
            bool hasChanged = _lastPromptCount == -1 || currentCount != _lastPromptCount;
            
            // 如果发生变化且需要更新，则自动更新 LastPromptCount
            if (hasChanged && updateIfChanged)
            {
                _lastPromptCount = currentCount;
                SaveSettings();
                Debug.Log($"[McpLocalSettings] Prompt数量已变化，自动更新并保存: {_lastPromptCount}");
            }
            
            return hasChanged;
        }
        
        /// <summary>
        /// 检查 Resource 数量是否发生变化
        /// </summary>
        /// <param name="currentCount">当前 Resource 数量</param>
        /// <param name="updateIfChanged">如果发生变化，是否自动更新 LastResourceCount（默认为 true）</param>
        /// <returns>是否发生变化</returns>
        public bool HasResourceCountChanged(int currentCount, bool updateIfChanged = true)
        {
            bool hasChanged = _lastResourceCount == -1 || currentCount != _lastResourceCount;
            
            // 如果发生变化且需要更新，则自动更新 LastResourceCount
            if (hasChanged && updateIfChanged)
            {
                _lastResourceCount = currentCount;
                SaveSettings();
                Debug.Log($"[McpLocalSettings] Resource数量已变化，自动更新并保存: {_lastResourceCount}");
            }
            
            return hasChanged;
        }

        /// <summary>
        /// 重置工具数量记录（用于测试）
        /// </summary>
        public void ResetToolCountRecord()
        {
            LastToolCount = -1;
            SaveSettings();
        }
        
        /// <summary>
        /// 重置 Prompt 数量记录（用于测试）
        /// </summary>
        public void ResetPromptCountRecord()
        {
            _lastPromptCount = -1;
            SaveSettings();
        }
        
        /// <summary>
        /// 重置 Resource 数量记录（用于测试）
        /// </summary>
        public void ResetResourceCountRecord()
        {
            _lastResourceCount = -1;
            SaveSettings();
        }
        
        /// <summary>
        /// 重置所有计数记录（用于测试）
        /// </summary>
        public void ResetAllCountRecords()
        {
            LastToolCount = -1;
            _lastPromptCount = -1;
            _lastResourceCount = -1;
            SaveSettings();
            Debug.Log("[McpLocalSettings] 已重置所有计数记录");
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
        /// 初始化默认设置
        /// </summary>
        private void OnEnable()
        {
            // 记录主线程ID（OnEnable 总是在主线程调用）
            if (mainThreadId == -1)
            {
                mainThreadId = Thread.CurrentThread.ManagedThreadId;
                Debug.Log($"[McpLocalSettings] 记录主线程ID: {mainThreadId}");
            }
            
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
                   $"- 上次Prompt数量: {_lastPromptCount}\n" +
                   $"- 上次Resource数量: {_lastResourceCount}\n" +
                   $"- 服务开启状态: {McpOpenState}\n" +
                   $"- Resources功能状态: {ResourcesCapability}\n" +
                   $"- 当前语言: {(string.IsNullOrEmpty(CurrentLanguage) ? "系统默认" : CurrentLanguage)}\n" +
                   $"- 禁用工具数量: {(_disabledTools?.Count ?? 0)}\n" +
                   $"- 禁用工具列表: {disabledToolsList}\n" +
                   $"- 禁用资源数量: {(_disabledResources?.Count ?? 0)}\n" +
                   $"- 禁用资源列表: {disabledResourcesList}\n" +
                   $"- 禁用提示词数量: {(_disabledPrompts?.Count ?? 0)}\n" +
                   $"- 禁用提示词列表: {disabledPromptsList}";
        }

    }
}
