"""
Storage - PrefersTool
Used to manageEditorPrefsAndPlayerPrefsStorage and retrieval of
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_storage_prefers_tools(mcp: FastMCP):
    @mcp.tool("prefers")
    def prefers(
        ctx: Context,
        action: Annotated[str, Field(
            title="Preference operation type",
            description="Preference operation to perform: set(Set), get(Get), delete(Delete), has(Check existence), delete_all(Delete all)",
            examples=["set", "get", "delete", "has", "delete_all"]
        )],
        pref_type: Annotated[Optional[str], Field(
            title="Preference type",
            description="Preference type: editor(Editor preferences) Or player(Player preferences)，Defaulteditor",
            default="editor",
            examples=["editor", "player"]
        )] = "editor",
        key: Annotated[Optional[str], Field(
            title="Key name",
            description="Preference key name（Most operations require）",
            default=None,
            examples=["LastOpenScene", "HighScore", "Username", "Volume"]
        )] = None,
        value: Annotated[Optional[str], Field(
            title="Value",
            description="Value to set（setOperation requires）",
            default=None,
            examples=["MainScene", "100", "true", "0.8"]
        )] = None,
        value_type: Annotated[Optional[str], Field(
            title="Value type",
            description="Value type: string, int, float, bool，Defaultstring",
            default="string",
            examples=["string", "int", "float", "bool"]
        )] = "string",
        default_value: Annotated[Optional[str], Field(
            title="Default value",
            description="Default value（getReturn when key does not exist）",
            default=None,
            examples=["0", "false", "", "1.0"]
        )] = None
    ) -> Dict[str, Any]:
        """UnityPreferences management tool，Used to manageEditorPrefsAndPlayerPrefs。（Secondary tool）

        Supports multiple preference operations，Suitable for：
        - Editor configuration：Save and read editor settings（EditorPrefs）
        - Game saves：Save and read player data（PlayerPrefs）
        - Development tools：Temporarily store debug info and work state
        - User preferences：Save user preferences

        Example usage：
        1. SetEditorPrefsString:
           {"action": "set", "pref_type": "editor", "key": "LastOpenScene", "value": "MainScene"}

        2. GetPlayerPrefsInteger:
           {"action": "get", "pref_type": "player", "key": "HighScore", "value_type": "int", "default_value": "0"}

        3. Delete specified key:
           {"action": "delete", "pref_type": "editor", "key": "TempData"}

        4. Check if key exists:
           {"action": "has", "pref_type": "player", "key": "Username"}

        Notes：
        - EditorPrefs: Stored at editor level，Shared across projects
        - PlayerPrefs: Stored at application level，Tied to the application
        - delete_allOperation will delete all preferences，Use with caution
        - UnityEnumerating all keys is not supported，Requires a known key name
        """
        
        return get_common_call_response("prefers")

