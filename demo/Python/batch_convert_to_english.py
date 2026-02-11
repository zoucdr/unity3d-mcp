# -*- coding: utf-8 -*-
"""
Batch convert Chinese text to English in Unity MCP Tool files
"""

import os
import re
import sys

# Category mappings
CATEGORY_MAP = {
    "开发工具": "Development Tools",
    "层级管理": "Hierarchy Management",
    "窗口管理": "Window Management",
    "UI管理": "UI Management",
    "调试工具": "Debug Tools",
    "系统管理": "System Management",
    "资源管理": "Resource Management",
    "网络工具": "Network Tools",
    "项目管理": "Project Management",
    "游戏控制": "Gameplay Control"
}

# Description mappings
DESC_MAP = {
    "层级创建工具，支持在场景层级中创建各种类型的游戏对象": "Hierarchy creation tool, supports creating various types of game objects in scene hierarchy",
    "层级搜索工具，支持在场景层级中搜索和查找游戏对象": "Hierarchy search tool, supports searching and finding game objects in scene hierarchy",
    "层级应用工具，用于处理游戏对象预制体的应用和连接操作": "Hierarchy apply tool, handles prefab apply and connection operations for game objects",
    "场景视图管理和控制工具，支持获取信息、聚焦、最大化、截图、设置轴心等操作": "Scene view management and control tool, supports getting info, focusing, maximizing, screenshots, setting pivot, etc.",
    "游戏视图管理和控制工具，支持设置分辨率、获取统计信息、设置垂直同步、设置目标帧率、截图等操作": "Game view management and control tool, supports setting resolution, getting statistics, VSync, target framerate, screenshots, etc.",
    "UGUI布局管理工具，用于处理RectTransform的修改操作，支持执行布局修改、获取布局属性、设置锚点预设等功能": "UGUI layout management tool, handles RectTransform modifications, supports layout changes, getting properties, setting anchor presets, etc.",
    "偏好设置管理工具，用于处理EditorPrefs和PlayerPrefs的存储、检索、删除等操作": "Preference settings management tool, handles EditorPrefs and PlayerPrefs storage, retrieval, deletion, etc.",
    "资源和文件夹位置操作工具，支持在资源管理器中显示文件、打开文件夹、定位资源、选择资源和获取资源路径等功能": "Asset and folder location operation tool, supports revealing files in explorer, opening folders, pinging assets, selecting assets, getting asset paths, etc.",
    "标签和图层管理工具，用于处理Unity项目中的标签和图层操作，支持添加、删除和获取标签，添加、删除和获取图层等功能": "Tag and layer management tool, handles Unity project tags and layers, supports adding, removing, getting tags and layers, etc.",
    "Unity包管理器工具，支持添加、删除、列表和搜索包等操作": "Unity package manager tool, supports adding, removing, listing, searching packages, etc.",
    "C#代码执行工具，支持编译和运行任意C#方法，处理代码执行结果和错误信息": "C# code execution tool, supports compiling and running arbitrary C# methods, handling execution results and errors",
    "Python脚本执行工具，支持验证和运行Python代码，处理脚本输出和错误信息": "Python script execution tool, supports validating and running Python code, handling script output and errors",
    "地形编辑工具，用于修改和管理地形资源属性": "Terrain editing tool, modifies and manages terrain asset properties",
    "纹理编辑工具，用于修改和管理纹理导入设置和属性": "Texture editing tool, modifies and manages texture import settings and properties",
    "ScriptableObject编辑工具，用于修改和管理ScriptableObject资源属性": "ScriptableObject editing tool, modifies and manages ScriptableObject asset properties",
    "着色器编辑工具，用于修改和管理着色器资源属性": "Shader editing tool, modifies and manages shader asset properties",
    "精灵图集编辑工具，用于修改和管理精灵图集资源属性": "Sprite atlas editing tool, modifies and manages sprite atlas asset properties",
    "场景编辑工具，用于修改和管理场景资源属性": "Scene editing tool, modifies and manages scene asset properties",
    "脚本编辑工具，用于修改和管理脚本资源属性": "Script editing tool, modifies and manages script asset properties",
    "预制体编辑工具，用于修改和管理预制体资源属性": "Prefab editing tool, modifies and manages prefab asset properties",
    "粒子系统编辑工具，用于修改和管理粒子系统资源属性": "Particle system editing tool, modifies and manages particle system asset properties",
    "网格编辑工具，用于修改和管理网格资源属性": "Mesh editing tool, modifies and manages mesh asset properties",
    "模型编辑工具，用于修改和管理模型资源属性": "Model editing tool, modifies and manages model asset properties",
    "材质编辑工具，用于修改和管理材质资源属性": "Material editing tool, modifies and manages material asset properties",
    "游戏对象编辑工具，用于修改和管理游戏对象属性": "GameObject editing tool, modifies and manages game object properties",
    "音频编辑工具，用于修改和管理音频资源属性": "Audio editing tool, modifies and manages audio asset properties",
    "组件编辑工具，用于修改和管理游戏对象组件属性": "Component editing tool, modifies and manages game object component properties",
    "动画剪辑编辑工具，用于修改和管理动画剪辑资源属性": "Animation clip editing tool, modifies and manages animation clip asset properties",
    "HTTP请求工具，用于处理网络操作，包括HTTP请求、文件下载、上传和API调用等功能": "HTTP request tool, handles network operations including HTTP requests, file downloads, uploads, API calls, etc.",
    "项目资源搜索工具，支持按类型和关键词搜索项目中的资源文件": "Project asset search tool, supports searching asset files by type and keywords",
    "项目资源操作工具，支持导入、修改、移动、复制、重命名等资源管理操作": "Project asset operation tool, supports importing, modifying, moving, copying, renaming asset management operations",
    "项目资源创建工具，支持在项目窗口中创建各种类型的资源文件": "Project asset creation tool, supports creating various types of asset files in project window",
    "对象删除工具，用于处理Unity对象（GameObject、资源等）的删除操作，支持交互式确认": "Object deletion tool, handles deletion of Unity objects (GameObjects, assets, etc.), supports interactive confirmation",
    "游戏窗口管理工具，支持输入模拟、窗口操作、截图和图像处理等功能": "Game window management tool, supports input simulation, window operations, screenshots, image processing, etc.",
    "Unity编辑器状态管理工具，用于获取编辑器状态、窗口信息、选择对象和执行菜单命令等功能": "Unity editor state management tool, gets editor state, window info, selected objects, executes menu commands, etc.",
}

