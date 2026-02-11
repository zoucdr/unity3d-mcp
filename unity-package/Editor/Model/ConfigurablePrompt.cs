using System.Collections.Generic;
using UnityEngine;
using UniMcp.Executer;

namespace UniMcp
{
    /// <summary>
    /// 可配置的提示词类，实现IPrompts接口
    /// 用于在编辑器中配置提示词，而不是通过代码实现
    /// </summary>
    [System.Serializable]
    public class ConfigurablePrompt : IPrompts, IMcpSubSettings
    {
        [SerializeField]
        private string _name = "";
        
        [SerializeField]
        private string _description = "";
        
        [SerializeField]
        private string _promptText = "";
        
        [SerializeField]
        private List<ConfigurableMethodKey> _keys = new List<ConfigurableMethodKey>();

        // IPrompts实现
        public string Name => _name;
        public string Description => _description;
        public string PromptText => _promptText;
        
        public MethodKey[] Keys
        {
            get
            {
                if (_keys == null || _keys.Count == 0)
                    return new MethodKey[0];
                
                var methodKeys = new List<MethodKey>();
                foreach (var key in _keys)
                {
                    if (key == null) continue;
                    methodKeys.Add(key.ToMethodKey());
                }
                return methodKeys.ToArray();
            }
        }

        // IMcpSubSettings实现
        string IMcpSubSettings.Name => $"ConfigurablePrompt_{_name}";

        public ConfigurablePrompt()
        {
        }

        public ConfigurablePrompt(string name, string description, string promptText)
        {
            _name = name;
            _description = description;
            _promptText = promptText;
        }

        public void SetName(string name) => _name = name;
        public void SetDescription(string description) => _description = description;
        public void SetPromptText(string promptText) => _promptText = promptText;
        public void SetKeys(List<ConfigurableMethodKey> keys) => _keys = keys ?? new List<ConfigurableMethodKey>();
        public List<ConfigurableMethodKey> GetKeys() => _keys;
    }

    /// <summary>
    /// 可配置的方法键，用于序列化存储
    /// </summary>
    [System.Serializable]
    public class ConfigurableMethodKey
    {
        [SerializeField]
        public string key = "";
        
        [SerializeField]
        public string desc = "";
        
        [SerializeField]
        public bool optional = true;
        
        [SerializeField]
        public string type = "string";
        
        [SerializeField]
        public List<string> examples = new List<string>();
        
        [SerializeField]
        public List<string> enumValues = new List<string>();
        
        [SerializeField]
        public string defaultValue = "";

        public MethodKey ToMethodKey()
        {
            var methodKey = new MethodKey(key, desc, optional, type);
            
            foreach (var example in examples)
            {
                if (!string.IsNullOrEmpty(example))
                    methodKey.AddExample(example);
            }
            
            if (enumValues != null && enumValues.Count > 0)
            {
                methodKey.SetEnumValues(enumValues.ToArray());
            }
            
            if (!string.IsNullOrEmpty(defaultValue))
            {
                // 尝试解析默认值
                if (bool.TryParse(defaultValue, out bool boolValue))
                    methodKey.SetDefault(boolValue);
                else if (int.TryParse(defaultValue, out int intValue))
                    methodKey.SetDefault(intValue);
                else if (float.TryParse(defaultValue, out float floatValue))
                    methodKey.SetDefault(floatValue);
                else
                    methodKey.SetDefault(defaultValue);
            }
            
            return methodKey;
        }
    }
}
