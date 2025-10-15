"""
UnityHierarchy creation tool，IncludeGameObjectCreation capability。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_hierarchy_create_tools(mcp: FastMCP):
    @mcp.tool("hierarchy_create")
    def hierarchy_create(
        ctx: Context,
        source: Annotated[str, Field(
            title="Creation source type",
            description="GameObjectCreation source: menu(Menu), primitive(Primitive), prefab(Prefab), empty(Empty object), copy(Duplicate existing object)",
            examples=["menu", "primitive", "prefab", "empty", "copy"]
        )],
        name: Annotated[str, Field(
            title="Object name",
            description="To createGameObjectName",
            examples=["Player", "Enemy", "MainCamera", "UI_Canvas"]
        )],
        primitive_type: Annotated[Optional[str], Field(
            title="Basic primitive type",
            description="WhensourceIsprimitiveWhen，Specify primitive type",
            default=None,
            examples=["Cube", "Sphere", "Cylinder", "Plane", "Capsule", "Quad"]
        )] = None,
        prefab_path: Annotated[Optional[str], Field(
            title="Prefab path",
            description="WhensourceIsprefabWhen，Specify prefab asset path",
            default=None,
            examples=["Assets/Prefabs/Player.prefab", "Prefabs/Enemy.prefab"]
        )] = None,
        copy_source: Annotated[Optional[str], Field(
            title="Source object to duplicate",
            description="WhensourceIscopyWhen，Specify source to duplicateGameObjectName",
            default=None,
            examples=["Player", "ExistingObject", "Template"]
        )] = None,
        parent: Annotated[Optional[str], Field(
            title="Parent object",
            description="CreatedGameObjectParent object name，Leave empty to create at root",
            default=None,
            examples=["Canvas", "Player", "Environment"]
        )] = None,
        menu_path: Annotated[Optional[str], Field(
            title="Menu path",
            description="WhensourceIsmenuWhen，SpecifyUnityMenu path",
            default=None,
            examples=["GameObject/3D Object/Cube", "GameObject/UI/Button", "GameObject/Light/Directional Light"]
        )] = None,
        tag: Annotated[Optional[str], Field(
            title="GameObjectTag",
            description="SetGameObjectTag",
            default=None,
            examples=["Player", "Enemy", "Untagged", "MainCamera"]
        )] = None,
        layer: Annotated[Optional[int], Field(
            title="GameObjectLayer",
            description="SetGameObjectLayer",
            default=None,
            examples=[0, 8, 10]
        )] = None,
        parent_id: Annotated[Optional[int], Field(
            title="Unique parentID",
            description="Parent object instanceID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        position: Annotated[Optional[List[float]], Field(
            title="Position coordinates",
            description="Position coordinates [x, y, z]",
            default=None,
            examples=[[0, 0, 0], [1.5, 2, -3]]
        )] = None,
        rotation: Annotated[Optional[List[float]], Field(
            title="Rotation",
            description="Rotation [x, y, z]",
            default=None,
            examples=[[0, 0, 0], [0, 90, 0]]
        )] = None,
        scale: Annotated[Optional[List[float]], Field(
            title="Scale",
            description="Scale [x, y, z]",
            default=None,
            examples=[[1, 1, 1], [2, 2, 2]]
        )] = None,
        save_as_prefab: Annotated[Optional[bool], Field(
            title="Whether to save as prefab",
            description="Whether the createdGameObjectSave as prefab",
            default=None
        )] = None,
        set_active: Annotated[Optional[bool], Field(
            title="Set active state",
            description="SetGameObjectActive state",
            default=None
        )] = None
    ) -> Dict[str, Any]:
        """UnityHierarchy creation tool，Used to create various types in the sceneGameObject。（Secondary tool）

        Supports multiple creation methods，Suitable for：
        - Menu creation：ViaUnityCreate object via menu system
        - Rapid prototyping：Create basic primitives for testing
        - Scene construction：Create complex objects from prefabs
        - Object duplication：Duplicate existing objects for batch creation
        - UIBuild：Create empty object as container

        """
        return get_common_call_response("hierarchy_create")
