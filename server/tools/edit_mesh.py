"""
Unitymesh editing tool，include3Dmesh import、export、optimization and processing features。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_mesh_tools(mcp: FastMCP):
    @mcp.tool("edit_mesh")
    def edit_mesh(
        ctx: Context,
        action: Annotated[str, Field(
            title="mesh operation type",
            description="mesh operation to perform: import(import), export(export), optimize(optimize), generate_uv(generateUV), calculate_normals(compute normals)",
            examples=["import", "export", "optimize", "generate_uv", "calculate_normals"]
        )],
        mesh_path: Annotated[str, Field(
            title="mesh file path",
            description="mesh file path，can beAssetsinternal or external file path",
            examples=["Assets/Models/character.fbx", "D:/Models/building.obj", "Models/weapon.dae"]
        )],
        target_path: Annotated[Optional[str], Field(
            title="target path",
            description="target path for import or export",
            default=None,
            examples=["Assets/Models/imported_model.fbx", "D:/Exports/optimized_mesh.obj"]
        )] = None,
        import_settings: Annotated[Optional[Dict[str, Any]], Field(
            title="import settings",
            description="settings for mesh import",
            default=None,
            examples=[
                {"scale_factor": 1.0, "generate_colliders": True},
                {"import_materials": True, "optimize_mesh": True}
            ]
        )] = None,
        optimization_level: Annotated[Optional[str], Field(
            title="optimization level",
            description="level of mesh optimization：low(low), medium(medium), high(high)",
            default="medium",
            examples=["low", "medium", "high"]
        )] = "medium"
    ) -> Dict[str, Any]:
        """Unitymesh editing tool，used to import、export、optimization and processing3Dmesh asset。

        supports various mesh operations，suitable for：
        - model import：import from external file3Dmodel into project
        - mesh optimization：reduce faces and improve performance
        - UVgenerate：auto generate for modelUVcoordinates
        - normal calculation：recalculate vertex normals
        """
        return get_common_call_response("edit_mesh")
