"""
Unity模型管理工具，用于处理3D模型资源的导入、修改、复制和删除等操作。

支持的功能：
- 模型导入：import
- 模型修改：modify
- 模型复制：duplicate
- 模型删除：delete
- 获取信息：get_info
- 模型搜索：search
- 设置导入参数：set_import_settings
- 提取材质：extract_materials
- 模型优化：optimize
- 材质重定向：remap_materials
"""
from typing import Annotated, Dict, Any, Optional, List, Union
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_model_tools(mcp: FastMCP):
    @mcp.tool("edit_model")
    def edit_model(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="要执行的模型操作: import(导入), modify(修改), duplicate(复制), delete(删除), get_info(获取信息), search(搜索), set_import_settings(设置导入参数), extract_materials(提取材质), optimize(优化), remap_materials(材质重定向)",
            examples=["import", "modify", "duplicate", "delete", "get_info", "search", "set_import_settings", "extract_materials", "optimize", "remap_materials"]
        )],
        path: Annotated[Optional[str], Field(
            title="模型资源路径",
            description="模型资源路径，Unity标准格式：Assets/Models/ModelName.fbx",
            default=None,
            examples=["Assets/Models/Character.fbx", "Assets/Models/Weapon.obj"]
        )] = None,
        source_file: Annotated[Optional[str], Field(
            title="源文件路径",
            description="源文件路径（导入时使用）",
            default=None,
            examples=["C:/Models/Character.fbx", "D:/Downloads/Weapon.obj"]
        )] = None,
        destination: Annotated[Optional[str], Field(
            title="目标路径",
            description="目标路径（复制/移动时使用）",
            default=None,
            examples=["Assets/Models/CharacterCopy.fbx", "Assets/Models/New/Weapon.obj"]
        )] = None,
        query: Annotated[Optional[str], Field(
            title="搜索模式",
            description="搜索模式，如*.fbx, *.obj",
            default=None,
            examples=["*.fbx", "Character*", "*.obj"]
        )] = None,
        recursive: Annotated[Optional[bool], Field(
            title="是否递归搜索子文件夹",
            description="是否递归搜索子文件夹",
            default=None,
            examples=[True, False]
        )] = None,
        force: Annotated[Optional[bool], Field(
            title="是否强制执行操作",
            description="是否强制执行操作（覆盖现有文件等）",
            default=None,
            examples=[True, False]
        )] = None,
        import_settings: Annotated[Optional[Dict[str, Any]], Field(
            title="导入设置",
            description="模型导入设置，包含各种导入参数",
            default=None,
            examples=[{
                "scale_factor": 1.0,
                "use_file_scale": True,
                "import_blend_shapes": True,
                "optimize_mesh": True
            }]
        )] = None,
        material_remaps: Annotated[Optional[Dict[str, str]], Field(
            title="材质重定向映射",
            description="材质重定向映射，格式为{\"source_name\":\"target_path\"}的字典",
            default=None,
            examples=[{
                "DefaultMaterial": "Assets/Materials/CustomMaterial.mat",
                "SecondMaterial": "Assets/Materials/AnotherMaterial.mat"
            }]
        )] = None,
        extract_path: Annotated[Optional[str], Field(
            title="提取材质路径",
            description="提取材质的目标路径（extract_materials操作使用）",
            default=None,
            examples=["Assets/Materials/Extracted", "Assets/Materials/ModelName"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity模型管理工具，用于处理3D模型资源的导入、修改、复制和删除等操作。

        支持多种模型管理功能，适用于：
        - 模型导入：从外部文件导入3D模型到项目中
        - 模型修改：修改模型的导入设置和参数
        - 模型复制：复制现有模型创建新副本
        - 模型删除：删除不需要的模型资源
        - 信息查询：获取模型的详细信息和元数据
        - 模型搜索：按条件搜索项目中的模型资源
        - 导入设置：配置模型的导入参数
        - 材质提取：从模型中提取材质
        - 模型优化：优化模型以提高性能
        - 材质重定向：将模型中的材质重定向到项目中的其他材质
        
        示例用法：
        1. 导入3D模型:
           {"action": "import", "path": "Assets/Models/character.fbx", "source_file": "D:/Models/character.fbx"}
           
        2. 修改模型导入设置:
           {"action": "modify", "path": "Assets/Models/character.fbx", "import_settings": {"scale_factor": 0.01, "optimize_mesh": true}}
           
        3. 复制模型:
           {"action": "duplicate", "path": "Assets/Models/character.fbx", "destination": "Assets/Models/character_copy.fbx"}
           
        4. 获取模型信息:
           {"action": "get_info", "path": "Assets/Models/character.fbx"}
           
        5. 搜索模型:
           {"action": "search", "query": "*.fbx", "recursive": true}
           
        6. 材质重定向:
           {"action": "remap_materials", "path": "Assets/Models/character.fbx", "material_remaps": {"DefaultMaterial": "Assets/Materials/CustomMaterial.mat"}}
        """
        params = {
            "action": action,
            "path": path,
            "source_file": source_file,
            "destination": destination,
            "query": query,
            "recursive": recursive,
            "force": force,
            "import_settings": import_settings,
            "material_remaps": material_remaps,
            "extract_path": extract_path
        }
        return send_to_unity("edit_model", params)
