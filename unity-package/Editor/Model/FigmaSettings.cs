using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityMcp
{
    /// <summary>
    /// Figma设置类，用于管理与Figma集成相关的配置
    /// </summary>
    [System.Serializable]
    public class FigmaSettings
    {
        /// <summary>
        /// 默认下载路径
        /// </summary>
        public string default_download_path
        {
            get
            {
                if (string.IsNullOrEmpty(_default_download_path))
                    _default_download_path = "Assets/UI/Figma";
                return _default_download_path;
            }
            set { _default_download_path = value; }
        }
        [SerializeField] private string _default_download_path;

        /// <summary>
        /// Figma资产数据路径
        /// </summary>
        public string figma_assets_path
        {
            get
            {
                if (string.IsNullOrEmpty(_figma_assets_path))
                    _figma_assets_path = "Assets/FigmaAssets";
                return _figma_assets_path;
            }
            set { _figma_assets_path = value; }
        }
        [SerializeField] private string _figma_assets_path;

        /// <summary>
        /// Figma预览图保存路径
        /// </summary>
        public string figma_preview_path
        {
            get
            {
                if (string.IsNullOrEmpty(_figma_preview_path))
                    _figma_preview_path = "Assets/FigmaAssets/Previews";
                return _figma_preview_path;
            }
            set { _figma_preview_path = value; }
        }
        [SerializeField] private string _figma_preview_path;

        /// <summary>
        /// 自动下载图片
        /// </summary>
        public bool auto_download_images = true;

        /// <summary>
        /// 图片缩放倍数
        /// </summary>
        public float image_scale = 2.0f;

        /// <summary>
        /// 自动转换图片为Sprite格式
        /// </summary>
        public bool auto_convert_to_sprite = true;

        /// <summary>
        /// 预览图最大尺寸
        /// </summary>
        public int preview_max_size = 100;

        /// <summary>
        /// 引擎支持效果
        /// </summary>
        public EngineSupportEffect engineSupportEffect;

        /// <summary>
        /// Figma访问令牌（保存在EditorPrefs中，不会被提交到版本控制）
        /// </summary>
        public string figma_access_token
        {
            get
            {
                return EditorPrefs.GetString("UnityMcp.Figma.AccessToken", "");
            }
            set
            {
                EditorPrefs.SetString("UnityMcp.Figma.AccessToken", value);
            }
        }

        /// <summary>
        /// 引擎支持效果
        /// </summary>
        [System.Serializable]
        public class EngineSupportEffect
        {
            public bool roundCorner;
            public bool outLineImg;
            public bool gradientImg;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public FigmaSettings()
        {
            // 初始化默认值
            _default_download_path = "Assets/UI/Figma";
            _figma_assets_path = "Assets/FigmaAssets";
            _figma_preview_path = "Assets/FigmaAssets/Previews";
            auto_download_images = true;
            image_scale = 2.0f;
            auto_convert_to_sprite = true;
            preview_max_size = 100;
            engineSupportEffect = new EngineSupportEffect();
        }
    }
}
