"""
Storage - SourceLocationTool
Used for asset and folder locating，Supports opening and locating files in the system file browser
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_storage_source_location_tools(mcp: FastMCP):
    @mcp.tool("source_location")
    def source_location(
        ctx: Context,
        action: Annotated[str, Field(
            title="Asset locating operation type",
            description="Asset-locating operation to perform: reveal_in_finder(Show in file browser), open_folder(Open folder), ping_asset(Highlight), select_asset(Select asset), get_asset_path(Get path)",
            examples=["reveal_in_finder", "open_folder", "ping_asset", "select_asset", "get_asset_path"]
        )],
        asset_path: Annotated[Optional[str], Field(
            title="Asset path",
            description="UnityAsset path（Such as Assets/Scripts/MyScript.cs）",
            default=None,
            examples=["Assets/Scripts/PlayerController.cs", "Assets/Prefabs/Player.prefab", "Assets/Textures/icon.png"]
        )] = None,
        folder_path: Annotated[Optional[str], Field(
            title="Folder path",
            description="Folder path（UnityPath or system path），Used foropen_folderOperation",
            default=None,
            examples=["Assets/Scripts", "Assets/Prefabs", "D:/Projects/MyGame/Assets"]
        )] = None,
        instance_id: Annotated[Optional[str], Field(
            title="InstanceID",
            description="GameObjectOr asset instanceID",
            default=None,
            examples=["12345", "67890"]
        )] = None,
        guid: Annotated[Optional[str], Field(
            title="AssetGUID",
            description="Of assetGUID（Globally unique identifier）",
            default=None,
            examples=["a1b2c3d4e5f6g7h8", "12345678901234567890123456789012"]
        )] = None,
        object_name: Annotated[Optional[str], Field(
            title="Object name",
            description="GameObjectOr asset name",
            default=None,
            examples=["MainCamera", "Player", "GameManager", "Canvas"]
        )] = None
    ) -> Dict[str, Any]:
        """UnityAsset locating tool，Used to locate and operate assets in the file system。（Secondary tool）

        Supports multiple locating operations，Suitable for：
        - File browsing：Open and show file in the system file browser（WindowsAsset explorer/Mac Finder）
        - Asset locating：InUnity ProjectQuickly locate asset in the window
        - Object selection：Select in sceneGameObjectOr project asset
        - Path query：Get detailed asset path info（IncludingGUID、Absolute path）

        Example usage：
        1. Show asset in the file browser:
           {"action": "reveal_in_finder", "asset_path": "Assets/Scripts/PlayerController.cs"}

        2. OpenAssetsFolder:
           {"action": "open_folder", "folder_path": "Assets/Scripts"}

        3. InProjectHighlight asset in window:
           {"action": "ping_asset", "asset_path": "Assets/Prefabs/Player.prefab"}

        4. Select in sceneGameObject:
           {"action": "select_asset", "object_name": "MainCamera"}

        5. Get asset path info:
           {"action": "get_asset_path", "instance_id": "12345"}

        6. ViaGUIDLocate asset:
           {"action": "reveal_in_finder", "guid": "a1b2c3d4e5f6g7h8"}

        Notes：
        - reveal_in_finder: Opens and selects the file in the system file browser
        - open_folder: Open folder only，Do not select a specific file
        - ping_asset: InUnityHighlight animation in
        - Supports locating assets in multiple ways：Path、InstanceID、GUID、Name
        """
        
        return get_common_call_response("source_location")

