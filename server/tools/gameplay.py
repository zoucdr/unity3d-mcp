"""
UnityGameplay control tool，Includes play controls、Screenshot、Game state and related features。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_gameplay_tools(mcp: FastMCP):
    @mcp.tool("gameplay")
    def gameplay(
        ctx: Context,
        action: Annotated[str, Field(
            title="Gameplay operation type",
            description="Gameplay operation to perform: play(Play), pause(Pause), stop(Stop), screenshot(Screenshot), get_status(Get status)",
            examples=["play", "pause", "stop", "screenshot", "get_status"]
        )],
        format: Annotated[Optional[str], Field(
            title="Screenshot format",
            description="Image format used for screenshots，Only whenactionIsscreenshotEffective when",
            default=None,
            examples=["PNG", "JPG", "JPEG"]
        )] = None,
        width: Annotated[Optional[int], Field(
            title="Screenshot width",
            description="Screenshot width in pixels，Only whenactionIsscreenshotEffective when",
            default=None,
            ge=1,
            examples=[1920, 1280, 1024]
        )] = None,
        height: Annotated[Optional[int], Field(
            title="Screenshot height",
            description="Screenshot height in pixels，Only whenactionIsscreenshotEffective when",
            default=None,
            ge=1,
            examples=[1080, 720, 768]
        )] = None,
        path: Annotated[Optional[str], Field(
            title="Save path",
            description="Screenshot save path，Only whenactionIsscreenshotEffective when",
            default=None,
            examples=["Screenshots/game.png", "Assets/Images/capture.jpg"]
        )] = None
    ) -> Dict[str, Any]:
        """UnityGameplay control tool，Used to control play state and take screenshots。（Secondary tool）

        Supports multiple gameplay controls，Applicable to：
        - Automated testing：Control play for testing
        - Content creation：Capture gameplay screenshot
        - Development debugging：Control state for debugging
        - Demo recording：Control flow for demos
        """
        return get_common_call_response("gameplay")
