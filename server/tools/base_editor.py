"""
Unity编辑器基础管理工具，用于编辑器状态查询和菜单管理。（二级工具）

支持的功能：
- 状态查询：get_state, get_windows, get_selection
- 菜单管理：execute_menu, get_menu_items

注意：标签和层级管理功能已移至 tag_layer 工具
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_base_editor_tools(mcp: FastMCP):
    @mcp.tool("base_editor")
    def base_editor(
        ctx: Context,
        action: Annotated[str, Field(
            title="编辑器操作类型",
            description="要执行的编辑器操作: get_state(获取状态), get_windows(获取窗口列表), get_selection(获取选择), execute_menu(执行菜单), get_menu_items(获取菜单列表)",
            examples=["get_state", "get_windows", "get_selection", "execute_menu", "get_menu_items"]
        )],
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
        )] = False
    ) -> Dict[str, Any]:
        """Unity编辑器基础管理工具，用于编辑器状态查询和菜单管理。（二级工具）

        支持的功能，适用于：
        - 状态查询：获取编辑器状态、窗口列表、当前选择等
        - 菜单管理：执行菜单命令、获取菜单列表（支持反射动态获取）
        
        注意：标签和层级管理功能已移至 tag_layer 工具
        
        示例用法：
        1. 获取编辑器状态：
           {"action": "get_state"}
        
        2. 获取GameObject/UI下的所有菜单：
           {"action": "get_menu_items", "root_path": "GameObject/UI", "verify_exists": true}
        
        3. 执行菜单创建Canvas：
           {"action": "execute_menu", "menu_path": "GameObject/UI/Canvas"}
        
        4. 获取当前选择的对象：
           {"action": "get_selection"}
        """
        return get_common_call_response("base_editor")
