"""
Unity资产管理工具
提供Unity资产的各种操作，包括导入、修改、移动、复制等
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_asset_tools(mcp: FastMCP):
    @mcp.tool("edit_asset")
    def edit_asset(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型，如 import/modify/move/duplicate 等",
            examples=["import", "modify", "move", "duplicate", "search", "get_info", "create_folder"]
        ),
        path: str = Field(
            ...,
            title="资产路径",
            description="资产路径，Unity标准格式：Assets/Folder/File.extension",
            examples=["Assets/Textures/icon.png", "Assets/Scripts/PlayerController.cs", "Assets/Materials/RedMaterial.mat"]
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="资产属性",
            description="资产属性字典，用于设置资产的各种属性",
            examples=[{"playerSpeed": 5.0, "maxHealth": 100}]
        ),
        destination: Optional[str] = Field(
            None,
            title="目标路径",
            description="目标路径（移动/复制时使用）",
            examples=["Assets/Scripts/NewName.cs", "Assets/Materials/RedMaterialCopy.mat"]
        ),
        query: Optional[str] = Field(
            None,
            title="搜索模式",
            description="搜索模式，如*.prefab",
            examples=["*.prefab", "Player*", "*.mat"]
        ),
        force: Optional[bool] = Field(
            False,
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）"
        )
    ) -> Dict[str, Any]:
        """
        Unity资产管理工具
        
        支持的操作:
        - import: 重新导入资产
        - modify: 修改资产属性
        - duplicate: 复制资产
        - move: 移动/重命名资产
        - rename: 移动/重命名资产（与move相同）
        - search: 搜索资产
        - get_info: 获取资产信息
        - create_folder: 创建文件夹
        """
        return send_to_unity("edit_asset", {
            "action": action,
            "path": path,
            "properties": properties,
            "destination": destination,
            "query": query,
            "force": force
        })
