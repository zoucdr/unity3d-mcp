"""
ScriptableObject管理工具
专门管理Unity中的ScriptableObject资源，提供创建、修改、复制、搜索等操作

支持的功能：
- 资源创建：从脚本类创建新的ScriptableObject资源
- 属性修改：动态修改已存在资源的属性值
- 资源复制：快速复制现有配置创建变体
- 资源搜索：按条件搜索项目中的ScriptableObject
- 信息查询：获取资源的详细信息和元数据
"""

from typing import Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_edit_scriptableobject_tools(mcp: FastMCP):
    @mcp.tool("edit_scriptableobject")
    def edit_scriptableobject(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型: create(创建), modify(修改), duplicate(复制), get_info(获取信息), search(搜索)",
            examples=["create", "modify", "duplicate", "get_info", "search"]
        ),
        path: str = Field(
            ...,
            title="ScriptableObject路径",
            description="ScriptableObject资源路径，Unity标准格式：Assets/Data/MyData.asset",
            examples=["Assets/Data/PlayerData.asset", "Assets/Settings/GameSettings.asset", "Assets/Config/LevelConfig.asset"]
        ),
        script_class: Optional[str] = Field(
            None,
            title="脚本类名",
            description="ScriptableObject脚本类名（创建时必需），不需要命名空间",
            examples=["PlayerData", "GameSettings", "LevelConfig", "ItemDatabase"]
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="属性字典",
            description="要设置的ScriptableObject属性键值对，支持基础类型、Vector、Color等Unity类型",
            examples=[
                {"playerName": "Player1", "level": 1, "health": 100},
                {"volume": 0.8, "musicEnabled": True},
                {"position": [0, 0, 0], "color": [1, 0, 0, 1]}
            ]
        ),
        destination: Optional[str] = Field(
            None,
            title="目标路径",
            description="目标路径（复制时使用），如不提供则自动生成唯一路径",
            examples=["Assets/Data/PlayerDataCopy.asset", "Assets/Backup/GameSettings.asset"]
        ),
        query: Optional[str] = Field(
            None,
            title="搜索模式",
            description="搜索模式或关键词（搜索时使用）",
            examples=["PlayerData", "Settings", "Config*"]
        ),
        recursive: Optional[bool] = Field(
            True,
            title="递归搜索",
            description="是否递归搜索子文件夹（搜索时使用）"
        ),
        force: Optional[bool] = Field(
            False,
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）"
        ),
        page_size: Optional[int] = Field(
            50,
            title="页面大小",
            description="搜索结果每页的数量，默认50",
            examples=[10, 50, 100]
        ),
        page_number: Optional[int] = Field(
            1,
            title="页码",
            description="搜索结果的页码，从1开始",
            examples=[1, 2, 3]
        ),
        generate_preview: Optional[bool] = Field(
            False,
            title="生成预览",
            description="是否生成资源预览图（Base64编码的PNG）"
        )
    ) -> Dict[str, Any]:
        """
        ScriptableObject管理工具，专门用于Unity中的数据资源管理。

        ScriptableObject是Unity中用于存储游戏配置、关卡数据、角色属性等的数据容器，
        本工具提供完整的ScriptableObject资源生命周期管理功能。

        主要功能：
        - 资源创建：从脚本类创建新的ScriptableObject资源
        - 属性修改：动态修改已存在资源的属性值
        - 资源复制：快速复制现有配置创建变体
        - 资源搜索：按条件搜索项目中的ScriptableObject
        - 信息查询：获取资源的详细信息和元数据

        操作类型详解：

        1. **create** - 创建ScriptableObject
           必需参数：path, script_class
           可选参数：properties（初始属性值）
           
           示例：创建玩家数据
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

        2. **modify** - 修改ScriptableObject属性
           必需参数：path, properties
           
           示例：更新游戏设置
           {
             "action": "modify",
             "path": "Assets/Settings/GameSettings.asset",
             "properties": {
               "volume": 0.8,
               "fullscreen": True,
               "resolution": [1920, 1080]
             }
           }

        3. **duplicate** - 复制ScriptableObject
           必需参数：path
           可选参数：destination（目标路径，不提供则自动生成）
           
           示例：复制关卡配置
           {
             "action": "duplicate",
             "path": "Assets/Levels/Level1.asset",
             "destination": "Assets/Levels/Level2.asset"
           }

        4. **search** - 搜索ScriptableObject
           可选参数：query, path（搜索范围文件夹）, page_size, page_number, generate_preview
           
           示例：搜索所有玩家数据
           {
             "action": "search",
             "query": "PlayerData",
             "path": "Assets/Data",
             "page_size": 20,
             "page_number": 1,
             "generate_preview": False
           }

        5. **get_info** - 获取ScriptableObject信息
           必需参数：path
           可选参数：generate_preview
           
           示例：查看资源详情
           {
             "action": "get_info",
             "path": "Assets/Data/PlayerData.asset",
             "generate_preview": True
           }

        属性设置支持的类型：
        - 基础类型：int, float, bool, string
        - Unity向量：Vector2 [x, y], Vector3 [x, y, z], Vector4 [x, y, z, w]
        - Unity颜色：Color [r, g, b, a]（值范围0-1）
        - Unity四元数：Quaternion [x, y, z, w]
        - 枚举类型：使用字符串名称
        - 资源引用：使用资源路径（如Material、Texture等）

        使用场景：
        - 游戏配置管理：关卡设置、难度配置、平衡参数
        - 角色数据：玩家属性、敌人配置、NPC数据
        - UI配置：主题设置、语言本地化
        - 关卡编辑：关卡参数、波次配置
        - 物品数据库：道具、装备、技能配置

        注意事项：
        - script_class不需要包含命名空间，工具会自动搜索
        - properties中的属性名不区分大小写
        - 修改会自动标记为Dirty并保存到磁盘
        - 支持Undo/Redo操作
        - 预览生成可能较慢，建议仅在需要时启用
        """

        return send_to_unity("edit_scriptableobject", {
            "action": action,
            "path": path,
            "script_class": script_class,
            "properties": properties,
            "destination": destination,
            "query": query,
            "recursive": recursive,
            "force": force,
            "page_size": page_size,
            "page_number": page_number,
            "generate_preview": generate_preview
        })
