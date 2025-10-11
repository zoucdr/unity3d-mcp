"""
Unity编辑器管理工具，包含编辑器状态控制、标签层级管理、菜单管理等功能。（二级工具）

支持多种编辑器管理功能：
- 播放控制：play, pause, stop
- 状态查询：get_state, get_windows, get_active_tool, get_selection
- 标签管理：add_tag, remove_tag, get_tags
- 层级管理：add_layer, remove_layer, get_layers
- 菜单管理：execute_menu, get_menu_items
- 分辨率设置：set_resolution
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_manage_editor_tools(mcp: FastMCP):
    @mcp.tool("manage_editor")
    def manage_editor(
        ctx: Context,
        action: Annotated[str, Field(
            title="编辑器操作类型",
            description="要执行的编辑器操作: play(播放), pause(暂停), stop(停止), get_state(状态), get_windows(窗口), get_active_tool(工具), get_selection(选择), set_active_tool(设置工具), add_tag(添加标签), remove_tag(移除标签), get_tags(获取标签), add_layer(添加层), remove_layer(移除层), get_layers(获取层), execute_menu(执行菜单), get_menu_items(获取菜单列表), set_resolution(设置分辨率)",
            examples=["play", "pause", "stop", "get_state", "add_tag", "execute_menu", "get_menu_items"]
        )],
        tool_name: Annotated[Optional[str], Field(
            title="工具名称",
            description="工具名称，用于set_active_tool操作",
            default=None,
            examples=["View", "Move", "Rotate", "Scale", "Rect", "Transform"]
        )] = None,
        tag_name: Annotated[Optional[str], Field(
            title="标签名称",
            description="标签名称，用于add_tag和remove_tag操作",
            default=None,
            examples=["Player", "Enemy", "Item", "NPC"]
        )] = None,
        layer_name: Annotated[Optional[str], Field(
            title="层级名称",
            description="层级名称，用于add_layer和remove_layer操作",
            default=None,
            examples=["Ground", "Character", "UI", "Effects"]
        )] = None,
        menu_path: Annotated[Optional[str], Field(
            title="菜单路径",
            description="Unity菜单路径，用于execute_menu操作",
            default=None,
            examples=["GameObject/UI/Canvas", "GameObject/UI/Button", "GameObject/3D Object/Cube"]
        )] = None,
        root_path: Annotated[Optional[str], Field(
            title="根菜单路径",
            description="根菜单路径，用于get_menu_items操作获取指定路径下的所有菜单项",
            default="GameObject/UI",
            examples=["GameObject/UI", "GameObject/3D Object", "Component"]
        )] = "GameObject/UI",
        include_submenus: Annotated[Optional[bool], Field(
            title="包含子菜单",
            description="是否包含子菜单，用于get_menu_items操作",
            default=True
        )] = True,
        verify_exists: Annotated[Optional[bool], Field(
            title="验证菜单存在",
            description="是否验证每个菜单项是否真实存在，用于get_menu_items操作",
            default=False
        )] = False,
        width: Annotated[Optional[int], Field(
            title="窗口宽度",
            description="Game窗口宽度，用于set_resolution操作",
            default=None,
            examples=[1920, 1280, 1024]
        )] = None,
        height: Annotated[Optional[int], Field(
            title="窗口高度",
            description="Game窗口高度，用于set_resolution操作",
            default=None,
            examples=[1080, 720, 768]
        )] = None
    ) -> Dict[str, Any]:
        """Unity编辑器管理工具，用于控制编辑器的各种状态和操作。（二级工具）

        支持多种编辑器管理功能，适用于：
        - 播放控制：控制编辑器播放、暂停、停止
        - 状态查询：获取编辑器状态、窗口列表、当前选择等
        - 标签管理：添加、删除、获取项目标签
        - 层级管理：添加、删除、获取项目层级
        - 菜单管理：执行菜单命令、获取菜单列表（支持反射动态获取）
        - 分辨率设置：设置Game窗口分辨率
        
        示例用法：
        1. 获取GameObject/UI下的所有菜单：
           {"action": "get_menu_items", "root_path": "GameObject/UI", "verify_exists": true}
        
        2. 执行菜单创建Canvas：
           {"action": "execute_menu", "menu_path": "GameObject/UI/Canvas"}
        
        3. 添加自定义标签：
           {"action": "add_tag", "tag_name": "Boss"}
        
        4. 设置Game窗口分辨率：
           {"action": "set_resolution", "width": 1920, "height": 1080}
        """
        return get_common_call_response("manage_editor")
