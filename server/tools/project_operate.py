"""
UnityProject operation tool，Includes project asset management。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_project_operate_tools(mcp: FastMCP):
    @mcp.tool("project_operate")
    def project_operate(
        ctx: Context,
        action: Annotated[str, Field(
            title="Project operation type",
            description="Project operation to perform: refresh(Refresh project), import_asset(Import assets), export_package(Export package), create_folder(Create folders), delete_asset(Delete assets), tree(Get folder structure)",
            examples=["refresh", "import_asset", "export_package", "create_folder", "delete_asset", "tree"]
        )],
        path: Annotated[Optional[str], Field(
            title="Asset path",
            description="Asset path，UnityStandard format：Assets/Folder/File.extension，treeRoot directory path for the operation（DefaultAssets）",
            default=None,
            examples=["Assets/NewFolder", "Assets/Scripts", "Assets/Prefabs"]
        )] = None,
        target_path: Annotated[Optional[str], Field(
            title="Target path",
            description="Target path（Move/Used when copying）",
            default=None,
            examples=["Assets/NewFolder", "Assets/Models/model.fbx"]
        )] = None,
        source_path: Annotated[Optional[str], Field(
            title="Source path",
            description="Source file path，Only whenimport_assetUsed during operation",
            default=None,
            examples=["D:/Models/character.fbx", "C:/Textures/grass.png"]
        )] = None,
        package_name: Annotated[Optional[str], Field(
            title="Package name",
            description="Name of the exported package，Only whenexport_packageUsed during operation",
            default=None,
            examples=["MyAssets.unitypackage", "ScriptsPackage"]
        )] = None,
        include_dependencies: Annotated[bool, Field(
            title="Include dependencies",
            description="Whether to include asset dependencies，Suitable forexport_packageOperation",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """UnityProject operation tool，Used to perform various project operations。（Secondary tool）

        Supports multiple project operations，Suitable for：
        - Asset management：Import external assets into the project
        - Project organization：Create folder structure，Delete unnecessary assets
        - Package management：Export packages for sharing or backup
        - Project maintenance：Refresh project status，Clean invalid references
        - Folder structure：ViewAssetsFolder hierarchy and file count under the directory（YAMLFormat）

        """
        
        # ⚠️ Important notes：This function only provides parameter docs
        # Use in actual calls single_call Function
        # Example：single_call(func="project_operate", args={"action": "refresh"})
        
        return get_common_call_response("project_operate")
