"""
图集(Sprite Atlas)编辑工具
处理Unity中精灵图集的创建、编辑和管理操作
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_sprite_atlas_tools(mcp: FastMCP):
    @mcp.tool("edit_sprite_atlas")
    def edit_sprite_atlas(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型：create(创建), add_sprites(添加精灵), remove_sprites(移除精灵), set_settings(设置参数), get_settings(获取参数), pack(打包图集)",
            examples=["create", "add_sprites", "remove_sprites", "set_settings", "get_settings", "pack"]
        ),
        atlas_path: str = Field(
            ...,
            title="图集资源路径",
            description="图集资源路径（相对于Assets）",
            examples=["Assets/Atlas/UIAtlas.spriteatlas", "Assets/Textures/CharacterAtlas.spriteatlas"]
        ),
        sprite_paths: Optional[List[str]] = Field(
            None,
            title="精灵路径数组",
            description="要添加或移除的精灵路径数组",
            examples=[["Assets/Textures/icon1.png", "Assets/Textures/icon2.png"]]
        ),
        folder_paths: Optional[List[str]] = Field(
            None,
            title="文件夹路径数组",
            description="要添加或移除的文件夹路径数组",
            examples=[["Assets/Textures/UI", "Assets/Textures/Icons"]]
        ),
        include_subfolders: Optional[bool] = Field(
            None,
            title="包含子文件夹",
            description="是否包含子文件夹"
        ),
        filter_pattern: Optional[str] = Field(
            None,
            title="筛选模式",
            description="筛选模式，例如 *.png",
            examples=["*.png", "*.jpg", "icon_*"]
        ),
        type: Optional[str] = Field(
            None,
            title="图集类型",
            description="图集类型：Master(主图集), Variant(变体)",
            examples=["Master", "Variant"]
        ),
        master_atlas_path: Optional[str] = Field(
            None,
            title="主图集路径",
            description="主图集路径（仅当type为Variant时有效）",
            examples=["Assets/Atlas/MainAtlas.spriteatlas"]
        ),
        allow_rotation: Optional[bool] = Field(
            None,
            title="允许旋转",
            description="是否允许旋转精灵以获得更好的打包效果"
        ),
        tight_packing: Optional[bool] = Field(
            None,
            title="紧凑排列",
            description="是否使用紧凑排列"
        ),
        padding: Optional[int] = Field(
            None,
            title="图像间距",
            description="图像间距（像素）",
            examples=[2, 4, 8]
        ),
        readable: Optional[bool] = Field(
            None,
            title="可读",
            description="是否可读"
        ),
        generate_mip_maps: Optional[bool] = Field(
            None,
            title="生成Mip贴图",
            description="是否生成Mip贴图"
        ),
        filter_mode: Optional[str] = Field(
            None,
            title="过滤模式",
            description="过滤模式：Point, Bilinear, Trilinear",
            examples=["Point", "Bilinear", "Trilinear"]
        ),
        compression: Optional[str] = Field(
            None,
            title="压缩格式",
            description="压缩格式：None, LowQuality, NormalQuality, HighQuality",
            examples=["None", "LowQuality", "NormalQuality", "HighQuality"]
        ),
        platform: Optional[str] = Field(
            None,
            title="平台名称",
            description="平台名称：Android, iOS, Standalone, WebGL等",
            examples=["Android", "iOS", "Standalone", "WebGL"]
        ),
        max_texture_size: Optional[int] = Field(
            None,
            title="平台最大纹理尺寸",
            description="平台最大纹理尺寸：32, 64, 128, 256, 512, 1024, 2048, 4096, 8192",
            examples=[1024, 2048, 4096]
        ),
        format: Optional[str] = Field(
            None,
            title="平台纹理格式",
            description="平台纹理格式：Automatic, RGBA32, RGB24, ASTC_4x4, ASTC_6x6, ASTC_8x8, ETC2_RGBA8, DXT5等。如果未指定，将使用平台推荐的默认格式（Android=ETC2_RGBA8, iOS=ASTC_6x6, PC=DXT5）",
            examples=["ETC2_RGBA8", "ASTC_6x6", "ASTC_8x8", "DXT5", "RGBA32"]
        ),
        compression_quality: Optional[int] = Field(
            None,
            title="压缩质量",
            description="压缩质量：0-100，仅对某些格式有效。如果未指定，默认使用50（平衡质量和大小）",
            examples=[50, 80, 100]
        ),
        override_for_platform: Optional[bool] = Field(
            None,
            title="覆盖平台默认设置",
            description="是否覆盖平台默认设置，默认为true"
        )
    ) -> Dict[str, Any]:
        """
        图集(Sprite Atlas)编辑工具
        
        支持的操作:
        - create: 创建新的精灵图集
        - add_sprites: 向图集添加精灵或文件夹
        - remove_sprites: 从图集移除精灵或文件夹
        - set_settings: 设置图集参数（包括平台特定设置）
        - get_settings: 获取图集参数
        - pack: 打包图集
        
        平台特定设置:
        当提供platform参数时，可以设置平台特定的纹理配置：
        - platform: 目标平台名称（Android, iOS, Standalone, WebGL等）
        - max_texture_size: 该平台的最大纹理尺寸
        - format: 纹理格式（如果未指定，自动使用平台推荐格式）
          * Android推荐: ETC2_RGBA8
          * iOS推荐: ASTC_6x6
          * PC推荐: DXT5
        - compression_quality: 压缩质量0-100（默认50）
        - override_for_platform: 是否覆盖平台默认设置（默认true）
        
        示例 - 仅设置Android最大尺寸（format和quality使用默认值）:
          platform="Android", max_texture_size=1024
        
        示例 - 完整设置Android平台配置:
          platform="Android", max_texture_size=1024, 
          format="ETC2_RGBA8", compression_quality=50
        """
        return send_to_unity("edit_sprite_atlas", {
            "action": action,
            "atlas_path": atlas_path,
            "sprite_paths": sprite_paths,
            "folder_paths": folder_paths,
            "include_subfolders": include_subfolders,
            "filter_pattern": filter_pattern,
            "type": type,
            "master_atlas_path": master_atlas_path,
            "allow_rotation": allow_rotation,
            "tight_packing": tight_packing,
            "padding": padding,
            "readable": readable,
            "generate_mip_maps": generate_mip_maps,
            "filter_mode": filter_mode,
            "compression": compression,
            "platform": platform,
            "max_texture_size": max_texture_size,
            "format": format,
            "compression_quality": compression_quality,
            "override_for_platform": override_for_platform
        })

