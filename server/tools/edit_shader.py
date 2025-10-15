"""
Shadermanagement tool
manageUnityinShaderasset，including create、modify、delete and get info
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response

def register_edit_shader_tools(mcp: FastMCP):
    @mcp.tool("edit_shader")
    def edit_shader(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type",
            examples=["create", "modify", "delete", "get_info", "search", "duplicate", "move", "rename", "compile", "validate"]
        ),
        path: str = Field(
            ...,
            title="Shaderpath",
            description="Shaderpath，Unitystandard format：Assets/Shaders/MyShader.shader",
            examples=["Assets/Shaders/MyShader.shader", "Assets/Materials/Shaders/CustomShader.shader"]
        ),
        shader_name: Optional[str] = Field(
            None,
            title="Shadername",
            description="Shadername",
            examples=["Custom/MyShader", "Unlit/MyShader", "Standard/MyShader"]
        ),
        shader_type: Optional[str] = Field(
            None,
            title="Shadertype",
            description="Shadertype",
            examples=["Unlit", "Standard", "Custom", "UI", "Sprite", "Particle"]
        ),
        shader_code: Optional[str] = Field(
            None,
            title="Shadercode",
            description="Shadercode content"
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="property dict",
            description="property dict，used to setShaderproperty of",
            examples=[{"_MainTex": "white", "_Color": [1, 0, 0, 1], "_Metallic": 0.5}]
        ),
        destination: Optional[str] = Field(
            None,
            title="target path",
            description="target path（move/used when copying）",
            examples=["Assets/Shaders/MyShaderCopy.shader", "Assets/Backup/CustomShader.shader"]
        ),
        query: Optional[str] = Field(
            None,
            title="search mode",
            description="search mode，such as*.shader",
            examples=["*.shader", "Custom*", "Unlit*"]
        ),
        recursive: Optional[bool] = Field(
            True,
            title="recursive search",
            description="recursively search subfolders"
        ),
        force: Optional[bool] = Field(
            False,
            title="force execute",
            description="whether to force execution（overwrite existing files etc）"
        ),
        create_folder: Optional[bool] = Field(
            True,
            title="create folder",
            description="auto create missing folders"
        ),
        backup: Optional[bool] = Field(
            True,
            title="backup",
            description="backup original before modify"
        ),
        validate_syntax: Optional[bool] = Field(
            True,
            title="validate syntax",
            description="validate or notShadersyntax"
        ),
        compile_shader: Optional[bool] = Field(
            True,
            title="compileShader",
            description="compile or notShader"
        ),
        check_errors: Optional[bool] = Field(
            True,
            title="check errors",
            description="check compile errors"
        ),
        apply_immediately: Optional[bool] = Field(
            True,
            title="apply immediately",
            description="apply changes immediately"
        ),
        mark_dirty: Optional[bool] = Field(
            True,
            title="mark as dirty",
            description="mark asset as dirty"
        ),
        save_assets: Optional[bool] = Field(
            True,
            title="save asset",
            description="save asset to disk"
        ),
        refresh_assets: Optional[bool] = Field(
            True,
            title="refresh assets",
            description="refresh asset database"
        ),
        include_variants: Optional[bool] = Field(
            False,
            title="include variants",
            description="whether to includeShadervariant"
        ),
        platform_specific: Optional[bool] = Field(
            False,
            title="platform specific",
            description="generate platform specificShader"
        ),
        optimization_level: Optional[str] = Field(
            None,
            title="optimization level",
            description="optimization level",
            examples=["None", "Low", "Medium", "High"]
        ),
        debug_mode: Optional[bool] = Field(
            False,
            title="debug mode",
            description="enable debug mode"
        )
    ) -> Dict[str, Any]:
        """
        Shadermanagement tool（secondary tool）
        
        supported operations: create(createShader), modify(modifyShader), delete(deleteShader), get_info(getShaderinformation), search(searchShader), duplicate(copyShader), move(move/renameShader), rename(move/renameShader，andmovesame), compile(compileShader), validate(validateShader)
        """
        return get_common_call_response("edit_shader")