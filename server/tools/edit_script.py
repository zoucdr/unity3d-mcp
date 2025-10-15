"""
Unityscript editing tool
specially manageUnityin projectC#script file，provide creation、read、update、full script management including delete
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_edit_script_tools(mcp: FastMCP):
    @mcp.tool("edit_script")
    def edit_script(
        ctx: Context,
        action: Annotated[str, Field(
            title="operation type",
            description="operation type: create(create), read(read), update(update), delete(delete)",
            examples=["create", "read", "update", "delete"]
        )],
        name: Annotated[str, Field(
            title="script name",
            description="script name（without.csextension），must be validC#identifier",
            examples=["PlayerController", "GameManager", "EnemyAI"]
        )],
        path: Annotated[Optional[str], Field(
            title="script path",
            description="script directory path（relative toAssets），defaults to'Scripts'",
            default=None,
            examples=["Assets/Scripts", "Scripts", "Scripts/Player"]
        )] = None,
        lines: Annotated[Optional[List[str]], Field(
            title="code content",
            description="C#code content（lines split by newline）。optional on create，auto generate if not provided",
            default=None,
            examples=[
                ["using UnityEngine;", "", "public class PlayerController : MonoBehaviour", "{", "    void Start()", "    {", "        Debug.Log(\"Player started\");", "    }", "}"]
            ]
        )] = None,
        script_type: Annotated[Optional[str], Field(
            title="script type",
            description="script base class type，auto generate template on create",
            default="MonoBehaviour",
            examples=["MonoBehaviour", "ScriptableObject", "Editor", "EditorWindow"]
        )] = "MonoBehaviour",
        namespace: Annotated[Optional[str], Field(
            title="namespace",
            description="C#namespace，used when creating",
            default=None,
            examples=["MyGame", "MyGame.Player", "MyGame.Managers"]
        )] = None
    ) -> Dict[str, Any]:
        """Unityscript editing tool，used to manageUnityin projectC#script file。（secondary tool）

        full support forC#script lifecycle management：create、read、update and delete operations。
        auto generate compliant on createUnitystandard code template（MonoBehaviour、ScriptableObjectetc），
        with basic syntax validation（bracket matching）。use recycle bin for delete，can be restored。

        main operations：
        - create: create new script，custom code or use templates（default path"Scripts"）
        - read: read script content，return full text and line array
        - update: update script content，auto trigger recompile
        - delete: safely delete script，move to recycle bin

        script naming rules：
        - letters only、digits and underscores，cannot start with a digit
        - must be validC#identifier
        - must match class name（Unityrequirements）

        notes：
        - supportedscript_type: MonoBehaviour, ScriptableObject, Editor, EditorWindow
        - after script modifiedUnitywill recompile automatically
        - path supports relative toAssetspath of，auto create missing directories
        """

        return get_common_call_response("edit_script")
