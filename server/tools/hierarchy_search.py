"""
UnityHierarchy search tool，IncludeGameObjectSearch capability。
"""
from typing import Annotated, Dict, Any
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_hierarchy_search_tools(mcp: FastMCP):
    @mcp.tool("hierarchy_search")
    def hierarchy_search(
        ctx: Context,
        query: Annotated[str, Field(
            title="Search query",
            description="To searchGameObjectName or name pattern",
            examples=["Player", "Enemy*", "*Camera", "UI_*"]
        )],
        search_type: Annotated[str, Field(
            title="Search type",
            description="Type of search: by_name(By name), by_id(ByID), by_tag(By tag), by_layer(By hierarchy), by_component(By component), by_query(General query)",
            default="by_name",
            examples=["by_name", "by_id", "by_tag", "by_layer", "by_component", "by_query"]
        )] = "by_name",
        select_many: Annotated[bool, Field(
            title="Find multiple matches",
            description="Whether to find all matches",
            default=True
        )] = True,
        include_hierarchy: Annotated[bool, Field(
            title="Includes full hierarchy info",
            description="Whether to include full hierarchy of all children",
            default=False
        )] = False,
        include_inactive: Annotated[bool, Field(
            title="Include inactive objects",
            description="Whether to include inactive objects in resultsGameObject",
            default=False
        )] = False,
        use_regex: Annotated[bool, Field(
            title="Use regular expressions",
            description="Whether to use regex for searching",
            default=False
        )] = False
    ) -> Dict[str, Any]:
        """UnityHierarchy search tool，Used to search in the sceneGameObject。（Secondary tool）
        
        Supports multiple search modes，Suitable for：
        - Object locating：Quickly find by specific nameGameObject
        - Batch operations：Find all matching objects
        - Debug analysis：Check object states in the scene
        - Automation scripts：Get object list for processing
        - Hierarchy：Get full parent-child relations
        
        New feature：
        - include_hierarchy: Get full hierarchy，Include complete data of all children
        - Supports multiple search types：By name、ID、Tag、Hierarchy、Components
        - Supports regex and wildcard search
        - Default search feature：When not specifiedsearch_typeAutomatically use name search when
        """
        
        # ⚠️ Important notes：This function only provides parameter docs
        # Use in actual calls single_call Function
        # Example：single_call(func="hierarchy_search", args={"query": "Player", "search_type": "name"})
        
        return get_common_call_response("hierarchy_search")

