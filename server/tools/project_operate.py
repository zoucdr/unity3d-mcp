"""
Unity项目操作工具，包含项目资源管理功能。

支持的功能：
- 资源导入：import（重新导入资源）
- 项目刷新：refresh（刷新项目）
- 资源修改：modify（修改资源属性）
- 资源复制：duplicate（复制资源）
- 资源移动：move/rename（移动或重命名资源）
- 资源信息：get_info（获取资源信息）
- 文件夹创建：create_folder（创建文件夹）
- 资源选择：select（选择资源）
- 资源定位：ping（在Project窗口中高亮显示）
- 依赖选择：select_depends（选择依赖项）
- 引用选择：select_usage（选择引用）
- 文件夹结构：tree（获取文件夹结构）
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_project_operate_tools(mcp: FastMCP):
    @mcp.tool("project_operate")
    def project_operate(
        ctx: Context,
        action: Annotated[str, Field(
            title="项目操作类型",
            description="要执行的项目操作: import(重新导入资源), refresh(刷新项目), modify(修改资源属性), duplicate(复制资源), move(移动资源), rename(重命名资源), get_info(获取资源信息), create_folder(创建文件夹), select(选择资源), ping(定位资源), select_depends(选择依赖项), select_usage(选择引用), tree(获取文件夹结构)",
            examples=["import", "refresh", "modify", "duplicate", "move", "rename", "get_info", "create_folder", "select", "ping", "select_depends", "select_usage", "tree"]
        )],
        path: Annotated[Optional[str], Field(
            title="资产路径",
            description="资产路径，Unity标准格式：Assets/Folder/File.extension，tree操作时为根目录路径（默认Assets）",
            default=None,
            examples=["Assets/NewFolder", "Assets/Scripts", "Assets/Prefabs"]
        )] = None,
        properties: Annotated[Optional[Dict[str, Any]], Field(
            title="资产属性",
            description="资产属性字典，用于设置资产的各种属性",
            default=None,
            examples=[{"playerSpeed": 5.0, "maxHealth": 100}]
        )] = None,
        destination: Annotated[Optional[str], Field(
            title="目标路径",
            description="目标路径（移动/复制时使用）",
            default=None,
            examples=["Assets/Scripts/NewName.cs", "Assets/Materials/RedMaterialCopy.mat"]
        )] = None,
        target_path: Annotated[Optional[str], Field(
            title="目标路径",
            description="目标路径（移动/复制时使用），与destination相同",
            default=None,
            examples=["Assets/NewFolder", "Assets/Models/model.fbx"]
        )] = None,
        source_path: Annotated[Optional[str], Field(
            title="源路径",
            description="源文件路径，仅在import_asset操作时使用",
            default=None,
            examples=["D:/Models/character.fbx", "C:/Textures/grass.png"]
        )] = None,
        package_name: Annotated[Optional[str], Field(
            title="包名称",
            description="导出包的名称，仅在export_package操作时使用",
            default=None,
            examples=["MyAssets.unitypackage", "ScriptsPackage"]
        )] = None,
        include_dependencies: Annotated[Optional[bool], Field(
            title="包含依赖",
            description="是否包含资源的依赖项，适用于export_package和select_depends操作",
            default=True
        )] = True,
        force: Annotated[Optional[bool], Field(
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）",
            default=False
        )] = False,
        refresh_type: Annotated[Optional[str], Field(
            title="刷新类型",
            description="刷新类型：all(全部), assets(仅资产), scripts(仅脚本)，默认all",
            default="all",
            examples=["all", "assets", "scripts"]
        )] = "all",
        save_before_refresh: Annotated[Optional[bool], Field(
            title="刷新前保存",
            description="刷新前是否保存所有资产，默认true",
            default=True
        )] = True,
        include_indirect: Annotated[Optional[bool], Field(
            title="包含间接依赖",
            description="是否包含间接依赖/引用，默认false",
            default=False
        )] = False,
        max_results: Annotated[Optional[int], Field(
            title="最大结果数量",
            description="最大结果数量，默认100",
            default=100,
            examples=[50, 100, 200]
        )] = 100,
        generate_preview: Annotated[Optional[bool], Field(
            title="生成预览",
            description="是否生成资源预览图，默认false",
            default=False
        )] = False
    ) -> Dict[str, Any]:
        """Unity项目操作工具，用于执行各种项目管理和资产操作。

        支持多种项目操作，适用于：
        - 资源管理：导入、修改、复制、移动资产
        - 项目组织：创建文件夹结构，删除不需要的资源
        - 包管理：导出资源包用于分享或备份
        - 项目维护：刷新项目状态，清理无效引用
        - 文件夹结构：查看Assets目录下的文件夹层次结构和文件数量（YAML格式）
        - 资产查询：获取资产信息，查找依赖和引用
        - 编辑器交互：选择和定位资产

        详细功能：
        - import: 重新导入资源
        - refresh: 刷新项目
        - modify: 修改资产属性
        - duplicate: 复制资产
        - move/rename: 移动或重命名资产
        - get_info: 获取资产信息
        - create_folder: 创建文件夹
        - select: 选择资产
        - ping: 在Project窗口中高亮显示资产
        - select_depends: 选择资产的依赖项
        - select_usage: 选择引用该资产的资源
        - tree: 获取文件夹结构
        """
        
        return send_to_unity("project_operate",{
            "action": action,
            "path": path,
            "properties": properties,
            "destination": destination,
            "target_path": target_path,
            "source_path": source_path,
            "package_name": package_name,
            "include_dependencies": include_dependencies,
            "force": force,
            "refresh_type": refresh_type,
            "save_before_refresh": save_before_refresh,
            "include_indirect": include_indirect,
            "max_results": max_results,
            "generate_preview": generate_preview
        })
