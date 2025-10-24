"""
Unity材质编辑工具，包含材质的创建、修改和管理功能。

支持的功能：
- 创建材质：create - 从零开始创建新材质
- 设置属性：set_properties - 修改材质的各种属性参数
- 复制材质：duplicate - 复制已有材质创建新材质
- 获取信息：get_info - 获取材质详细信息
- 搜索材质：search - 搜索项目中的材质资源
- 复制属性：copy_properties - 在不同材质间复制属性
- 更改着色器：change_shader - 更改材质的着色器
- 启用关键字：enable_keyword - 启用着色器关键字
- 禁用关键字：disable_keyword - 禁用着色器关键字

示例用法：
1. 创建红色材质：
   {"action": "create", "path": "Assets/Materials/RedMaterial.mat", "shader_name": "Standard", "properties": {"_Color": [1, 0, 0, 1]}}

2. 设置材质属性：
   {"action": "set_properties", "path": "Assets/Materials/MyMaterial.mat", "properties": {"_Metallic": 0.8, "_Smoothness": 0.5}}

3. 复制材质：
   {"action": "duplicate", "path": "Assets/Materials/SourceMaterial.mat", "destination": "Assets/Materials/NewMaterial.mat"}
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_material_tools(mcp: FastMCP):
    @mcp.tool("edit_material")
    def edit_material(
        ctx: Context,
        action: Annotated[str, Field(
            title="材质操作类型",
            description="要执行的材质操作: create(创建), set_properties(设置属性), duplicate(复制), get_info(获取信息), search(搜索), copy_properties(复制属性), change_shader(更改着色器), enable_keyword(启用关键字), disable_keyword(禁用关键字)",
            examples=["create", "set_properties", "duplicate", "get_info", "search", "copy_properties", "change_shader", "enable_keyword", "disable_keyword"]
        )],
        path: Annotated[str, Field(
            title="材质路径",
            description="材质资源路径，Unity标准格式：Assets/Materials/MaterialName.mat",
            examples=["Assets/Materials/PlayerMaterial.mat", "Materials/Ground.mat"]
        )],
        shader_name: Annotated[Optional[str], Field(
            title="着色器名称",
            description="着色器名称或路径",
            default=None,
            examples=["Standard", "Universal Render Pipeline/Lit", "Unlit/Color"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="材质属性",
            description="材质属性字典，包含颜色、纹理、浮点数等属性",
            default=None,
            examples=[
                {"_Color": [1.0, 0.0, 0.0, 1.0]},
                {"_Metallic": 0.5, "_Smoothness": 0.8},
                {"_MainTex": "Assets/Textures/diffuse.png"}
            ]
        )] = None,
        source_path: Annotated[Optional[str], Field(
            title="源材质路径",
            description="源材质路径（复制时使用）",
            default=None,
            examples=["Assets/Materials/SourceMaterial.mat"]
        )] = None,
        destination: Annotated[Optional[str], Field(
            title="目标路径",
            description="目标路径（复制/移动时使用）",
            default=None,
            examples=["Assets/Materials/NewMaterial.mat"]
        )] = None,
        query: Annotated[Optional[str], Field(
            title="搜索模式",
            description="搜索模式，如*.mat",
            default=None,
            examples=["*.mat", "Player*", "Red*"]
        )] = None,
        recursive: Annotated[Optional[bool], Field(
            title="递归搜索",
            description="是否递归搜索子文件夹",
            default=True
        )] = True,
        force: Annotated[Optional[bool], Field(
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）",
            default=False
        )] = False,
        render_queue: Annotated[Optional[int], Field(
            title="渲染队列",
            description="渲染队列值",
            default=None,
            examples=[2000, 3000]
        )] = None,
        enable_instancing: Annotated[Optional[bool], Field(
            title="启用实例化",
            description="是否启用GPU实例化",
            default=None
        )] = None,
        double_sided_global_illumination: Annotated[Optional[bool], Field(
            title="双面全局光照",
            description="是否启用双面全局光照",
            default=None
        )] = None,
        keyword: Annotated[Optional[str], Field(
            title="着色器关键字",
            description="着色器关键字，用于enable_keyword和disable_keyword操作",
            default=None,
            examples=["_EMISSION", "_NORMALMAP", "_METALLICGLOSSMAP"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity材质编辑工具，用于创建、修改和管理材质资源。

        支持完整的材质编辑功能，适用于：
        - 材质创建：从零开始创建新材质
        - 属性调整：修改材质的各种属性参数
        - 复制材质：复制已有材质创建新材质
        - 属性复制：在不同材质间复制属性
        - 着色器管理：更改材质的着色器
        - 关键字控制：启用/禁用着色器关键字
        - 批量处理：搜索和管理多个材质

        示例用法：
        1. 创建红色材质：
           {"action": "create", "path": "Assets/Materials/RedMaterial.mat", "shader": "Standard", "properties": {"_Color": [1, 0, 0, 1]}}

        2. 设置材质属性：
           {"action": "set_properties", "path": "Assets/Materials/MyMaterial.mat", "properties": {"_Metallic": 0.8, "_Smoothness": 0.5}}

        3. 复制材质：
           {"action": "duplicate", "path": "Assets/Materials/SourceMaterial.mat", "destination": "Assets/Materials/NewMaterial.mat"}

        4. 更改材质着色器：
           {"action": "change_shader", "path": "Assets/Materials/MyMaterial.mat", "shader": "Universal Render Pipeline/Lit"}

        5. 启用着色器关键字：
           {"action": "enable_keyword", "path": "Assets/Materials/MyMaterial.mat", "keyword": "_EMISSION"}
        """
        return send_to_unity("edit_material", {
            "action": action,
            "path": path,
            "shader_name": shader_name,
            "properties": properties,
            "source_path": source_path,
            "destination": destination,
            "query": query,
            "recursive": recursive,
            "force": force,
            "render_queue": render_queue,
            "enable_instancing": enable_instancing,
            "double_sided_global_illumination": double_sided_global_illumination,
            "keyword": keyword
        })
