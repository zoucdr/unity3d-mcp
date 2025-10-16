"""
纹理导入设置修改工具
修改Unity中纹理资源的导入设置，包括设置为Sprite类型、调整压缩质量等

支持的功能：
- 设置纹理类型：Default, NormalMap, Sprite, Cursor等
- 配置Sprite设置：模式、轴心点、像素密度等
- 调整压缩设置：压缩格式、最大尺寸等
- 设置过滤和包装模式：Point, Bilinear, Repeat, Clamp等
- 配置可读写、Mip贴图和sRGB设置
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_texture_tools(mcp: FastMCP):
    @mcp.tool("edit_texture")
    def edit_texture(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="要执行的纹理操作: set_type(设置纹理类型), set_sprite_settings(设置Sprite详细参数), get_settings(获取当前纹理设置)",
            examples=["set_type", "set_sprite_settings", "get_settings"]
        ),
        texture_path: str = Field(
            ...,
            title="纹理资源路径",
            description="纹理资源路径（相对于Assets）",
            examples=["Assets/Pics/rabbit.jpg", "Assets/Textures/icon.png", "Assets/UI/button.png"]
        ),
        texture_type: Optional[str] = Field(
            None,
            title="纹理类型",
            description="纹理类型：Default, NormalMap, EditorGUIAndLegacy, Sprite, Cursor, Cookie, Lightmap, HDR",
            examples=["Default", "NormalMap", "Sprite", "Cursor", "Cookie", "Lightmap", "HDR"]
        ),
        sprite_mode: Optional[str] = Field(
            None,
            title="Sprite模式",
            description="Sprite模式：Single, Multiple, Polygon",
            examples=["Single", "Multiple", "Polygon"]
        ),
        pixels_per_unit: Optional[float] = Field(
            None,
            title="每单位像素数",
            description="每单位像素数",
            examples=[100, 200, 1]
        ),
        sprite_pivot: Optional[str] = Field(
            None,
            title="Sprite轴心点",
            description="Sprite轴心：Center, TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight, Custom",
            examples=["Center", "TopLeft", "TopCenter", "TopRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "BottomLeft", "BottomCenter", "BottomRight"]
        ),
        generate_physics_shape: Optional[bool] = Field(
            None,
            title="生成物理形状",
            description="生成物理形状"
        ),
        mesh_type: Optional[str] = Field(
            None,
            title="网格类型",
            description="网格类型：FullRect, Tight",
            examples=["FullRect", "Tight"]
        ),
        compression: Optional[str] = Field(
            None,
            title="压缩格式",
            description="压缩格式：Uncompressed, LowQuality, NormalQuality, HighQuality",
            examples=["Uncompressed", "LowQuality", "NormalQuality", "HighQuality"]
        ),
        max_texture_size: Optional[int] = Field(
            None,
            title="最大纹理尺寸",
            description="最大纹理尺寸：32, 64, 128, 256, 512, 1024, 2048, 4096, 8192",
            examples=[512, 1024, 2048, 4096]
        ),
        filter_mode: Optional[str] = Field(
            None,
            title="过滤模式",
            description="过滤模式：Point, Bilinear, Trilinear",
            examples=["Point", "Bilinear", "Trilinear"]
        ),
        wrap_mode: Optional[str] = Field(
            None,
            title="包装模式",
            description="包装模式：Repeat, Clamp, Mirror, MirrorOnce",
            examples=["Repeat", "Clamp", "Mirror", "MirrorOnce"]
        ),
        readable: Optional[bool] = Field(
            None,
            title="可读写",
            description="是否启用纹理可读写"
        ),
        generate_mip_maps: Optional[bool] = Field(
            None,
            title="生成Mip贴图",
            description="是否生成Mip贴图"
        ),
        srgb_texture: Optional[bool] = Field(
            None,
            title="sRGB纹理",
            description="是否启用sRGB纹理"
        ),
        extrude_edges: Optional[int] = Field(
            None,
            title="边缘挤出",
            description="边缘挤出值"
        )
    ) -> Dict[str, Any]:
        """
        纹理导入设置修改工具
        
        支持的操作:
        - set_type: 设置纹理类型（Default, NormalMap, Sprite等）
        - set_sprite_settings: 设置Sprite详细参数（模式、轴心点、像素密度等）
        - get_settings: 获取当前纹理设置
        
        示例用法：
        1. 将纹理设置为Sprite类型:
           {"action": "set_type", "texture_path": "Assets/Textures/icon.png", "texture_type": "Sprite"}
           
        2. 设置Sprite的详细参数:
           {"action": "set_sprite_settings", "texture_path": "Assets/Textures/icon.png", 
            "sprite_mode": "Single", "pixels_per_unit": 100, "sprite_pivot": "Center"}
            
        3. 获取纹理的当前设置:
           {"action": "get_settings", "texture_path": "Assets/Textures/icon.png"}
        """
        return send_to_unity("edit_texture", {
            "action": action,
            "texture_path": texture_path,
            "texture_type": texture_type,
            "sprite_mode": sprite_mode,
            "pixels_per_unit": pixels_per_unit,
            "sprite_pivot": sprite_pivot,
            "generate_physics_shape": generate_physics_shape,
            "mesh_type": mesh_type,
            "compression": compression,
            "max_texture_size": max_texture_size,
            "filter_mode": filter_mode,
            "wrap_mode": wrap_mode,
            "readable": readable,
            "generate_mip_maps": generate_mip_maps,
            "srgb_texture": srgb_texture,
            "extrude_edges": extrude_edges
        })
