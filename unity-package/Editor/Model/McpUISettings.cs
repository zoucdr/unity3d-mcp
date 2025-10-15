using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityMcp
{
    [System.Serializable]
    public class McpUISettings
    {
        /// <summary>
        /// Currently selectedUIType
        /// </summary>
        public UIType selectedUIType
        {
            get { return _selectedUIType; }
            set { _selectedUIType = value; }
        }
        [SerializeField] private UIType _selectedUIType = UIType.UGUI;
        /// <summary>
        /// Common sprite folder
        /// </summary>  
        [SerializeField] private List<string> _commonSpriteFolders = new List<string>();
        /// <summary>
        /// Common texture folder
        /// </summary>
        [SerializeField] private List<string> _commonTextureFolders = new List<string>();
        /// <summary>
        /// Common font folder
        /// </summary>
        [SerializeField] private List<string> _commonFontFolders = new List<string>();

        /// <summary>
        /// Common sprite folder
        /// </summary>
        public List<string> commonSpriteFolders
        {
            get { return _commonSpriteFolders; }
            set { _commonSpriteFolders = value; }
        }
        /// <summary>
        /// Common texture folder
        /// </summary>
        public List<string> commonTextureFolders
        {
            get { return _commonTextureFolders; }
            set { _commonTextureFolders = value; }
        }
        /// <summary>
        /// Common font folder
        /// </summary>
        public List<string> commonFontFolders
        {
            get { return _commonFontFolders; }
            set { _commonFontFolders = value; }
        }
        /// <summary>
        /// AllUIType's data
        /// </summary>
        public Dictionary<UIType, UITypeData> uiTypeDataDict
        {
            get
            {
                if (_uiTypeDataDict == null)
                    InitializeUITypeData();
                return _uiTypeDataDict;
            }
        }
        [System.NonSerialized] private Dictionary<UIType, UITypeData> _uiTypeDataDict;

        /// <summary>
        /// SerializedUIType data list（ForUnitySerialization）
        /// </summary>
        [SerializeField] private List<UITypeDataSerializable> _serializedUITypeData = new List<UITypeDataSerializable>();

        /// <summary>
        /// UIBuild steps（Return currentUIType's steps）
        /// </summary>
        public List<string> ui_build_steps
        {
            get
            {
                return GetCurrentUITypeData().buildSteps;
            }
            set
            {
                GetCurrentUITypeData().buildSteps = value;
            }
        }

        /// <summary>
        /// UIBuild environment（Return currentUIType's environment）
        /// </summary>
        public List<string> ui_build_enviroments
        {
            get
            {
                return GetCurrentUITypeData().buildEnvironments;
            }
            set
            {
                GetCurrentUITypeData().buildEnvironments = value;
            }
        }

        /// <summary>
        /// InitializationUIType data
        /// </summary>
        private void InitializeUITypeData()
        {
            _uiTypeDataDict = new Dictionary<UIType, UITypeData>();

            // Restore from serialized data
            foreach (var serializedData in _serializedUITypeData)
            {
                _uiTypeDataDict[serializedData.uiType] = serializedData.ToUITypeData();
            }

            // Ensure allUIAll types have data
            foreach (UIType uiType in System.Enum.GetValues(typeof(UIType)))
            {
                if (!_uiTypeDataDict.ContainsKey(uiType))
                {
                    _uiTypeDataDict[uiType] = CreateDefaultUITypeData(uiType);
                }
            }
        }

        /// <summary>
        /// Get currentUIType's data
        /// </summary>
        private UITypeData GetCurrentUITypeData()
        {
            if (!uiTypeDataDict.ContainsKey(selectedUIType))
            {
                uiTypeDataDict[selectedUIType] = CreateDefaultUITypeData(selectedUIType);
            }
            return uiTypeDataDict[selectedUIType];
        }

        /// <summary>
        /// SerializationUIType data
        /// </summary>
        public void SerializeUITypeData()
        {
            _serializedUITypeData.Clear();
            if (_uiTypeDataDict != null)
            {
                foreach (var kvp in _uiTypeDataDict)
                {
                    _serializedUITypeData.Add(new UITypeDataSerializable(kvp.Key, kvp.Value));
                }
            }
        }

        /// <summary>
        /// Create defaultUIType data
        /// </summary>
        private UITypeData CreateDefaultUITypeData(UIType uiType)
        {
            var data = new UITypeData(uiType.ToString());
            data.buildSteps = GetDefaultBuildSteps(uiType);
            data.buildEnvironments = GetDefaultBuildEnvironments(uiType);
            return data;
        }

        /// <summary>
        /// Get the defaultUIBuild steps
        /// </summary>
        public static List<string> GetDefaultBuildSteps()
        {
            return GetDefaultBuildSteps(UIType.UGUI);
        }

        /// <summary>
        /// According toUIGet the default by typeUIBuild steps
        /// </summary>
        public static List<string> GetDefaultBuildSteps(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.UGUI:
                    return new List<string>
                    {
                        "Reviewunity-mcpInstructions for tool usage",
                        "Utilizefigma_manageDownload and analyze the structure of the design draft",
                        "CreateCanvasAnd the root container and set the proper size",
                        "WillGameSize andUISize matching",
                        "Create necessary (items) according to the design draftUIComponent",
                        "According to idealUIAdjust components by hierarchy",
                        "Record createdUIComponent name and the original nodeidTo rule file",
                        "Configure component properties",
                        "Based onugui_layoutOfmcpTool and design draft information，Adjust UI layout",
                        "Optimize screen adaptation",
                        "Record modification method to rule file",
                        "Download image resources required by UI controls",
                        "Record image information to a rule file",
                        "Downloaded images，UtilizemcpLoad to the specified (place)UIOn component"
                    };

                case UIType.UIToolkit:
                    return new List<string>
                    {
                        "Reviewunity-mcpInstructions for tool usage",
                        "AnalyzeUI ToolkitDesign requirements",
                        "CreateUI DocumentAnd rootVisualElement",
                        "DesignUSSStyle file",
                        "CreateUXMLStructure file",
                        "ConfigurationUI BuilderLayout",
                        "BindC#Script logic",
                        "Handle events and interactions",
                        "Optimize responsive layout",
                        "Test adaptation for different resolutions"
                    };

                case UIType.NGUI:
                    return new List<string>
                    {
                        "Reviewunity-mcpInstructions for tool usage",
                        "CreateNGUI RootAndCamera",
                        "SettingUI AtlasTexture",
                        "CreateNGUIPanel and component(s)",
                        "Configure anchor points and layout",
                        "ProcessNGUIEvent system",
                        "OptimizeDraw Call",
                        "Configure fonts and localization"
                    };

                case UIType.FairyGUI:
                    return new List<string>
                    {
                        "Reviewunity-mcpInstructions for tool usage",
                        "ImportFairyGUIEditor resources",
                        "CreateFairyGUIPackage and component(s)",
                        "SettingUIAdaptation rule",
                        "Configure animation and transition effects",
                        "Bind code logic",
                        "Optimize performance and memory",
                        "Test multi-platform compatibility"
                    };

                case UIType.Custom:
                default:
                    return new List<string>
                    {
                        "Analyze customizationUISystem requirements",
                        "DesignUIArchitecture",
                        "Core implementationUIComponent",
                        "Configure rendering pipeline",
                        "Handle input and events",
                        "Optimize performance",
                        "Testing and debugging"
                    };
            }
        }

        /// <summary>
        /// Get the defaultUIEnvironment description
        /// </summary>
        public static List<string> GetDefaultBuildEnvironments()
        {
            return GetDefaultBuildEnvironments(UIType.UGUI);
        }

        /// <summary>
        /// According toUIGet the default by typeUIEnvironment description
        /// </summary>
        public static List<string> GetDefaultBuildEnvironments(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.UGUI:
                    return new List<string>
                    {
                        "Based onUGUIInterface",
                        "SupportTMPFont",
                        "Text-related components must useTMP",
                        "The coordinate system of the design draft has been unified asUnityCoordinate system，Center as origin",
                        "Only images with rounded corners，May not need to download，Directly (put)ImageReplace withProceduralUIImage"
                    };

                case UIType.UIToolkit:
                    return new List<string>
                    {
                        "Based onUI ToolkitSystem",
                        "UseUSSStyle sheet",
                        "UXMLFile definition structure",
                        "SupportFlexboxLayout",
                        "Responsive design first",
                        "VectorGraphics support",
                        "ModernWebStandard compatibility"
                    };

                case UIType.NGUI:
                    return new List<string>
                    {
                        "Based onNGUISystem",
                        "UseAtlasTexture management",
                        "SupportBMFontFont",
                        "Draw CallOptimize importance",
                        "Anchor system layout",
                        "Independent event system",
                        "Suitable for mobile platforms"
                    };

                case UIType.FairyGUI:
                    return new List<string>
                    {
                        "Based onFairyGUIEditor",
                        "VisualizationUIDesign",
                        "Component-based development",
                        "Richer animation support",
                        "Multi-resolution adaptation",
                        "Support complex interactions",
                        "Cross-platform compatibility"
                    };

                case UIType.Custom:
                default:
                    return new List<string>
                    {
                        "CustomizeUISystem",
                        "Customize according to project requirements",
                        "Extensible architecture design",
                        "Performance optimization first",
                        "Flexible rendering pipeline"
                    };
            }
        }
    }


    /// <summary>
    /// UIType enumeration
    /// </summary>
    [System.Serializable]
    public enum UIType
    {
        UGUI = 0,
        UIToolkit = 1,
        NGUI = 2,
        FairyGUI = 3,
        Custom = 4
    }

    /// <summary>
    /// UIType data
    /// </summary>
    [System.Serializable]
    public class UITypeData
    {
        public string typeName;
        public List<string> buildSteps;
        public List<string> buildEnvironments;

        public UITypeData(string name)
        {
            typeName = name;
            buildSteps = new List<string>();
            buildEnvironments = new List<string>();
        }
    }

    /// <summary>
    /// SerializableUIType data（ForUnitySerialization）
    /// </summary>
    [System.Serializable]
    public class UITypeDataSerializable
    {
        public UIType uiType;
        public string typeName;
        public List<string> buildSteps;
        public List<string> buildEnvironments;

        public UITypeDataSerializable(UIType type, UITypeData data)
        {
            uiType = type;
            typeName = data.typeName;
            buildSteps = new List<string>(data.buildSteps);
            buildEnvironments = new List<string>(data.buildEnvironments);
        }

        public UITypeData ToUITypeData()
        {
            var data = new UITypeData(typeName);
            data.buildSteps = new List<string>(buildSteps);
            data.buildEnvironments = new List<string>(buildEnvironments);
            return data;
        }
    }

}