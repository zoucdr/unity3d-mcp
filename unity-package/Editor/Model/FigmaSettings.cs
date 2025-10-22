using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.Mcp
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
        /// 当前选择的UI类型
        /// </summary>
        public UIType selectedUIType
        {
            get { return _selectedUIType; }
            set { _selectedUIType = value; }
        }
        [SerializeField] private UIType _selectedUIType = UIType.UGUI;

        /// <summary>
        /// 序列化的AI转换提示词列表（按UIType分类）
        /// </summary>
        [SerializeField] private List<AIPromptData> _aiPromptDataList = new List<AIPromptData>();

        /// <summary>
        /// AI转换提示词字典（运行时使用）
        /// </summary>
        [System.NonSerialized] private Dictionary<UIType, string> _aiPromptDict;

        /// <summary>
        /// 获取当前UI类型的AI转换提示词
        /// </summary>
        public string ai_conversion_prompt
        {
            get
            {
                return GetPromptForUIType(selectedUIType);
            }
            set
            {
                SetPromptForUIType(selectedUIType, value);
            }
        }

        /// <summary>
        /// Figma访问令牌（保存在EditorPrefs中，不会被提交到版本控制）
        /// </summary>
        public string figma_access_token
        {
            get
            {
                return EditorPrefs.GetString("Unity.Mcp.Figma.AccessToken", "");
            }
            set
            {
                EditorPrefs.SetString("Unity.Mcp.Figma.AccessToken", value);
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
        /// AI转换提示词数据（可序列化）
        /// </summary>
        [System.Serializable]
        public class AIPromptData
        {
            public UIType uiType;
            [TextArea(10, 30)]
            public string prompt;

            public AIPromptData(UIType type, string promptText)
            {
                uiType = type;
                prompt = promptText;
            }
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
            _selectedUIType = UIType.UGUI;
            InitializeAIPrompts();
        }

        /// <summary>
        /// 初始化AI提示词字典
        /// </summary>
        private void InitializeAIPrompts()
        {
            if (_aiPromptDict == null)
            {
                _aiPromptDict = new Dictionary<UIType, string>();
            }

            // 从序列化数据恢复
            if (_aiPromptDataList != null)
            {
                foreach (var data in _aiPromptDataList)
                {
                    _aiPromptDict[data.uiType] = data.prompt;
                }
            }

            // 确保所有UIType都有默认提示词
            foreach (UIType uiType in System.Enum.GetValues(typeof(UIType)))
            {
                if (!_aiPromptDict.ContainsKey(uiType) || string.IsNullOrEmpty(_aiPromptDict[uiType]))
                {
                    _aiPromptDict[uiType] = GetDefaultPrompt(uiType);
                }
            }
        }

        /// <summary>
        /// 序列化AI提示词数据
        /// </summary>
        public void SerializeAIPrompts()
        {
            _aiPromptDataList.Clear();
            if (_aiPromptDict != null)
            {
                foreach (var kvp in _aiPromptDict)
                {
                    _aiPromptDataList.Add(new AIPromptData(kvp.Key, kvp.Value));
                }
            }
        }

        /// <summary>
        /// 获取指定UI类型的提示词
        /// </summary>
        public string GetPromptForUIType(UIType uiType)
        {
            if (_aiPromptDict == null)
            {
                InitializeAIPrompts();
            }

            if (_aiPromptDict.ContainsKey(uiType))
            {
                return _aiPromptDict[uiType];
            }

            string defaultPrompt = GetDefaultPrompt(uiType);
            _aiPromptDict[uiType] = defaultPrompt;
            return defaultPrompt;
        }

        /// <summary>
        /// 设置指定UI类型的提示词
        /// </summary>
        public void SetPromptForUIType(UIType uiType, string prompt)
        {
            if (_aiPromptDict == null)
            {
                InitializeAIPrompts();
            }

            _aiPromptDict[uiType] = prompt;
        }

        /// <summary>
        /// 获取默认提示词（向后兼容）
        /// </summary>
        private static string GetDefaultPrompt()
        {
            return GetDefaultPrompt(UIType.UGUI);
        }

        /// <summary>
        /// 根据UI类型获取默认提示词
        /// </summary>
        private static string GetDefaultPrompt(UIType uiType)
        {
            switch (uiType)
            {
                case UIType.UGUI:
                    return GetDefaultUGUIPrompt();

                case UIType.UIToolkit:
                    return GetDefaultUIToolkitPrompt();

                case UIType.NGUI:
                    return GetDefaultNGUIPrompt();

                case UIType.FairyGUI:
                    return GetDefaultFairyGUIPrompt();

                case UIType.Custom:
                default:
                    return GetDefaultCustomPrompt();
            }
        }

        /// <summary>
        /// 获取UGUI默认提示词
        /// </summary>
        private static string GetDefaultUGUIPrompt()
        {
            return @"# Figma到Unity UGUI坐标转换规则

## 坐标系差异
- Figma坐标系：原点在左上角，Y轴向下为正
- Unity UGUI坐标系：原点在容器中心，Y轴向上为正

## 精确计算公式
对于顶层元素（直接在Canvas下）：
- X坐标: anchored_position_x = figma_x - (canvas_width/2) + (element_width/2)
- Y坐标: anchored_position_y = (canvas_height/2) - figma_y - (element_height/2)

对于嵌套元素（在父元素内）：
- X坐标: anchored_position_x = (figma_x - parent_figma_x) - (parent_width/2) + (element_width/2)
- Y坐标: anchored_position_y = (parent_figma_y - figma_y) + (parent_height/2) - (element_height/2)

## 尺寸计算
- 宽度: size_delta_x = element_width
- 高度: size_delta_y = element_height

## 允许UI元素超出边界的设置

### RectTransform设置
- 设置RectTransform的anchorMin和anchorMax为[0.5, 0.5]，保持锚点在中心
- 不启用RectTransform的Clamp选项，确保元素可以超出父容器边界
- 对于需要超出边界的元素，不使用ContentSizeFitter组件限制大小

### Canvas设置
- 将Canvas的additionalShaderChannels设置包含""TexCoord1""以支持大尺寸UI
- 设置Canvas的renderMode为""Screen Space - Overlay""以避免相机裁剪
- 禁用Canvas的""Pixel Perfect""选项，以允许非整数像素位置

### 布局组件设置
- 不使用VerticalLayoutGroup或HorizontalLayoutGroup等自动布局组件
- 不使用GridLayoutGroup限制元素位置
- 如果使用LayoutGroup，设置childAlignment为MiddleCenter
- 禁用LayoutGroup的childForceExpandWidth和childForceExpandHeight选项

## 转换要求（必须严格执行）

### 坐标计算规则
1. 所有RectTransform的锚点必须设置为[0.5, 0.5]
2. 不启用任何裁剪组件（如Mask或RectMask2D）
3. 使用精确的数学计算，计算过程中保留至少2位小数精度
4. 不对超出父容器的元素进行任何位置调整
5. 如果计算结果与Figma设计有任何差异，必须使用计算结果
6. 在转换过程中记录每个元素的原始坐标和计算后的坐标

### Canvas配置
- 禁用Canvas的""Pixel Perfect""选项
- 设置Canvas.renderMode为""Screen Space - Overlay""

### 输出要求
对于每个UI元素，提供以下信息：
- 元素名称和ID
- Figma原始坐标
- 计算过程（包含公式和中间值）
- Unity中的最终anchoredPosition和sizeDelta
- 是否超出父容器边界的分析";
        }

        /// <summary>
        /// 获取UI Toolkit默认提示词
        /// </summary>
        private static string GetDefaultUIToolkitPrompt()
        {
            return @"# Figma到Unity UI Toolkit转换规则

## 坐标系说明
- Figma坐标系：原点在左上角，Y轴向下为正
- UI Toolkit坐标系：原点在左上角，Y轴向下为正（与Figma一致）

## 布局转换
### Position（位置）
- 使用absolute定位时：
  - left = figma_x
  - top = figma_y
  - right = parent_width - (figma_x + element_width)
  - bottom = parent_height - (figma_y + element_height)

### Size（尺寸）
- width = element_width
- height = element_height

## 样式映射
### USS样式属性
- position: absolute | relative
- flex-direction: row | column
- justify-content: flex-start | center | flex-end | space-between
- align-items: flex-start | center | flex-end | stretch

## UI Toolkit特性

### Flexbox布局
- 优先使用Flexbox布局系统
- 合理设置flex-grow、flex-shrink、flex-basis
- 使用justify-content和align-items进行对齐

### UXML结构
- 保持清晰的元素层级
- 使用合适的VisualElement类型（Button、Label、Image等）
- 合理命名class和name属性

### USS样式
- 分离结构和样式
- 使用class选择器复用样式
- 合理使用伪类（:hover、:active等）

## 转换建议
1. 优先使用相对布局和Flexbox
2. 适当使用absolute定位处理特殊情况
3. 保持响应式设计原则
4. 注意不同分辨率的适配";
        }

        /// <summary>
        /// 获取NGUI默认提示词
        /// </summary>
        private static string GetDefaultNGUIPrompt()
        {
            return @"# Figma到Unity NGUI转换规则

## 坐标系说明
- Figma坐标系：原点在左上角，Y轴向下为正
- NGUI坐标系：锚点系统，支持多种锚点模式

## 锚点转换
### 锚点设置
- 根据元素在父容器中的位置选择合适的锚点
- 常用锚点：TopLeft、Top、TopRight、Left、Center、Right、BottomLeft、Bottom、BottomRight

### 偏移计算
根据选择的锚点计算相对偏移：
- 相对偏移 = 元素位置 - 锚点位置

## NGUI特性

### UIWidget
- 设置depth控制渲染顺序
- 配置pivot点（锚点中心）
- 设置尺寸和offset

### UIPanel
- 合理设置clipping区域
- 优化depth管理减少DrawCall
- 配置culling mask

### UIAtlas
- 统一使用Atlas管理图片
- 合理规划图片打包
- 注意图片命名规范

## 性能优化
1. 合理使用UIPanel分割UI
2. 控制depth数量减少DrawCall
3. 使用Atlas合并图片资源
4. 避免频繁的UI更新";
        }

        /// <summary>
        /// 获取FairyGUI默认提示词
        /// </summary>
        private static string GetDefaultFairyGUIPrompt()
        {
            return @"# Figma到Unity FairyGUI转换规则

## 设计转换
- 推荐使用FairyGUI编辑器进行设计和导出
- 支持直接在编辑器中还原Figma设计

## 坐标系统
- FairyGUI使用左上角为原点，与Figma一致
- 坐标转换相对简单：
  - x = figma_x
  - y = figma_y

## 组件系统
### GObject
- 基础显示对象
- 支持位置、大小、旋转、缩放

### GComponent
- 容器组件
- 支持子对象管理
- 提供布局功能

### 控制器
- 用于状态切换
- 支持多页面管理
- 实现复杂交互逻辑

## 关系系统
- 支持自动布局关系
- 配置位置、大小关系
- 实现响应式布局

## 动画系统
- 过渡动画（Transition）
- 序列帧动画
- 骨骼动画支持

## 最佳实践
1. 合理使用组件封装
2. 善用控制器管理状态
3. 使用关系系统实现适配
4. 优化资源加载策略";
        }

        /// <summary>
        /// 获取自定义UI默认提示词
        /// </summary>
        private static string GetDefaultCustomPrompt()
        {
            return @"# Figma到自定义UI系统转换规则

## 基础转换
根据自定义UI系统的特点，需要考虑以下方面：

### 坐标系统
- 确定自定义UI系统的坐标原点和轴向
- 建立Figma到自定义系统的坐标映射关系
- 实现精确的坐标转换算法

### 布局系统
- 了解自定义UI的布局机制
- 映射Figma的布局到自定义系统
- 处理父子关系和层级结构

### 渲染系统
- 适配自定义的渲染管线
- 处理材质和shader映射
- 优化渲染性能

## 转换流程
1. 分析自定义UI系统架构
2. 建立坐标转换规则
3. 实现组件映射关系
4. 处理特殊效果和动画
5. 优化性能和资源管理

## 注意事项
- 充分理解自定义系统的特性
- 保持转换的灵活性和可扩展性
- 做好完整的测试验证
- 建立清晰的文档说明";
        }
    }
}
