"""
Unity脚本编辑工具
专门管理Unity项目中的C#脚本文件，提供创建、读取、更新、删除等完整的脚本管理功能
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
            title="操作类型",
            description="操作类型: create(创建), read(读取), update(更新), delete(删除)",
            examples=["create", "read", "update", "delete"]
        )],
        name: Annotated[str, Field(
            title="脚本名称",
            description="脚本名称（不含.cs扩展名），必须是有效的C#标识符",
            examples=["PlayerController", "GameManager", "EnemyAI"]
        )],
        path: Annotated[Optional[str], Field(
            title="脚本路径",
            description="脚本所在目录路径（相对于Assets），默认为'Scripts'",
            default=None,
            examples=["Assets/Scripts", "Scripts", "Scripts/Player"]
        )] = None,
        lines: Annotated[Optional[List[str]], Field(
            title="代码内容",
            description="C#代码内容（换行分割的字符串数组）。创建时可选，不提供则自动生成模板",
            default=None,
            examples=[
                ["using UnityEngine;", "", "public class PlayerController : MonoBehaviour", "{", "    void Start()", "    {", "        Debug.Log(\"Player started\");", "    }", "}"]
            ]
        )] = None,
        script_type: Annotated[Optional[str], Field(
            title="脚本类型",
            description="脚本基类类型，用于创建时自动生成模板",
            default="MonoBehaviour",
            examples=["MonoBehaviour", "ScriptableObject", "Editor", "EditorWindow"]
        )] = "MonoBehaviour",
        namespace: Annotated[Optional[str], Field(
            title="命名空间",
            description="C#命名空间，创建时使用",
            default=None,
            examples=["MyGame", "MyGame.Player", "MyGame.Managers"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity脚本编辑工具，用于管理Unity项目中的C#脚本文件。（二级工具）

        支持完整的C#脚本生命周期管理：创建、读取、更新和删除操作。
        创建时会自动生成符合Unity规范的代码模板（MonoBehaviour、ScriptableObject等），
        并进行基础语法验证（括号匹配）。删除操作使用回收站，可以恢复。

        主要操作：
        - create: 创建新脚本，可自定义代码或使用模板（默认路径"Scripts"）
        - read: 读取脚本内容，返回完整文本和按行数组
        - update: 更新脚本内容，自动触发重新编译
        - delete: 安全删除脚本，移到回收站

        脚本名称规则：
        - 只能包含字母、数字和下划线，不能以数字开头
        - 必须是有效的C#标识符
        - 必须与类名匹配（Unity要求）

        注意事项：
        - 支持的script_type: MonoBehaviour, ScriptableObject, Editor, EditorWindow
        - 修改脚本后Unity会自动重新编译
        - 路径支持相对于Assets的路径，自动创建不存在的目录
        """

        return get_common_call_response("edit_script")
