"""
UnityTags & layers management tool，Used to manage projectTagAndLayer。（Secondary tool）

Supported features：
- Tag management：add_tag, remove_tag, get_tags
- Layer management：add_layer, remove_layer, get_layers
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_tag_layer_tools(mcp: FastMCP):
    @mcp.tool("tag_layer")
    def tag_layer(
        ctx: Context,
        action: Annotated[str, Field(
            title="Operation type",
            description="Tag operation to perform/Layer operations: add_tag(Add tag), remove_tag(Remove tag), get_tags(Get tag list), add_layer(Add layer), remove_layer(Remove layer), get_layers(Get layer list)",
            examples=["add_tag", "remove_tag", "get_tags", "add_layer", "remove_layer", "get_layers"]
        )],
        tag_name: Annotated[Optional[str], Field(
            title="Tag name",
            description="Tag name，Used foradd_tagAndremove_tagOperation",
            default=None,
            examples=["Player", "Enemy", "Item", "NPC", "Boss"]
        )] = None,
        layer_name: Annotated[Optional[str], Field(
            title="Layer name",
            description="Layer name，Used foradd_layerAndremove_layerOperation",
            default=None,
            examples=["Ground", "Character", "UI", "Effects", "Water"]
        )] = None
    ) -> Dict[str, Any]:
        """UnityTags & layers management tool，Used to manage projectTagAndLayer。（Secondary tool）

        Supports tag and layer management features，Suitable for：
        - Tag management：Add、Delete、Get project tags
        - Layer management：Add、Delete、Get project layers（Index8-31For user layers）
        
        Notes：
        - Built-in tags cannot be removed（Such as "Untagged"）
        - Layer index 0-7 Reserved for system，Only index operations are allowed 8-31 User layer
        - Automatically assigns to the first free slot when adding a layer
        
        Example usage：
        1. Add custom tag：
           {"action": "add_tag", "tag_name": "Boss"}
        
        2. Remove tag：
           {"action": "remove_tag", "tag_name": "OldTag"}
        
        3. Get all tags：
           {"action": "get_tags"}
        
        4. Add custom layer：
           {"action": "add_layer", "layer_name": "Water"}
        
        5. Remove layer：
           {"action": "remove_layer", "layer_name": "OldLayer"}
        
        6. Get all layers：
           {"action": "get_layers"}
        """
        return get_common_call_response("tag_layer")

