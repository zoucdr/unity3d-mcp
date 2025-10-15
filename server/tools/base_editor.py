"""
Unitybasic editor management utilities，for editor state query and menu management。（secondary tool）

supported features：
- status query：get_state, get_windows, get_selection
- menu management：execute_menu, get_menu_items

notes：tag and layer management has moved to tag_layer tool
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
            title="editor action type",
            description="editor action to perform: get_state(get state), get_windows(get window list), get_selection(get selection), execute_menu(execute menu), get_menu_items(get menu list)",
            examples=["get_state", "get_windows", "get_selection", "execute_menu", "get_menu_items"]
        )],
        menu_path: Annotated[Optional[str], Field(
            title="menu path",
            description="Unitymenu path，forexecute_menuoperation",
            default=None,
            examples=["GameObject/UI/Canvas", "GameObject/UI/Button", "GameObject/3D Object/Cube"]
        )] = None,
        root_path: Annotated[Optional[str], Field(
            title="root menu path",
            description="root menu path，forget_menu_itemsfetch all menu items under the given path",
            default="GameObject/UI",
            examples=["GameObject/UI", "GameObject/3D Object", "Component"]
        )] = "GameObject/UI",
        include_submenus: Annotated[Optional[bool], Field(
            title="include submenus",
            description="whether to include submenus，forget_menu_itemsoperation",
            default=True
        )] = True,
        verify_exists: Annotated[Optional[bool], Field(
            title="verify menu exists",
            description="whether to verify each menu item exists，forget_menu_itemsoperation",
            default=False
        )] = False
    ) -> Dict[str, Any]:
        """Unitybasic editor management utilities，for editor state query and menu management。（secondary tool）

        supported features，suitable for：
        - status query：get editor state、window list、current selection etc
        - menu management：execute menu command、get menu list（supports reflection based retrieval）
        
        notes：tag and layer management has moved to tag_layer tool
        
        example usage：
        1. get editor state：
           {"action": "get_state"}
        
        2. fetchGameObject/UIall menus under：
           {"action": "get_menu_items", "root_path": "GameObject/UI", "verify_exists": true}
        
        3. execute menu creationCanvas：
           {"action": "execute_menu", "menu_path": "GameObject/UI/Canvas"}
        
        4. get current selection：
           {"action": "get_selection"}
        """
        return get_common_call_response("base_editor")
