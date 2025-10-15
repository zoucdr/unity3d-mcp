"""
scene management tool
managementUnityscene，including load、save、create and fetch hierarchy
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_scene_tools(mcp: FastMCP):
    @mcp.tool("edit_scene")
    def edit_scene(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type",
            examples=["load", "save", "create", "get_hierarchy"]
        ),
        name: str = Field(
            ...,
            title="scene name",
            description="scene name",
            examples=["MainMenu", "Level1", "TestScene"]
        ),
        path: str = Field(
            ...,
            title="asset path",
            description="asset path",
            examples=["Assets/Scenes/MainMenu.unity", "Assets/Scenes/", "Assets/Scenes/Level1.unity"]
        ),
        build_index: int = Field(
            ...,
            title="build index",
            description="build index",
            examples=[0, 1, 2]
        )
    ) -> Dict[str, Any]:
        """
        scene management tool（secondary tool）
        
        supported operations:
        - load: load scene
        - save: save scene
        - create: create scene
        - get_hierarchy: get scene hierarchy
        """
        return get_common_call_response("edit_scene")