# Parameter description mappings (most common ones)
PARAM_MAP = {
    "操作类型": "Action type",
    "GameObject名称": "GameObject name",
    "模板来源": "Template source",
    "GameObject标签": "GameObject tag",
    "GameObject所在层": "GameObject layer",
    "父对象名称或路径": "Parent object name or path",
    "父对象唯一ID": "Parent object unique ID",
    "位置坐标 [x, y, z]": "Position coordinates [x, y, z]",
    "位置坐标": "Position coordinates",
    "旋转角度 [x, y, z]": "Rotation angles [x, y, z]",
    "旋转角度": "Rotation angles",
    "缩放比例 [x, y, z]": "Scale values [x, y, z]",
    "缩放比例": "Scale values",
    "基元类型": "Primitive type",
    "预制体路径": "Prefab path",
    "菜单路径": "Menu path",
    "要复制的GameObject名称": "GameObject name to copy",
    "是否保存为预制体": "Whether to save as prefab",
    "设置激活状态": "Set active state",
    "目标GameObject层级路径（用于应用操作）": "Target GameObject hierarchy path (for apply operation)",
    "链接类型": "Connection type",
    "是否强制创建链接（覆盖现有连接）": "Whether to force create connection (override existing connection)",
    "对象层级路径": "Object hierarchy path",
    "对齐视图方向": "Align view direction",
    "截图保存路径": "Screenshot save path",
    "宽高比": "Aspect ratio",
    "锚点预设": "Anchor preset",
    "Terrain对象层级路径": "Terrain object hierarchy path",
    "TerrainData资源路径": "TerrainData asset path",
    "高度图文件路径": "Heightmap file path",
    "导出格式": "Export format",
    "纹理资源路径": "Texture asset path",
    "纹理类型": "Texture type",
    "Sprite模式": "Sprite mode",
    "Sprite轴心": "Sprite pivot",
    "网格类型": "Mesh type",
    "压缩格式": "Compression format",
    "过滤模式": "Filter mode",
    "包装模式": "Wrap mode",
    "ScriptableObject资产路径": "ScriptableObject asset path",
    "ScriptableObject脚本类名": "ScriptableObject script class name",
    "目标路径": "Destination path",
    "搜索模式": "Search pattern",
    "图集资源路径": "Atlas asset path",
    "筛选模式": "Filter pattern",
    "图集类型": "Atlas type",
    "偏好设置类型": "Preference settings type",
    "键名": "Key name",
    "值": "Value",
    "值类型": "Value type",
    "默认值": "Default value",
    "标签名称": "Tag name",
    "层名称": "Layer name",
    "包来源类型": "Package source type",
    "包名称": "Package name",
    "包完整标识符": "Package full identifier",
    "包版本": "Package version",
    "GitHub仓库URL": "GitHub repository URL",
    "GitHub分支名": "GitHub branch name",
    "包路径": "Package path",
    "搜索关键词": "Search keywords",
    "包范围过滤": "Package scope filter",
    "要执行的C#代码内容": "C# code content to execute",
    "Python脚本代码内容": "Python script code content",
    "代码功能描述": "Code functionality description",
    "脚本功能描述": "Script functionality description",
    "功能描述": "Functionality description",
    "类名，默认是CodeClass": "Class name, default is CodeClass",
    "入口方法名，默认是Execute": "Entry method name, default is Execute",
    "命名空间，默认是CodeNamespace": "Namespace, default is CodeNamespace",
    "脚本名称": "Script name",
    "Python解释器路径": "Python interpreter path",
    "Python脚本文件路径": "Python script file path",
    "工作目录": "Working directory",
    "要安装的Python包": "Python packages to install",
    "requirements.txt文件路径": "requirements.txt file path",
    "虚拟环境路径": "Virtual environment path",
    "资源GUID": "Asset GUID",
    "对象名称": "Object name",
    "资源路径": "Asset path",
    "文件夹路径": "Folder path",
    "消息类型列表，默认全部类型": "Message type list, defaults to all types",
    "最大返回消息数，不设置则获取全部": "Maximum number of messages to return, returns all if not set",
    "文本过滤器，过滤包含指定文本的日志": "Text filter, filters logs containing specified text",
    "输出格式，默认detailed": "Output format, defaults to detailed",
    "要写入的日志消息内容": "Log message content to write",
    "日志标签，用于分类和过滤": "Log tag for categorization and filtering",
    "上下文对象名称，用于在控制台中定位相关GameObject": "Context object name, used to locate related GameObject in console",
    "断言条件表达式（仅用于assert类型）": "Assert condition expression (only for assert type)",
}

