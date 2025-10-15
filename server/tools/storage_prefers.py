"""
Storage - Prefers工具
用于管理EditorPrefs和PlayerPrefs的存储和检索
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
            title="偏好设置操作类型",
            description="要执行的偏好设置操作: set(设置), get(获取), delete(删除), has(检查存在), delete_all(删除所有)",
            examples=["set", "get", "delete", "has", "delete_all"]
        )],
        pref_type: Annotated[Optional[str], Field(
            title="偏好类型",
            description="偏好类型: editor(编辑器偏好) 或 player(玩家偏好)，默认editor",
            default="editor",
            examples=["editor", "player"]
        )] = "editor",
        key: Annotated[Optional[str], Field(
            title="键名",
            description="偏好设置的键名（大多数操作需要）",
            default=None,
            examples=["LastOpenScene", "HighScore", "Username", "Volume"]
        )] = None,
        value: Annotated[Optional[str], Field(
            title="值",
            description="要设置的值（set操作需要）",
            default=None,
            examples=["MainScene", "100", "true", "0.8"]
        )] = None,
        value_type: Annotated[Optional[str], Field(
            title="值类型",
            description="值类型: string, int, float, bool，默认string",
            default="string",
            examples=["string", "int", "float", "bool"]
        )] = "string",
        default_value: Annotated[Optional[str], Field(
            title="默认值",
            description="默认值（get操作时键不存在时返回）",
            default=None,
            examples=["0", "false", "", "1.0"]
        )] = None
    ) -> Dict[str, Any]:
        """Unity偏好设置管理工具，用于管理EditorPrefs和PlayerPrefs。（二级工具）

        支持多种偏好设置操作，适用于：
        - 编辑器配置：保存和读取编辑器设置（EditorPrefs）
        - 游戏存档：保存和读取玩家数据（PlayerPrefs）
        - 开发工具：临时存储调试信息和工作状态
        - 用户偏好：保存用户的个性化设置

        示例用法：
        1. 设置EditorPrefs字符串:
           {"action": "set", "pref_type": "editor", "key": "LastOpenScene", "value": "MainScene"}

        2. 获取PlayerPrefs整数:
           {"action": "get", "pref_type": "player", "key": "HighScore", "value_type": "int", "default_value": "0"}

        3. 删除指定键:
           {"action": "delete", "pref_type": "editor", "key": "TempData"}

        4. 检查键是否存在:
           {"action": "has", "pref_type": "player", "key": "Username"}

        注意事项：
        - EditorPrefs: 存储在编辑器层面，不同项目共享
        - PlayerPrefs: 存储在应用层面，跟随应用走
        - delete_all操作会删除所有偏好设置，请谨慎使用
        - Unity不支持直接枚举所有键，需要使用已知的键名
        """
        
        return get_common_call_response("prefers")

