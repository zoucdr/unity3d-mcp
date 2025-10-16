"""
地形编辑工具
处理Unity场景中Terrain地形的编辑操作，包括创建、修改、高度图导入导出等

支持的功能：
- 地形创建：创建新的地形，设置尺寸和分辨率
- 高度编辑：设置地形高度，平坦化，平滑处理
- 纹理绘制：添加、移除和绘制地形纹理层
- 高度图处理：导入导出高度图数据
- 地形清理：清除树木和细节对象
"""

from typing import Optional, List, Dict, Any
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_terrain_tools(mcp: FastMCP):
    @mcp.tool("edit_terrain")
    def edit_terrain(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="要执行的地形操作：create(创建), modify(修改), set_height(设置高度), paint_texture(绘制纹理), add_layer(添加纹理层), remove_layer(移除纹理层), set_size(设置尺寸), flatten(平坦化), smooth(平滑), export_heightmap(导出高度图), import_heightmap(导入高度图), get_info(获取信息), clear_trees(清除树木), clear_details(清除细节)",
            examples=["create", "modify", "set_height", "paint_texture", "add_layer", "remove_layer", "set_size", "flatten", "smooth", "export_heightmap", "import_heightmap", "get_info", "clear_trees", "clear_details"]
        ),
        path: Optional[str] = Field(
            None,
            title="对象路径",
            description="场景中Terrain对象的层级路径",
            examples=["Terrain", "Environment/Terrain", "TerrainGroup/Terrain_0"]
        ),
        instance_id: Optional[int] = Field(
            None,
            title="实例ID",
            description="Terrain GameObject的实例ID"
        ),
        terrain_data_path: Optional[str] = Field(
            None,
            title="TerrainData路径",
            description="TerrainData资源路径，用于创建Terrain时",
            examples=["Assets/TerrainData/MyTerrain.asset"]
        ),
        position: Optional[List[float]] = Field(
            None,
            title="位置坐标",
            description="Terrain位置 [x, y, z]"
        ),
        terrain_size: Optional[List[float]] = Field(
            None,
            title="地形尺寸",
            description="Terrain尺寸 [width, height, length]"
        ),
        heightmap_resolution: Optional[int] = Field(
            None,
            title="高度图分辨率",
            description="高度图分辨率，必须是2的幂+1",
            examples=[513, 1025, 2049]
        ),
        heightmap_data: Optional[List[List[float]]] = Field(
            None,
            title="高度图数据",
            description="高度图数据二维数组，值范围0-1"
        ),
        heightmap_file: Optional[str] = Field(
            None,
            title="高度图文件",
            description="高度图文件路径（导入/导出用）"
        ),
        texture_layer: Optional[Dict[str, Any]] = Field(
            None,
            title="纹理层配置",
            description="纹理层配置字典"
        ),
        layer_index: Optional[int] = Field(
            None,
            title="纹理层索引",
            description="要操作的纹理层索引"
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="地形属性",
            description="要修改的Terrain属性字典"
        ),
        height: Optional[float] = Field(
            None,
            title="高度值",
            description="平坦化时的目标高度（0-1）"
        ),
        smooth_factor: Optional[float] = Field(
            None,
            title="平滑因子",
            description="平滑地形时的平滑强度（0-1）"
        ),
        export_format: Optional[str] = Field(
            None,
            title="导出格式",
            description="高度图导出格式",
            examples=["raw", "png"]
        ),
        force: Optional[bool] = Field(
            None,
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）"
        )
    ) -> Dict[str, Any]:
        """
        地形编辑工具，用于处理Unity场景中的地形对象。
        
        支持的操作:
        - create: 创建新的Terrain
        - modify: 修改Terrain属性
        - set_height: 设置地形高度
        - paint_texture: 绘制纹理
        - add_layer: 添加纹理层
        - remove_layer: 移除纹理层
        - set_size: 设置地形尺寸
        - flatten: 平坦化地形
        - smooth: 平滑地形
        - export_heightmap: 导出高度图
        - import_heightmap: 导入高度图
        - get_info: 获取地形信息
        - clear_trees: 清除树木
        - clear_details: 清除细节（草等）
        
        示例用法：
        1. 创建新地形:
           {"action": "create", "path": "Terrain", "position": [0, 0, 0], "terrain_size": [1000, 600, 1000], "heightmap_resolution": 513}
           
        2. 设置地形高度:
           {"action": "set_height", "path": "Terrain", "heightmap_data": [[0.1, 0.2], [0.3, 0.4]]}
           
        3. 平坦化地形:
           {"action": "flatten", "path": "Terrain", "height": 0.5}
           
        4. 添加纹理层:
           {"action": "add_layer", "path": "Terrain", "texture_layer": {"diffuse": "Assets/Textures/grass.jpg", "normal": "Assets/Textures/grass_normal.jpg"}}
           
        5. 导出高度图:
           {"action": "export_heightmap", "path": "Terrain", "heightmap_file": "Assets/HeightMaps/terrain_height.raw", "export_format": "raw"}
        """
        return send_to_unity("edit_terrain", {
            "action": action,
            "path": path,
            "instance_id": instance_id,
            "terrain_data_path": terrain_data_path,
            "position": position,
            "terrain_size": terrain_size,
            "heightmap_resolution": heightmap_resolution,
            "heightmap_data": heightmap_data,
            "heightmap_file": heightmap_file,
            "texture_layer": texture_layer,
            "layer_index": layer_index,
            "properties": properties,
            "height": height,
            "smooth_factor": smooth_factor,
            "export_format": export_format,
            "force": force
        })

