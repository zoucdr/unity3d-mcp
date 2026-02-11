using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniMcp.Gui;
using UniMcp.Executer;

namespace UniMcp
{
    /// <summary>
    /// MCP设置管理器，用于管理MCP相关的配置和偏好设置
    /// 保存到ProjectSettings/McpSettings.asset
    /// </summary>
    [FilePath("ProjectSettings/McpSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpSettings : ScriptableSingleton<McpSettings>
    {
        [SerializeReference]
        public List<IMcpSubSettings> subSettings;

        [SerializeField]
        private List<ConfigurablePrompt> configurablePrompts = new List<ConfigurablePrompt>();

        [SerializeField]
        private List<ConfigurableResource> configurableResources = new List<ConfigurableResource>();

        /// <summary>
        /// 获取所有可配置的提示词
        /// </summary>
        public List<ConfigurablePrompt> GetConfigurablePrompts()
        {
            if (configurablePrompts == null)
                configurablePrompts = new List<ConfigurablePrompt>();
            return configurablePrompts;
        }

        /// <summary>
        /// 获取所有可配置的资源
        /// </summary>
        public List<ConfigurableResource> GetConfigurableResources()
        {
            if (configurableResources == null)
                configurableResources = new List<ConfigurableResource>();
            return configurableResources;
        }

        /// <summary>
        /// 添加可配置的提示词
        /// </summary>
        public void AddConfigurablePrompt(ConfigurablePrompt prompt)
        {
            if (prompt == null) return;
            if (configurablePrompts == null)
                configurablePrompts = new List<ConfigurablePrompt>();
            
            // 检查是否已存在同名提示词
            int index = configurablePrompts.FindIndex(p => p.Name == prompt.Name);
            if (index >= 0)
                configurablePrompts[index] = prompt;
            else
                configurablePrompts.Add(prompt);
        }

        /// <summary>
        /// 移除可配置的提示词
        /// </summary>
        public void RemoveConfigurablePrompt(string name)
        {
            if (configurablePrompts == null) return;
            configurablePrompts.RemoveAll(p => p.Name == name);
        }

        /// <summary>
        /// 添加可配置的资源
        /// </summary>
        public void AddConfigurableResource(ConfigurableResource resource)
        {
            if (resource == null) return;
            if (configurableResources == null)
                configurableResources = new List<ConfigurableResource>();
            
            // 检查是否已存在同名资源
            int index = configurableResources.FindIndex(r => r.Name == resource.Name);
            if (index >= 0)
                configurableResources[index] = resource;
            else
                configurableResources.Add(resource);
        }

        /// <summary>
        /// 移除可配置的资源
        /// </summary>
        public void RemoveConfigurableResource(string name)
        {
            if (configurableResources == null) return;
            configurableResources.RemoveAll(r => r.Name == name);
        }

        /// <summary>
        /// 获取MCP设置实例
        /// </summary>
        public static McpSettings Instance => instance;

        /// <summary>
        /// 添加子设置对象到subSettings列表中（按name唯一，已存在则替换）
        /// </summary>
        /// <param name="subSettings">实现IMcpSubSettings的子设置对象</param>
        public void AddSubSettings(IMcpSubSettings subSettings)
        {
            if (subSettings == null || string.IsNullOrEmpty(subSettings.Name))
                return;

            // 检查是否已存在同名项，存在则替换，不存在则添加
            int index = this.subSettings.FindIndex(x => x.Name == subSettings.Name);
            if (index >= 0)
            {
                this.subSettings[index] = subSettings;
            }
            else
            {
                this.subSettings.Add(subSettings);
            }
        }

        /// <summary>
        /// 从subSettings列表中移除指定名称的子设置对象
        /// </summary>
        /// <param name="name">子设置对象的唯一名称</param>
        public void RemoveSubSettings(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            this.subSettings.RemoveAll(x => x.Name == name);
        }

        /// <summary>
        /// 获取指定类型T的子设置对象，如果不存在则返回null
        /// </summary>
        /// <typeparam name="T">目标子设置类型，需实现IMcpSubSettings接口</typeparam>
        /// <returns>类型为T的子设置对象或null</returns>
        public T GetSubSettings<T>(string name) where T : IMcpSubSettings
        {
            return (T)this.subSettings.Find(x => x.Name == name);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            Save(true);
        }
    }

}