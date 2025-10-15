"""
UnityObject deletion tool，IncludeGameObject、Assets and otherUnityObject deletion feature。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_object_delete_tools(mcp: FastMCP):
    @mcp.tool("object_delete")
    def object_delete(
        ctx: Context,
        path: Annotated[Optional[str], Field(
            title="Object path",
            description="Hierarchy path of the object to delete",
            default=None,
            examples=["Player", "Canvas/UI/Button", "Assets/Materials/OldMaterial.mat"]
        )] = None,
        instance_id: Annotated[Optional[int], Field(
            title="InstanceID",
            description="Instance of the object to deleteID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        confirm: Annotated[Optional[bool], Field(
            title="Force confirmation",
            description="Whether to force confirmation dialog：true=Always confirm，false/unset=Smart confirmation（≤3Objects auto-deleted，>3Dialogs shown）",
            default=None
        )] = None
    ) -> Dict[str, Any]:
        """UnityObject deletion tool，Used to deleteGameObject、Assets and otherUnityObject。（Secondary tool）

        Supports multiple delete modes with smart confirmation，Suitable for：
        - Scene object deletion：Delete in sceneGameObject
        - Asset deletion：Delete asset files in the project
        - Bulk delete：Delete multiple objects
        - Safe delete：Deletion with confirmation
        """
        
        return get_common_call_response("object_delete")
