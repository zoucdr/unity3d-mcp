"""
UnityPackage management tool，Includes package installation、Uninstall、Search、Listing and more。
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_manage_package_tools(mcp: FastMCP):
    @mcp.tool("manage_package")
    def manage_package(
        ctx: Context,
        action: Annotated[str, Field(
            title="Package operation type",
            description="Package operation to perform: add(Add package), remove(Remove package), list(List packages), search(Search packages), get_info(Get package info), update(Update package)",
            examples=["add", "remove", "list", "search", "get_info", "update"]
        )],
        package_name: Annotated[Optional[str], Field(
            title="Package name",
            description="Package name to operate on",
            default=None,
            examples=["com.unity.textmeshpro", "com.unity.cinemachine", "com.unity.inputsystem"]
        )] = None,
        version: Annotated[Optional[str], Field(
            title="Package version",
            description="Package version to install，Leave empty to install latest",
            default=None,
            examples=["1.0.0", "latest", "1.2.3-preview.1"]
        )] = None,
        search_query: Annotated[Optional[str], Field(
            title="Search query",
            description="Query string for package search",
            default=None,
            examples=["text", "input", "cinemachine", "ui"]
        )] = None,
        include_prerelease: Annotated[bool, Field(
            title="Include prerelease versions",
            description="Whether to include prerelease versions",
            default=False
        )] = False,
        timeout: Annotated[Optional[int], Field(
            title="Timeout",
            description="Operation timeout（Seconds）",
            default=60,
            ge=10,
            le=300
        )] = 60
    ) -> Dict[str, Any]:
        """UnityPackage management tool，Used to manageUnity Package ManagerPackages in。（Secondary tool）
        Supports various package management operations，Suitable for：
        - Package install：Add a new package to the project
        - Package management：Remove unwanted package
        - Package search：Find available packages
        - Package information：Get package details
        - Package update：Update package to latest
        """
        return get_common_call_response("manage_package")
