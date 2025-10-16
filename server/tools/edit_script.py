"""
Unity脚本编辑工具
专门管理Unity项目中的C#脚本文件，提供创建、读取、更新、删除等完整的脚本管理功能

支持的功能：
- 脚本创建：自动生成代码
- 脚本读取：获取脚本内容，支持按行返回
- 脚本更新：修改脚本内容，自动触发重新编译
- 脚本删除：安全删除脚本，移到回收站
- 类型搜索：在所有程序集中搜索匹配的类型
"""
from typing import Annotated, Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_script_tools(mcp: FastMCP):
    @mcp.tool("edit_script")
    def edit_script(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="操作类型: create(创建), read(读取), modify(修改), delete(删除), search(搜索类型), import(导入)",
            examples=["create", "read", "modify", "delete", "search", "import"]
        )],
        name: Annotated[str, Field(
            title="脚本名称",
            description="脚本名称（不含.cs扩展名），必须是有效的C#标识符",
            examples=["PlayerController", "GameManager", "EnemyAI"]
        )],
        folder: Annotated[Optional[str], Field(
            title="脚本所在文件夹",
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
        namespace: Annotated[Optional[str], Field(
            title="命名空间",
            description="C#命名空间，创建时使用",
            default=None,
            examples=["MyGame", "MyGame.Player", "MyGame.Managers"]
        )] = None,
        query: Annotated[Optional[str], Field(
            title="查询字符串",
            description="用于搜索类型的查询字符串，支持通配符*，仅在action为search时使用",
            default=None,
            examples=["Controller", "*Manager", "Unity*"]
        )] = None,
        source_path: Annotated[Optional[str], Field(
            title="源文件路径",
            description="导入操作时的源文件路径，可以是绝对路径或相对路径",
            default=None,
            examples=["/path/to/source/script.cs", "C:\\Scripts\\MyScript.cs"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity脚本编辑工具，用于管理Unity项目中的C#脚本文件。

        支持完整的C#脚本生命周期管理：创建、读取、更新和删除操作。
        创建时会自动生成符合Unity规范的代码，
        并进行基础语法验证（括号匹配）。删除操作使用回收站，可以恢复。
        还支持在所有程序集中搜索类型。

        主要操作：
        - create: 创建新脚本，可自定义代码或使用模板（默认路径"Scripts"）
        - read: 读取脚本内容，返回完整文本和按行数组
        - update: 更新脚本内容，自动触发重新编译
        - delete: 安全删除脚本，移到回收站
        - search: 在所有程序集中搜索匹配的类型，返回类型信息列表

        脚本名称规则：
        - 只能包含字母、数字和下划线，不能以数字开头
        - 必须是有效的C#标识符
        - 必须与类名匹配（Unity要求）

        搜索功能：
        - 使用query参数指定搜索字符串
        - 支持通配符*进行模糊匹配
        - 返回匹配类型的名称、全名、程序集、基类和命名空间信息

        注意事项：
        - 默认使用MonoBehaviour作为基类
        - 修改脚本后Unity会自动重新编译
        - 路径支持相对于Assets的路径，自动创建不存在的目录
        """

        return send_to_unity("edit_script", {
            "action": action,
            "name": name,
            "folder": folder,
            "lines": lines,
            "namespace": namespace,
            "query": query,
            "source_path": source_path
        })
