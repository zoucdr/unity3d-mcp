"""
Unity游戏玩法控制工具，包含游戏播放控制、游戏状态和输入模拟等功能。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_gameplay_tools(mcp: FastMCP):
    @mcp.tool("gameplay")
    def gameplay(
        ctx: Context,
        action: Annotated[str, Field(
            title="游戏操作类型",
            description="要执行的游戏操作: play(播放), pause(暂停), stop(停止), simulate_click(模拟点击), simulate_drag(模拟拖拽), get_info(获取信息)",
            examples=["play", "pause", "stop", "simulate_click", "simulate_drag", "get_info"]
        )],
        # 截图功能已移至GameView和SceneView工具
    ) -> Dict[str, Any]:
        """Unity游戏玩法控制工具，用于控制游戏的播放状态和截图等操作。

        支持多种游戏控制操作，适用于：
        - 自动化测试：控制游戏播放进行测试
        - 输入模拟：模拟鼠标和键盘操作
        - 开发调试：控制游戏状态进行调试
        - 演示录制：控制游戏流程进行演示
        """
        return send_to_unity("gameplay", {
            "action": action
        })
