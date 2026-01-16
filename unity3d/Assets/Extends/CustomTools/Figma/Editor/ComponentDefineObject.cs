using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniMcp.Models
{
    /// <summary>
    /// 组件定义对象，用于配置Figma组件ID对应Unity预制件引用的信息
    /// </summary>
    public class ComponentDefineObject : ScriptableObject, IMcpRule
    {
        [Tooltip("组件库名称")]
        public string libraryName;

        [Tooltip("组件映射列表")]
        public List<ComponentMapping> componentMappings = new List<ComponentMapping>();

        // 缓存字典，用于快速查找
        private static Dictionary<string, Object> _componentCache = new Dictionary<string, Object>();

        /// <summary>
        /// 根据组件ID查找预制件路径
        /// </summary>
        /// <param name="componentId">组件ID</param>
        /// <returns>预制件路径，如果未找到则返回空字符串</returns>
        public static string GetPrefabPathById(string componentId)
        {
            if (string.IsNullOrEmpty(componentId))
                return string.Empty;

            // 尝试从缓存获取
            if (_componentCache.ContainsKey(componentId) && _componentCache[componentId] != null)
            {
                return AssetDatabase.GetAssetPath(_componentCache[componentId]);
            }

            // 查找所有ComponentDefineObject资源
            string[] guids = AssetDatabase.FindAssets("t:ComponentDefineObject");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ComponentDefineObject componentDefine = AssetDatabase.LoadAssetAtPath<ComponentDefineObject>(path);

                if (componentDefine != null)
                {
                    foreach (ComponentMapping mapping in componentDefine.componentMappings)
                    {
                        if (mapping.id == componentId && mapping.prefab != null)
                        {
                            // 添加到缓存
                            _componentCache[componentId] = mapping.prefab;
                            return AssetDatabase.GetAssetPath(mapping.prefab);
                        }
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 根据组件ID查找预制件引用
        /// </summary>
        /// <param name="componentId">组件ID</param>
        /// <returns>预制件引用，如果未找到则返回null</returns>
        public static Object GetPrefabById(string componentId)
        {
            if (string.IsNullOrEmpty(componentId))
                return null;

            // 尝试从缓存获取
            if (_componentCache.ContainsKey(componentId) && _componentCache[componentId] != null)
            {
                return _componentCache[componentId];
            }

            // 查找所有ComponentDefineObject资源
            string[] guids = AssetDatabase.FindAssets("t:ComponentDefineObject");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ComponentDefineObject componentDefine = AssetDatabase.LoadAssetAtPath<ComponentDefineObject>(path);

                if (componentDefine != null)
                {
                    foreach (ComponentMapping mapping in componentDefine.componentMappings)
                    {
                        if (mapping.id == componentId && mapping.prefab != null)
                        {
                            // 添加到缓存
                            _componentCache[componentId] = mapping.prefab;
                            return mapping.prefab;
                        }
                    }
                }
            }

            return null;
        }

    }

    /// <summary>
    /// 组件映射，关联Figma组件ID和Unity预制件
    /// </summary>
    [System.Serializable]
    public class ComponentMapping
    {
        public string id;
        public Object prefab;
        public string url;
    }
}
