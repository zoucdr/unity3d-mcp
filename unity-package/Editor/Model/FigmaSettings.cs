using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityMcp
{
    /// <summary>
    /// FigmaSettings class，Used to manage andFigmaIntegration related configuration
    /// </summary>
    [System.Serializable]
    public class FigmaSettings
    {
        /// <summary>
        /// Default download path
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
        /// FigmaAsset data path
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
        /// FigmaPreview image save path
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
        /// Automatically download images
        /// </summary>
        public bool auto_download_images = true;

        /// <summary>
        /// Image scale factor
        /// </summary>
        public float image_scale = 2.0f;

        /// <summary>
        /// Automatically convert images toSpriteFormat
        /// </summary>
        public bool auto_convert_to_sprite = true;

        /// <summary>
        /// Maximum size of preview image
        /// </summary>
        public int preview_max_size = 100;

        /// <summary>
        /// Engine-supported features
        /// </summary>
        public EngineSupportEffect engineSupportEffect;

        /// <summary>
        /// FigmaAccess token（Save inEditorPrefsIn，Will not be committed to version control）
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
        /// Engine-supported features
        /// </summary>
        [System.Serializable]
        public class EngineSupportEffect
        {
            public bool roundCorner;
            public bool outLineImg;
            public bool gradientImg;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FigmaSettings()
        {
            // Initialize default values
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
