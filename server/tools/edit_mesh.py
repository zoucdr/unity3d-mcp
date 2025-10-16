"""
Unity网格编辑工具，包含3D网格的导入、导出、优化和处理功能。

支持的功能：
- 网格导入：从外部文件导入3D模型到项目中
- 网格导出：将网格导出为多种3D文件格式
- 网格优化：减少面数和提升性能
- 网格生成：创建基本几何体和自定义网格
- 网格处理：细分、平滑、法线计算、UV生成等
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_mesh_tools(mcp: FastMCP):
    @mcp.tool("edit_mesh")
    def edit_mesh(
        ctx: Context,
        action: Annotated[str, Field(
            title="网格操作类型",
            description="要执行的网格操作: create(创建), modify(修改), optimize(优化), generate_primitive(生成基本体), subdivide(细分), smooth(平滑), export(导出), import(导入), generate_uv(生成UV), calculate_normals(计算法线)",
            examples=["create", "import", "export", "optimize", "generate_primitive", "subdivide", "smooth", "generate_uv", "calculate_normals"]
        )],
        mesh_path: Annotated[str, Field(
            title="网格文件路径",
            description="网格文件的路径，可以是Assets内路径或外部文件路径",
            examples=["Assets/Models/character.fbx", "D:/Models/building.obj", "Models/weapon.dae"]
        )],
        target_path: Annotated[Optional[str], Field(
            title="目标路径",
            description="导入或导出的目标路径",
            default=None,
            examples=["Assets/Models/imported_model.fbx", "D:/Exports/optimized_mesh.obj"]
        )] = None,
        import_settings: Annotated[Optional[Dict[str, Any]], Field(
            title="导入设置",
            description="网格导入时的设置参数",
            default=None,
            examples=[
                {"scale_factor": 1.0, "generate_colliders": True},
                {"import_materials": True, "optimize_mesh": True}
            ]
        )] = None,
        optimization_level: Annotated[Optional[str], Field(
            title="优化级别",
            description="网格优化的级别：low(低), medium(中), high(高)",
            default="medium",
            examples=["low", "medium", "high"]
        )] = "medium",
        mesh_type: Annotated[Optional[str], Field(
            title="网格类型",
            description="网格类型：cube(立方体), sphere(球体), cylinder(圆柱体), plane(平面), custom(自定义)",
            default=None,
            examples=["cube", "sphere", "cylinder", "plane", "custom"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="网格属性",
            description="网格属性字典，包含顶点、面、UV等数据",
            default=None,
            examples=[{"vertices": [[0,0,0], [1,0,0], [0,1,0]], "triangles": [0, 1, 2]}]
        )] = None,
        source_path: Annotated[Optional[str], Field(
            title="源网格路径",
            description="源网格路径（修改时使用）",
            default=None,
            examples=["Assets/Models/source_model.fbx"]
        )] = None,
        subdivision_level: Annotated[Optional[int], Field(
            title="细分级别",
            description="细分级别（细分时使用）",
            default=None,
            examples=[1, 2, 3]
        )] = None,
        smooth_factor: Annotated[Optional[float], Field(
            title="平滑因子",
            description="平滑因子（平滑时使用）",
            default=None,
            examples=[0.5, 1.0, 2.0]
        )] = None,
        export_format: Annotated[Optional[str], Field(
            title="导出格式",
            description="导出格式：obj, fbx, stl等",
            default=None,
            examples=["obj", "fbx", "stl"]
        )] = None,
        force: Annotated[Optional[bool], Field(
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）",
            default=False
        )] = False
    ) -> Dict[str, Any]:
        """Unity网格编辑工具，用于创建、导入、导出、优化和处理3D网格资源。

        支持多种网格处理功能，适用于：
        - 网格创建：创建基本几何体或自定义网格
        - 模型导入：从外部文件导入3D模型到项目中
        - 网格导出：将网格导出为多种3D文件格式
        - 网格优化：减少面数和提升性能
        - 网格处理：细分、平滑、UV生成、法线计算等
        
        示例用法：
        1. 导入3D模型:
           {"action": "import", "mesh_path": "D:/Models/character.fbx", "target_path": "Assets/Models/character.fbx"}
           
        2. 优化网格:
           {"action": "optimize", "mesh_path": "Assets/Models/character.fbx", "optimization_level": "medium"}
           
        3. 生成基本几何体:
           {"action": "generate_primitive", "mesh_path": "Assets/Models/cube.asset", "mesh_type": "cube"}
           
        4. 细分网格:
           {"action": "subdivide", "mesh_path": "Assets/Models/model.fbx", "subdivision_level": 2}
           
        5. 导出网格:
           {"action": "export", "mesh_path": "Assets/Models/model.fbx", "target_path": "D:/Exports/model.obj", "export_format": "obj"}
        """
        return send_to_unity("edit_mesh", {
            "action": action,
            "mesh_path": mesh_path,
            "target_path": target_path,
            "import_settings": import_settings,
            "optimization_level": optimization_level,
            "mesh_type": mesh_type,
            "properties": properties,
            "source_path": source_path,
            "subdivision_level": subdivision_level,
            "smooth_factor": smooth_factor,
            "export_format": export_format,
            "force": force
        })
