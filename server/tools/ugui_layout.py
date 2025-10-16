"""
Unity UGUI布局工具，用于修改和获取RectTransform的布局属性。

使用双状态树架构处理RectTransform修改操作：
- 第一棵树：目标定位（使用GameObjectSelector）
- 第二棵树：基于action类型的布局操作

主要操作：
- do_layout: 执行综合布局修改（可同时设置多个属性，不包含锚点预设）
- get_layout: 获取RectTransform属性（获取所有属性信息）
- tattoo: 设置锚点预设（专门处理tattoo_preset、tattoo_self、preserve_visual_position）

特殊参数：
- tattoo_self: 当为true时，锚点预设将基于元素当前位置而不是父容器的预设位置
  * stretch_all + tattoo_self = tattoo功能（等同于UGUIUtil.AnchorsToCorners）
  * top_center + tattoo_self = 将锚点设置到元素自己的顶部中心位置
  * 其他预设 + tattoo_self = 将锚点设置到元素自身对应的位置

适用场景：
- UI布局设计：精确控制UI元素的位置和尺寸
- 响应式布局：使用锚点预设适配不同分辨率
- 动态UI调整：运行时修改UI元素布局
- 批量布局操作：统一调整多个UI元素
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_ugui_layout_tools(mcp: FastMCP):
    @mcp.tool("ugui_layout")
    def ugui_layout(
        ctx: Context,
        instance_id: Annotated[Optional[int], Field(
            title="实例ID",
            description="GameObject的实例ID",
            default=None,
            examples=[12345, 67890]
        )] = None,
        path: Annotated[Optional[str], Field(
            title="对象路径",
            description="GameObject的层次结构路径",
            default=None,
            examples=["Canvas/UI/Button", "Canvas/Panel/Text"]
        )] = None,
        action: Annotated[Optional[str], Field(
            title="操作类型",
            description="操作类型: do_layout(综合布局,不包含锚点预设), get_layout(获取属性), tattoo(设置锚点预设)",
            default="do_layout",
            examples=["do_layout", "get_layout", "tattoo"]
        )] = "do_layout",
        anchored_position: Annotated[Optional[List[float]], Field(
            title="锚点位置",
            description="锚点位置 [x, y]",
            default=None,
            examples=[[0, 0], [100, -50], [200, 100]]
        )] = None,
        size_delta: Annotated[Optional[List[float]], Field(
            title="尺寸增量",
            description="尺寸增量 [width, height]",
            default=None,
            examples=[[0, 0], [200, 100], [400, 200]]
        )] = None,
        anchor_min: Annotated[Optional[List[float]], Field(
            title="锚点最小值",
            description="锚点最小值 [x, y]",
            default=None,
            examples=[[0, 0], [0.5, 0.5], [1, 1]]
        )] = None,
        anchor_max: Annotated[Optional[List[float]], Field(
            title="锚点最大值",
            description="锚点最大值 [x, y]",
            default=None,
            examples=[[0, 0], [0.5, 0.5], [1, 1]]
        )] = None,
        tattoo_preset: Annotated[Optional[str], Field(
            title="锚点预设",
            description="锚点预设类型: top_left, top_center, top_right, middle_left, middle_center, middle_right, bottom_left, bottom_center, bottom_right, stretch_horizontal, stretch_vertical, stretch_all",
            default=None,
            examples=["stretch_all", "top_center", "middle_center", "bottom_center"]
        )] = None,
        tattoo_self: Annotated[Optional[bool], Field(
            title="锚点自身",
            description="当为true时，锚点预设将基于元素当前位置而不是父容器的预设位置（默认：false）",
            default=False
        )] = False,
        pivot: Annotated[Optional[List[float]], Field(
            title="轴心点",
            description="轴心点 [x, y]",
            default=None,
            examples=[[0.5, 0.5], [0, 0], [1, 1]]
        )] = None,
        sibling_index: Annotated[Optional[int], Field(
            title="层级索引",
            description="在父对象中的子对象索引",
            default=None,
            examples=[0, 1, 2]
        )] = None
    ) -> Dict[str, Any]:
        """Unity UGUI布局工具，用于修改和获取RectTransform的布局属性。

        支持多种布局操作，适用于：
        - 布局修改：设置UI元素的位置、大小、锚点等属性（注意：do_layout不支持tattoo_preset）
        - 属性获取：获取UI元素的当前布局属性
        - 锚点预设：使用预定义的锚点配置（使用tattoo操作）
        - 智能布局：基于元素当前位置设置锚点
        - Tattoo功能：stretch_all + tattoo_self 实现锚点钉到四角
        
        示例用法：
        1. 设置固定位置和大小（不使用锚点预设）：
           {"action": "do_layout", "path": "Canvas/Button", "anchored_position": [0, -50], "size_delta": [200, 80]}
        
        2. 使用锚点预设拉伸填充父容器：
           {"action": "tattoo", "path": "Canvas/Background", "tattoo_preset": "stretch_all"}
        
        3. Tattoo功能（锚点钉到四角）：
           {"action": "tattoo", "path": "Canvas/Panel", "tattoo_preset": "stretch_all", "tattoo_self": true}
        
        4. 设置锚点预设到顶部中心：
           {"action": "tattoo", "path": "Canvas/Title", "tattoo_preset": "top_center", "tattoo_self": true}
        
        5. 获取布局属性：
           {"action": "get_layout", "path": "Canvas/Button"}
           
        注意：do_layout操作不支持tattoo相关参数，如需设置锚点预设请使用tattoo操作。
        """
        return send_to_unity("ugui_layout", {
            "instance_id": instance_id,
            "path": path,
            "action": action,
            "anchored_position": anchored_position,
            "size_delta": size_delta,
            "anchor_min": anchor_min,
            "anchor_max": anchor_max,
            "tattoo_preset": tattoo_preset,
            "tattoo_self": tattoo_self,
            "pivot": pivot,
            "sibling_index": sibling_index
        })
