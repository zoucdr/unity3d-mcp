"""
ScriptableObjectmanagement tool
specially manageUnityinScriptableObjectasset，provide creation、modify、copy、search and related operations
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_scriptableobject_tools(mcp: FastMCP):
    @mcp.tool("edit_scriptableobject")
    def edit_scriptableobject(
        ctx: Context,
        action: str = Field(
            ...,
            title="operation type",
            description="operation type: create(create), modify(modify), duplicate(copy), get_info(get information), search(search)",
            examples=["create", "modify", "duplicate", "get_info", "search"]
        ),
        path: str = Field(
            ...,
            title="ScriptableObjectpath",
            description="ScriptableObjectasset path，Unitystandard format：Assets/Data/MyData.asset",
            examples=["Assets/Data/PlayerData.asset", "Assets/Settings/GameSettings.asset", "Assets/Config/LevelConfig.asset"]
        ),
        script_class: Optional[str] = Field(
            None,
            title="script class name",
            description="ScriptableObjectscript class name（required on creation），namespace not needed",
            examples=["PlayerData", "GameSettings", "LevelConfig", "ItemDatabase"]
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="property dict",
            description="to setScriptableObjectproperty key values，supports primitive types、Vector、ColoretcUnitytype",
            examples=[
                {"playerName": "Player1", "level": 1, "health": 100},
                {"volume": 0.8, "musicEnabled": True},
                {"position": [0, 0, 0], "color": [1, 0, 0, 1]}
            ]
        ),
        destination: Optional[str] = Field(
            None,
            title="target path",
            description="target path（used when duplicating），auto generate unique path if not provided",
            examples=["Assets/Data/PlayerDataCopy.asset", "Assets/Backup/GameSettings.asset"]
        ),
        query: Optional[str] = Field(
            None,
            title="search mode",
            description="search mode or keyword（used when searching）",
            examples=["PlayerData", "Settings", "Config*"]
        ),
        recursive: Optional[bool] = Field(
            True,
            title="recursive search",
            description="recursively search subfolders（used when searching）"
        ),
        force: Optional[bool] = Field(
            False,
            title="force execute",
            description="whether to force execution（overwrite existing files etc）"
        ),
        page_size: Optional[int] = Field(
            50,
            title="page size",
            description="items per page in search，default50",
            examples=[10, 50, 100]
        ),
        page_number: Optional[int] = Field(
            1,
            title="page",
            description="page number of results，from1start",
            examples=[1, 2, 3]
        ),
        generate_preview: Optional[bool] = Field(
            False,
            title="generate preview",
            description="whether to generate preview（Base64encodedPNG）"
        )
    ) -> Dict[str, Any]:
        """
        ScriptableObjectmanagement tool，specially used forUnitydata asset management。（secondary tool）

        ScriptableObjectisUnityused to store game config、level data、data containers such as character stats，
        tool provides completeScriptableObjectasset lifecycle management。

        main features：
        - asset creation：create new from script classScriptableObjectasset
        - property modification：dynamically modify properties of existing assets
        - asset duplication：quickly copy config to create variant
        - asset search：conditional search in projectScriptableObject
        - information query：get detailed info and metadata of assets

        operation types in detail：

        1. **create** - createScriptableObject
           required parameter：path, script_class
           optional parameters：properties（initial property values）
           
           example：create player data
           {
             "action": "create",
             "path": "Assets/Data/PlayerData.asset",
             "script_class": "PlayerData",
             "properties": {
               "playerName": "Hero",
               "level": 1,
               "health": 100,
               "position": [0, 0, 0]
             }
           }

        2. **modify** - modifyScriptableObjectproperty
           required parameter：path, properties
           
           example：update game settings
           {
             "action": "modify",
             "path": "Assets/Settings/GameSettings.asset",
             "properties": {
               "volume": 0.8,
               "fullscreen": True,
               "resolution": [1920, 1080]
             }
           }

        3. **duplicate** - copyScriptableObject
           required parameter：path
           optional parameters：destination（target path，auto generate if not provided）
           
           example：duplicate level config
           {
             "action": "duplicate",
             "path": "Assets/Levels/Level1.asset",
             "destination": "Assets/Levels/Level2.asset"
           }

        4. **search** - searchScriptableObject
           optional parameters：query, path（search scope folder）, page_size, page_number, generate_preview
           
           example：search all player data
           {
             "action": "search",
             "query": "PlayerData",
             "path": "Assets/Data",
             "page_size": 20,
             "page_number": 1,
             "generate_preview": False
           }

        5. **get_info** - getScriptableObjectinformation
           required parameter：path
           optional parameters：generate_preview
           
           example：view asset details
           {
             "action": "get_info",
             "path": "Assets/Data/PlayerData.asset",
             "generate_preview": True
           }

        supported types for property set：
        - primitive types：int, float, bool, string
        - Unityvector：Vector2 [x, y], Vector3 [x, y, z], Vector4 [x, y, z, w]
        - Unitycolor：Color [r, g, b, a]（value range0-1）
        - Unityquaternion：Quaternion [x, y, z, w]
        - enum types：use string name
        - asset reference：use asset path（such asMaterial、Textureetc）

        use cases：
        - game config management：level settings、difficulty configuration、Balance parameters
        - character data：player attributes、enemy configuration、NPCdata
        - UIconfiguration：theme settings、language localization
        - level editing：level parameters、wave configuration
        - item database：item、equipment、skill configuration

        notes：
        - script_classnamespace not required，tool will search automatically
        - propertiesproperty names are case insensitive
        - modifications auto mark asDirtysave to disk
        - supportUndo/Redooperation
        - preview generation may be slow，recommended only when needed
        """

        return get_common_call_response("edit_scriptableobject")
