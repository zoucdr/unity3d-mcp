"""
prefab management tool
specially manageUnityprefab asset in，provide prefab creation、modify、copy、instantiate and related actions
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_prefab_tools(mcp: FastMCP):
    @mcp.tool("edit_prefab")
    def edit_prefab(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type",
            examples=["create", "modify", "duplicate", "get_info", "search", "instantiate", "unpack", "pack", "create_variant", "connect_to_prefab", "apply_changes", "revert_changes", "break_connection"]
        ),
        path: str = Field(
            ...,
            title="prefab asset path",
            description="prefab asset path，Unitystandard format：Assets/Prefabs/PrefabName.prefab",
            examples=["Assets/Prefabs/Player.prefab", "Assets/Prefabs/Enemy.prefab"]
        ),
        source_object: Optional[str] = Field(
            None,
            title="sourceGameObjectname or path",
            description="sourceGameObjectname or path（used when creating）",
            examples=["Player", "Enemy", "Canvas/UI/Button"]
        ),
        destination: Optional[str] = Field(
            None,
            title="target path",
            description="target path（used when copying）",
            examples=["Assets/Prefabs/PlayerCopy.prefab", "Assets/Prefabs/EnemyVariant.prefab"]
        ),
        query: Optional[str] = Field(
            None,
            title="search mode",
            description="search mode，such as*.prefab",
            examples=["*.prefab", "Player*", "Enemy*"]
        ),
        recursive: Optional[bool] = Field(
            True,
            title="recursive search",
            description="recursively search subfolders"
        ),
        force: Optional[bool] = Field(
            False,
            title="force execute",
            description="whether to force execution（overwrite existing files etc）"
        ),
        prefab_variant: Optional[bool] = Field(
            False,
            title="prefab variant",
            description="create prefab variant"
        ),
        unpack_mode: Optional[str] = Field(
            None,
            title="unpack mode",
            description="unpack mode：Completely, OutermostRoot",
            examples=["Completely", "OutermostRoot"]
        ),
        pack_mode: Optional[str] = Field(
            None,
            title="pack mode",
            description="pack mode：Default, ReuseExisting",
            examples=["Default", "ReuseExisting"]
        ),
        connect_to_prefab: Optional[bool] = Field(
            None,
            title="connect to prefab",
            description="connect to prefab"
        ),
        apply_prefab_changes: Optional[bool] = Field(
            None,
            title="apply prefab changes",
            description="apply prefab changes"
        ),
        revert_prefab_changes: Optional[bool] = Field(
            None,
            title="revert prefab changes",
            description="revert prefab changes"
        ),
        break_prefab_connection: Optional[bool] = Field(
            None,
            title="break prefab connection",
            description="break prefab connection"
        ),
        prefab_type: Optional[str] = Field(
            None,
            title="prefab type",
            description="prefab type：Regular, Variant",
            examples=["Regular", "Variant"]
        ),
        parent_prefab: Optional[str] = Field(
            None,
            title="parent prefab path",
            description="parent prefab path（used for variant）",
            examples=["Assets/Prefabs/BaseEnemy.prefab"]
        ),
        scene_path: Optional[str] = Field(
            None,
            title="scene path",
            description="scene path（used when instantiating）",
            examples=["Assets/Scenes/MainScene.unity"]
        ),
        position: Optional[List[float]] = Field(
            None,
            title="position coordinates",
            description="position coordinates [x, y, z]",
            examples=[[0, 0, 0], [1, 2, 3]]
        ),
        rotation: Optional[List[float]] = Field(
            None,
            title="rotation",
            description="rotation [x, y, z]",
            examples=[[0, 0, 0], [0, 90, 0]]
        ),
        scale: Optional[List[float]] = Field(
            None,
            title="scale",
            description="scale [x, y, z]",
            examples=[[1, 1, 1], [2, 2, 2]]
        ),
        parent_path: Optional[str] = Field(
            None,
            title="parent object name or path",
            description="parent object name or path",
            examples=["Canvas", "Environment", "Player"]
        )
    ) -> Dict[str, Any]:
        """
        prefab management tool，supported operations: 
        
        create(create), modify(modify), duplicate(copy), get_info(get info), search(search), instantiate(instantiate), unpack(unpack), pack(pack), create_variant(create variant), connect_to_prefab(connect), apply_changes(apply changes), revert_changes(revert changes), break_connection(disconnect)
        """

        return get_common_call_response("edit_prefab")
