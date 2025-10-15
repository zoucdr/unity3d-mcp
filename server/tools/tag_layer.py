"""
Unity标签和层级管理工具，用于管理项目的Tag和Layer。

支持的功能：
- 标签管理：add_tag, remove_tag, get_tags
- 层级管理：add_layer, remove_layer, get_layers
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_tag_layer_tools(mcp: FastMCP):
    @mcp.tool("tag_layer")
    def tag_layer(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="要执行的标签/层级操作: add_tag(添加标签), remove_tag(移除标签), get_tags(获取标签列表), add_layer(添加层级), remove_layer(移除层级), get_layers(获取层级列表)",
            examples=["add_tag", "remove_tag", "get_tags", "add_layer", "remove_layer", "get_layers"]
        )],
        tag_name: Annotated[Optional[str], Field(
            title="标签名称",
            description="标签名称，用于add_tag和remove_tag操作",
            default=None,
            examples=["Player", "Enemy", "Item", "NPC", "Boss"]
        )] = None,
        layer_name: Annotated[Optional[str], Field(
            title="层级名称",
            description="层级名称，用于add_layer和remove_layer操作",
            default=None,
            examples=["Ground", "Character", "UI", "Effects", "Water"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity标签和层级管理工具，用于管理项目的Tag和Layer。

        支持多种标签和层级管理功能，适用于：
        - 标签管理：添加、删除、获取项目标签
        - 层级管理：添加、删除、获取项目层级（索引8-31为用户层级）
        
        注意事项：
        - 不能删除内置标签（如 "Untagged"）
        - 层级索引 0-7 为系统保留，只能操作索引 8-31 的用户层级
        - 添加层级时会自动分配到第一个空闲槽位
        
        示例用法：
        1. 添加自定义标签：
           {"action": "add_tag", "tag_name": "Boss"}
        
        2. 移除标签：
           {"action": "remove_tag", "tag_name": "OldTag"}
        
        3. 获取所有标签：
           {"action": "get_tags"}
        
        4. 添加自定义层级：
           {"action": "add_layer", "layer_name": "Water"}
        
        5. 移除层级：
           {"action": "remove_layer", "layer_name": "OldLayer"}
        
        6. 获取所有层级：
           {"action": "get_layers"}
        """
        return send_to_unity("tag_layer", {
            "action": action,
            "tag_name": tag_name,
            "layer_name": layer_name
        })

