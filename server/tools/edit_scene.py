"""
场景管理工具
管理Unity场景，包括加载、保存、创建和获取层级结构

支持的功能：
- 场景加载：通过名称、路径或构建索引加载场景
- 场景保存：保存当前活动场景或指定场景
- 场景创建：创建新的空场景
- 层级获取：获取场景中的GameObject层级结构
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_scene_tools(mcp: FastMCP):
    @mcp.tool("edit_scene")
    def edit_scene(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型：load(加载), save(保存), create(创建), get_hierarchy(获取层级), get_active(获取活动场景)",
            examples=["load", "save", "create", "get_hierarchy", "get_active"]
        ),
        name: Optional[str] = Field(
            None,
            title="场景名称",
            description="场景名称",
            examples=["MainMenu", "Level1", "TestScene"]
        ),
        path: Optional[str] = Field(
            None,
            title="资产路径",
            description="场景资产路径",
            examples=["Assets/Scenes/MainMenu.unity", "Assets/Scenes/", "Assets/Scenes/Level1.unity"]
        ),
        build_index: Optional[int] = Field(
            None,
            title="构建索引",
            description="场景在构建设置中的索引",
            examples=[0, 1, 2]
        )
    ) -> Dict[str, Any]:
        """
        场景管理工具，用于管理Unity场景的加载、保存、创建和查询。
        
        支持的操作:
        - load: 加载场景（通过名称、路径或构建索引）
        - save: 保存场景（当前活动场景或指定场景）
        - create: 创建新的空场景
        - get_hierarchy: 获取场景中的GameObject层级结构
        - get_active: 获取当前活动场景信息
        
        示例用法：
        1. 加载场景:
           {"action": "load", "path": "Assets/Scenes/MainMenu.unity"}
           
        2. 通过构建索引加载场景:
           {"action": "load", "build_index": 0}
           
        3. 保存当前场景:
           {"action": "save"}
           
        4. 创建新场景:
           {"action": "create", "name": "NewScene", "path": "Assets/Scenes/NewScene.unity"}
           
        5. 获取场景层级结构:
           {"action": "get_hierarchy"}
        """
        return send_to_unity("edit_scene", {
            "action": action,
            "name": name,
            "path": path,
            "build_index": build_index
        })