# Comment mappings
COMMENT_MAP = {
    "创建当前方法支持的参数键列表": "Create the list of parameter keys supported by this method",
    "对应方法名:": "Corresponding method name:",
    "支持:": "Supports:",
}

def process_file(filepath):
    """Process a single file"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        modified = False
        
        # 1. Replace ToolName categories
        for zh, en in CATEGORY_MAP.items():
            pattern = rf'\[ToolName\([^,]+,\s*"{re.escape(zh)}"\)\]'
            match = re.search(pattern, content)
            if match:
                content = content.replace(match.group(), match.group().replace(zh, en))
                modified = True
                print(f"  - Replaced category: {zh} -> {en}")
        
        # 2. Replace Description content
        for zh, en in DESC_MAP.items():
            if f'"{zh}"' in content:
                content = content.replace(f'"{zh}"', f'"{en}"')
                modified = True
                print(f"  - Replaced description")
        
        # 3. Replace parameter descriptions
        for zh, en in PARAM_MAP.items():
            if f'"{zh}"' in content:
                content = content.replace(f'"{zh}"', f'"{en}"')
                modified = True
        
        # 4. Replace common comments
        for zh, en in COMMENT_MAP.items():
            if zh in content:
                content = content.replace(zh, en)
                modified = True
        
        # Save if modified
        if modified and content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"✓ Processed: {os.path.basename(filepath)}")
            return True
        
        return False
        
    except Exception as e:
        print(f"✗ Error processing {os.path.basename(filepath)}: {str(e)}")
        return False

def main():
    # Base directory
    base_dir = r"c:\WareHouse\unity3d-mcp\demo\Packages\unity-package\Editor\Tools"
    
    if not os.path.exists(base_dir):
        print(f"Error: Directory not found: {base_dir}")
        return
    
    # Find all .cs files
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    
    print(f"Found {len(cs_files)} .cs files")
    print("="*60)
    
    # Skip files already processed
    skip_files = ['ConsoleRead.cs', 'ConsoleWrite.cs', 'HierarchyCreate.cs', 'HierarchyApply.cs']
    
    processed = 0
    skipped = 0
    errors = 0
    
    for filepath in cs_files:
        filename = os.path.basename(filepath)
        
        if filename in skip_files:
            skipped += 1
            continue
        
        # Check if file contains Chinese
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        if not re.search(r'[\u4e00-\u9fff]', content):
            skipped += 1
            continue
        
        print(f"\nProcessing: {filename}")
        if process_file(filepath):
            processed += 1
        else:
            errors += 1
    
    print("\n" + "="*60)
    print(f"Summary:")
    print(f"  Processed: {processed}")
    print(f"  Skipped: {skipped}")
    print(f"  Errors: {errors}")
    print(f"  Total: {len(cs_files)}")

if __name__ == "__main__":
    main()
