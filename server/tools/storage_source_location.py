"""
Storage - SourceLocation工具
用于资源和文件夹定位，支持在系统文件浏览器中打开和定位文件
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_storage_source_location_tools(mcp: FastMCP):
    @mcp.tool("source_location")
    def source_location(
        ctx: Context,
        action: Annotated[str, Field(
            title="资源定位操作类型",
            description="要执行的资源定位操作: reveal_in_finder(在文件浏览器显示), open_folder(打开文件夹), ping_asset(高亮显示), select_asset(选择资源), get_asset_path(获取路径)",
            examples=["reveal_in_finder", "open_folder", "ping_asset", "select_asset", "get_asset_path"]
        )],
        asset_path: Annotated[Optional[str], Field(
            title="资源路径",
            description="Unity资源路径（如 Assets/Scripts/MyScript.cs）",
            default=None,
            examples=["Assets/Scripts/PlayerController.cs", "Assets/Prefabs/Player.prefab", "Assets/Textures/icon.png"]
        )] = None,
        folder_path: Annotated[Optional[str], Field(
            title="文件夹路径",
            description="文件夹路径（Unity路径或系统路径），用于open_folder操作",
            default=None,
            examples=["Assets/Scripts", "Assets/Prefabs", "D:/Projects/MyGame/Assets"]
        )] = None,
        instance_id: Annotated[Optional[str], Field(
            title="实例ID",
            description="GameObject或资源的实例ID",
            default=None,
            examples=["12345", "67890"]
        )] = None,
        guid: Annotated[Optional[str], Field(
            title="资源GUID",
            description="资源的GUID（全局唯一标识符）",
            default=None,
            examples=["a1b2c3d4e5f6g7h8", "12345678901234567890123456789012"]
        )] = None,
        object_name: Annotated[Optional[str], Field(
            title="对象名称",
            description="GameObject或资源的名称",
            default=None,
            examples=["MainCamera", "Player", "GameManager", "Canvas"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity资源定位工具，用于在文件系统中定位和操作资源。（二级工具）

        支持多种资源定位操作，适用于：
        - 文件浏览：在系统文件浏览器中打开和显示文件（Windows资源管理器/Mac Finder）
        - 资源定位：在Unity Project窗口中快速定位资源
        - 对象选择：选择场景中的GameObject或项目资源
        - 路径查询：获取资源的详细路径信息（包括GUID、绝对路径等）

        示例用法：
        1. 在文件浏览器中显示资源:
           {"action": "reveal_in_finder", "asset_path": "Assets/Scripts/PlayerController.cs"}

        2. 打开Assets文件夹:
           {"action": "open_folder", "folder_path": "Assets/Scripts"}

        3. 在Project窗口中高亮资源:
           {"action": "ping_asset", "asset_path": "Assets/Prefabs/Player.prefab"}

        4. 选择场景中的GameObject:
           {"action": "select_asset", "object_name": "MainCamera"}

        5. 获取资源路径信息:
           {"action": "get_asset_path", "instance_id": "12345"}

        6. 通过GUID定位资源:
           {"action": "reveal_in_finder", "guid": "a1b2c3d4e5f6g7h8"}

        注意事项：
        - reveal_in_finder: 会在系统默认文件浏览器中打开并选中文件
        - open_folder: 只打开文件夹，不选中特定文件
        - ping_asset: 在Unity中产生高亮动画效果
        - 支持通过多种方式定位资源：路径、实例ID、GUID、名称
        """
        
        return get_common_call_response("source_location")

