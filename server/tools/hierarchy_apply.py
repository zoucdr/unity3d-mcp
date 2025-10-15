"""
UnityHierarchy apply tool，IncludeGameObjectPrefab apply and link operations。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_hierarchy_apply_tools(mcp: FastMCP):
    @mcp.tool("hierarchy_apply")
    def hierarchy_apply(
        ctx: Context,
        action: Annotated[str, Field(
            title="Apply operation type",
            description="Apply operation to perform: apply(Apply prefab)",
            examples=["apply"]
        )],
        target_object: Annotated[str, Field(
            title="Target object",
            description="TargetGameObjectIdentifier（Used for apply operations）",
            examples=["Player", "Canvas/UI/Button", "Enemy_01"]
        )],
        prefab_path: Annotated[Optional[str], Field(
            title="Prefab path",
            description="Prefab path",
            default=None,
            examples=["Assets/Prefabs/Player.prefab", "Prefabs/Enemy.prefab"]
        )] = None,
        apply_type: Annotated[Optional[str], Field(
            title="Apply type",
            description="Apply type: connect_to_prefab(Link to prefab), apply_prefab_changes(Apply prefab changes), break_prefab_connection(Disconnect prefab link)",
            default="connect_to_prefab",
            examples=["connect_to_prefab", "apply_prefab_changes", "break_prefab_connection"]
        )] = "connect_to_prefab",
        force_apply: Annotated[Optional[bool], Field(
            title="Force apply",
            description="Whether to force link creation（Override existing link）",
            default=False
        )] = False
    ) -> Dict[str, Any]:
        """UnityHierarchy apply tool，Used to handleGameObjectPrefab apply and connection operations。（Secondary tool）

        Supports multiple prefab operations，Suitable for：
        - Prefab link：WillGameObjectLink to prefab
        - Prefab apply：Apply prefab changes to instance
        - Link disconnected：DisconnectGameObjectConnection to prefab
        - Force apply：Override existing prefab link
        """
        
        return get_common_call_response("hierarchy_apply")
