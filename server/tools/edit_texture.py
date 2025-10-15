"""
texture import settings editor
modifyUnityimport settings of texture assets，including set toSpritetype、adjust compression quality etc
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_texture_tools(mcp: FastMCP):
    @mcp.tool("edit_texture")
    def edit_texture(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type",
            examples=["set_type", "set_sprite_settings", "get_settings"]
        ),
        texture_path: str = Field(
            ...,
            title="texture asset path",
            description="texture asset path（relative toAssets）",
            examples=["Assets/Pics/rabbit.jpg", "Assets/Textures/icon.png", "Assets/UI/button.png"]
        ),
        texture_type: Optional[str] = Field(
            None,
            title="texture type",
            description="texture type",
            examples=["Default", "NormalMap", "Sprite", "Cursor", "Cookie", "Lightmap", "HDR"]
        ),
        sprite_mode: Optional[str] = Field(
            None,
            title="Spritemode",
            description="Spritemode",
            examples=["Single", "Multiple", "Polygon"]
        ),
        pixels_per_unit: Optional[float] = Field(
            None,
            title="pixels per unit",
            description="pixels per unit",
            examples=[100, 200, 1]
        ),
        sprite_pivot: Optional[str] = Field(
            None,
            title="Spritepivot",
            description="Spritepivot",
            examples=["Center", "TopLeft", "TopCenter", "TopRight", "MiddleLeft", "MiddleCenter", "MiddleRight", "BottomLeft", "BottomCenter", "BottomRight"]
        ),
        generate_physics_shape: Optional[bool] = Field(
            None,
            title="generate physics shape",
            description="generate physics shape"
        ),
        mesh_type: Optional[str] = Field(
            None,
            title="mesh type",
            description="mesh type"
        ),
        compression: Optional[str] = Field(
            None,
            title="compression format",
            description="compression format",
            examples=["HighQuality", "NormalQuality", "LowQuality"]
        ),
        max_texture_size: Optional[int] = Field(
            None,
            title="max texture size",
            description="max texture size",
            examples=[1024, 2048, 4096]
        ),
        filter_mode: Optional[str] = Field(
            None,
            title="filter mode",
            description="filter mode",
            examples=["Point", "Bilinear", "Trilinear"]
        ),
        wrap_mode: Optional[str] = Field(
            None,
            title="wrap mode",
            description="wrap mode",
            examples=["Repeat", "Clamp", "Mirror", "MirrorOnce"]
        ),
        readable: Optional[bool] = Field(
            None,
            title="read write",
            description="read write"
        ),
        generate_mip_maps: Optional[bool] = Field(
            None,
            title="generateMiptexture",
            description="generateMiptexture"
        ),
        srgb_texture: Optional[bool] = Field(
            None,
            title="sRGBtexture",
            description="sRGBtexture"
        )
    ) -> Dict[str, Any]:
        """
        texture import settings editor（secondary tool）
        
        supported operations:
        - set_type: set texture type
        - set_sprite_settings: setSpritedetailed parameters
        - get_settings: get current texture settings
        """
        return get_common_call_response("edit_texture")
