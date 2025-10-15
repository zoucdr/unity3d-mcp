"""
Unity UGUILayout tool，Used to modify and getRectTransformLayout properties of。（Secondary tool）

Use dual-state tree architectureRectTransformModify operations：
- First tree：Target positioning（UseGameObjectSelector）
- Second tree：Based onactionType of layout operation

Primary operations：
- do_layout: Perform comprehensive layout changes（Set multiple properties at once，Without anchor presets）
- get_layout: GetRectTransformProperty（Get all property info）
- tattoo: Set anchor preset（Specially handlestattoo_preset、tattoo_self、preserve_visual_position）

Special parameters：
- tattoo_self: When it istrueWhen，Anchor presets are based on the element’s current position
  * stretch_all + tattoo_self = tattooFunction（Equivalent toUGUIUtil.AnchorsToCorners）
  * top_center + tattoo_self = Set anchors to the element’s own top-center
  * Other presets + tattoo_self = Set anchors to the element’s corresponding self position
- preserve_visual_position: Whether to keep visual position when changing presets（Default：true）

Applicable scenarios：
- UILayout design：Precise controlUIElement position and size
- Responsive layout：Use anchor presets to adapt to resolutions
- DynamicUIAdjust：Runtime modificationsUIElement layout
- Batch layout operations：Uniformly adjust multipleUIElement
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_ugui_layout_tools(mcp: FastMCP):
    @mcp.tool("ugui_layout")
    def ugui_layout(
        ctx: Context,
        instance_id: Annotated[Optional[int], Field(
            title="InstanceID",
            description="GameObjectInstance ofID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        path: Annotated[Optional[str], Field(
            title="Object path",
            description="GameObjectHierarchy path of",
            default=None,
            examples=["Canvas/UI/Button", "Canvas/Panel/Text"]
        )] = None,
        action: Annotated[Optional[str], Field(
            title="Operation type",
            description="Operation type: do_layout(Composite layout,Without anchor presets), get_layout(Get properties), tattoo(Set anchor preset)",
            default="do_layout",
            examples=["do_layout", "get_layout", "tattoo"]
        )] = "do_layout",
        anchored_position: Annotated[Optional[List[float]], Field(
            title="Anchor position",
            description="Anchor position [x, y]",
            default=None,
            examples=[[0, 0], [100, -50], [200, 100]]
        )] = None,
        size_delta: Annotated[Optional[List[float]], Field(
            title="Size delta",
            description="Size delta [width, height]",
            default=None,
            examples=[[0, 0], [200, 100], [400, 200]]
        )] = None,
        anchor_min: Annotated[Optional[List[float]], Field(
            title="Anchor min",
            description="Anchor min [x, y]",
            default=None,
            examples=[[0, 0], [0.5, 0.5], [1, 1]]
        )] = None,
        anchor_max: Annotated[Optional[List[float]], Field(
            title="Anchor max",
            description="Anchor max [x, y]",
            default=None,
            examples=[[0, 0], [0.5, 0.5], [1, 1]]
        )] = None,
        tattoo_preset: Annotated[Optional[str], Field(
            title="Anchor presets",
            description="Anchor preset type: top_left, top_center, top_right, middle_left, middle_center, middle_right, bottom_left, bottom_center, bottom_right, stretch_horizontal, stretch_vertical, stretch_all",
            default=None,
            examples=["stretch_all", "top_center", "middle_center", "bottom_center"]
        )] = None,
        tattoo_self: Annotated[Optional[bool], Field(
            title="Anchor self",
            description="When it istrueWhen，Anchor presets are based on the element’s current position（Default：false）",
            default=False
        )] = False,
        preserve_visual_position: Annotated[Optional[bool], Field(
            title="Keep visual position",
            description="Whether to keep visual position when changing presets（Default：true）",
            default=True
        )] = True,
        pivot: Annotated[Optional[List[float]], Field(
            title="Pivot",
            description="Pivot [x, y]",
            default=None,
            examples=[[0.5, 0.5], [0, 0], [1, 1]]
        )] = None,
        sibling_index: Annotated[Optional[int], Field(
            title="Hierarchy index",
            description="Child index in parent",
            default=None,
            examples=[0, 1, 2]
        )] = None
    ) -> Dict[str, Any]:
        """Unity UGUILayout tool，Used to modify and getRectTransformLayout properties of。（Secondary tool）

        Supports multiple layout operations，Suitable for：
        - Layout modifications：SetUIElement position、Size、Anchor and related properties（Note：do_layoutNot supportedtattoo_preset）
        - Property retrieval：GetUICurrent layout properties of the element
        - Anchor presets：Use predefined anchor configs（UsetattooOperation）
        - Smart layout：Set anchors based on current position
        - TattooFunction：stretch_all + tattoo_self Pin anchors to all four corners
        
        Example usage：
        1. Set fixed position and size（Do not use anchor presets）：
           {"action": "do_layout", "path": "Canvas/Button", "anchored_position": [0, -50], "size_delta": [200, 80]}
        
        2. Use anchor preset to stretch and fill parent：
           {"action": "tattoo", "path": "Canvas/Background", "tattoo_preset": "stretch_all"}
        
        3. TattooFunction（Anchors to corners）：
           {"action": "tattoo", "path": "Canvas/Panel", "tattoo_preset": "stretch_all", "tattoo_self": true}
        
        4. Set anchor preset to top-center：
           {"action": "tattoo", "path": "Canvas/Title", "tattoo_preset": "top_center", "tattoo_self": true}
        
        5. Get layout properties：
           {"action": "get_layout", "path": "Canvas/Button"}
           
        Note：do_layoutOperation not supportedtattoo_presetParameters，To set anchor presetstattooOperation。
        """
        # ⚠️ Important notes：This function only provides parameter docs
        # Use in actual calls single_call Function
        # Example：single_call(func="ugui_layout", args={...})
        
        return get_common_call_response("ugui_layout")
