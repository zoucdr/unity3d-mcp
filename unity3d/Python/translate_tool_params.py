#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Batch translate Chinese parameter descriptions to English in Unity Tool files.
Only translates the second string parameter in MethodKey constructors (the description),
not comments or other strings.
"""

import re
import os

# Translation dictionary for common parameter descriptions
TRANSLATIONS = {
    # Common operation types
    "操作类型": "Operation type",
    
    # Paths
    "对象层级路径": "Object hierarchy path",
    "GameObject在层次结构中的路径": "GameObject path in hierarchy",
    "GameObject的实例ID，用于唯一标识对象": "GameObject instance ID for unique identification",
    "对象实例ID": "Object instance ID",
    "音频资源路径": "Audio asset path",
    "源文件路径": "Source file path",
    "目标路径": "Destination path",
    "搜索模式": "Search pattern",
    "纹理资源路径": "Texture asset path",
    "材质资源路径，Unity标准格式": "Material asset path in Unity standard format",
    "源材质路径（复制属性时使用）": "Source material path (for copying properties)",
    "目标路径（复制/移动时使用）": "Destination path (for copy/move operations)",
    "搜索模式，支持通配符": "Search pattern with wildcard support",
    "模型资源路径": "Model asset path",
    "网格资产路径": "Mesh asset path",
    "源网格路径": "Source mesh path",
    
    # Component related
    "组件类型名称（继承自Component的类型名）": "Component type name (inherits from Component)",
    "属性字典，用于设置组件属性值": "Property dictionary for setting component property values",
    "组件类型": "Component type",
    "组件属性": "Component properties",
    
    # GameObject properties
    "GameObject名称": "GameObject name",
    "GameObject标签": "GameObject tag",
    "GameObject层": "GameObject layer",
    "父对象实例ID": "Parent object instance ID",
    "父对象场景路径": "Parent object scene path",
    "位置坐标 [x, y, z]": "Position coordinates [x, y, z]",
    "旋转角度 [x, y, z]": "Rotation angles [x, y, z]",
    "缩放比例 [x, y, z]": "Scale ratio [x, y, z]",
    "激活状态": "Active state",
    
    # Audio properties
    "递归搜索": "Recursive search",
    "强制执行": "Force execution",
    "导入设置": "Import settings",
    "目标格式": "Target format",
    "强制单声道": "Force mono",
    "加载类型": "Load type",
    "压缩格式": "Compression format",
    "质量": "Quality",
    "采样率设置": "Sample rate setting",
    "采样率": "Sample rate",
    "预加载音频数据": "Preload audio data",
    "后台加载": "Load in background",
    "环绕声渲染": "Ambisonic rendering",
    "DSP缓冲区大小": "DSP buffer size",
    "静音时虚拟化": "Virtualize when silent",
    "空间化": "Spatialization",
    "用户数据": "User data",
    "资源包名称": "Asset bundle name",
    "资源包变体": "Asset bundle variant",
    
    # Texture properties
    "纹理类型": "Texture type",
    "Sprite模式": "Sprite mode",
    "每单位像素数": "Pixels per unit",
    "Sprite轴心": "Sprite pivot",
    "生成物理形状": "Generate physics shape",
    "网格类型": "Mesh type",
    "边缘挤出": "Extrude edges",
    "最大纹理尺寸": "Max texture size",
    "过滤模式": "Filter mode",
    "包装模式": "Wrap mode",
    "可读写": "Readable",
    "生成Mip贴图": "Generate mip maps",
    "sRGB纹理": "sRGB texture",
    
    # Material properties
    "着色器名称或路径": "Shader name or path",
    "材质属性字典，包含颜色、纹理、浮点数等属性": "Material property dictionary with colors, textures, floats, etc.",
    "是否递归搜索子文件夹": "Recursively search subfolders",
    "是否强制执行操作（覆盖现有文件等）": "Force operation (overwrite existing files, etc.)",
    "渲染队列值，控制渲染顺序": "Render queue value controlling render order",
    "是否启用GPU实例化": "Enable GPU instancing",
    "是否启用双面全局光照": "Enable double-sided global illumination",
    "着色器关键字名称": "Shader keyword name",
    
    # Model properties
    "缩放因子": "Scale factor",
    "使用文件缩放": "Use file scale",
    "导入混合形状": "Import blend shapes",
    "导入相机": "Import cameras",
    "保持层级": "Preserve hierarchy",
    "优化网格": "Optimize mesh",
    "次要UV硬角度": "Secondary UV hard angle",
    "次要UV打包边距": "Secondary UV pack margin",
    "次要UV角度扭曲": "Secondary UV angle distortion",
    "次要UV面积扭曲": "Secondary UV area distortion",
    "次要UV边缘扭曲": "Secondary UV edge distortion",
    "启用读写": "Enable read/write",
    "导入材质": "Import materials",
    "材质搜索模式": "Material search mode",
    "提取材质": "Extract materials",
    "网格压缩": "Mesh compression",
    "添加碰撞器": "Add collider",
    "焊接顶点": "Weld vertices",
    "传统混合形状法线": "Legacy blend shape normals",
    "切线模式": "Tangent mode",
    "平滑度来源": "Smoothness source",
    "平滑度": "Smoothness",
    "法线导入模式": "Normal import mode",
    "法线贴图模式": "Normal map mode",
    "高度贴图模式": "Height map mode",
    "材质重定向映射": "Material remap dictionary",
    
    # Mesh properties
    "网格属性": "Mesh properties",
    "细分级别": "Subdivision level",
    "平滑因子": "Smooth factor",
}

def translate_parameter_descriptions(file_path):
    """
    Translate Chinese parameter descriptions to English in a Unity Tool file.
    Only translates the description (second string parameter) in MethodKey constructors.
    """
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    # Pattern to match MethodKey constructor calls with Chinese descriptions
    # Matches: new MethodXxx("param_name", "Chinese description", ...)
    pattern = r'(new Method(?:Str|Int|Bool|Float|Obj|Arr|Vector)\([^,]+,\s*)"([^"]*[\u4e00-\u9fa5][^"]*)"'
    
    def replace_func(match):
        prefix = match.group(1)
        chinese_desc = match.group(2)
        
        # Try to find exact translation
        if chinese_desc in TRANSLATIONS:
            english_desc = TRANSLATIONS[chinese_desc]
            return f'{prefix}"{english_desc}"'
        else:
            # Keep original if no translation found
            print(f"  Warning: No translation for: {chinese_desc}")
            return match.group(0)
    
    # Replace all matches
    content = re.sub(pattern, replace_func, content)
    
    # Only write if changes were made
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def main():
    """Process all ResEdit tool files."""
    base_dir = "Packages/unity-package/Editor/Tools/ResEdit"
    
    files_to_process = [
        "EditComponent.cs",
        "EditAudio.cs",
        "EditGameObject.cs",
        "EditTexture.cs",
        "EditMaterial.cs",
        "EditModel.cs",
        "EditMesh.cs",
        "EditParticleSystem.cs",
        "EditPrefab.cs",
        "EditScene.cs",
        "EditScript.cs",
        "EditScriptableObject.cs",
        "EditShader.cs",
        "EditSpriteAtlas.cs",
        "EditTerrain.cs",
    ]
    
    for filename in files_to_process:
        file_path = os.path.join(base_dir, filename)
        if os.path.exists(file_path):
            print(f"Processing {filename}...")
            if translate_parameter_descriptions(file_path):
                print(f"  ✓ Translated")
            else:
                print(f"  - No changes needed")
        else:
            print(f"  ✗ File not found: {file_path}")

if __name__ == "__main__":
    main()
