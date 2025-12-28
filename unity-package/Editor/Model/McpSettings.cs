using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniMcp.Gui;

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