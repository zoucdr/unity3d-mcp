"""
Unityasset management tool
provideUnityvarious asset operations，including import、modify、move、copy etc
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_asset_tools(mcp: FastMCP):
    @mcp.tool("edit_asset")
    def edit_asset(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type，such as import/modify/move/duplicate etc",
            examples=["import", "modify", "move", "duplicate", "search", "get_info", "create_folder"]
        ),
        path: str = Field(
            ...,
            title="asset path",
            description="asset path，Unitystandard format：Assets/Folder/File.extension",
            examples=["Assets/Textures/icon.png", "Assets/Scripts/PlayerController.cs", "Assets/Materials/RedMaterial.mat"]
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="asset properties",
            description="asset property dict，for setting various asset properties",
            examples=[{"playerSpeed": 5.0, "maxHealth": 100}]
        ),
        destination: Optional[str] = Field(
            None,
            title="target path",
            description="target path（move/used when copying）",
            examples=["Assets/Scripts/NewName.cs", "Assets/Materials/RedMaterialCopy.mat"]
        ),
        query: Optional[str] = Field(
            None,
            title="search mode",
            description="search mode，such as*.prefab",
            examples=["*.prefab", "Player*", "*.mat"]
        ),
        force: Optional[bool] = Field(
            False,
            title="force execute",
            description="whether to force execution（overwrite existing files etc）"
        )
    ) -> Dict[str, Any]:
        """
        Unityasset management tool（secondary tool）
        
        supported operations:
        - import: reimport asset
        - modify: modify asset properties
        - duplicate: copy asset
        - move: move/rename asset
        - rename: move/rename asset（andmovesame）
        - search: search assets
        - get_info: get asset info
        - create_folder: create folder
        """
        # ⚠️ important note：this function only provides parameter description and docs
        # for actual calls please use single_call function
        # example：single_call(func="edit_asset", args={...})

        return get_common_call_response("edit_asset")
