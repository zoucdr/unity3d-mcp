using UnityEngine;
using UniMcp.Executer;

namespace UniMcp
{
    /// <summary>
    /// 可配置的资源类，实现IRes接口
    /// 用于在编辑器中配置资源，支持Unity Object和URL链接
    /// </summary>
    [System.Serializable]
    public class ConfigurableResource : IRes, IMcpSubSettings
    {
        [SerializeField]
        private string _name = "";
        
        [SerializeField]
        private string _description = "";
        
        [SerializeField]
        private string _url = "";
        
        [SerializeField]
        private string _mimeType = "application/octet-stream";
        
        [SerializeField]
        private Object _unityObject;
        
        [SerializeField]
        private ResourceSourceType _sourceType = ResourceSourceType.Url;

        [SerializeField]
        private string _cachedUrl = ""; // 缓存的URL，用于UnityObject类型，避免在后台线程调用AssetDatabase

        public string Name => _name;
        public string Description => _description;
        public string MimeType => _mimeType;
        
        public string Url
        {
            get
            {
                if (_sourceType == ResourceSourceType.UnityObject && _unityObject != null)
                {
                    // 如果已有缓存的URL，直接返回（避免在后台线程调用AssetDatabase）
                    if (!string.IsNullOrEmpty(_cachedUrl))
                    {
                        return _cachedUrl;
                    }
                    
                    // 只有在主线程时才计算URL
                    if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                    {
                        // 将Unity Object转换为file:// URL
                        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(_unityObject);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // 转换为绝对路径
                            string fullPath = System.IO.Path.Combine(
                                System.IO.Path.GetDirectoryName(Application.dataPath),
                                assetPath
                            );
                            _cachedUrl = $"file:///{fullPath.Replace("\\", "/")}";
                            return _cachedUrl;
                        }
                    }
                    else
                    {
                        // 后台线程：返回缓存的URL，如果缓存为空则返回空字符串
                        return _cachedUrl ?? "";
                    }
                }
                return _url;
            }
        }

        // IMcpSubSettings实现
        string IMcpSubSettings.Name => $"ConfigurableResource_{_name}";

        public Object UnityObject
        {
            get => _unityObject;
            set
            {
                _unityObject = value;
                // 当设置UnityObject时，立即计算并缓存URL（必须在主线程中调用）
                if (value != null && System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(value);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        string fullPath = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(Application.dataPath),
                            assetPath
                        );
                        _cachedUrl = $"file:///{fullPath.Replace("\\", "/")}";
                    }
                    else
                    {
                        _cachedUrl = "";
                    }
                }
                else if (value == null)
                {
                    _cachedUrl = "";
                }
            }
        }

        public ResourceSourceType SourceType
        {
            get => _sourceType;
            set => _sourceType = value;
        }

        public ConfigurableResource()
        {
        }

        public ConfigurableResource(string name, string description, string url, string mimeType = "application/octet-stream")
        {
            _name = name;
            _description = description;
            _url = url;
            _mimeType = mimeType;
            _sourceType = ResourceSourceType.Url;
        }

        public void SetName(string name) => _name = name;
        public void SetDescription(string description) => _description = description;
        public void SetUrl(string url) => _url = url;
        public void SetMimeType(string mimeType) => _mimeType = mimeType;

        /// <summary>
        /// 确保URL已计算（用于UnityObject类型，必须在主线程中调用）
        /// </summary>
        public void EnsureUrlCalculated()
        {
            if (_sourceType == ResourceSourceType.UnityObject && _unityObject != null && string.IsNullOrEmpty(_cachedUrl))
            {
                // 检查是否在主线程
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == 1)
                {
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(_unityObject);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        string fullPath = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(Application.dataPath),
                            assetPath
                        );
                        _cachedUrl = $"file:///{fullPath.Replace("\\", "/")}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 资源来源类型
    /// </summary>
    public enum ResourceSourceType
    {
        Url,        // URL链接
        UnityObject // Unity对象
    }
}
