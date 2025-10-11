"""
图集(Sprite Atlas)编辑工具
处理Unity中精灵图集的创建、编辑和管理操作
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


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
        )
    ) -> Dict[str, Any]:
        """
        图集(Sprite Atlas)编辑工具（二级工具）
        
        支持的操作:
        - create: 创建新的精灵图集
        - add_sprites: 向图集添加精灵或文件夹
        - remove_sprites: 从图集移除精灵或文件夹
        - set_settings: 设置图集参数
        - get_settings: 获取图集参数
        - pack: 打包图集
        """
        return get_common_call_response("edit_sprite_atlas")

