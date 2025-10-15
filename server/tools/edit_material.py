"""
Unitymaterial editing tool，includes material creation、modify and manage features。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_material_tools(mcp: FastMCP):
    @mcp.tool("edit_material")
    def edit_material(
        ctx: Context,
        action: Annotated[str, Field(
            title="material operation type",
            description="material operation to perform: create(create), modify(modify properties), duplicate(copy), delete(delete), apply_texture(apply texture)",
            examples=["create", "modify", "duplicate", "delete", "apply_texture"]
        )],
        material_path: Annotated[str, Field(
            title="material path",
            description="of the material fileAssetspath",
            examples=["Assets/Materials/PlayerMaterial.mat", "Materials/Ground.mat"]
        )],
        shader_name: Annotated[Optional[str], Field(
            title="shader name",
            description="shader name to use，only whencreateandmodifyused during operation",
            default=None,
            examples=["Standard", "Universal Render Pipeline/Lit", "Unlit/Color"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="material properties",
            description="material property key values to set，only whencreateandmodifyused during operation",
            default=None,
            examples=[
                {"_Color": [1.0, 0.0, 0.0, 1.0]},
                {"_Metallic": 0.5, "_Smoothness": 0.8},
                {"_MainTex": "Assets/Textures/diffuse.png"}
            ]
        )] = None,
        texture_path: Annotated[Optional[str], Field(
            title="texture path",
            description="texture file path to apply，only whenapply_textureused during operation",
            default=None,
            examples=["Assets/Textures/brick.png", "Textures/wood_diffuse.jpg"]
        )] = None,
        texture_slot: Annotated[Optional[str], Field(
            title="texture slot",
            description="texture slot name to apply，only whenapply_textureused during operation",
            default="_MainTex",
            examples=["_MainTex", "_BumpMap", "_MetallicGlossMap"]
        )] = "_MainTex"
    ) -> Dict[str, Any]:
        """Unitymaterial editing tool，used to create、modify and manage material assets。

        full featured material editing，suitable for：
        - material creation：create new material from scratch
        - property adjustment：modify various material parameters
        - texture management：apply or replace material textures
        - batch processing：apply to multiple materials
        """
        return get_common_call_response("edit_material")
