"""
terrain editing tool
processUnityin sceneTerrainterrain editing operations，including create、modify、heightmap import export etc
"""

from typing import Optional, List, Dict, Any
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_terrain_tools(mcp: FastMCP):
    @mcp.tool("edit_terrain")
    def edit_terrain(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="terrain operation to perform",
            examples=["create", "modify", "set_height", "add_layer", "flatten", "smooth", "export_heightmap", "get_info"]
        ),
        path: Optional[str] = Field(
            None,
            title="object path",
            description="in sceneTerrainobject hierarchy path",
            examples=["Terrain", "Environment/Terrain", "TerrainGroup/Terrain_0"]
        ),
        instance_id: Optional[int] = Field(
            None,
            title="instanceID",
            description="Terrain GameObjectinstance ofID"
        ),
        terrain_data_path: Optional[str] = Field(
            None,
            title="TerrainDatapath",
            description="TerrainDataasset path，used to createTerrainwhen",
            examples=["Assets/TerrainData/MyTerrain.asset"]
        ),
        position: Optional[List[float]] = Field(
            None,
            title="position coordinates",
            description="Terrainposition [x, y, z]"
        ),
        terrain_size: Optional[List[float]] = Field(
            None,
            title="terrain size",
            description="Terrainsize [width, height, length]"
        ),
        heightmap_resolution: Optional[int] = Field(
            None,
            title="heightmap resolution",
            description="heightmap resolution，must be2power of+1",
            examples=[513, 1025, 2049]
        ),
        heightmap_data: Optional[List[List[float]]] = Field(
            None,
            title="heightmap data",
            description="heightmap data 2D array，value range0-1"
        ),
        heightmap_file: Optional[str] = Field(
            None,
            title="heightmap file",
            description="heightmap file path（import/for export）"
        ),
        texture_layer: Optional[Dict[str, Any]] = Field(
            None,
            title="texture layer config",
            description="texture layer config dict"
        ),
        layer_index: Optional[int] = Field(
            None,
            title="texture layer index",
            description="texture layer index to operate"
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="terrain properties",
            description="to modifyTerrainproperty dict"
        ),
        height: Optional[float] = Field(
            None,
            title="height value",
            description="target height for flatten（0-1）"
        ),
        smooth_factor: Optional[float] = Field(
            None,
            title="smoothing factor",
            description="smoothing strength for terrain（0-1）"
        ),
        export_format: Optional[str] = Field(
            None,
            title="export format",
            description="heightmap export format",
            examples=["raw", "png"]
        ),
        force: Optional[bool] = Field(
            None,
            title="force execute",
            description="whether to force execution（overwrite existing files etc）"
        )
    ) -> Dict[str, Any]:
        """
        terrain editing tool（secondary tool）
        
        supported operations:
        - create: create newTerrain
        - modify: modifyTerrainproperty
        - set_height: set terrain height
        - paint_texture: paint texture
        - add_layer: add texture layer
        - remove_layer: remove texture layer
        - set_size: set terrain size
        - flatten: flatten terrain
        - smooth: smooth terrain
        - export_heightmap: export heightmap
        - import_heightmap: import heightmap
        - get_info: get terrain info
        - clear_trees: clear trees
        - clear_details: clear details（grass etc）
        """
        return get_common_call_response("edit_terrain")

