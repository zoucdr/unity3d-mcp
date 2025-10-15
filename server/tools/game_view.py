"""
Unity GameWindow management tool，Used to controlGameView display and performance settings。（Secondary tool）

Supported features：
- Resolution management：set_resolution, get_resolution
- Performance settings：set_vsync, set_target_framerate
- Window control：maximize
- Statistics：get_stats
- Aspect ratio settings：set_aspect_ratio
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_game_view_tools(mcp: FastMCP):
    @mcp.tool("game_view")
    def game_view(
        ctx: Context,
        action: Annotated[str, Field(
            title="Operation type",
            description="To executeGameWindow operations: set_resolution(Set resolution), get_resolution(Get resolution), get_stats(Get statistics), set_vsync(Set vertical sync), set_target_framerate(Set target frame rate), maximize(Maximize window), set_aspect_ratio(Set aspect ratio)",
            examples=["set_resolution", "get_resolution", "get_stats", "set_vsync", "set_target_framerate", "maximize", "set_aspect_ratio"]
        )],
        width: Annotated[Optional[int], Field(
            title="Window width",
            description="GameWindow width（Pixels），Used forset_resolutionOperation",
            default=None,
            examples=[1920, 1280, 1024, 800]
        )] = None,
        height: Annotated[Optional[int], Field(
            title="Window height",
            description="GameWindow height（Pixels），Used forset_resolutionOperation",
            default=None,
            examples=[1080, 720, 768, 600]
        )] = None,
        vsync_count: Annotated[Optional[int], Field(
            title="VSync count",
            description="VSyncCount：0=Close, 1=Per-frame sync, 2=Per2Frame sync, Used forset_vsyncOperation",
            default=None,
            examples=[0, 1, 2]
        )] = None,
        target_framerate: Annotated[Optional[int], Field(
            title="Target frame rate",
            description="Target frame rate（FPS），-1Indicates unlimited，Used forset_target_framerateOperation",
            default=None,
            examples=[60, 30, 120, -1]
        )] = None,
        aspect_ratio: Annotated[Optional[str], Field(
            title="Aspect ratio",
            description="Aspect ratio settings，Such as'16:9', '4:3', 'Free'Etc.，Used forset_aspect_ratioOperation",
            default=None,
            examples=["16:9", "16:10", "4:3", "Free", "21:9"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity GameWindow management tool，Used to controlGameView display and performance settings。（Secondary tool）

        Supports multipleGameWindow management features，Suitable for：
        - Resolution management：Set and queryGameWindow resolution
        - Performance optimization：ConfigurationVSyncAnd target frame rate
        - Window control：MaximizeGameWindow
        - Metrics monitoring：Get current performance and display stats
        - Display settings：Configure aspect ratio
        
        Notes：
        - Resolution settings affectGameWindow display size
        - VSyncSettings affect performance and smoothness
        - Target frame rate-1Indicates no FPS limit
        - Aspect ratio setting is a simplified implementation，Full functionality requires more complexUnityInternalAPIAccess
        
        Example usage：
        1. SettingsGameWindow resolution is1920x1080：
           {"action": "set_resolution", "width": 1920, "height": 1080}
        
        2. Get current resolution：
           {"action": "get_resolution"}
        
        3. GetGameWindow statistics：
           {"action": "get_stats"}
        
        4. EnableVSync（Per-frame sync）：
           {"action": "set_vsync", "vsync_count": 1}
        
        5. CloseVSync：
           {"action": "set_vsync", "vsync_count": 0}
        
        6. Set target frame rate to60FPS：
           {"action": "set_target_framerate", "target_framerate": 60}
        
        7. Set unlimited frame rate：
           {"action": "set_target_framerate", "target_framerate": -1}
        
        8. MaximizeGameWindow：
           {"action": "maximize"}
        
        9. Settings16:9Aspect ratio：
           {"action": "set_aspect_ratio", "aspect_ratio": "16:9"}
        """
        return get_common_call_response("game_view")

