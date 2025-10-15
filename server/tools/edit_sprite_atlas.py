"""
atlas(Sprite Atlas)editing tool
processUnitycreation of sprite atlas in、edit and manage operations
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
            title="operation type",
            description="operation type：create(create), add_sprites(add sprites), remove_sprites(remove sprites), set_settings(set parameters), get_settings(get parameters), pack(pack atlas)",
            examples=["create", "add_sprites", "remove_sprites", "set_settings", "get_settings", "pack"]
        ),
        atlas_path: str = Field(
            ...,
            title="atlas asset path",
            description="atlas asset path（relative toAssets）",
            examples=["Assets/Atlas/UIAtlas.spriteatlas", "Assets/Textures/CharacterAtlas.spriteatlas"]
        ),
        sprite_paths: Optional[List[str]] = Field(
            None,
            title="sprite path array",
            description="sprite path array to add or remove",
            examples=[["Assets/Textures/icon1.png", "Assets/Textures/icon2.png"]]
        ),
        folder_paths: Optional[List[str]] = Field(
            None,
            title="folder path array",
            description="folder path array to add or remove",
            examples=[["Assets/Textures/UI", "Assets/Textures/Icons"]]
        ),
        include_subfolders: Optional[bool] = Field(
            None,
            title="include subfolders",
            description="include subfolders"
        ),
        filter_pattern: Optional[str] = Field(
            None,
            title="filter mode",
            description="filter mode，for example *.png",
            examples=["*.png", "*.jpg", "icon_*"]
        ),
        type: Optional[str] = Field(
            None,
            title="atlas type",
            description="atlas type：Master(main atlas), Variant(variant)",
            examples=["Master", "Variant"]
        ),
        master_atlas_path: Optional[str] = Field(
            None,
            title="main atlas path",
            description="main atlas path（only whentypeasVarianteffective when）",
            examples=["Assets/Atlas/MainAtlas.spriteatlas"]
        ),
        allow_rotation: Optional[bool] = Field(
            None,
            title="allow rotation",
            description="allow sprite rotation for better packing"
        ),
        tight_packing: Optional[bool] = Field(
            None,
            title="tight packing",
            description="use tight packing"
        ),
        padding: Optional[int] = Field(
            None,
            title="texture padding",
            description="texture padding（pixels）",
            examples=[2, 4, 8]
        ),
        readable: Optional[bool] = Field(
            None,
            title="readable",
            description="readable or not"
        ),
        generate_mip_maps: Optional[bool] = Field(
            None,
            title="generateMiptextures",
            description="generate or notMiptextures"
        ),
        filter_mode: Optional[str] = Field(
            None,
            title="filtering mode",
            description="filtering mode：Point, Bilinear, Trilinear",
            examples=["Point", "Bilinear", "Trilinear"]
        ),
        compression: Optional[str] = Field(
            None,
            title="compression format",
            description="compression format：None, LowQuality, NormalQuality, HighQuality",
            examples=["None", "LowQuality", "NormalQuality", "HighQuality"]
        ),
        platform: Optional[str] = Field(
            None,
            title="platform name",
            description="platform name：Android, iOS, Standalone, WebGLetc",
            examples=["Android", "iOS", "Standalone", "WebGL"]
        ),
        max_texture_size: Optional[int] = Field(
            None,
            title="platform max texture size",
            description="platform max texture size：32, 64, 128, 256, 512, 1024, 2048, 4096, 8192",
            examples=[1024, 2048, 4096]
        ),
        format: Optional[str] = Field(
            None,
            title="platform texture format",
            description="platform texture format：Automatic, RGBA32, RGB24, ASTC_4x4, ASTC_6x6, ASTC_8x8, ETC2_RGBA8, DXT5etc。if not specified，use platform recommended defaults（Android=ETC2_RGBA8, iOS=ASTC_6x6, PC=DXT5）",
            examples=["ETC2_RGBA8", "ASTC_6x6", "ASTC_8x8", "DXT5", "RGBA32"]
        ),
        compression_quality: Optional[int] = Field(
            None,
            title="compression quality",
            description="compression quality：0-100，valid only for some formats。if not specified，default to50（balance quality and size）",
            examples=[50, 80, 100]
        ),
        override_for_platform: Optional[bool] = Field(
            None,
            title="override platform defaults",
            description="override platform defaults，defaults totrue"
        )
    ) -> Dict[str, Any]:
        """
        atlas(Sprite Atlas)editing tool（secondary tool）
        
        supported operations:
        - create: create new sprite atlas
        - add_sprites: add sprites or folders to atlas
        - remove_sprites: remove sprites or folders from atlas
        - set_settings: set atlas parameters（include platform specific settings）
        - get_settings: get atlas parameters
        - pack: pack atlas
        
        platform specific settings:
        When providedplatformwhen setting parameters，allow platform specific texture config：
        - platform: target platform name（Android, iOS, Standalone, WebGLetc）
        - max_texture_size: max texture size for platform
        - format: texture format（if not specified，auto use platform defaults）
          * Androidrecommended: ETC2_RGBA8
          * iOSrecommended: ASTC_6x6
          * PCrecommended: DXT5
        - compression_quality: compression quality0-100（default50）
        - override_for_platform: override platform defaults（defaulttrue）
        
        example - set onlyAndroidmax size（formatandqualityuse defaults）:
          platform="Android", max_texture_size=1024
        
        example - full settingsAndroidplatform config:
          platform="Android", max_texture_size=1024, 
          format="ETC2_RGBA8", compression_quality=50
        """
        return get_common_call_response("edit_sprite_atlas")

