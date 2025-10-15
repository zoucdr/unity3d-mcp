"""
Unityconsole operation tool，includes console read and write。
"""
from typing import Annotated, List, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_console_tools(mcp: FastMCP):
    @mcp.tool("console_read")
    def console_read(
        ctx: Context,
        action: Annotated[str, Field(
            title="operation type",
            description="Consoleoperation type: get(get logs-no stack trace), get_full(get logs-include stack trace), clear(clear console)",
            examples=["get", "get_full", "clear"]
        )],
        types: Annotated[Optional[List[str]], Field(
            title="list of message types",
            description="message types to get，allow combinations of multiple types",
            default=None,
            examples=[
                ["error", "warning", "log"],
                ["error"],
                ["warning", "log"]
            ]
        )] = None,
        count: Annotated[Optional[int], Field(
            title="max message count",
            description="limit number of returned messages，fetch all messages if not set",
            default=None,
            ge=1,
            examples=[10, 50, 100]
        )] = None,
        filterText: Annotated[Optional[str], Field(
            title="text filter",
            description="filter log messages containing specified text，supports fuzzy match",
            default=None,
            examples=["Error", "NullReference", "GameObject"]
        )] = None,
        format: Annotated[str, Field(
            title="output format",
            description="console output format type",
            default="detailed",
            examples=["plain", "detailed", "json"]
        )] = "detailed"
    ) -> Dict[str, Any]:
        """Unityconsole read utility，can read or clearUnityEditor console messages。（secondary tool）

        supports multiple modes and flexible filters，suitable for：
        - debug info collection：get error and warning messages
        - log analysis：filter messages with specific content
        - console management：clear console history

        """
        
        # ⚠️ important note：this function only provides parameter description and docs
        # for actual calls please use single_call function
        # example：single_call(func="console_read", args={"action": "get", "types": ["error", "warning"]})
        
        return get_common_call_response("console_read")

    @mcp.tool("console_write")
    def console_write(
        ctx: Context,
        action: Annotated[str, Field(
            title="log types",
            description="log message type to write，different types inUnitydifferent display effects and colors in console",
            examples=["error", "warning", "log", "assert", "exception"]
        )],
        message: Annotated[str, Field(
            title="log message",
            description="to write toUnityconcrete console message content",
            examples=[
                "GameObject not found",
                "Player health is low",
                "Loading scene completed",
                "NullReferenceException occurred"
            ]
        )],
        tag: Annotated[Optional[str], Field(
            title="log tags",
            description="tags for classifying and filtering logs，helps filter related messages in console",
            default=None,
            examples=["Player", "GameManager", "UI", "Network"]
        )] = None,
        context: Annotated[Optional[str], Field(
            title="context object",
            description="relatedGameObjectname，On log click you canHierarchyhighlight the object in",
            default=None,
            examples=["Player", "MainCamera", "Canvas", "GameManager"]
        )] = None,
        condition: Annotated[Optional[str], Field(
            title="assertion condition",
            description="assertion conditionExpression，only whenactionas'assert'used when，describe conditions for assertion failures",
            default=None,
            examples=["health > 0", "player != null", "lives >= 0"]
        )] = None
    ) -> Dict[str, Any]:
        """Unityconsole write utility，can write toUnitywrite different log message types to the editor console。

        supports multiple log levels and rich tags，suitable for：
        - debug output：record runtime state and variables
        - error report：output exceptions and errors
        - performance monitoring：record time and status of key actions
        - user feedback：display game state and tips
        """
        
        # ⚠️ important note：this function only provides parameter description and docs
        # for actual calls please use single_call function
        # example：single_call(func="console_write", args={"action": "log", "message": "Hello Unity!"})
        
        return get_common_call_response("console_write")
