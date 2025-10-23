using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Unity.Mcp
{
    [System.Serializable]
    public class McpUISettings
    {
        /// <summary>
        /// 当前选择的UI类型
        /// </summary>
        public UIType selectedUIType
        {
            get { return _selectedUIType; }
            set { _selectedUIType = value; }
        }
        [SerializeField] private UIType _selectedUIType = UIType.UGUI;
        /// <summary>
        /// 通用精灵文件夹
        /// </summary>  
        [SerializeField] private List<string> _commonSpriteFolders = new List<string>();
        /// <summary>
        /// 通用纹理文件夹
        /// </summary>
        [SerializeField] private List<string> _commonTextureFolders = new List<string>();
        /// <summary>
        /// 通用字体文件夹
        /// </summary>
        [SerializeField] private List<string> _commonFontFolders = new List<string>();

        /// <summary>
        /// 通用精灵文件夹
        /// </summary>
        public List<string> commonSpriteFolders
        {
            get { return _commonSpriteFolders; }
            set { _commonSpriteFolders = value; }
        }
        /// <summary>
        /// 通用纹理文件夹
        /// </summary>
        public List<string> commonTextureFolders
        {
            get { return _commonTextureFolders; }
            set { _commonTextureFolders = value; }
        }
        /// <summary>
        /// 通用字体文件夹
        /// </summary>
        public List<string> commonFontFolders
        {
            get { return _commonFontFolders; }
            set { _commonFontFolders = value; }
        }
        /// <summary>
        /// 所有UI类型的数据
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
        /// 序列化的UI类型数据列表（用于Unity序列化）
        /// </summary>
        [SerializeField] private List<UITypeDataSerializable> _serializedUITypeData = new List<UITypeDataSerializable>();

        /// <summary>
        /// UI构建步骤（返回当前UI类型的步骤）
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
        /// UI构建环境（返回当前UI类型的环境）
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
        /// 初始化UI类型数据
        /// </summary>
        private void InitializeUITypeData()
        {
            _uiTypeDataDict = new Dictionary<UIType, UITypeData>();

            // 从序列化数据中恢复
            foreach (var serializedData in _serializedUITypeData)
            {
                _uiTypeDataDict[serializedData.uiType] = serializedData.ToUITypeData();
            }

            // 确保所有UI类型都有数据
            foreach (UIType uiType in System.Enum.GetValues(typeof(UIType)))
            {
                if (!_uiTypeDataDict.ContainsKey(uiType))
                {
                    _uiTypeDataDict[uiType] = CreateDefaultUITypeData(uiType);
                }
            }
        }

        /// <summary>
        /// 获取当前UI类型的数据
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
        /// 序列化UI类型数据
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
        /// 创建默认的UI类型数据
        /// </summary>
        private UITypeData CreateDefaultUITypeData(UIType uiType)
        {
            var data = new UITypeData(uiType.ToString());
            data.buildSteps = GetDefaultBuildSteps(uiType);
            data.buildEnvironments = GetDefaultBuildEnvironments(uiType);
            return data;
        }

        /// <summary>
        /// 获取默认的UI构建步骤
        /// </summary>
        // 自动生成的默认构建步骤 - UGUI (2025-10-20 10:30:59)
        // 自动生成的默认构建步骤 - UGUI (2025-10-22 11:18:48)
        // 自动生成的默认构建步骤 - UGUI (2025-10-23 10:25:28)
        // 自动生成的默认构建步骤 - UGUI (2025-10-23 10:32:12)
        // 自动生成的默认构建步骤 - UGUI (2025-10-23 15:47:14)
        public static List<string> GetDefaultBuildSteps()
        {
            return new List<string>
            {
                "利用figma_manage下载页面节点信息",
                "利用figma_manage获取figma转换UGUI的规则",
                "结合页面信息和预览图，合理设计UGUI层级",
                "分析层级信息，尽量多的分配可交互控件，非交互组件尽量合并",
                "创建Canvas和根容器并设置好尺寸",
                "将Game窗口尺寸和UI尺寸匹配",
                "按照设计稿，逐步创建所需UI组件",
                "检查缺失控件，修复后再次检查，直到所有缺失补全",
                "按理想的UI层级进行组件调整",
                "利用ui_rule_manage记录创建的UI组件名称和原来的节点id到规则文件",
                "配置组件参数属性",
                "基于ugui_layout的mcp工具和设计稿信息，进行界面布局调整",
                "根节点如果是全屏尺寸，需要设置全屏拉伸",
                "基于ui_rule_manage记录更改方式到规则文件",
                "界面控件有特效开启的需要的图片资源，子节点就不用解析了，直接使用图片",
                "将下载的图片，利用mcp加载到对应的UI组件上",
                "基于ui_rule_manage将图片信息通过记录到规则文件",
                "优化屏幕适配，使用mcp调用ugui_layout，每个组件根据所在的位置自动选择合适的锚点方式（anchor_preset）",
                "使用mcp进行game窗口屏幕截图，及利用figma_manage进行界面整体预览",
                "并分析实现的UI还原度，如果还原度低需要继续调整"
            };
        }

        /// <summary>
        /// 根据UI类型获取默认的UI构建步骤
        /// </summary>
        public static List<string> GetDefaultBuildSteps(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.UGUI:
                    return new List<string>
                    {
                        "回顾unity-mcp工具使用方法",
                        "利用figma_manage下载并分析设计稿结构",
                        "创建Canvas和根容器并设置好尺寸",
                        "将Game尺寸和UI尺寸匹配",
                        "按照设计稿创建必要的UI组件",
                        "按理想的UI层级进行组件调整",
                        "记录创建的UI组件名称和原来的节点id到规则文件",
                        "配置组件属性",
                        "基于ugui_layout的mcp工具和设计稿信息，进行界面布局调整",
                        "优化屏幕适配",
                        "记录更改方式到规则文件",
                        "下载界面控件需要的图片资源",
                        "将图片信息记录到规则文件",
                        "将下载的图片，利用mcp加载到指定的UI组件上"
                    };

                case UIType.UIToolkit:
                    return new List<string>
                    {
                        "回顾unity-mcp工具使用方法",
                        "分析UI Toolkit设计需求",
                        "创建UI Document和根VisualElement",
                        "设计USS样式文件",
                        "创建UXML结构文件",
                        "配置UI Builder布局",
                        "绑定C#脚本逻辑",
                        "处理事件和交互",
                        "优化响应式布局",
                        "测试不同分辨率适配"
                    };

                case UIType.NGUI:
                    return new List<string>
                    {
                        "回顾unity-mcp工具使用方法",
                        "创建NGUI Root和Camera",
                        "设置UI Atlas纹理",
                        "创建NGUI面板和组件",
                        "配置锚点和布局",
                        "处理NGUI事件系统",
                        "优化Draw Call",
                        "配置字体和本地化"
                    };

                case UIType.FairyGUI:
                    return new List<string>
                    {
                        "回顾unity-mcp工具使用方法",
                        "导入FairyGUI编辑器资源",
                        "创建FairyGUI包和组件",
                        "设置UI适配规则",
                        "配置动画和过渡效果",
                        "绑定代码逻辑",
                        "优化性能和内存",
                        "测试多平台兼容性"
                    };

                case UIType.Custom:
                default:
                    return new List<string>
                    {
                        "分析自定义UI系统需求",
                        "设计UI架构",
                        "实现核心UI组件",
                        "配置渲染管线",
                        "处理输入和事件",
                        "优化性能",
                        "测试和调试"
                    };
            }
        }

        /// <summary>
        /// 获取默认的UI环境说明
        /// </summary>
        public static List<string> GetDefaultBuildEnvironments()
        {
            return GetDefaultBuildEnvironments(UIType.UGUI);
        }

        /// <summary>
        /// 根据UI类型获取默认的UI环境说明
        /// </summary>
        public static List<string> GetDefaultBuildEnvironments(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.UGUI:
                    return new List<string>
                    {
                        "基于UGUI界面",
                        "支持TMP字体",
                        "文本相关组件必须使用TMP",
                        "设置稿的坐标系已统一为Unity坐标系，中心为原点",
                        "仅圆角的图片，可以不下载，直接将Image替换为ProceduralUIImage"
                    };

                case UIType.UIToolkit:
                    return new List<string>
                    {
                        "基于UI Toolkit系统",
                        "使用USS样式表",
                        "UXML文件定义结构",
                        "支持Flexbox布局",
                        "响应式设计优先",
                        "Vector图形支持",
                        "现代Web标准兼容"
                    };

                case UIType.NGUI:
                    return new List<string>
                    {
                        "基于NGUI系统",
                        "使用Atlas纹理管理",
                        "支持BMFont字体",
                        "Draw Call优化重要",
                        "锚点系统布局",
                        "事件系统独立",
                        "适合移动平台"
                    };

                case UIType.FairyGUI:
                    return new List<string>
                    {
                        "基于FairyGUI编辑器",
                        "可视化UI设计",
                        "组件化开发",
                        "丰富的动画支持",
                        "多分辨率适配",
                        "支持复杂交互",
                        "跨平台兼容"
                    };

                case UIType.Custom:
                default:
                    return new List<string>
                    {
                        "自定义UI系统",
                        "根据项目需求定制",
                        "可扩展架构设计",
                        "性能优化优先",
                        "灵活的渲染管线"
                    };
            }
        }
    }

    /// <summary>
    /// UI类型数据
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
    /// 可序列化的UI类型数据（用于Unity序列化）
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