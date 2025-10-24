"""
Unity Game窗口管理工具，用于控制Game视图的显示和性能设置。

支持的功能：
- 分辨率管理：set_resolution, get_resolution
- 性能设置：set_vsync, set_target_framerate
- 窗口控制：maximize
- 统计信息：get_stats
- 宽高比设置：set_aspect_ratio
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_game_view_tools(mcp: FastMCP):
    @mcp.tool("game_view")
    def game_view(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="要执行的Game窗口操作: set_resolution(设置分辨率), get_resolution(获取分辨率), get_stats(获取统计信息), set_vsync(设置垂直同步), set_target_framerate(设置目标帧率), maximize(最大化窗口), set_aspect_ratio(设置宽高比), screenshot(截图)",
            examples=["set_resolution", "get_resolution", "get_stats", "set_vsync", "set_target_framerate", "maximize", "set_aspect_ratio", "screenshot"]
        )],
        width: Annotated[Optional[int], Field(
            title="窗口宽度",
            description="Game窗口宽度（像素），用于set_resolution操作",
            default=None,
            examples=[1920, 1280, 1024, 800]
        )] = None,
        height: Annotated[Optional[int], Field(
            title="窗口高度",
            description="Game窗口高度（像素），用于set_resolution操作",
            default=None,
            examples=[1080, 720, 768, 600]
        )] = None,
        vsync_count: Annotated[Optional[int], Field(
            title="垂直同步计数",
            description="VSync计数：0=关闭, 1=每帧同步, 2=每2帧同步, 用于set_vsync操作",
            default=None,
            examples=[0, 1, 2]
        )] = None,
        target_framerate: Annotated[Optional[int], Field(
            title="目标帧率",
            description="目标帧率（FPS），-1表示无限制，用于set_target_framerate操作",
            default=None,
            examples=[60, 30, 120, -1]
        )] = None,
        aspect_ratio: Annotated[Optional[str], Field(
            title="宽高比",
            description="宽高比设置，如'16:9', '4:3', 'Free'等，用于set_aspect_ratio操作",
            default=None,
            examples=["16:9", "16:10", "4:3", "Free", "21:9"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="截图保存的文件路径，仅在action为screenshot时有效",
            default=None,
        )] = None
    ) -> Dict[str, Any]:
        """Unity Game窗口管理工具，用于控制Game视图的显示和性能设置。

        支持多种Game窗口管理功能，适用于：
        - 分辨率管理：设置和查询Game窗口分辨率
        - 性能优化：配置VSync和目标帧率
        - 窗口控制：最大化Game窗口
        - 统计监控：获取当前性能和显示统计信息
        - 显示设置：配置宽高比
        
        注意事项：
        - 分辨率设置会影响Game窗口的显示大小
        - VSync设置会影响性能和画面流畅度
        - 目标帧率-1表示不限制帧率
        - 宽高比设置是简化实现，完整功能需要复杂的Unity内部API访问
        
        示例用法：
        1. 设置Game窗口分辨率为1920x1080：
           {"action": "set_resolution", "width": 1920, "height": 1080}
        
        2. 获取当前分辨率：
           {"action": "get_resolution"}
        
        3. 获取Game窗口统计信息：
           {"action": "get_stats"}
        
        4. 开启VSync（每帧同步）：
           {"action": "set_vsync", "vsync_count": 1}
        
        5. 关闭VSync：
           {"action": "set_vsync", "vsync_count": 0}
        
        6. 设置目标帧率为60FPS：
           {"action": "set_target_framerate", "target_framerate": 60}
        
        7. 设置无限制帧率：
           {"action": "set_target_framerate", "target_framerate": -1}
        
        8. 最大化Game窗口：
           {"action": "maximize"}
        
        9. 设置16:9宽高比：
           {"action": "set_aspect_ratio", "aspect_ratio": "16:9"}
        
        10. 获取Game窗口截图：
           {"action": "screenshot", "save_path": "Assets/Screenshots/game_view.jpg"}
        """
        return send_to_unity("game_view", {
            "action": action,
            "width": width,
            "height": height,
            "vsync_count": vsync_count,
            "target_framerate": target_framerate,
            "aspect_ratio": aspect_ratio,
            "save_path": save_path
        })

