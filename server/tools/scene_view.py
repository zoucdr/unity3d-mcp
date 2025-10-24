"""
Unity Scene窗口管理工具，用于控制Scene视图的显示和操作。

支持的功能：
- 窗口信息：get_info
- 窗口控制：focus, maximize
- 截图功能：screenshot
- 视图设置：set_pivot, set_rotation, set_2d_mode
- 视图对齐：align_with_view
- 对象定位：frame_selected
"""
from typing import Annotated, Dict, Any, Optional, List, Union
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity
from mcp.server.fastmcp import FastMCP

def register_scene_view_tools(mcp: FastMCP):
    @mcp.tool("scene_view")
    def scene_view(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="要执行的Scene窗口操作: get_info(获取信息), focus(聚焦窗口), maximize(最大化窗口), screenshot(截图), set_pivot(设置pivot点), set_rotation(设置旋转), set_2d_mode(设置2D模式), align_with_view(对齐视图), frame_selected(帧定位选中对象)",
            examples=["get_info", "focus", "maximize", "screenshot", "set_pivot", "set_rotation", "set_2d_mode", "align_with_view", "frame_selected"]
        )],
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="截图保存的文件路径，仅在action为screenshot时有效",
            default=None,
        )] = None,
        pivot_position: Annotated[Optional[List[float]], Field(
            title="Pivot位置",
            description="Pivot位置坐标 [x, y, z]，仅在action为set_pivot时有效",
            default=None,
            examples=[[0, 0, 0], [10, 5, -3]]
        )] = None,
        rotation: Annotated[Optional[List[float]], Field(
            title="旋转角度",
            description="旋转角度 [x, y, z]，仅在action为set_rotation时有效",
            default=None,
            examples=[[0, 0, 0], [30, 45, 0]]
        )] = None,
        orthographic: Annotated[Optional[bool], Field(
            title="正交模式",
            description="是否使用正交模式（2D视图），仅在action为set_2d_mode时有效",
            default=None,
            examples=[True, False]
        )] = None,
        align_view: Annotated[Optional[str], Field(
            title="视图对齐方向",
            description="视图对齐方向: top, bottom, left, right, front, back，仅在action为align_with_view时有效",
            default=None,
            examples=["top", "front", "right"]
        )] = None
    ) :
        """Unity Scene窗口管理工具，用于控制Scene视图的显示和操作。

        支持多种Scene窗口管理功能，适用于：
        - 窗口信息：获取Scene视图的详细信息
        - 窗口控制：聚焦和最大化Scene窗口
        - 截图功能：获取Scene视图的截图
        - 视图设置：设置pivot点、旋转角度和2D/3D模式
        - 视图对齐：将视图对齐到特定方向
        - 对象定位：将视图聚焦到选中的对象
        
        示例用法：
        1. 获取Scene窗口信息：
           {"action": "get_info"}
        
        2. 聚焦Scene窗口：
           {"action": "focus"}
        
        3. 最大化Scene窗口：
           {"action": "maximize"}
        
        4. 获取Scene视图截图：
           {"action": "screenshot", "save_path": "Assets/Screenshots/scene_view.jpg"}
        
        5. 设置Scene视图pivot点：
           {"action": "set_pivot", "pivot_position": [0, 5, 0]}
        
        6. 设置Scene视图旋转：
           {"action": "set_rotation", "rotation": [30, 45, 0]}
        
        7. 设置Scene视图2D模式：
           {"action": "set_2d_mode", "orthographic": true}
        
        8. 将Scene视图对齐到顶视图：
           {"action": "align_with_view", "align_view": "top"}
        
        9. 将Scene视图聚焦到选中对象：
           {"action": "frame_selected"}
        """
        # Image(path=file_path, format="png")
        return send_to_unity("scene_view", {
            "action": action,
            "save_path": save_path,
            "pivot_position": pivot_position,
            "rotation": rotation,
            "orthographic": orthographic,
            "align_view": align_view
        })
