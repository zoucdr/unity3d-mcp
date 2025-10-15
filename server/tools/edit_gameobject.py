"""
Unity GameObjectediting tool，includeGameObjectcreation of、modify、component management features。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_gameobject_tools(mcp: FastMCP):
    @mcp.tool("edit_gameobject")
    def edit_gameobject(
        ctx: Context,
        action: Annotated[str, Field(
            title="operation type",
            description="operation type: create(create), modify(modify), get_components(get component), add_component(add component), remove_component(remove component), set_parent(set parent)",
            examples=["create", "modify", "get_components", "add_component", "remove_component", "set_parent"]
        )],
        path: Annotated[Optional[str], Field(
            title="object path",
            description="GameObjecthierarchy path of",
            default=None,
            examples=["Player", "Canvas/UI/Button", "Enemy_01"]
        )] = None,
        instance_id: Annotated[Optional[int], Field(
            title="instanceID",
            description="GameObjectinstance ofID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        name: Annotated[Optional[str], Field(
            title="GameObjectname",
            description="GameObjectname of",
            default=None,
            examples=["NewObject", "Player", "Enemy"]
        )] = None,
        tag: Annotated[Optional[str], Field(
            title="tag",
            description="GameObjecttag of",
            default=None,
            examples=["Player", "Enemy", "Untagged"]
        )] = None,
        layer: Annotated[Optional[int], Field(
            title="layer",
            description="GameObjectlayer of",
            default=None,
            examples=[0, 8, 10]
        )] = None,
        parent_id: Annotated[Optional[int], Field(
            title="parentID",
            description="parent object instanceID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        parent_path: Annotated[Optional[str], Field(
            title="parent path",
            description="hierarchy path of the parent",
            default=None,
            examples=["Canvas", "Player", "Environment"]
        )] = None,
        position: Annotated[Optional[List[float]], Field(
            title="position",
            description="GameObjectposition coordinates of [x, y, z]",
            default=None,
            examples=[[0, 0, 0], [1.5, 2.0, -3.0]]
        )] = None,
        rotation: Annotated[Optional[List[float]], Field(
            title="rotation",
            description="GameObjectrotation of [x, y, z]",
            default=None,
            examples=[[0, 0, 0], [0, 90, 0]]
        )] = None,
        scale: Annotated[Optional[List[float]], Field(
            title="scale",
            description="GameObjectscale of [x, y, z]",
            default=None,
            examples=[[1, 1, 1], [2.0, 2.0, 2.0]]
        )] = None,
        component_type: Annotated[Optional[str], Field(
            title="component type",
            description="component type name to add or remove",
            default=None,
            examples=["Rigidbody", "BoxCollider", "AudioSource", "Light"]
        )] = None,
        active: Annotated[Optional[bool], Field(
            title="active state",
            description="GameObjectactive state of",
            default=None
        )] = None,
        static_flags: Annotated[Optional[int], Field(
            title="static flags",
            description="GameObjectstatic flags of",
            default=None,
            examples=[0, 1, 2, 4]
        )] = None
    ) -> Dict[str, Any]:
        """Unity GameObjectediting tool，used to create、modify and manageGameObject。（secondary tool）

        supports multipleGameObjectoperation，suitable for：
        - object creation：create newGameObject
        - property modification：modifyGameObjectbasic properties of
        - component management：add、remove and get components
        - hierarchy：set parent child relationship
        - transform operations：set position、rotation、scale
        """
        
        # ⚠️ important note：this function only provides parameter description and docs
        # for actual calls please use single_call function
        # example：single_call(func="edit_gameobject", args={"path": "Player", "action": "modify"})
        
        return get_common_call_response("edit_gameobject")
